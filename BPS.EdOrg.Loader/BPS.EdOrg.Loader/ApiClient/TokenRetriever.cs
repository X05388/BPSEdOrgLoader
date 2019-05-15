using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader.ApiClient
{
    public class TokenRetriever
    {
        private class AccessCodeResponse
        {
            public string Code { get; set; }
            public string Error { get; set; }
        }

        private class BearerTokenResponse
        {
            public string Access_token { get; set; }
            public string Token_type { get; set; }
            public string Error { get; set; }
        }

        private readonly Configuration _configuration;

        public TokenRetriever(Configuration configuration)
        {
            _configuration = configuration;
        }

        public string ObtainNewBearerToken()
        {
            var oauthUrl = _configuration.CrossWalkOAuthUrl;
            var oauthKey = _configuration.CrossWalkKey;
            var oauthSecret = _configuration.CrossWalkSecret;
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
