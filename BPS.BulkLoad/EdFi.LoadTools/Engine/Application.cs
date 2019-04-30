using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using log4net;
using EdFi.LoadTools.Engine.ResourcePipeline;

namespace EdFi.LoadTools.Engine
{
    public class Application
    {
        private readonly IInterchangeElementOrderFactory _interchangeOrderFactory;
        private readonly InterchangePipeline.InterchangePipeline _interchangePipeline;
        private readonly ResourcePipeline.ResourcePipeline _resourcePipeline;
        private readonly SubmitResource _submitResourcesProcessor;
        private readonly IResourceHashCache _xmlResourceHashCache;
        private readonly IXmlReferenceCacheFactory _xmlReferenceCacheFactory;
        private readonly IApiConfiguration _apiConfiguration;

        private ILog Log => LogManager.GetLogger(GetType().Name);

        public Application(
            IInterchangeElementOrderFactory interchangeOrderFactory,
            InterchangePipeline.InterchangePipeline interchangePipeline,
            ResourcePipeline.ResourcePipeline resourcePipeline,
            SubmitResource submitResourcesProcessor,
            IResourceHashCache xmlResourceHashCache,
            IXmlReferenceCacheFactory xmlReferenceCacheFactory,
            IApiConfiguration apiConfiguration)
        {
            _interchangeOrderFactory = interchangeOrderFactory;
            _interchangePipeline = interchangePipeline;
            _resourcePipeline = resourcePipeline;
            _submitResourcesProcessor = submitResourcesProcessor;
            _xmlResourceHashCache = xmlResourceHashCache;
            _xmlReferenceCacheFactory = xmlReferenceCacheFactory;
            _apiConfiguration = apiConfiguration;
        }

        public async Task<int> Run()
        {
            _xmlResourceHashCache.Load();
            var interchangeOrder = _interchangeOrderFactory.GetInterchangeElementOrder();
            foreach (var interchange in interchangeOrder)
            {
                var retryQueue = new ConcurrentQueue<IResource>();
                var resourcePipeline = CreateResourcePipeline(retryQueue);
                foreach (var resource in _interchangePipeline.RetrieveResourcesFromInterchange(interchange))
                {
                    await resourcePipeline.StartBlock.SendAsync(resource);
                }
                resourcePipeline.StartBlock.Complete();
                await resourcePipeline.Completion;

                if (retryQueue.Count > 0)
                {
                    var retryPipeline = CreateRetryPipeline(retryQueue.Count);
                    foreach (var retryResource in retryQueue)
                    {
                        await retryPipeline.StartBlock.SendAsync(retryResource);
                    }
                    await retryPipeline.Completion;
                }
                // Cleanup reference caches
                _xmlReferenceCacheFactory.Cleanup();
            }

            return 0;
        }

        private DataFlowPipeline<IResource> CreateResourcePipeline(ConcurrentQueue<IResource> retryQueue)
        {
            // Create blocks
            var resourcePipelineBlock = _resourcePipeline.CreatePipelineBlock();

            var postingBlock = new TransformBlock<IResource, IResource>(
                x => _submitResourcesProcessor.ProcessAsync(x)
                , new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = _apiConfiguration.MaxSimultaneousRequests,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                });

            var successBlock = new ActionBlock<IResource>(
                x => _xmlResourceHashCache.Add(x.Hash)
                , new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = _apiConfiguration.TaskCapacity,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                });

            var noPostBlock = new ActionBlock<IResource>(x =>
            {
                //string contextPrefix = LogContext.BuildContextPrefix(x);
                Log.Debug($"Found in cache - Not Submitted");
            });

            var retryQueueBlock = new ActionBlock<IResource>(delegate(IResource resource)
            {
                if (_apiConfiguration.Retries > 0)
                {
                    retryQueue.Enqueue(resource);
                }
                else
                {
                    using (LogContext.SetResourceName(resource.ElementName))
                    {
                        using (LogContext.SetResourceHash(resource.HashString))
                        {
                            LogContext.SetContextPrefix(resource);
                            Log.Error(
                                $"{resource.Responses.Last().StatusCode} - {resource.Responses.Last().Content}{Environment.NewLine}{resource.XElement}{Environment.NewLine}{resource.Json}");
                        }
                    }
                }
            }, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = _apiConfiguration.MaxSimultaneousRequests,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            });

            // Link blocks

            resourcePipelineBlock.LinkTo(postingBlock,
                new DataflowLinkOptions { PropagateCompletion = true },
                x => x != null);

            resourcePipelineBlock.LinkTo(noPostBlock,
                new DataflowLinkOptions { PropagateCompletion = true },
                x => x == null);

            postingBlock.LinkTo(successBlock,
                new DataflowLinkOptions { PropagateCompletion = true },
                x => x.Responses.Any(y => y.IsSuccess));

            postingBlock.LinkTo(retryQueueBlock,
                new DataflowLinkOptions { PropagateCompletion = true });

            return new DataFlowPipeline<IResource>(resourcePipelineBlock, Task.WhenAll(noPostBlock.Completion, retryQueueBlock.Completion, successBlock.Completion));
        }

        private DataFlowPipeline<IResource> CreateRetryPipeline(int numResourcesToRetry)
        {
            int totalResources = numResourcesToRetry;
            var retryBufferBlock = new BufferBlock<IResource>();

    var retryBlock = new TransformBlock<IResource, IResource>(
        x => _submitResourcesProcessor.ProcessAsync(x),
        new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = _apiConfiguration.MaxSimultaneousRequests,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        });

            var successBlock = new TransformBlock<IResource, IResource>(delegate(IResource resource)
            {
                _xmlResourceHashCache.Add(resource.Hash);
                return resource;
            });

            var errorBlock = new TransformBlock<IResource, IResource>(delegate (IResource resource)
            {
                using (LogContext.SetResourceName(resource.ElementName))
                {
                    using (LogContext.SetResourceHash(resource.HashString))
                    {
                        LogContext.SetContextPrefix(resource);
                        Log.Error($"{resource.Responses.Last().StatusCode} - {resource.Responses.Last().Content}{Environment.NewLine}{resource.XElement}{Environment.NewLine}{resource.Json}");
                    }
                }
                return resource;
            });

            var completionCheckBlock = new ActionBlock<IResource>(delegate
            {
                totalResources--;
                if (totalResources == 0)
                    retryBufferBlock.Complete();
            },
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = Environment.ProcessorCount });

            retryBufferBlock.LinkTo(retryBlock,
                new DataflowLinkOptions {PropagateCompletion = true}
                );

            retryBlock.LinkTo(retryBufferBlock,
                new DataflowLinkOptions {PropagateCompletion = true},
                x => x.Responses.Count < _apiConfiguration.Retries && !x.Responses.Any(y => y.IsSuccess));

            retryBlock.LinkTo(successBlock,
                new DataflowLinkOptions { PropagateCompletion = true },
                x => x.Responses.Any(y => y.IsSuccess));

            retryBlock.LinkTo(errorBlock,
                new DataflowLinkOptions { PropagateCompletion = true });

            successBlock.LinkTo(completionCheckBlock,
                new DataflowLinkOptions {PropagateCompletion = true});

            errorBlock.LinkTo(completionCheckBlock,
                new DataflowLinkOptions { PropagateCompletion = true });

            return new DataFlowPipeline<IResource>(retryBufferBlock, completionCheckBlock.Completion);
        }

        private class DataFlowPipeline<T>
        {
            public DataFlowPipeline(ITargetBlock<T> startBlock, Task completionTask)
            {
                StartBlock = startBlock;
                Completion = completionTask;
            }

            public ITargetBlock<T> StartBlock { get; private set; }
            public Task Completion { get; private set; }
        }
    }
}
