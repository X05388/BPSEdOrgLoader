using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks.Dataflow;
using EdFi.LoadTools.Engine;
using EdFi.LoadTools.Engine.Factories;
using EdFi.LoadTools.Engine.InterchangePipeline;
using EdFi.LoadTools.Engine.ResourcePipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdFi.LoadTools.Test
{
    public class XmlResourcePipelineTests
    {
        [TestClass]
        public class WhenRunningTheXmlResourcePipeline
        {
            private class TestXmlReferenceCacheFactory : IXmlReferenceCacheFactory
            {
                public void InitializeCache(string fileName)
                {
                }

                public void Cleanup()
                {
                }
            }

            private class TestXmlResourcePipelineStep : IResourcePipelineStep
            {
                public int Count { get; private set; }

                public bool Process(IResource resource)
                {
                    Count++;
                    return true;
                }
            }

            private class TestResourceStreamFactory : IResourceStreamFactory
            {
                public Stream GetStream(string interchangeFileName)
                {
                    const string crlf = "\r\n";
                    const string xml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>" + crlf +
                        "<InterchangeTest>" + crlf +
                        "<Element> <A>foobar</A> </Element>" + crlf +
                        "<Element> </Element>" + crlf +
                        "<Element> </Element>" + crlf +
                        "<Element> </Element>" + crlf +
                        "</InterchangeTest>";
                    return new MemoryStream(Encoding.UTF8.GetBytes(xml));
                }

                public IEnumerable<string> GetInterchangeFileNames(Interchange interchange)
                {
                    yield return "test";
                }
            }

            private class TestConfiguration : IThrottleConfiguration
            {
                public int ConnectionLimit
                {
                    get
                    {
                        return 50;
                    }
                }

                public int TaskCapacity
                {
                    get
                    {
                        return 50;
                    }
                }
                public int MaxDegreeOfParallelism
                {
                    get
                    {
                        return 1;
                    }
                }

                public int MaxSimultaneousRequests
                {
                    get
                    {
                        return 50;
                    }
                }
            }


            private TestXmlResourcePipelineStep _pipelineStep;
            private InterchangePipeline _interchangePipeline;
            private ResourcePipeline _pipeline;

            [TestInitialize]
            public void Setup()
            {
                _interchangePipeline = new InterchangePipeline(new TestResourceStreamFactory(), new TestXmlReferenceCacheFactory(), new IInterchangePipelineStep[] {} );

                _pipelineStep = new TestXmlResourcePipelineStep();
                
                _pipeline = new ResourcePipeline(
                    new IResourcePipelineStep[] { _pipelineStep }, new TestConfiguration()
                    );

                var interchange = new Interchange
                {
                    Order = 1,
                    Name = "TestInterchange",
                    Elements = new List<Element>
                    {
                        new Element { Name = "Element" }
                    }
                };

                var iblock = _interchangePipeline.CreatePipelineBlock();
                
                var block = new ActionBlock<IResource>(x => _pipelineStep.Process(x));

                iblock.LinkTo(block, new DataflowLinkOptions {PropagateCompletion = true});

                iblock.Post(interchange);
                iblock.Complete();
                
                block.Completion.Wait();
            }

            [TestMethod]
            public void Should_process_every_element()
            {
                Assert.AreEqual(4, _pipelineStep.Count);
            }
        }
    }
}
