using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using log4net;
using EdFi.LoadTools.Engine;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable CollectionNeverUpdated.Local
// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable AccessToDisposedClosure

namespace EdFi.LoadTools.ApiClient
{
    public class SwaggerMetadataRetriever
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SwaggerMetadataRetriever).Name);

        private class Metadata
        {
            public string value { get; set; }
            public string sectionPrefix { get; set; }
        }

        private class ApiDoc
        {
            public class Api { public string path { get; set; } }
            public Api[] apis { get; set; }
        }

        private class Resource
        {
            public class Items
            {
                [JsonProperty(PropertyName = "ref")]
                public string reference { get; set; }
            }

            public class Property
            {
                public string type { get; set; }
                public bool required { get; set; }
                public Items items { get; set; }
                public string description { get; set; }
            }

            public class Model
            {
                public Dictionary<string, Property> properties { get; set; }
            }

            public Dictionary<string, Model> models { get; set; }
        }

        private readonly IApiMetadataConfiguration _configuration;

        public SwaggerMetadataRetriever(IApiMetadataConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<JsonModelMetadata>> GetMetadata()
        {
            if (_configuration.Force)
            {
                File.Delete(Filename);
                await LoadMetadata();
            }
            if (!MetadataExists)
            {
                //On deployment, it is probable that we will be launching 5 instances very close together.
                //This random delay should allow one item to win the race condition.
                Random waitTime = new Random();
                var delay = waitTime.Next(1000, 10001);
                //Put the thread to sleep waiting to see if someone else writes the file
                System.Threading.Thread.Sleep(delay);
                if (!MetadataExists)
                {
                    //Assume if it is still not there, that you have won the race.
                    await LoadMetadata();
                }
            }
            //Make sure th efile is not in the process of being written
            await WaitForMetadata();
            return await ReadMetadata();
        }

        private string Filename => Path.Combine(_configuration.Folder, "metadata.json");

        public bool MetadataExists => File.Exists(Filename);

        public async Task<IEnumerable<JsonModelMetadata>> ReadMetadata()
        {
            var result = new List<JsonModelMetadata>();
            if (!MetadataExists) return result;
            using (var reader = new StreamReader(Filename))
            {
                while (!reader.EndOfStream)
                {
                    var obj = await reader.ReadLineAsync();
                    var info = JsonConvert.DeserializeObject<JsonModelMetadata>(obj);
                    result.Add(info);
                }
                reader.Close();
            }
            return result;
        }

        public async Task WaitForMetadata()
        {
            bool logged = false;
            int timeout = 600000;
            int incr = 10000;
            while (timeout > 0)
            {
                try
                {
                    using (var inputStream = File.Open(Filename, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        break;
                    }
                }
                catch (IOException)
                {
                    if (!logged) Log.Info("Waiting for other process to finish downloading API Metadata");
                    logged = true;
                }

                await Task.Delay(incr);
                timeout -= incr;
            }
        }

        public async Task LoadMetadata()
        {
            Log.Info("Loading API Metadata");
            using (var writer = new StreamWriter(Filename))
            {
                var metadataBlock = new BufferBlock<string>();
                var apidocsBlock = new TransformManyBlock<string, string>(async x =>
                {
                    var j = await LoadJsonString(x);
                    var docs = JsonConvert.DeserializeObject<ApiDoc>(j);
                    return docs.apis.Select(y => $"{x}{y.path}");
                });
                var resourcesBlock = new TransformManyBlock<string, JsonModelMetadata>(async x =>
                {
                    var j = await LoadJsonString(x);
                    j = j.Replace("\"$ref\"", "\"ref\"");
                    var parts = x.Split(Path.AltDirectorySeparatorChar);
                    var resources = JsonConvert.DeserializeObject<Resource>(j, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects });
                    var result = (from model in resources.models
                                  from property in model.Value.properties
                                  select new JsonModelMetadata
                                  {
                                      Category = parts[0],
                                      Resource = parts[2],
                                      Model = model.Key,
                                      Property = property.Key,
                                      Type = property.Value.type == "array" ? property.Value.items.reference : property.Value.type,
                                      IsArray = property.Value.type == "array",
                                      IsRequired = property.Value.required,
                                      Description = property.Value.description
                                  }).ToList();

                    return result;
                });
                var outputBlock = new ActionBlock<JsonModelMetadata>(async x =>
                {
                    var str = JsonConvert.SerializeObject(x, Formatting.None);
                    await writer.WriteLineAsync(str);
                });

                //link blocks
                metadataBlock.LinkTo(apidocsBlock, new DataflowLinkOptions { PropagateCompletion = true });
                apidocsBlock.LinkTo(resourcesBlock, new DataflowLinkOptions { PropagateCompletion = true });
                resourcesBlock.LinkTo(outputBlock, new DataflowLinkOptions { PropagateCompletion = true });

                //prime the pipeline
                var json = await LoadJsonString("");
                var metadata = JsonConvert.DeserializeObject<Metadata[]>(json);
                foreach (var mUrl in metadata.Where(m => m.sectionPrefix == null).Select(m => $"{m.value}/api-docs"))
                {
                    metadataBlock.Post(mUrl);
                }
                metadataBlock.Complete();

                await outputBlock.Completion;
                writer.Close();
            }
        }

        private async Task<string> LoadJsonString(string localUrl)
        {
            using (var client = new HttpClient { Timeout = new TimeSpan(0, 0, 5, 0) })
            {
                var response = await client.GetAsync($"{_configuration.Url}/{localUrl}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
