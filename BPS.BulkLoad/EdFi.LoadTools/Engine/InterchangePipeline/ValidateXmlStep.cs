using System.IO;
using System.Xml;
using System.Xml.Schema;
using log4net;

namespace EdFi.LoadTools.Engine.InterchangePipeline
{
    public class ValidateXmlStep : IInterchangePipelineStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ValidateXmlStep));
        private readonly XmlSchemaSet _schemaSet;
        private readonly IXsdConfiguration _configuration;

        public ValidateXmlStep(XmlSchemaSet schemaSet, IXsdConfiguration configuration)
        {
            _schemaSet = schemaSet;
            _configuration = configuration;
        }

        public bool Process(string sourceFileName, Stream stream)
        {
            if (_configuration.DoNotValidateXml)
            {
                Log.Warn("XML validation step skipped");
                return true;
            }

            var result = true;

            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = _schemaSet,
                Async = true,
            };

            settings.ValidationEventHandler += (s, e) =>
            {
                result = false;
                Log.Error(e.Message);
            };

            using (var reader = XmlReader.Create(stream, settings))
            {
                while (reader.Read())
                {
                }
            }
            Log.Info("Validated");
            return result;
        }
    }
}
