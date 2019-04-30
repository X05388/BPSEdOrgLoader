using System.Collections.Generic;
using System.Linq;
using log4net;
using EdFi.LoadTools.ApiClient;
using EdFi.LoadTools.Engine.Mapping;

namespace EdFi.LoadTools.Engine.Factories
{
    public class MetadataMappingFactory : IMetadataMappingFactory
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MetadataMappingFactory).Name);

        private readonly IEnumerable<IMetadataMapper> _mappingStrategies;
        private readonly JsonModelMetadata[] _jsonMetadata;
        private readonly XmlModelMetadata[] _xmlMetadata;

        public MetadataMappingFactory(
            IEnumerable<JsonModelMetadata> jsonMetadata,
            IEnumerable<XmlModelMetadata> xmlMetadata,
            IEnumerable<IMetadataMapper> mappingStrategies)
        {
            _mappingStrategies = mappingStrategies;
            _jsonMetadata = jsonMetadata
                .Distinct(new ModelMetadataEqualityComparer<JsonModelMetadata>())
                .ToArray();
            _xmlMetadata = xmlMetadata.ToArray();
        }

        public IEnumerable<MetadataMapping> GetMetadataMappings()
        {
            Log.Info("Creating XSD to Json data mappings");

            var mappings = new List<MetadataMapping>();

            var jsonModels = _jsonMetadata.Select(x => x.Model).Distinct()
                .Where(x => !_jsonMetadata.Select(y => y.Type).Contains(x))
                .ToArray();

            var xmlModels = _xmlMetadata.Select(x => x.Model).Distinct()
                .Where(x => !_xmlMetadata.Select(y => y.Type).Contains(x))
                .ToArray();

            var maps = xmlModels.SelectMany(x => jsonModels.Select(j =>
                new { x, j, m = x.PercentMatchTo(j) }
                )).OrderByDescending(o => o.m).ToList();

            while (maps.Count > 0)
            {
                var map = maps.First();
                // Useful if you want to only look at one mapping
                //if (map.x == "Account")
                //{
                    mappings.Add(new MetadataMapping
                    {
                        XmlName = map.x,
                        JsonName = map.j
                    });
                //}
                maps.RemoveAll(x => x.x == map.x || x.j == map.j);
            }

            foreach (var mapping in mappings)
            {
                CreateMetadataMappings(mapping);
            }
            return mappings;
        }

        private void CreateMetadataMappings(MetadataMapping mapping)
        {
            var jsonModels = new List<ModelMetadata>();
            var tmp = _jsonMetadata.First(j => j.Model == mapping.JsonName);
            PopulateJsonModelMetadata(jsonModels, mapping.JsonName, string.Empty);

            var xmlModels = new List<ModelMetadata>();
            PopulateXmlModelMetadata(xmlModels, mapping.XmlName, string.Empty);

            foreach (var strategy in _mappingStrategies)
            {
                strategy.CreateMetadataMappings(mapping, jsonModels, xmlModels);
            }
        }

        private void PopulateJsonModelMetadata(ICollection<ModelMetadata> jsonModels, string type, string prefix)
        {
            var items = _jsonMetadata.Where(j => j.Model == type)
                .Select(j => new ModelMetadata
                {
                    Model = j.Model,
                    Property = j.Property,
                    Type = j.Type,
                    IsArray = j.IsArray,
                    IsRequired = j.IsRequired,
                    IsSimpleType = j.IsSimpleType,
                    PropertyPath = string.IsNullOrEmpty(prefix) ? j.Property : $"{prefix}/{j.Property}"
                }).ToArray();

            foreach (var item in items)
            {
                jsonModels.Add(item);
                if (!item.IsSimpleType)
                    PopulateJsonModelMetadata(jsonModels, item.Type, item.PropertyPath);
            }
        }

        private void PopulateXmlModelMetadata(ICollection<ModelMetadata> xmlModels, string type, string prefix)
        {
            var items = _xmlMetadata.Where(x => x.Model == type)
                .Select(x => new ModelMetadata
                {
                    Model = x.Model,
                    Property = x.Property,
                    Type = x.Type,
                    IsArray = x.IsArray,
                    IsRequired = x.IsRequired,
                    IsSimpleType = x.IsSimpleType,
                    PropertyPath = string.IsNullOrEmpty(prefix) ? x.Property : $"{prefix}/{x.Property}"
                }).ToArray();

            foreach (var item in items)
            {
                xmlModels.Add(item);
                if (!item.IsSimpleType)
                    PopulateXmlModelMetadata(xmlModels, item.Type, item.PropertyPath);
            }
        }
    }
}
