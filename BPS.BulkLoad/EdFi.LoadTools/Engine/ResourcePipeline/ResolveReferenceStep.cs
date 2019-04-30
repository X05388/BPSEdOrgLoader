using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using log4net;
using EdFi.LoadTools.ApiClient;

namespace EdFi.LoadTools.Engine.ResourcePipeline
{
    /// <summary>
    /// Resolve references. This goes before the mapper and after the unneeded 
    /// elements have been filtered out, and obviously after we've created the reference.
    /// </summary>
    public class ResolveReferenceStep : IResourcePipelineStep
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ResolveReferenceStep).Name);
        private readonly IXmlReferenceCacheProvider _referenceCacheProvider;
        private readonly XmlModelMetadata[] _metadata;

        public ResolveReferenceStep(IXmlReferenceCacheProvider referenceCacheProvider, IEnumerable<XmlModelMetadata> metadata)
        {
            _referenceCacheProvider = referenceCacheProvider;
            _metadata = metadata.ToArray();
        }

        public bool Process(IResource resource)
        {
            var values = new[]
            {
                $"{resource.ElementName}Extension",
                resource.ElementName
            };
            var xmlModelMetadata = _metadata.FirstOrDefault(x => values.Contains(x.Model));
            if (xmlModelMetadata == null) return false;

            var targetModel = xmlModelMetadata.Model;
            var referenceCache =
                new Lazy<IXmlReferenceCache>(() => _referenceCacheProvider.GetXmlReferenceCache(resource.SourceFileName));
            ResolveReferences(referenceCache, resource.XElement, targetModel);
            return true;
        }

        private void ResolveReferences(Lazy<IXmlReferenceCache> referenceCache, XElement element, string targetModel)
        {
            var targets = element.Elements().Where(x =>
                x.Name.LocalName.EndsWith("Reference") &&
                x.Attribute("ref") != null);

            foreach (var target in targets)
            {
                // Clean the children to avoid issues with child elements messing up the imported ref
                target.Elements().Remove();
                var id = target.Attribute("ref").Value;
                var source = referenceCache.Value.VisitReference(id);
                if (source == null) { Log.Error($"A resource with id='{id}' was not found in the reference cache."); continue; }
                var map = _metadata.FirstOrDefault(x => x.Model == targetModel && x.Property == target.Name.LocalName);
                if (map == null) { Log.Error($"No replacement mapping could be found for model '{targetModel}' and property '{target.Name.LocalName}'"); continue; }
                PerformMapping(source, target, map.Type);
            }

            foreach (var childElement in element.Elements())
            {
                var metadata = _metadata.FirstOrDefault(x => x.Model == targetModel && x.Property == childElement.Name.LocalName);
                if (metadata == null) continue;
                var modelName = metadata.Type;
                ResolveReferences(referenceCache, childElement, modelName);
            }
        }

        private void PerformMapping(XElement source, XElement target, string targetModel)
        {
            var ns = target.Name.Namespace;
            var targetModels = _metadata.Where(x => x.Model == targetModel);

            var maps = targetModels.SelectMany(t => source.Elements().Select(s =>
                new
                {
                    s,
                    t,
                    m = t.Property.PercentMatchTo(s.Name.LocalName)
                }))
                .OrderByDescending(o => o.m)
                .ToList();

            while (maps.Count > 0)
            {
                var map = maps.First();
                if (map.s.HasElements)
                {
                    var targetChild = new XElement(ns + map.t.Property);
                    target.Add(targetChild);
                    PerformMapping(map.s, targetChild, map.t.Type);
                }
                else
                {
                    target.Add(new XElement(ns + map.t.Property, map.s.Value));
                }
                maps.RemoveAll(m => m.s == map.s || m.t == map.t);
            }
        }
    }
}