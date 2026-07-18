using UnityEditor;

namespace ArcToon.Editor.ShaderEditor
{
    public static class RegionPropertyHelper
    {
        public const int MaxRegionCount = 8;

        public static class BaseName
        {
            public const string OutlineColor = "_OutlineColor";
        }

        public static readonly string[] MigratedProperties =
        {
            BaseName.OutlineColor,
        };

    }
}
