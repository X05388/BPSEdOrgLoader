using EdFi.LoadTools.Engine;
using EdFi.LoadTools.Engine.InterchangePipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace EdFi.LoadTools.Test
{
    [TestClass]
    public class ValidateXmlStepTests: IXsdConfiguration
    {
        string IXsdConfiguration.Folder => string.Empty;
        bool IXsdConfiguration.DoNotValidateXml => true;

        private TestAppender _testAppender;

        [TestInitialize]
        public void Setup()
        {
            _testAppender = new TestAppender();
            _testAppender.AttachToRoot();
        }

        [TestCleanup]
        public void Teardown()
        {
            _testAppender.DetachFromRoot();
        }

        [TestMethod]
        public void Should_skip_processing_when_DoNotValidateXml_is_set()
        {
            var step = new ValidateXmlStep(null, this);
            step.Process(null, null);

            var log = _testAppender.Logs.SingleOrDefault(x => x.Level.Name == "WARN" && x.LoggerName == typeof(ValidateXmlStep).ToString());
            Assert.IsNotNull(log);
            Assert.IsTrue(log.RenderedMessage.Contains("XML validation step skipped"));
        }
    }
}
