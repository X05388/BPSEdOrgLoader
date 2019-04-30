using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.LoadTools.ApiClient;

namespace EdFi.LoadTools.Engine.Mapping
{
    public class DescriptorReferenceMetadataMapper : IMetadataMapper
    {
        public virtual void CreateMetadataMappings(MetadataMapping mapping, List<ModelMetadata> jsonModels, List<ModelMetadata> xmlModels)
        {
            var xModels = xmlModels.Where(x => x.Type.EndsWith("DescriptorReferenceType"))
                .Select(x => new
                {
                    model = x,
                    properties = xmlModels.Where(y => y.Model == x.Type && y.PropertyPath.StartsWith(x.PropertyPath))
                })
                .Where(
                    x =>
                        x.properties.Any(y => y.Property == "Namespace") &&
                        x.properties.Any(y => y.Property == "CodeValue")).ToList();

            var jModels = jsonModels.Where(j => j.Property.EndsWith("descriptor", StringComparison.InvariantCultureIgnoreCase) && j.Type == Constants.JsonTypes.String).ToList();

            var maps = xModels.SelectMany(x => jModels.Select(j =>
                new
                {
                    x,
                    j,
                    m = x.model.PropertyPath.PercentMatchTo(j.PropertyPath)
                }))
                .Where(o => o.m > 0)
                .OrderByDescending(o => o.m)
                .ToList();

            while (maps.Count > 0)
            {
                var map = maps.First();

                mapping.Properties.Add(new PropertyMapping
                {
                    XmlName = map.x.model.PropertyPath,
                    XmlType = map.x.model.Type,
                    JsonName = map.j.PropertyPath,
                    JsonType = map.j.Type,
                    IsArray = map.x.model.IsArray,
                    MappingStrategy = new DescriptorReferenceTypeToStringMappingStrategy(map.j.PropertyPath)
                });

                mapping.Properties.AddRange(map.x.properties.Select(p =>
                    new PropertyMapping
                    {
                        XmlName = p.PropertyPath,
                        XmlType = p.Type,
                        JsonName = "{none}",
                        JsonType = "{none}",
                        IsArray = p.IsArray,
                        MappingStrategy = new NoOperationMappingStrategy()
                    }));

                xmlModels.RemoveAll(x => x == map.x.model || map.x.properties.Any(p => p == x));
                jsonModels.RemoveAll(j => j == map.j);

                maps.RemoveAll(m => m.x == map.x || m.j == map.j);
            }
        }
    }
}
