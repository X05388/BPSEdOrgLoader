using System.Collections.Generic;
using System.Linq;
using EdFi.LoadTools.ApiClient;

namespace EdFi.LoadTools.Engine.Mapping
{
    public class ArrayMetadataMapper : IMetadataMapper
    {
        public void CreateMetadataMappings(MetadataMapping mapping, List<ModelMetadata> jsonModels, List<ModelMetadata> xmlModels)
        {
            var xModels = xmlModels.Where(x => x.IsArray).ToArray();
            var jModels = jsonModels.Where(j => j.IsArray).ToArray();

            var maps = xModels.SelectMany(x => jModels.Select(j =>
                new
                {
                    x,
                    j,
                    m = x.PropertyPath.PercentMatchTo(j.PropertyPath)
                }))
                .Where(o => o.m > 0)
                .OrderByDescending(o => o.m)
                .ToList();

            while (maps.Count > 0)
            {
                var map = maps.First();

                mapping.Properties.Add(new PropertyMapping
                {
                    XmlName = map.x.PropertyPath,
                    XmlType = map.x.Type,
                    JsonName = map.j.PropertyPath,
                    JsonType = map.j.Type,
                    IsArray = map.x.IsArray,
                    MappingStrategy = new ArrayToArrayMappingStrategy(map.j.PropertyPath)
                });

                maps.RemoveAll(m => m.x == map.x || m.j == map.j);
            }
        }
    }
}
