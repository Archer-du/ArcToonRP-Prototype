using UnityEngine.Rendering.RenderGraphModule;

namespace ArcToon.Runtime.Data
{
    public readonly ref struct ShadowTextureData
    {
        public readonly TextureHandle directionalAtlas;
        public readonly TextureHandle spotAtlas;
        public readonly TextureHandle pointAtlas;

        public ShadowTextureData(
            TextureHandle directionalAtlas,
            TextureHandle spotAtlas, 
            TextureHandle pointAtlas)
        {
            this.directionalAtlas = directionalAtlas;
            this.spotAtlas = spotAtlas;
            this.pointAtlas = pointAtlas;
        }
    }
}