using System.Xml.Linq;

namespace EdFi.LoadTools.Engine.Mapping
{
    public class CopySimplePropertyMappingStrategy : MappingStrategy
    {
        protected readonly string _path;

        public CopySimplePropertyMappingStrategy(string path)
        {
            _path = path;
        }

        public override void MapElementToJson(XElement element, XElement jsonXElement)
        {
            SetPathValue(jsonXElement, _path, element.Value);
        }
    }
}