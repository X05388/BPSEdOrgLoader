using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace EdFi.LoadTools.Engine
{
    public class ResourceHashCache : IResourceHashCache, IDisposable
    {
        private class ByteArrayComparer : IComparer<byte[]>, IEqualityComparer<byte[]>
        {
            public int Compare(byte[] x, byte[] y)
            {
                return x.Select((t, i) => t.CompareTo(y[i])).FirstOrDefault(result => result != 0);
            }

            public bool Equals(byte[] x, byte[] y)
            {
                return Compare(x, y) == 0;
            }

            public int GetHashCode(byte[] obj)
            {
                var result = BitConverter.ToInt32(obj, 0);
                return result;
            }
        }

        private class WriteBlock
        {
            public string Filename { get; set; }
            public byte[] Hash { get; set; }
        }

        private readonly string _folder;
        private readonly IResourceHashProvider _hashProvider;
        private readonly ConcurrentDictionary<byte[], bool> _hashes;

        private readonly BufferBlock<WriteBlock> _writeBuffer = new BufferBlock<WriteBlock>();
        private readonly ActionBlock<WriteBlock> _writeBlock = new ActionBlock<WriteBlock>(writeBlock =>
        {
            using (var stream = new FileStream(writeBlock.Filename, FileMode.Append))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(writeBlock.Hash);
            }
        });
        
        IReadOnlyDictionary<byte[], bool> IResourceHashCache.Hashes => _hashes;

        public ResourceHashCache(IHashCacheConfiguration configuration, IResourceHashProvider hashProvider)
        {
            _hashProvider = hashProvider;
            _folder = configuration.Folder;
            _hashes = new ConcurrentDictionary<byte[], bool>(new ByteArrayComparer());
            _writeBuffer.LinkTo(_writeBlock, new DataflowLinkOptions { PropagateCompletion = true });
        }

        private static byte[] GetCopyOf(byte[] bytearray)
        {
            var copy = new byte[bytearray.Length];
            Array.Copy(bytearray, copy, bytearray.Length);
            return copy;
        }

        public void Add(byte[] hash)
        {
            var copy = GetCopyOf(hash);
            _hashes.AddOrUpdate(copy, true, (k, v) => v);
            WriteToFile(copy);
        }

        public bool Exists(byte[] hash)
        {
            return _hashes.ContainsKey(hash);
        }

        private void WriteToFile(byte[] hash)
        {
            var block = new WriteBlock
            {
                Filename = Filename.Value,
                Hash = hash
            };
            _writeBuffer.Post(block);
        }

        public void Visited(byte[] hash)
        {
            var copy = GetCopyOf(hash);
            if (!_hashes.ContainsKey(hash)) return;
            _hashes[copy] = true;
            WriteToFile(copy);
        }

        public void Load()
        {
            var files = Directory.GetFiles(_folder, $"*.hash").ToList();
            var file = files.OrderByDescending(f => f).FirstOrDefault();
            Load(file);
        }

        public void Load(string filename)
        {
            _hashes.Clear();
            if (string.IsNullOrEmpty(filename) || !File.Exists(filename)) return;
            using (var fileStream = new FileStream(filename, FileMode.Open))
            using (var bufferedStream = new BufferedStream(fileStream))
            using (var reader = new BinaryReader(bufferedStream))
            {
                var pos = 0L;
                var length = reader.BaseStream.Length;
                while (pos < length)
                {
                    var bytes = reader.ReadBytes(_hashProvider.Bytes);
                    _hashes.AddOrUpdate(bytes, false, (k, v) => v);
                    pos += _hashProvider.Bytes;
                }
            }
        }

        public void Save()
        {
            var timestamp = DateTime.UtcNow.Ticks;
            var filename = Path.ChangeExtension(Path.Combine(_folder, $"{timestamp}"), ".hash");
            Save(filename);
        }

        public void Save(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.CreateNew))
            using (var buffer = new BufferedStream(stream))
            using (var writer = new BinaryWriter(buffer))
            {
                foreach (var value in _hashes.Where(x => x.Value).Select(x => x.Key))
                {
                    writer.Write(value);
                }
                writer.Flush();
            }
        }

        private string _filename;
        private Lazy<string> Filename => new Lazy<string>(() =>
        {
            if (!string.IsNullOrEmpty(_filename)) return _filename;
            var timestamp = DateTime.UtcNow.Ticks;
            return _filename = Path.ChangeExtension(Path.Combine(_folder, $"{timestamp}"), ".hash");
        });

        public void Dispose()
        {
            _writeBuffer.Complete();
            _writeBlock.Completion.Wait();
        }
    }
}