using System.Threading.Tasks;
using log4net;
using EdFi.LoadTools.ApiClient;

namespace EdFi.LoadTools.Engine.ResourcePipeline
{
    public class SubmitResource
    {
        private ILog Log => LogManager.GetLogger(this.GetType().Name);
        private readonly ResourcePoster _poster;

        public SubmitResource(ResourcePoster poster)
        {
            _poster = poster;
        }

        private int _count;

        public async Task<IResource> ProcessAsync(IResource resource)
        {
            using (LogContext.SetResourceName(resource.ElementName))
            {
                using (LogContext.SetResourceHash(resource.HashString))
                {
                    LogContext.SetContextPrefix(resource);
                    Log.Debug($"{_count++} submitting");
                    Log.Debug($"{resource.XElement}");
                    Log.Debug($"{resource.Json}");
                    using (var response = await _poster.PostResource(resource.Json, resource.ElementName))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            Log.Info($"{_count} {response.ReasonPhrase}");
                        }
                        else
                        {
                            Log.Warn($"{_count} {response.ReasonPhrase}");
                        }
                        resource.AddSubmissionResult(response);
                    }
                }
            }
            return resource;
        }
    }
}