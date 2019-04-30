using System.Net;
using System.Security.Authentication;
using EdFi.LoadTools.Engine;
using RestSharp;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace EdFi.LoadTools.ApiClient
{
    public class TokenRetriever
    {
        private class AccessCodeResponse
        {
            public string Code { get; set; }
            //public string State { get; set; }
            public string Error { get; set; }
        }

        private class BearerTokenResponse
        {
            public string Access_token { get; set; }
            //public string Expires_in { get; set; }
            public string Token_type { get; set; }
            public string Error { get; set; }
        }

        private readonly IOAuthTokenConfiguration _configuration;

        public TokenRetriever(IOAuthTokenConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ObtainNewBearerToken()
        {
            var oauthUrl = _configuration.Url;
            var oauthKey = _configuration.Key;
            var oauthSecret = _configuration.Secret;
            var oauthClient = new RestClient(oauthUrl);
            var accessCode = GetAccessCode(oauthClient, oauthKey);
            return GetBearerToken(oauthClient, oauthKey, oauthSecret, accessCode);
        }

        private static string GetAccessCode(IRestClient oauthClient, string clientKey)
        {
            var accessCodeRequest = new RestRequest("oauth/authorize", Method.POST);
            accessCodeRequest.AddParameter("Client_id", clientKey);
            accessCodeRequest.AddParameter("Response_type", "code");

            var accessCodeResponse = oauthClient.Execute<AccessCodeResponse>(accessCodeRequest);
            if (accessCodeResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new AuthenticationException("Unable to retrieve an authorization code. Error message: " +
                                                  accessCodeResponse.ErrorMessage);
            }
            if (accessCodeResponse.Data.Error != null)
            {
                throw new AuthenticationException(
                    "Unable to retrieve an authorization code. Please verify that your application key is correct. Alternately, the service address may not be correct: " +
                    oauthClient.BaseUrl);
            }

            return accessCodeResponse.Data.Code;
        }

        private static string GetBearerToken(IRestClient oauthClient, string clientKey, string clientSecret, string accessCode)
        {
            var bearerTokenRequest = new RestRequest("oauth/token", Method.POST);
            bearerTokenRequest.AddParameter("Client_id", clientKey);
            bearerTokenRequest.AddParameter("Client_secret", clientSecret);
            bearerTokenRequest.AddParameter("Code", accessCode);
            bearerTokenRequest.AddParameter("Grant_type", "authorization_code");

            var bearerTokenResponse = oauthClient.Execute<BearerTokenResponse>(bearerTokenRequest);
            if (bearerTokenResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new AuthenticationException("Unable to retrieve an access token. Error message: " +
                                                  bearerTokenResponse.ErrorMessage);
            }

            if (bearerTokenResponse.Data.Error != null || bearerTokenResponse.Data.Token_type != "bearer")
            {
                throw new AuthenticationException(
                    "Unable to retrieve an access token. Please verify that your application secret is correct.");
            }

            return bearerTokenResponse.Data.Access_token;
        }
    }
}
