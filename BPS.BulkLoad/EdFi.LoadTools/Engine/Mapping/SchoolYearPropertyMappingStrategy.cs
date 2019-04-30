using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EdFi.LoadTools.Engine.Mapping
{
    public class SchoolYearPropertyMappingStrategy : CopySimplePropertyMappingStrategy
    {
        private readonly Regex _regex = new Regex(Constants.SchoolYearRegex);

        public SchoolYearPropertyMappingStrategy(string path) : base(path) { }

        public override void MapElementToJson(XElement element, XElement jsonXElement)
        {
            var match = _regex.Match(element.Value);
            if (match.Success)
            {
                var value = match.Groups["Year"].Value;
                SetPathValue(jsonXElement, _path, value);
            }
            else
            {
                base.MapElementToJson(element, jsonXElement);
            }
        }
    }
}