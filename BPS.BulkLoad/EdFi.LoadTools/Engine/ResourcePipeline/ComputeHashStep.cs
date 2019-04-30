namespace EdFi.LoadTools.Engine.ResourcePipeline
{
    /// <summary>
    /// Computing the hash of the element should be done first.
    /// Presumably we will get the elements in the same order because they come from automated systems.
    /// </summary>
    public class ComputeHashStep : IResourcePipelineStep
    {
        private readonly IResourceHashProvider _xmlResourceHashProvider;

        public ComputeHashStep(IResourceHashProvider xmlResourceHashProvider)
        {
            _xmlResourceHashProvider = xmlResourceHashProvider;
        }

        public bool Process(IResource resource)
        {
            var hash = _xmlResourceHashProvider.Hash(resource);
            resource.SetHash(hash);
            return true;
        }
    }
}