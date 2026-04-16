using ArcToon.Config;
using UnityEngine.Rendering;

namespace ArcToon.Passes.PostProcessing
{
    /// <summary>
    /// Base class for processors that are paired with a specific VolumeConfig type.
    /// The volumeConfig reference is injected at construction time by the owning VolumeConfig.
    /// </summary>
    public abstract class VolumePostProcessor<TVolumeConfig> : PostProcessor 
        where TVolumeConfig : PostProcessVolumeConfig
    {
        protected readonly TVolumeConfig volumeConfig;

        private ProfilingSampler sampler;
        public override ProfilingSampler Sampler => sampler ??= new ProfilingSampler(GetProfilerName());

        private static string GetProfilerName()
        {
            var name = typeof(TVolumeConfig).Name;
            if (name.EndsWith("VolumeConfig"))
                name = name[..^"VolumeConfig".Length];
            return name;
        }
        
        protected VolumePostProcessor(TVolumeConfig config)
        {
            volumeConfig = config;
        }
    }
}