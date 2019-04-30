using log4net;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BPS.EdOrg.Loader
{
    public class RestServiceManager
    {
        private readonly Configuration _configuration = null;
        private readonly string _accessToken = string.Empty;
        private readonly ILog _log;
        public RestServiceManager(Configuration configuration,string token, ILog logger)
        {
            _configuration = configuration;
            _accessToken = token;
            _log = logger;
        }

        public List<SchoolResponse> GetSchoolList()
        {
            List<SchoolResponse> schoolList = new List<SchoolResponse>();
            try
            {
                if (!string.IsNullOrEmpty(_accessToken))
                {
                    var httpClient = new RestClient(_configuration.CrossWalkSchoolApiUrl);
                    var request = new RestRequest(Method.GET);
                    request.AddHeader("Authorization", "Bearer " + _accessToken);
                    request.RequestFormat = RestSharp.DataFormat.Json;
                    request.AddHeader("Content-Type", "application/json");
                    var response = httpClient.Execute<List<SchoolResponse>>(request);
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        _log.Error($"Unable to retrieve school list from {httpClient.BaseUrl}");
                    }
                    else
                    {
                        schoolList = response.Data;
                    }
                }
                else
                {
                    _log.Error($"Bearer token not provided for retrieving school list from API");
                }               
            }
            catch(Exception ex)
            {
                _log.Error($"Exception while retrieve school list : {ex.Message}");
            }
            return schoolList;
        }
    }
}
