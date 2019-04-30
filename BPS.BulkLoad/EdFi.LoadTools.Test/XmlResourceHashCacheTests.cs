using System;
using System.Collections.Generic;
using System.IO;
using EdFi.LoadTools.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdFi.LoadTools.Test
{

    public class XmlResourceHashCacheTests
    {
        private class Configuration : IHashCacheConfiguration
        {
            public string Folder => Directory.GetCurrentDirectory();
        }

        [TestClass]
        public class WhenCreatingLotsOfHashes
        {
            private readonly Configuration _configuration = new Configuration();
            private readonly List<byte[]> _bytes = new List<byte[]>();
            private readonly ResourceHashProvider _hashProvider = new ResourceHashProvider();
            private ResourceHashCache _cache;

            [TestInitialize]
            public void Setup()
            {
                var rnd = new Random(100);
                _cache = new ResourceHashCache(_configuration, _hashProvider);
                for (var i = 0; i < 1000000; i++)
                {
                    var bytes = new byte[32];
                    rnd.NextBytes(bytes);
                    _cache.Add(bytes);
                    if (i % 100 == 0) _bytes.Add(bytes);
                }
            }

            [TestMethod]
            public void Should_find_all_cached_values()
            {
                foreach (var item in _bytes)
                {
                    Assert.IsTrue(_cache.Exists(item));
                }
            }
        }

        [TestClass]
        public class WhenSavingAndLoadingToFile
        {
            private readonly Configuration _configuration = new Configuration();
            private readonly List<byte[]> _bytes = new List<byte[]>();
            private readonly ResourceHashProvider _hashProvider = new ResourceHashProvider();
            private string _filename;
            private ResourceHashCache _cache1;
            private ResourceHashCache _cache2;

            [TestInitialize]
            public void Setup()
            {
                _filename = Path.Combine(_configuration.Folder, Path.GetRandomFileName());
                var rnd = new Random(100);
                _cache1 = new ResourceHashCache(_configuration, _hashProvider);
                for (var i = 0; i < 1000000; i++)
                {
                    var bytes = new byte[_hashProvider.Bytes];
                    rnd.NextBytes(bytes);
                    _cache1.Add(bytes);
                    _bytes.Add(bytes);
                }
                _cache1.Save(_filename);

                _cache2 = new ResourceHashCache(_configuration, _hashProvider);
                _cache2.Load(_filename);
            }

            [TestCleanup]
            public void Cleanup()
            {
                File.Delete(_filename);
            }

            [TestMethod]
            public void Should_find_all_cached_values_in_loaded_cache()
            {
                foreach (var item in _bytes)
                {
                    Assert.IsTrue(_cache2.Exists(item));
                }
            }
        }

    }
}
