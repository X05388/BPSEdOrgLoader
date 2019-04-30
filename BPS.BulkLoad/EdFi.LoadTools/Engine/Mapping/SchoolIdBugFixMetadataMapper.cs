using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.LoadTools.ApiClient;

namespace EdFi.LoadTools.Engine.Mapping
{
    [Obsolete("This class should be removed as soon as Ed-Fi Bug DATASTD-839 is fixed.")]
    public class SchoolIdBugFixMetadataMapper : IMetadataMapper
    {
        private const string XmlPath = "SchoolReference/SchoolIdentity/SchoolId";

        private readonly string[] _jsonPaths =
        {
            "beginCalendarDateReference/schoolId",
            "endCalendarDateReference/schoolId"
        };

        public void CreateMetadataMappings(MetadataMapping mapping, List<ModelMetadata> jsonModels, List<ModelMetadata> xmlModels)
        {
            var xModel = xmlModels.SingleOrDefault(x => x.PropertyPath == XmlPath);
            var jModels = jsonModels.Where(j => _jsonPaths.Contains(j.PropertyPath)).ToList();

            if (xModel == null || jModels.Count() != 2) return;

            foreach (var jModel in jModels)
            {
                mapping.Properties.Add(
                    new PropertyMapping
                    {
                        XmlName = xModel.PropertyPath,
                        XmlType = xModel.Type,
                        JsonName = jModel.PropertyPath,
                        JsonType = jModel.Type,
                        IsArray = jModel.IsArray,
                        MappingStrategy = new SchoolIdBugFixMappingStrategy(jModel.PropertyPath)
                    });
            }
        }

        [Obsolete("This class should be removed as soon as Ed-Fi Bug DATASTD-839 is fixed.")]
        public class SchoolIdBugFixMappingStrategy : CopySimplePropertyMappingStrategy
        {
            public SchoolIdBugFixMappingStrategy(string path) : base(path){}
        }
    }
}
