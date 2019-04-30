namespace EdFi.LoadTools.Engine.ResourcePipeline
{
    /// <summary>
    /// Filter resources fourth, because any additional work on the resource is unnecessary.
    /// </summary>
    public class FilterResourceStep : IResourcePipelineStep
    {
        private readonly IResourceHashCache _hashCache;

        public FilterResourceStep(IResourceHashCache xmlResourceHashCache)
        {
            _hashCache = xmlResourceHashCache;
        }
        
        public bool Process(IResource resource)
        {
            if (!_hashCache.Exists(resource.Hash)) return true;
            _hashCache.Visited(resource.Hash);
            return false;
        }
    }
}