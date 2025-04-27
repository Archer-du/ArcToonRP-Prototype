using System.Collections.Generic;
using ArcToon.Runtime.Behavior;

namespace ArcToon.Runtime.Passes.Lighting
{
    public class PerObjectShadowCasterManager
    {
        private static readonly HashSet<PerObjectShadowCaster> perObjectCasters = new();
        private static int perObjectShadowCasterGUID = 1;
        
        public static void Register(PerObjectShadowCaster caster)
        {
            if (perObjectCasters.Add(caster))
            {
                caster.perObjectShadowCasterID = perObjectShadowCasterGUID;
                perObjectShadowCasterGUID++;
            }
        }

        public static void Unregister(PerObjectShadowCaster caster) => perObjectCasters.Remove(caster);
    }
}