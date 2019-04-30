using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace EdFi.LoadTools.Engine
{
    public class ResourceHashProvider: IResourceHashProvider
    {
        private readonly ThreadLocal<HashAlgorithm> _algorithm = new ThreadLocal<HashAlgorithm>(() => new SHA1CryptoServiceProvider());

        public int Bytes => _algorithm.Value.HashSize / 8 ;

        public byte[] Hash(IResource resource)
        {
            var bytes = Encoding.UTF8.GetBytes(resource.XElement.ToString());
            return _algorithm.Value.ComputeHash(bytes);
        }
    }
}
