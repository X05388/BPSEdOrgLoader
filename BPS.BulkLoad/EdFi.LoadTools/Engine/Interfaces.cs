using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Xml.Linq;
using EdFi.LoadTools.ApiClient;
using EdFi.LoadTools.Engine.Factories;
using EdFi.LoadTools.Engine.Mapping;

namespace EdFi.LoadTools.Engine
{
    public interface IApiConfiguration : IThrottleConfiguration
    {
        string Url { get; }
        int SchoolYear { get; }
        string Profile { get; }
        int Retries { get; }
        string InstanceKey { get; }
    }

    public interface IThrottleConfiguration
    {
        int ConnectionLimit { get; }
        int TaskCapacity { get; }
        int MaxSimultaneousRequests { get; }
    }

    public interface IHashCacheConfiguration
    {
        string Folder { get; }
    }

    public interface IMappingStrategy
    {
        void MapElementToJson(XElement element, XElement jsonXElement);
    }

    public interface IMetadataFactory<out T> where T : IModelMetadata
    {
        IEnumerable<T> GetMetadata();
    }

    public interface IMetadataMapper
    {
        void CreateMetadataMappings(
            MetadataMapping mapping,
            List<ModelMetadata> jsonModels,
            List<ModelMetadata> xmlModels);
    }

    public interface IModelMetadata
    {
        string Model { get; set; }
        string Property { get; set; }
        string Type { get; set; }
        bool IsArray { get; set; }
        bool IsRequired { get; set; }
        bool IsSimpleType { get; set; }
    }

    public interface IXsdConfiguration
    {
        string Folder { get; }
        bool DoNotValidateXml { get; }
    }

    public interface IInterchangeOrderConfiguration
    {
        string Folder { get; }
    }

    public interface IDataConfiguration
    {
        string Folder { get; }
    }

    public interface IInterchangePipelineStep
    {
        bool Process(string sourceFileName, Stream stream);
    }

    public interface IInterchangeElementOrderFactory
    {
        IEnumerable<Interchange> GetInterchangeElementOrder();
    }

    public interface IInterchangeLoadOrderStreamFactory
    {
        Stream GetStream();
    }

    public interface IMetadataMappingFactory
    {
        IEnumerable<MetadataMapping> GetMetadataMappings();
    }

    public interface IResourceStreamFactory
    {
        IEnumerable<string> GetInterchangeFileNames(Interchange interchange);
        Stream GetStream(string interchangFileName);
    }

    public interface IResourceHashProvider
    {
        int Bytes { get; }
        byte[] Hash(IResource resource);
    }

    public interface IOAuthTokenConfiguration
    {
        string Url { get; }
        string Key { get; }
        string Secret { get; }
    }

    public interface IResponse
    {
        bool IsSuccess { get; }
        string ErrorMessage { get; }
        string Content { get; }
        HttpStatusCode StatusCode { get; }
    }

    public interface IWorkItem { }

    public interface IResource : IWorkItem
    {
        string ElementName { get; }
        byte[] Hash { get; }
        string HashString { get; }
        string InterchangeName { get; }
        string SourceFileName { get; }
        string Json { get; }
        IList<IResponse> Responses { get; }
        XElement XElement { get; }

        void SetJsonXElement(XElement xElement);
        void AddSubmissionResult(HttpResponseMessage response);
        void SetHash(byte[] hash);
    }

    public interface IResourceHashCache
    {
        IReadOnlyDictionary<byte[], bool> Hashes { get; }
        void Add(byte[] hash);
        bool Exists(byte[] hash);
        void Load();
        void Visited(byte[] hash);
    }

    public interface IXmlReferenceCacheFactory
    {
        void InitializeCache(string fileName);
        void Cleanup();
    }

    public interface IXmlReferenceCacheProvider
    {
        IXmlReferenceCache GetXmlReferenceCache(string fileName);
    }

    public interface IXmlReferenceCache
    {
        void PreloadReferenceSource(string id, XElement sourceElement);
        void LoadReferenceSource(string id, XElement sourceElement);
        void LoadReference(string id);
        XElement VisitReference(string id);
        bool Exists(string id);
        int RemainingReferenceCount(string id);
        int NumberOfLoadedReferences { get; }
        int NumberOfReferences { get; }
    }

    public interface IResourcePipelineStep
    {
        bool Process(IResource resource);
    }

    public interface IApiMetadataConfiguration
    {
        bool Force { get; }
        string Url { get; }
        string Folder { get; }
    }
}
