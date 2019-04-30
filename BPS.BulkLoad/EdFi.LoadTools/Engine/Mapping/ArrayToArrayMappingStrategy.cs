using System.Linq;
using System.Xml.Linq;

namespace EdFi.LoadTools.Engine.Mapping
{
    public class ArrayToArrayMappingStrategy : MappingStrategy
    {
        private readonly XNamespace _json = "http://james.newtonking.com/projects/json";
        private readonly string _propertyPath;

        public ArrayToArrayMappingStrategy(string propertyPath)
        {
            this._propertyPath = propertyPath;
        }

        public override void MapElementToJson(XElement element, XElement jsonXElement)
        {
            var newElement = SetPathValue(jsonXElement, _propertyPath, null);
            var attributes = newElement.Attributes().ToList();
            attributes.Insert(0, new XAttribute(_json + "Array", true));
            newElement.ReplaceAttributes(attributes);
        }
    }
}