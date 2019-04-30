using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Xml;
using System.Xml.Linq;
using EdFi.LoadTools.Engine.Factories;

namespace EdFi.LoadTools.Engine.InterchangePipeline
{
    public class InterchangePipeline
    {
        private readonly IResourceStreamFactory _streamFactory;
        private readonly IXmlReferenceCacheFactory _xmlReferenceCacheFactory;
        private readonly IInterchangePipelineStep[] _steps;

        public InterchangePipeline(IResourceStreamFactory streamFactory,
            IXmlReferenceCacheFactory xmlReferenceCacheFactory,
            IInterchangePipelineStep[] steps)
        {
            _streamFactory = streamFactory;
            _xmlReferenceCacheFactory = xmlReferenceCacheFactory;
            _steps = steps;
        }

        public IPropagatorBlock<Interchange, IResource> CreatePipelineBlock()
        {
            var work = new TransformManyBlock<Interchange, IResource>(
                interchange => RetrieveResourcesFromInterchange(interchange),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 1
                });

            return work;
        }

        public IEnumerable<IResource> RetrieveResourcesFromInterchange(Interchange interchange)
        {
            var elements = interchange.Elements ?? new List<Element> { };
            var elementNames = elements.Select(x => x.Name).ToArray();

            using (LogContext.SetInterchangeName(interchange.Name))
            {
                var interchangeFiles = _streamFactory.GetInterchangeFileNames(interchange).ToList();
                // Top Level File Processing
                var interchangeFilesToProcess = new List<string>();
                foreach (var interchangeFileName in interchangeFiles)
                {
                    _xmlReferenceCacheFactory.InitializeCache(interchangeFileName);
                    if (_steps.All(s =>
                    {
                        using (var stream = _streamFactory.GetStream(interchangeFileName))
                            return s.Process(interchangeFileName, stream);
                    }))
                    {
                        interchangeFilesToProcess.Add(interchangeFileName);
                    }
                }
                foreach (var elementName in elementNames)
                {
                    foreach (var interchangeFileName in interchangeFilesToProcess)
                    {
                        using (LogContext.SetFileName(Path.GetFileName(interchangeFileName)))
                        {
                            using (var reader = new XmlTextReader(_streamFactory.GetStream(interchangeFileName)))
                            {
                                while (reader.Read())
                                {
                                    if (reader.NodeType != XmlNodeType.Element) continue;
                                    if (reader.Name.StartsWith("Interchange")) continue;
                                    using (var r = reader.ReadSubtree())
                                    {
                                        var xElement = XElement.Load(r);
                                        if (xElement.Name.LocalName != elementName) continue;
                                        yield return
                                            new ResourceWorkItem(interchange.Name, interchangeFileName, xElement);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
