using System.Collections.Generic;
using System.Linq;
using EdFi.LoadTools.ApiClient;

namespace EdFi.LoadTools.Engine.Factories
{
    public class JsonMetadataFactory : IMetadataFactory<JsonModelMetadata>
    {
        private readonly SwaggerMetadataRetriever _swaggerMetadata;

        public JsonMetadataFactory(SwaggerMetadataRetriever swaggerMetadata)
        {
            _swaggerMetadata = swaggerMetadata;
        }

        public IEnumerable<JsonModelMetadata> GetMetadata()
        {
            return _swaggerMetadata.GetMetadata()
                .Result
                .Where(j => j.Property != "_etag" 
                && j.Property != "id" 
                && j.Property != "link"
                && j.Property != "priorDescriptorId"
                && !(j.Model.EndsWith("Descriptor") && j.Property == $"{j.Model}Id"));
        }
    }
}
