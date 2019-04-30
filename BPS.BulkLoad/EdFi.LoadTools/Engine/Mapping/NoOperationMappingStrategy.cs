using System.Xml.Linq;

namespace EdFi.LoadTools.Engine.Mapping
{
    public class NoOperationMappingStrategy : IMappingStrategy
    {
        public void MapElementToJson(XElement element, XElement jsonXElement)
        {
            // do nothing
        }
    }
}