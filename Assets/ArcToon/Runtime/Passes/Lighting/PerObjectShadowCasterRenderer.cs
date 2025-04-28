using System.Collections.Generic;
using UnityEngine;

namespace ArcToon.Runtime.Passes.Lighting
{
    public struct DrawCallData
    {
        public Material material;
        public int subMeshIndex;
        public int passIndex;

        public DrawCallData(Material material, int subMeshIndex, int passIndex)
        {
            this.material = material;
            this.subMeshIndex = subMeshIndex;
            this.passIndex = passIndex;
        }
    }
    public struct PerObjectShadowCasterRenderer
    {
        public Renderer renderer;
        public List<DrawCallData> drawCallList;
        public PerObjectShadowCasterRenderer(Renderer renderer, List<DrawCallData> drawCallList)
        {
            this.renderer = renderer;
            this.drawCallList = drawCallList;
        }
    }

}