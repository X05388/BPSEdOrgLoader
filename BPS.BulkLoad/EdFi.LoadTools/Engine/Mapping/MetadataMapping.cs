using System.Collections.Generic;

namespace EdFi.LoadTools.Engine.Mapping
{
    public class MetadataMapping
    {
        public string XmlName { get; set; }
        public string JsonName { get; set; }
        public List<PropertyMapping> Properties { get; set; } = new List<PropertyMapping>();
    }
    public class PropertyMapping
    {
        public string XmlName { get; set; }
        public string XmlType { get; set; }
        public string JsonName { get; set; }
        public string JsonType { get; set; }
        public bool IsArray { get; set; }
        public IMappingStrategy MappingStrategy { get; set; }
    }
}
