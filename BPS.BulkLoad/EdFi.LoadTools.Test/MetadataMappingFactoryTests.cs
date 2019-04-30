using System;
using System.Collections.Generic;
using System.Linq;
using EdFi.LoadTools.ApiClient;
using EdFi.LoadTools.Engine;
using EdFi.LoadTools.Engine.Factories;
using EdFi.LoadTools.Engine.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdFi.LoadTools.Test
{
    [TestClass]
    public class MetadataMappingFactoryTests
    {
        private List<JsonModelMetadata> _jsonMetadata;
        private List<XmlModelMetadata> _xmlMetadata;
        private MetadataMapping[] _mappings;

        [TestInitialize]
        public void Initialize()
        {
            var retriever = new SwaggerMetadataRetriever(JsonMetadataTests.ApiMetadataConfiguration);
            var ssbuilder = new SchemaSetFactory(new XsdStreamsRetriever(JsonMetadataTests.XmlMetadataConfiguration));
            var xfactory = new XsdMetadataFactory(ssbuilder.GetSchemaSet());
            _xmlMetadata = xfactory.GetMetadata().ToList();

            var jfactory = new JsonMetadataFactory(retriever);
            _jsonMetadata = jfactory.GetMetadata().ToList();

            var strategies = new IMetadataMapper[]
            {
                new DiminishingMetadataMapper(),
                new ArrayMetadataMapper(),
                new DescriptorReferenceMetadataMapper(),
                new NameMatchingMetadataMapper(),
                new SchoolIdBugFixMetadataMapper()
            };

            var factory = new MetadataMappingFactory(_jsonMetadata.AsEnumerable(), _xmlMetadata.AsEnumerable(), strategies);
            _mappings = factory.GetMetadataMappings().ToArray();
        }

        [TestMethod, TestCategory("RunManually")]
        public void Should_Map_Metadata()
        {
            foreach (var m in _mappings.OrderBy(x=> x.XmlName))
            {
                var xmlModels = new List<ModelMetadata>();
                PopulateXmlModelMetadata(xmlModels, m.XmlName);
                var unmappedXmlProperties = xmlModels.Where(xm => xm.IsSimpleType && m.Properties.All(p => p.XmlName != xm.PropertyPath)).ToList();
                var jsonModels = new List<ModelMetadata>();
                PopulateJsonModelMetadata(jsonModels, m.JsonName);
                var unmappedJsonProperties = jsonModels.Where(jm => jm.IsSimpleType && m.Properties.All(p => p.JsonName != jm.PropertyPath)).ToList();

                // Uncomment to only see resources with missing mappings
                //if (!unmappedXmlProperties.Any() && !unmappedJsonProperties.Any())
                //    continue;

                Console.WriteLine($"{m.XmlName}, {m.JsonName}");
                foreach (var p in m.Properties.OrderBy(x=> x.XmlName))
                {
                    Console.WriteLine(p.IsArray
                        ? $"\t{p.XmlName} ({p.XmlType}[]), {p.JsonName} ({p.JsonType}[])\t{p.MappingStrategy.GetType().Name}"
                        : $"\t{p.XmlName} ({p.XmlType}), {p.JsonName} ({p.JsonType})\t{p.MappingStrategy.GetType().Name}");
                }
                if (unmappedJsonProperties.Any())
                {
                    Console.WriteLine($"\tUnmapped Json Properties:");
                    foreach (var unmappedProperty in unmappedJsonProperties.Select(up => up.PropertyPath).Distinct())
                    {
                        Console.WriteLine($"\t\t{unmappedProperty}");
                    }
                }
                if (unmappedXmlProperties.Any())
                {
                    Console.WriteLine($"\tUnmapped Xml Properties:");
                    foreach (var unmappedProperty in unmappedXmlProperties.Select(up => up.PropertyPath).Distinct())
                    {
                        Console.WriteLine($"\t\t{unmappedProperty}");
                    }
                }
                Console.WriteLine();
            }
        }

        private void PopulateJsonModelMetadata(ICollection<ModelMetadata> jsonModels, string type, string prefix = "")
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

        private void PopulateXmlModelMetadata(ICollection<ModelMetadata> xmlModels, string type, string prefix = "")
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