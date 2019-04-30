using System.Collections.Generic;
using System.IO;

namespace EdFi.LoadTools.Engine.Factories
{
    public class ResourceFileStreamFactory : IResourceStreamFactory
    {
        private readonly IDataConfiguration _configuration;

        public ResourceFileStreamFactory(IDataConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IEnumerable<string> GetInterchangeFileNames(Interchange interchange)
        {
            var result = new List<string>();
            //  a) are named [interchangeName].xml
            result.AddRange(Directory.GetFiles(_configuration.Folder, $"{interchange.Name}.xml"));
            //  b) match the pattern [interchangeName]-*.xml
            result.AddRange(Directory.GetFiles(_configuration.Folder, $"{interchange.Name}-*.xml"));
            //  c) are in a directory called [interchangeName] and are named *.xml
            if (Directory.Exists(Path.Combine(_configuration.Folder, interchange.Name)))
                result.AddRange(Directory.GetFiles(Path.Combine(_configuration.Folder, interchange.Name), "*.xml"));
            return result;
        }

        public Stream GetStream(string interchangFileName)
        {
            return new FileStream(interchangFileName, FileMode.Open, FileAccess.Read);
        }
    }
}