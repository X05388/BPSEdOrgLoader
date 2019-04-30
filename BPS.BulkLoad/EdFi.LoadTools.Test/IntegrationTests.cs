using EdFi.LoadTools.ApiClient;
using EdFi.LoadTools.Engine;
using EdFi.LoadTools.Test.Properties;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdFi.LoadTools.Test
{
    /// <summary>
    /// These tests are meant to be run manually with a functioning API
    /// </summary>
    [TestClass]
    public class TokenRetrieverTest
    {
        private class TestConfiguration : IOAuthTokenConfiguration
        {
            public string Key { get; set; }
            public string Secret { get; set; }
            public string Url { get; set; }
        }

        [TestMethod, TestCategory("RunManually")]
        public void ShouldSuccessfullyRetrieveBearerToken()
        {
            var config = new TestConfiguration
            {
                Url = Settings.Default.OauthUrl,
                Key = Settings.Default.OauthKey,
                Secret = Settings.Default.OauthSecret
            };

            var tokenRetriever = new TokenRetriever(config);
            var token = tokenRetriever.ObtainNewBearerToken();
            Assert.IsTrue(!string.IsNullOrEmpty(token));
        }
    }
}
