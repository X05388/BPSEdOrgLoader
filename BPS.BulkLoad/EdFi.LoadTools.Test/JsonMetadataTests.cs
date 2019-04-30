using System;
using EdFi.LoadTools.Test.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EdFi.LoadTools.ApiClient;
using EdFi.LoadTools.Engine;

namespace EdFi.LoadTools.Test
{
    [TestClass]
    public class JsonMetadataTests
    {
        private class TestApiMetadataConfiguration : IApiMetadataConfiguration, IXsdConfiguration
        {
            public bool Force => false;
            string IApiMetadataConfiguration.Folder => Settings.Default.WorkingFolder;
            string IXsdConfiguration.Folder => Settings.Default.XsdFolder;
            bool IXsdConfiguration.DoNotValidateXml => false;
            public string Url => Settings.Default.SwaggerUrl;
        }

        private class TestInterchangeOrderConfiguration : IInterchangeOrderConfiguration
        {
            public string Folder => Settings.Default.InterchangeOrderFolder;
        }
        public static IApiMetadataConfiguration ApiMetadataConfiguration => new TestApiMetadataConfiguration();
        public static IXsdConfiguration XmlMetadataConfiguration => new TestApiMetadataConfiguration();
        public static IInterchangeOrderConfiguration InterchangeOrderConfiguration => new TestInterchangeOrderConfiguration();

        [TestMethod, TestCategory("RunManually")]
        public void Should_display_all_Json_metadata()
        {
            var loader = new SwaggerMetadataRetriever(ApiMetadataConfiguration);
            var jsonMetadata = loader.GetMetadata().Result;
            foreach (var metadata in jsonMetadata)
            {
                Console.WriteLine($"{metadata.Model},{metadata.Property},{metadata.Type},{metadata.IsArray},{metadata.IsRequired},{metadata.IsSimpleType}");
            }
        }
    }
}
