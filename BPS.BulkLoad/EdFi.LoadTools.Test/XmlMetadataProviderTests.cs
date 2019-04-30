using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Schema;
using EdFi.LoadTools.ApiClient;
using EdFi.LoadTools.Engine.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdFi.LoadTools.Test
{
    [TestClass]
    public class XmlMetadataProviderTests
    {
        private XmlSchemaSet _schemaSet;
        private IEnumerable<XmlModelMetadata> _metadata;

        [TestInitialize]
        public void Initialize()
        {
            var ssBuilder = new SchemaSetFactory(new XsdStreamsRetriever(JsonMetadataTests.XmlMetadataConfiguration));
            var provider = new XsdMetadataFactory(ssBuilder.GetSchemaSet());
            _schemaSet = provider.Schemas;
            _metadata = provider.GetMetadata();
        }

        [TestMethod, TestCategory("RunManually")]
        public void Should_display_all_Xml_metadata()
        {
            Assert.IsTrue(_metadata.Any());
            Console.WriteLine(@"Model,Property,Type,IsArray,IsRequired,IsSimpleType");
            foreach (var metadata in _metadata)
            {
                Console.WriteLine($"{metadata.Model},{metadata.Property},{metadata.Type},{metadata.IsArray},{metadata.IsRequired},{metadata.IsSimpleType}");
            }
        }
    }
}
