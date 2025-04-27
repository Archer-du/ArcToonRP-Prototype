using UnityEngine;
using UnityEngine.Rendering;

namespace ArcToon.Runtime.Utils
{
    public static class CommandBufferExtensions
    {
        public static Vector2 SetTileViewport(this CommandBuffer commandBuffer, int tileIndex, int split, float tileSize)
        {
            var offset = new Vector2(tileIndex % split, tileIndex / split);
            commandBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
            return offset;
        }

        public static void SetKeywords(this CommandBuffer commandBuffer, GlobalKeyword[] keywords, int enabledIndex)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                commandBuffer.SetKeyword(keywords[i], i == enabledIndex);
            }
        }
    }
}