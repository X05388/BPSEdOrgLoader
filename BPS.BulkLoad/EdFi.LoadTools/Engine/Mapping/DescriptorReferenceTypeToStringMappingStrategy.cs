using System.Linq;
using System.Xml.Linq;

namespace EdFi.LoadTools.Engine.Mapping
{
    public class DescriptorReferenceTypeToStringMappingStrategy : MappingStrategy
    {
        private readonly string _propertyPath;

        public DescriptorReferenceTypeToStringMappingStrategy(string propertyPath)
        {
            this._propertyPath = propertyPath;
        }

        public override void MapElementToJson(XElement element, XElement jsonXElement)
        {
            var tmp = new XElement("temporary", string.Empty);
            var ns = element.Name.Namespace;
            var myNamespace = element.Elements().SingleOrDefault(x => x.Name.LocalName == "Namespace");
            var myCodeValue = element.Elements().Single(x => x.Name.LocalName == "CodeValue");

            var value = myNamespace == null
                ? myCodeValue.Value
                : $"{myNamespace.Value}/{myCodeValue.Value}";

            SetPathValue(jsonXElement, _propertyPath, value);
        }
    }
}