using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using log4net;
using EdFi.LoadTools.ApiClient;
using EdFi.LoadTools.Engine;
using EdFi.LoadTools.Engine.ResourcePipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EdFi.LoadTools.Test
{
    public class XmlReferenceCacheTests
    {

        private class TestCacheProvider : IXmlReferenceCacheProvider
        {
            private readonly XmlReferenceCache _cache;

            public TestCacheProvider(XmlReferenceCache cache)
            {
                _cache = cache;
            }

            public IXmlReferenceCache GetXmlReferenceCache(string fileName)
            {
                return _cache;
            }
        }

        private const string Id1 = "1";
        private const string Id2 = "2";
        private const string ElementA = "A";
        private const string ElementB = "B";

        private static IEnumerable<XmlModelMetadata> CreateEmptyMetadata()
        {
            return Enumerable.Empty<XmlModelMetadata>();
        }

        private static XElement CreateElement(string elementType, string id)
        {
            return new XElement(elementType,
                    new XAttribute("id", id)
                    );
        }

        [TestClass]
        public class When_id_but_no_references
        {
            private XmlReferenceCache _cache;

            [TestInitialize]
            public void Setup()
            {
                _cache = new XmlReferenceCache(CreateEmptyMetadata());
                _cache.PreloadReferenceSource(Id1, CreateElement(ElementA, id: Id1));
            }

            [TestMethod]
            public void Should_not_have_reference_element()
            {
                Assert.IsTrue(_cache.Exists(Id1));
                Assert.IsNull(_cache.VisitReference(Id1));
            }
        }

        [TestClass]
        public class When_id_with_reference_second
        {
            private XmlReferenceCache _cache;

            [TestInitialize]
            public void Setup()
            {
                _cache = new XmlReferenceCache(CreateEmptyMetadata());
                _cache.PreloadReferenceSource(Id1, CreateElement(ElementA, Id1));
                _cache.LoadReference(Id1);
            }

            [TestMethod]
            public void Should_not_have_reference_element()
            {
                Assert.IsTrue(_cache.Exists(Id1));
                Assert.AreEqual(_cache.RemainingReferenceCount(Id1), 1);
                Assert.IsNull(_cache.VisitReference(Id1));
            }
        }

        [TestClass]
        public class When_id_with_reference_first
        {
            private XmlReferenceCache _cache;

            [TestInitialize]
            public void Setup()
            {
                _cache = new XmlReferenceCache(CreateEmptyMetadata());
                _cache.LoadReference(Id1);
                _cache.PreloadReferenceSource(Id1, CreateElement(ElementA, Id1));
            }

            [TestMethod]
            public void Should_have_reference_element()
            {
                Assert.IsTrue(_cache.Exists(Id1));
                Assert.AreEqual(_cache.RemainingReferenceCount(Id1), 1);
                Assert.IsNotNull(_cache.VisitReference(Id1));
            }
        }

        [TestClass]
        public class When_loading_reference_for_non_preloaded_reference
        {
            private XmlReferenceCache _cache;

            [TestInitialize]
            public void Setup()
            {
                _cache = new XmlReferenceCache(CreateEmptyMetadata());
                var element = CreateElement(ElementA, Id1);
                _cache.PreloadReferenceSource(Id1, element);
                _cache.LoadReference(Id1);
                _cache.LoadReferenceSource(Id1, element);
            }

            [TestMethod]
            public void Should_have_reference_element()
            {
                Assert.IsTrue(_cache.Exists(Id1));
                Assert.AreEqual(_cache.RemainingReferenceCount(Id1), 1);
                Assert.IsNotNull(_cache.VisitReference(Id1));
                Assert.AreEqual(_cache.RemainingReferenceCount(Id1), 0);
                Assert.IsNull(_cache.VisitReference(Id1));
            }
        }

        [TestClass]
        public class When_loading_reference_for_preloaded_reference
        {
            private XmlReferenceCache _cache;

            [TestInitialize]
            public void Setup()
            {
                _cache = new XmlReferenceCache(CreateEmptyMetadata());
                var element = CreateElement(ElementA, Id1);
                _cache.LoadReference(Id1);
                _cache.PreloadReferenceSource(Id1, element);
                _cache.LoadReferenceSource(Id1, element);
            }

            [TestMethod]
            public void Should_have_reference_element()
            {
                Assert.IsTrue(_cache.Exists(Id1));
                Assert.AreEqual(_cache.RemainingReferenceCount(Id1), 1);
                Assert.IsNotNull(_cache.VisitReference(Id1));
                Assert.AreEqual(_cache.RemainingReferenceCount(Id1), 0);
                Assert.IsNull(_cache.VisitReference(Id1));
            }
        }

        [TestClass]
        public class When_id_with_missing_subreference
        {
            private TestAppender _testAppender;
            private XmlReferenceCache _cache;

            [TestInitialize]
            public void Setup()
            {
                _testAppender = new TestAppender();
                _testAppender.AttachToRoot();

                var metadata = new XmlModelMetadata
                {
                    Model = $"{ElementA}IdentityType",
                    Property = ElementB,
                    Type = $"{ElementB}ReferenceType"
                };
                _cache = new XmlReferenceCache(new[] { metadata });
                var element =
                    XElement.Parse($"<{ElementA} ref='{Id1}'><{ElementB} ref='{Id2}'/></{ElementA}>");
                _cache.PreloadReferenceSource(Id1, element);
                _cache.LoadReference(Id1);
                _cache.LoadReferenceSource(Id1, element);
            }

            [TestCleanup]
            public void Teardown()
            {
                _testAppender.DetachFromRoot();
            }

            [TestMethod]
            public void Should_have_reference_element()
            {
                Assert.IsTrue(_cache.Exists(Id1));
                Assert.AreEqual(_cache.RemainingReferenceCount(Id1), 1);
                Assert.IsNotNull(_cache.VisitReference(Id1));
                Assert.AreEqual(_cache.RemainingReferenceCount(Id1), 0);
                Assert.IsNull(_cache.VisitReference(Id1));

                var log = _testAppender.Logs.SingleOrDefault(x => x.Level.Name == "ERROR" && x.LoggerName == typeof(XmlReferenceCache).Name);
                Assert.IsNotNull(log);
                Assert.IsTrue(log.RenderedMessage.Contains("Unable to resolve required subreference"));
                Assert.IsTrue(log.RenderedMessage.Contains($"'{Id2}'"));
                Assert.IsTrue(log.RenderedMessage.Contains($"'{ElementA}'"));
            }
        }

        [TestClass]
        public class When_id_with_valid_subreference
        {
            private TestAppender _testAppender;
            private XmlReferenceCache _cache;

            private XElement _element;
            private XElement _subElement;

            [TestInitialize]
            public void Setup()
            {
                _testAppender = new TestAppender();
                _testAppender.AttachToRoot();

                var metadata = new XmlModelMetadata
                {
                    Model = $"{ElementA}IdentityType",
                    Property = ElementB,
                    Type = $"{ElementB}ReferenceType"
                };
                _cache = new XmlReferenceCache(new[] { metadata });
                _element =
                    XElement.Parse($"<{ElementA} ref='{Id1}'><{ElementB} ref='{Id2}'/></{ElementA}>");
                _subElement = XElement.Parse($"<{ElementB} id='{Id2}' />");
                _cache.PreloadReferenceSource(Id2, _subElement);
                _cache.PreloadReferenceSource(Id1, _element);
                _cache.LoadReference(Id1);
                _cache.LoadReference(Id2);
            }

            [TestCleanup]
            public void Teardown()
            {
                _testAppender.DetachFromRoot();
            }

            [TestMethod]
            public void Should_have_reference_element()
            {
                Assert.IsTrue(_cache.Exists(Id1));
                Assert.AreEqual(1, _cache.RemainingReferenceCount(Id1));
                _cache.LoadReferenceSource(Id2, _subElement);

                Assert.IsTrue(_cache.Exists(Id2));
                Assert.AreEqual(1, _cache.RemainingReferenceCount(Id2));
                _cache.LoadReferenceSource(Id1, _element);
                Assert.AreEqual(0, _cache.RemainingReferenceCount(Id2));

                Assert.IsNotNull(_cache.VisitReference(Id1));
                Assert.AreEqual(0, _cache.RemainingReferenceCount(Id1));
                Assert.IsNull(_cache.VisitReference(Id1));
                Assert.IsNull(_cache.VisitReference(Id2));

                Assert.IsFalse(_testAppender.Logs.Any());
            }
        }

        [TestClass]
        public class When_id_with_valid_subreference_with_wrong_load_order
        {
            private TestAppender _testAppender;
            private XmlReferenceCache _cache;

            private XElement _element;
            private XElement _subElement;

            [TestInitialize]
            public void Setup()
            {
                _testAppender = new TestAppender();
                _testAppender.AttachToRoot();

                var metadata = new XmlModelMetadata
                {
                    Model = $"{ElementA}IdentityType",
                    Property = ElementB,
                    Type = $"{ElementB}ReferenceType"
                };
                _cache = new XmlReferenceCache(new[] { metadata });
                _element =
                    XElement.Parse($"<{ElementA} ref='{Id1}'><{ElementB} ref='{Id2}'/></{ElementA}>");
                _subElement = XElement.Parse($"<{ElementB} id='{Id2}' />");
                _cache.PreloadReferenceSource(Id1, _element);
                _cache.PreloadReferenceSource(Id2, _subElement);
                _cache.LoadReference(Id1);
                _cache.LoadReference(Id2);
            }

            [TestCleanup]
            public void Teardown()
            {
                _testAppender.DetachFromRoot();
            }

            [TestMethod]
            public void Should_have_reference_element()
            {
                Assert.IsTrue(_cache.Exists(Id1));
                Assert.IsTrue(_cache.Exists(Id2));
                Assert.AreEqual(1, _cache.RemainingReferenceCount(Id1));
                Assert.AreEqual(1, _cache.RemainingReferenceCount(Id2));
                _cache.LoadReferenceSource(Id1, _element);
                _cache.LoadReferenceSource(Id2, _subElement);

                Assert.IsNotNull(_cache.VisitReference(Id1));
                Assert.AreEqual(0, _cache.RemainingReferenceCount(Id1));
                Assert.AreEqual(0, _cache.RemainingReferenceCount(Id2));
                Assert.IsNull(_cache.VisitReference(Id1));

                var log = _testAppender.Logs.SingleOrDefault(x => x.Level.Name == "ERROR" && x.LoggerName == typeof(XmlReferenceCache).Name);
                Assert.IsNotNull(log);
                Assert.IsTrue(log.RenderedMessage.Contains("Unable to resolve required subreference"));
                Assert.IsTrue(log.RenderedMessage.Contains($"'{Id2}'"));
                Assert.IsTrue(log.RenderedMessage.Contains($"'{ElementA}'"));
            }
        }

        [TestClass]
        public class When_resolving_references
        {
            private TestAppender _testAppender;
            private TestCacheProvider _cacheProvider;
            private ResolveReferenceStep _step;
            private XmlModelMetadata _metadata;
            private IResource _resource;

            [TestInitialize]
            public void Setup()
            {
                _testAppender = new TestAppender();
                _testAppender.AttachToRoot();

                _cacheProvider = new TestCacheProvider(new XmlReferenceCache(CreateEmptyMetadata()));

                _metadata = new XmlModelMetadata
                {
                    Model = "A",
                    Property = "B",
                    Type = "string"
                };

                var element = XElement.Parse("<A><ResourceReference ref='1'/></A>");

                _step = new ResolveReferenceStep(_cacheProvider, new[] { _metadata });
                _resource = new ResourceWorkItem("interchange", "filename", element);
            }

            [TestCleanup]
            public void Teardown()
            {
                _testAppender.DetachFromRoot();
            }

            [TestMethod]
            public void Should_not_throw_exception_on_unresolved_references()
            {
                var result = _step.Process(_resource);
                var log = _testAppender.Logs.SingleOrDefault(x => x.Level.Name == "ERROR" && x.LoggerName == _step.GetType().Name);
                Assert.IsNotNull(log);
                Assert.IsTrue(log.RenderedMessage.Contains("'1'"));
            }
        }
    }
}
