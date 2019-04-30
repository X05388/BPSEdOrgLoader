using System;
using System.IO;
using System.Linq;
using Fclp;
using EdFi.ApiLoader.Console.Properties;

namespace EdFi.ApiLoader.Console.Application
{
    public class CommandLineParser : FluentCommandLineParser<Configuration>
    {
        public CommandLineParser()
        {
            Setup(arg => arg.ApiUrl).As('a', "apiurl")
                .WithDescription("The web API url (i.e. http://server/api/v1.0)")
                .SetDefault(Settings.Default.ApiUrl);

            Setup(arg => arg.SchoolYear).As('y', "year")
                .WithDescription("The target school year for the web API (i.e. 2016)")
                .SetDefault(GetFirstValue(
                        Settings.Default.SchoolYear,
                        DateTime.Today.Year));

            Setup(arg => arg.Retries).As('r', "retries")
                .WithDescription("The number of times to retry submitting a resource")
                .SetDefault(GetFirstValue(Settings.Default.MaxRetries, 3).GetValueOrDefault());

            Setup(arg => arg.DataFolder).As('d', "data")
                .WithDescription("Path to folder containing the data files to be submitted")
                .SetDefault(GetFirstValue(
                    Settings.Default.DataFolder,
                    Directory.GetCurrentDirectory()));

            Setup(arg => arg.OauthKey).As('k', "key")
                .WithDescription("The web API OAuth key")
                .SetDefault(Settings.Default.OauthKey);

            Setup(arg => arg.Metadata).As('f')
                .WithDescription("Force reload of metadata from metadata url")
                .SetDefault(false);

            Setup(arg => arg.MetadataUrl).As('m', "metadataurl")
                .WithDescription("The metadata url (i.e. http://server/metadata)")
                .SetDefault(Settings.Default.SwaggerUrl);

            Setup(arg => arg.MetadataFolder).As('q', "metadatafolder")
                .WithDescription("Path to a writable folder containing the metadata file")
                .SetDefault(Settings.Default.MetadataFolder);

            Setup(arg => arg.OauthUrl).As('o', "oauthurl")
                .WithDescription("The OAuth url (i.e. http://server/oauth)")
                .SetDefault(Settings.Default.OauthUrl);

            Setup(arg => arg.Profile).As('p', "profile")
                .WithDescription("The name of an API profile to use (optional)");

            Setup(arg => arg.OauthSecret).As('s', "secret")
                 .WithDescription("The web API OAuth secret")
                 .SetDefault(Settings.Default.OauthSecret);

            Setup(arg => arg.WorkingFolder).As('w', "working")
                .WithDescription("Path to a writable folder containing the working files")
                .SetDefault(GetFirstValue(
                    Settings.Default.WorkingFolder,
                    Directory.GetCurrentDirectory()));

            Setup(arg => arg.XsdFolder).As('x', "xsd")
                .WithDescription("Path to a folder containing the Ed-Fi Xsd Schema files")
                .SetDefault(GetFirstValue(
                    Settings.Default.XsdFolder,
                    Path.Combine(Settings.Default.WorkingFolder, "Schema"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Schema")
                    ));

            Setup(arg => arg.DoNotValidateXml).As('n',"novalidation")
                .WithDescription("Do not validate incoming XML documents against the XSD Schema")
                .SetDefault(false);

            Setup(arg => arg.InterchangeOrderFolder).As('i', "interchangeorder")
                .WithDescription("Path to a folder containing the Ed-Fi Interchange Order files")
                .SetDefault(GetFirstValue(
                    Settings.Default.InterchangeOrderFolder,
                    Directory.GetCurrentDirectory()
                    ));

            Setup(arg => arg.ConnectionLimit).As('c', "connectionlimit")
                .WithDescription("Maximum concurrent connections to api")
                .SetDefault(GetFirstValue(
                    Settings.Default.ConnectionLimit,
                    50
                    ));

            Setup(arg => arg.TaskCapacity).As('t', "taskcapacity")
                .WithDescription("Maximum concurrent tasks to be buffered")
                .SetDefault(GetFirstValue(
                    Settings.Default.TaskCapacity,
                    50
                    ));
            Setup(arg => arg.MaxSimultaneousRequests).As('l', "maxRequests")
                .WithDescription("Max number of simultaneous API requests")
                .SetDefault(GetFirstValue(
                    Settings.Default.MaxSimultaneousApiRequests,
                    1
                    ));
            Setup(arg => arg.InstanceKey).As('u', "InstanceKey")
                .WithDescription("Key for log entries to indicate context")
                .SetDefault(string.Empty);
        }

        private static string GetFirstValue(params string[] defaults)
            => defaults.FirstOrDefault(x => !string.IsNullOrEmpty(x));

        private static T GetFirstValue<T>(params T[] defaults)
            => defaults.FirstOrDefault((T x) => !Equals(x, default(T)));
    }
}