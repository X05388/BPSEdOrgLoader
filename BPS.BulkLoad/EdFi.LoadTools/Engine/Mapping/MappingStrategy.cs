using System.Linq;
using System.Xml.Linq;

namespace EdFi.LoadTools.Engine.Mapping
{
    public abstract class MappingStrategy : IMappingStrategy
    {
        public abstract void MapElementToJson(XElement element, XElement jsonXElement);

        public static XElement SetPathValue(XElement jsonXElement, string jsonName, string value)
        {
            var segments = jsonName.Split('/');
            var currentElement = jsonXElement;
            for (var i = 0; i < segments.Length - 1; i++)
            {
                var tmp = currentElement.Elements(segments[i]).LastOrDefault();
                if (tmp == null)
                {
                    tmp = new XElement(segments[i]);
                    currentElement.Add(tmp);
                }
                currentElement = tmp;
            }
            var newElement = new XElement(segments.Last(), value);
            currentElement.Add(newElement);
            return newElement;
        }

    }
}
