using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using EdFi.Ods.Common.Inflection;
using EdFi.Ods.Common.Utils.Profiles;
using log4net;
using EdFi.LoadTools.Engine;

namespace EdFi.LoadTools.ApiClient
{
    public class ResourcePoster
    {
        private ILog Log => LogManager.GetLogger(GetType().Name);

        private readonly IApiConfiguration _configuration;
        private readonly string _token;
        private HttpClient _client;

        public ResourcePoster(IApiConfiguration configuration, TokenRetriever tokenRetriever)
        {
            _configuration = configuration;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.ReusePort = true;
            ServicePointManager.DefaultConnectionLimit = configuration.ConnectionLimit;
            _token = tokenRetriever.ObtainNewBearerToken();
            _client = new HttpClient
                {
                    Timeout = new TimeSpan(0, 0, 5, 0),
                    BaseAddress = new Uri(_configuration.Url)
                };
        }

        public async Task<HttpResponseMessage> PostResource(string json, string elementName)
        {
            var resource = CompositeTermInflector.MakePlural(elementName);
            var contentType = BuildJsonMimeType(elementName);
            var content = new StringContent(json, Encoding.UTF8, contentType);

            HttpResponseMessage response;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, resource))
            {
                try
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                    requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
                    requestMessage.Content = content;
                    response = await _client.SendAsync(requestMessage);
                }
                catch (WebException ex)
                {
                    // Handling intermittent network issues
                    Log.Error("Unexpected WebException on resource post", ex);
                    response = CreateFakeErrorResponse(HttpStatusCode.ServiceUnavailable);
                }
                catch (TaskCanceledException ex)
                {
                    // Handling web timeout
                    Log.Error("Http Client timeout.", ex);
                    response = CreateFakeErrorResponse(HttpStatusCode.RequestTimeout);
                }
                catch (Exception ex)
                {
                    // Handling other issues
                    Log.Error("Unexpected Exception on resource post", ex);
                    response = CreateFakeErrorResponse(HttpStatusCode.SeeOther);
                }
            }
            return response;
        }

        private string BuildJsonMimeType(string resourceName)
        {
            if (string.IsNullOrEmpty(_configuration.Profile)) return "application/json";
            return ProfilesContentTypeHelper.CreateContentType(
                    resourceName,
                    _configuration.Profile,
                    ContentTypeUsage.Writable
                );
        }

        private HttpResponseMessage CreateFakeErrorResponse(HttpStatusCode httpStatusCode)
        {
            return new HttpResponseMessage(httpStatusCode);
        }
    }
}
