using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using log4net.Config;
using EdFi.LoadTools.ApiClient;
using EdFi.ApiLoader.Console.Application;
using EdFi.LoadTools.Engine;
using EdFi.LoadTools.Engine.Factories;
using EdFi.LoadTools.Engine.InterchangePipeline;
using EdFi.LoadTools.Engine.Mapping;
using EdFi.LoadTools.Engine.ResourcePipeline;
using SimpleInjector;

namespace EdFi.ApiLoader.Console
{
    public class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program).Name);
        private static void Main(string[] args)
        {
            XmlConfigurator.Configure();
            //BasicConfigurator.Configure();
            var exitCode = 0;
            var p = new CommandLineParser();
            p.SetupHelp("?", "Help").Callback(text =>
            {
                System.Console.WriteLine(text);
                Environment.Exit(0);
            });

            var result = p.Parse(args);

            if (result.HasErrors || !p.Object.IsValid)
            {
                exitCode = 1;
                System.Console.Write(result.ErrorText);
                System.Console.Write(p.Object.ErrorText);
            }
            else
            {
                try
                {
                    log4net.GlobalContext.Properties["InstanceKey"] = p.Object.InstanceKey;
                    using (var container = new Container())
                    {
                        LogConfiguration(p.Object);

                        // configure DI container
                        ConfigureCompositionRoot(container, p.Object);

                        // retrieve application
                        var application = container.GetInstance<EdFi.LoadTools.Engine.Application>();

                        // run application 
                        exitCode = application.Run().Result;
                    }
                }
                catch (AggregateException ex)
                {
                    exitCode = 1;
                    foreach (var e in ex.InnerExceptions)
                    {
                        Log.Error(e);
                    }
                }
            }
            Environment.Exit(exitCode);
        }

        private static void LogConfiguration(Configuration configuration)
        {
            Log.Info($"Api Url:                  {configuration.ApiUrl}");
            Log.Info($"School Year:              {configuration.SchoolYear}");
            Log.Info($"Retries:                  {configuration.Retries}");
            Log.Info($"Oauth Key:                {configuration.OauthKey}");
            Log.Info($"Metadata Url:             {configuration.MetadataUrl}");
            Log.Info($"Data Folder:              {configuration.DataFolder}");
            Log.Info($"Working Folder:           {configuration.WorkingFolder}");
            Log.Info($"Xsd Folder:               {configuration.XsdFolder}");
            Log.Info($"Interchange Order Folder: {configuration.InterchangeOrderFolder}");
        }

        private static void ConfigureCompositionRoot(Container container, Configuration configuration)
        {
            container.RegisterSingleton<SwaggerMetadataRetriever>();
            container.RegisterSingleton<XsdStreamsRetriever>();

            container.RegisterSingleton<IApiConfiguration>(() => configuration);
            container.RegisterSingleton<IApiMetadataConfiguration>(() => configuration);
            container.RegisterSingleton<IHashCacheConfiguration>(() => configuration);
            container.RegisterSingleton<IDataConfiguration>(() => configuration);
            container.RegisterSingleton<IOAuthTokenConfiguration>(() => configuration);
            container.RegisterSingleton<IXsdConfiguration>(() => configuration);
            container.RegisterSingleton<IInterchangeOrderConfiguration>(() => configuration);
            container.RegisterSingleton<IThrottleConfiguration>(() => configuration);

            container.RegisterSingleton<IEnumerable<JsonModelMetadata>>(
                () => container.GetInstance<IMetadataFactory<JsonModelMetadata>>().GetMetadata().ToArray());

            container.RegisterSingleton<IEnumerable<XmlModelMetadata>>(
                () => container.GetInstance<IMetadataFactory<XmlModelMetadata>>().GetMetadata().ToArray());

            container.RegisterSingleton(
                () => container.GetInstance<IMetadataMappingFactory>().GetMetadataMappings());

            var xmlReferenceCacheFactoryRegistration =
                Lifestyle.Singleton.CreateRegistration<XmlReferenceCacheFactory>(container);
            container.AddRegistration(typeof(IXmlReferenceCacheFactory), xmlReferenceCacheFactoryRegistration);
            container.AddRegistration(typeof(IXmlReferenceCacheProvider), xmlReferenceCacheFactoryRegistration);

            container.RegisterSingleton(() => container.GetInstance<SchemaSetFactory>().GetSchemaSet());

            container.Register<IResourceHashProvider, ResourceHashProvider>(Lifestyle.Singleton);
            container.Register<IResourceHashCache, ResourceHashCache>(Lifestyle.Singleton);
            container.Register<IInterchangeElementOrderFactory, InterchangeElementOrderFactory>(Lifestyle.Singleton);
            container.Register<IInterchangeLoadOrderStreamFactory, InterchangeLoadOrderFileStreamFactory>(Lifestyle.Singleton);
            container.Register<IMetadataFactory<JsonModelMetadata>, JsonMetadataFactory>(Lifestyle.Singleton);
            container.Register<IMetadataFactory<XmlModelMetadata>, XsdMetadataFactory>(Lifestyle.Singleton);
            container.Register<IMetadataMappingFactory, MetadataMappingFactory>(Lifestyle.Singleton);
            container.Register<IResourceStreamFactory, ResourceFileStreamFactory>(Lifestyle.Singleton);

            container.RegisterCollection<IInterchangePipelineStep>(new[]
            {
                typeof(IsNotEmptyStep),
                typeof(ValidateXmlStep),
                typeof(FindReferencesStep),
                typeof(PreloadReferencesStep)
            });

            container.RegisterCollection<IResourcePipelineStep>(new[] {
                typeof(ComputeHashStep),
                typeof(FilterResourceStep),
                typeof(ResolveReferenceStep),
                typeof(MapElementStep),
            });

            container.RegisterCollection<IMetadataMapper>(new[]
            {
                typeof (DiminishingMetadataMapper),
                typeof (ArrayMetadataMapper),
                typeof (DescriptorReferenceMetadataMapper),
                typeof (NameMatchingMetadataMapper),
                typeof (SchoolIdBugFixMetadataMapper)
            });

            container.Verify();
        }
    }
}
