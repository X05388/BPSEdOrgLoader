using System;
using System.IO;
using System.Text;
using EdFi.LoadTools.Engine;

namespace EdFi.ApiLoader.Console.Application
{
    public class Configuration : IApiConfiguration, IHashCacheConfiguration, 
        IDataConfiguration, IOAuthTokenConfiguration, IApiMetadataConfiguration, 
        IXsdConfiguration, IInterchangeOrderConfiguration, IThrottleConfiguration
    {
        public string ApiUrl { get; set; }
        public int Retries { get; set; }
        public int SchoolYear { get; set; }
        public string OauthKey { get; set; }
        public string OauthSecret { get; set; }
        public string OauthUrl { get; set; }
        public string DataFolder { get; set; }
        public string WorkingFolder { get; set; }
        public string Profile { get; set; }
        public bool Metadata { get; set; }
        public string MetadataFolder { get; set; }
        public string MetadataUrl { get; set; }
        public string XsdFolder { get; set; }
        public bool DoNotValidateXml { get; set; }
        public string InterchangeOrderFolder { get; set; }
        public int ConnectionLimit { get; set; }
        public int TaskCapacity { get; set; }
        public int MaxSimultaneousRequests { get; set; }
        public string InstanceKey { get; set; }

        string IApiConfiguration.Url => Path.Combine(ApiUrl, SchoolYear.ToString()) + "/";

        bool IApiMetadataConfiguration.Force => Metadata;
        string IApiMetadataConfiguration.Folder => !string.IsNullOrWhiteSpace(MetadataFolder) ? MetadataFolder : WorkingFolder;
        
        string IApiMetadataConfiguration.Url => MetadataUrl;

        string IHashCacheConfiguration.Folder => WorkingFolder;

        string IDataConfiguration.Folder => DataFolder;
        
        string IOAuthTokenConfiguration.Url => OauthUrl;
        string IOAuthTokenConfiguration.Key => OauthKey;
        string IOAuthTokenConfiguration.Secret => OauthSecret;

        string IXsdConfiguration.Folder => XsdFolder;
        bool IXsdConfiguration.DoNotValidateXml => DoNotValidateXml;

        string IInterchangeOrderConfiguration.Folder => InterchangeOrderFolder;

        internal string Token { get; set; }

        public bool LoadModelMetadata => !string.IsNullOrEmpty(MetadataUrl);

        public bool IsValid
        {
            get
            {
                var result =
                    !(
                        string.IsNullOrEmpty(ApiUrl) ||
                        string.IsNullOrEmpty(WorkingFolder) ||
                        string.IsNullOrEmpty(DataFolder) ||
                        string.IsNullOrEmpty(XsdFolder) ||
                        string.IsNullOrEmpty(InterchangeOrderFolder) ||
                        string.IsNullOrEmpty(MetadataUrl) ||
                        string.IsNullOrEmpty(OauthKey) ||
                        string.IsNullOrEmpty(OauthSecret) ||
                        string.IsNullOrEmpty(OauthUrl)
                    )
                    && Directory.Exists(DataFolder)
                    && Uri.IsWellFormedUriString(ApiUrl, UriKind.Absolute)
                    && Uri.IsWellFormedUriString(MetadataUrl, UriKind.Absolute)
                    && Uri.IsWellFormedUriString(OauthUrl, UriKind.Absolute);

                return result;
            }
        }

        public string ErrorText
        {
            get
            {
                var sb = new StringBuilder();

                if (string.IsNullOrEmpty(OauthKey))
                    sb.AppendLine("Option 'k:key' parse error. missing value.");

                if (string.IsNullOrEmpty(OauthSecret))
                    sb.AppendLine("Option 's:secret' parse error. missing value.");

                if (string.IsNullOrEmpty(ApiUrl) || !Uri.IsWellFormedUriString(ApiUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'a:apiurl' parse error. Provided value is not a url.");

                if (string.IsNullOrEmpty(DataFolder) || !Directory.Exists(DataFolder))
                    sb.AppendLine("Option 'd:data' parse error. Provided value is not a directory.");

                if (string.IsNullOrEmpty(MetadataUrl) || !Uri.IsWellFormedUriString(MetadataUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'metadataurl' parse error. Provided value is not a url.");

                if (string.IsNullOrEmpty(OauthUrl) || !Uri.IsWellFormedUriString(OauthUrl, UriKind.Absolute))
                    sb.AppendLine("Option 'o:oauthurl' parse error. Provided value is not a url.");

                if (string.IsNullOrEmpty(WorkingFolder) || !Directory.Exists(WorkingFolder))
                    sb.AppendLine("Option 'w:working' parse error. Provided value is not a directory.");
                
                return sb.ToString();
            }
        }
    }
}