using System;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using log4net;

namespace EdFi.LoadTools.Engine.ResourcePipeline
{
    public class ResourcePipeline
    {
        private ILog Log => LogManager.GetLogger(this.GetType().Name);
        private readonly IResourcePipelineStep[] _steps;
        private IThrottleConfiguration _configuration;

        public ResourcePipeline(IResourcePipelineStep[] steps, IThrottleConfiguration configuration)
        {
            _steps = steps;
            _configuration = configuration;
        }

        public IPropagatorBlock<IResource, IResource> CreatePipelineBlock()
        {
            var work = new TransformBlock<IResource, IResource>(item =>
            {
                return _steps.Any(step => !step.Process(item)) ? null : item;
            }, new ExecutionDataflowBlockOptions
            {
                BoundedCapacity = _configuration.TaskCapacity,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            });

            return work;
        }
    }
}
