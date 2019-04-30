using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EdFi.LoadTools.Engine.Mapping;

namespace EdFi.LoadTools.Engine.ResourcePipeline
{
    public class MapElementStep : IResourcePipelineStep
    {
        private readonly MetadataMapping[] _mappings;

        public MapElementStep(IEnumerable<MetadataMapping> mappings)
        {
            _mappings = mappings.ToArray();
        }

        public bool Process(IResource resource)
        {
            var values = new[]
            {
                $"{resource.ElementName}Extension",
                resource.ElementName
            };
            var map = _mappings.SingleOrDefault(m => values.Contains(m.XmlName));
            if (map == null) return false;

            var jsonXElement = new XElement(map.JsonName);
            
            foreach (var element in resource.XElement.Elements())
            {
                var path = element.Name.LocalName;
                PerformElementMapping(map, element, path, jsonXElement);
            }

            resource.SetJsonXElement(jsonXElement);

            return true;
        }

        private void PerformElementMapping(MetadataMapping map, XElement element, string path, XElement jsonXElement)
        {
            var propertyMappings = map.Properties.Where(p => p.XmlName == path);
            foreach (var mapping in propertyMappings)
            {
                mapping.MappingStrategy.MapElementToJson(element, jsonXElement);
            }
            foreach (var ele in element.Elements())
            {
                PerformElementMapping(map, ele, $"{path}/{ele.Name.LocalName}", jsonXElement);
            }
        }
    }
}
