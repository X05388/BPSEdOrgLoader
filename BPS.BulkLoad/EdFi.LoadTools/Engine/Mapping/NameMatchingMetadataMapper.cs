using System.Collections.Generic;
using System.Linq;
using EdFi.LoadTools.ApiClient;

namespace EdFi.LoadTools.Engine.Mapping
{
    /// <summary>
    /// Copy values when attribute types and cardinality are the same
    /// </summary>
    public class NameMatchingMetadataMapper : IMetadataMapper
    {
        public void CreateMetadataMappings(MetadataMapping mapping, List<ModelMetadata> jsonModels, List<ModelMetadata> xmlModels)
        {
            var maps = xmlModels.SelectMany(x => jsonModels.Select(j =>
                new
                {
                    x,
                    j,
                    m = x.IsSimpleType && j.IsSimpleType && ExtensionMethods.AreMatchingSimpleTypes(j.Type, x.Type)
                        ? x.PropertyPath.PercentMatchTo(j.PropertyPath)
                        : 0
                }))
                .Where(o => o.m > 0)
                .OrderByDescending(o => o.m)
                .ToList();

            while (maps.Count > 0)
            {
                var map = maps.First();

                var strategy = map.x.Type == Constants.XmlTypes.Token
                               && map.j.Type == Constants.JsonTypes.Integer
                               && map.x.Property.EndsWith("SchoolYear")
                    ? new SchoolYearPropertyMappingStrategy(map.j.PropertyPath)
                    : new CopySimplePropertyMappingStrategy(map.j.PropertyPath);

                mapping.Properties.Add(new PropertyMapping
                {
                    XmlName = map.x.PropertyPath,
                    XmlType = map.x.Type,
                    JsonName = map.j.PropertyPath,
                    JsonType = map.j.Type,
                    IsArray = map.x.IsArray,
                    MappingStrategy = strategy
                });

                maps.RemoveAll(m =>
                    m.j.PropertyPath == map.j.PropertyPath ||
                    m.x.PropertyPath == map.x.PropertyPath
                    );
            }
        }
    }
}
