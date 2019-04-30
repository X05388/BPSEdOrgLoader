using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using log4net;
using EdFi.LoadTools.ApiClient;
using EdFi.LoadTools.Engine.ResourcePipeline;

namespace EdFi.LoadTools.Engine
{
    public class XmlReferenceCache : IXmlReferenceCache
    {
        private class XmlReferenceTracker
        {
            private readonly object _lock = new object();

            public XmlReferenceTracker(string id)
            {
                Id = id;
            }

            public string Id { get; private set; }
            private int RefCount { get; set; }
            public bool ReferenceObjectLoaded { get; private set; }
            private XElement ReferenceObject { get; set; }

            public int GetRefCount()
            {
                return RefCount;
            }

            public void IncrementRefCount()
            {
                lock (_lock)
                {
                    RefCount++;
                }
            }

            public void SetReferenceObject(Func<XElement> referenceObjectCreationFunction)
            {
                lock (_lock)
                {
                    if (ReferenceObject == null && RefCount > 0)
                    {
                        ReferenceObjectLoaded = true;
                        ReferenceObject = referenceObjectCreationFunction();
                    }
                }
            }

            public XElement VisitReferenceObject()
            {
                lock (_lock)
                {
                    RefCount--;
                    var result = ReferenceObject;
                    if (RefCount <= 0)
                    {
                        ReferenceObjectLoaded = false;
                        ReferenceObject = null;
                    }
                    return result;
                }
            }
        }

        private static readonly ILog Log = LogManager.GetLogger(typeof(XmlReferenceCache).Name);

        private readonly ConcurrentDictionary<string, XmlReferenceTracker> _xmlReferences;
        private readonly XmlModelMetadata[] _metadata;

        public XmlReferenceCache(IEnumerable<XmlModelMetadata> metadata)
        {
            _xmlReferences = new ConcurrentDictionary<string, XmlReferenceTracker>();
            _metadata = metadata.ToArray();
        }

        public void PreloadReferenceSource(string id, XElement sourceElement)
        {
            bool referenceWasAdded = false;
            var referenceTracker = _xmlReferences.GetOrAdd(id, delegate (string s)
            {
                referenceWasAdded = true;
                return new XmlReferenceTracker(s);
            });

            if (!referenceWasAdded)
            {
                referenceTracker.SetReferenceObject(() => CreateReferenceElement(sourceElement));
            }
        }

        public void LoadReferenceSource(string id, XElement sourceElement)
        {
            var referenceTracker = _xmlReferences.GetOrAdd(id, new XmlReferenceTracker(id));
            referenceTracker.SetReferenceObject(() => CreateReferenceElement(sourceElement));
        }

        public void LoadReference(string id)
        {
            var referenceTracker = _xmlReferences.GetOrAdd(id, new XmlReferenceTracker(id));
            referenceTracker.IncrementRefCount();
        }

        public XElement VisitReference(string id)
        {
            XmlReferenceTracker referenceTracker;
            if (!_xmlReferences.TryGetValue(id, out referenceTracker)) return null;
            return referenceTracker.VisitReferenceObject();
        }

        public bool Exists(string id)
        {
            return _xmlReferences.ContainsKey(id);
        }

        public int RemainingReferenceCount(string id)
        {
            XmlReferenceTracker referenceTracker;
            if (!_xmlReferences.TryGetValue(id, out referenceTracker))
            {
                return 0;
            }
            return referenceTracker.GetRefCount();
        }

        public int NumberOfLoadedReferences
        {
            get { return _xmlReferences.Values.Count(v => v.ReferenceObjectLoaded); }
        }

        public int NumberOfReferences => _xmlReferences.Values.Count;

        private XElement CreateReferenceElement(XElement sourceElement)
        {
            var elementName = sourceElement.Name.LocalName;
            if (elementName.EndsWith("Reference"))
            {
                return sourceElement;
            }

            var ns = sourceElement.Name.Namespace;

            var identityName = ns + $"{elementName}Identity";
            var referenceProperties = _metadata.Where(x => x.Model == $"{elementName}IdentityType")
                .Select(x => x.Property)
                .Distinct();

            var element = new XElement(identityName);
            var identityElements = sourceElement.Elements().Where(x => referenceProperties.Contains(x.Name.LocalName));
            element.Add(identityElements);

            var additionalReferences = element.Descendants().Where(x =>
                x.Attribute("ref") != null)
                .Select(x => new {RefId = x.Attribute("ref").Value, Element = x})
                .Where(x => !string.IsNullOrWhiteSpace(x.RefId)).ToList();

            foreach (var additionalReference in additionalReferences)
            {
                var subReference = VisitReference(additionalReference.RefId);
                if (subReference == null)
                {
                    Log.Error($"Unable to resolve required subreference '{additionalReference.RefId}' while creating reference element of type '{sourceElement.Name}'.  This might be caused by an incorrect load order.");
                    continue;
                }
                additionalReference.Element.ReplaceWith(subReference);
            }

            var referenceName = ns + $"{elementName}Reference";
            return new XElement(referenceName, element);
        }
    }
}