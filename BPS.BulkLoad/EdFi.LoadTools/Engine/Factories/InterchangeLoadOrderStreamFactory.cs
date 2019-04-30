using System.IO;
using System.Linq;

namespace EdFi.LoadTools.Engine.Factories
{
    public class InterchangeLoadOrderFileStreamFactory : IInterchangeLoadOrderStreamFactory
    {
        private readonly IInterchangeOrderConfiguration _configuration;
        private readonly string[] _filenames = {
           "InterchangeOrderMetadata-Extension.xml",
            "InterchangeOrderMetadata.xml"
        };
        public InterchangeLoadOrderFileStreamFactory (IInterchangeOrderConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Stream GetStream()
        {
            var fileName = _filenames.First(f => File.Exists(Path.Combine(_configuration.Folder,f)));
            return new FileStream(Path.Combine(_configuration.Folder, fileName), FileMode.Open, FileAccess.Read);
        }
    }
}