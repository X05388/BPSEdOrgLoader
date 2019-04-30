using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EdFi.LoadTools.Engine.Factories
{
    public class XsdStreamsRetriever
    {
        private readonly IXsdConfiguration _configuration;
        private readonly Regex _regex = new Regex(Constants.XsdRegex);

        public XsdStreamsRetriever(IXsdConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IEnumerable<Stream> GetStreams()
        {
            var files = Directory.GetFiles(_configuration.Folder, "*.xsd").ToList();
            // Find any interchange with an extension, and select the core file name for removal
            var coreInterchangesToRemove =
                files.Select(f => _regex.Match(f))
                    .Where(match => match != null && match.Success)
                    .Select(match => $@"Interchange-{match.Groups["InterchangeType"].Value}.xsd");
            files = files.Where(f => coreInterchangesToRemove.All(ci => Path.GetFileName(f) != ci)).ToList();
            return files.Select(x => new FileStream(x, FileMode.Open, FileAccess.Read));
        }
    }
}