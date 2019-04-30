using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using log4net;
using EdFi.LoadTools.ApiClient;

namespace EdFi.LoadTools.Engine.Mapping
{
    [Obsolete("This class should be removed as soon as Ed-Fi Bug ODS-628 is fixed.")]
    public class DiminishingMetadataMapper : IMetadataMapper
    {
        private readonly string[] _diminishedTypes =
        {
            "LocalEducationAgencyFederalFunds",
            "StateEducationAgencyFederalFunds"
        };

        public void CreateMetadataMappings(MetadataMapping mapping, List<ModelMetadata> jsonModels, List<ModelMetadata> xmlModels)
        {
            var xModels = xmlModels.Where(x => _diminishedTypes.Contains(x.Type)).ToArray();

            mapping.Properties.AddRange(xModels.Select(x =>
                new PropertyMapping
                {
                    XmlName = x.PropertyPath,
                    XmlType = x.Type,
                    JsonName = "{none}",
                    JsonType = "{none}",
                    IsArray = x.IsArray,
                    MappingStrategy = new UnexpectedSourceDataMappingStrategy()
                }));

            foreach (var model in xModels)
            {
                 xmlModels.RemoveAll(x => x.PropertyPath.StartsWith(model.PropertyPath));
            }
        }
    }

    public class UnexpectedSourceDataMappingStrategy : IMappingStrategy
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UnexpectedSourceDataMappingStrategy).Name);
        public void MapElementToJson(XElement element, XElement jsonXElement)
        {
            Log.Warn($"This XML element type must not be submitted to the API: {element}");
        }
    }
}