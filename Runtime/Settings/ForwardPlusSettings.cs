using UnityEngine;

namespace ArcToon.Runtime.Settings
{
    [System.Serializable]
    public class ForwardPlusSettings
    {
        public enum TileSize
        {
            Default, _16 = 16, _32 = 32, _64 = 64, _128 = 128, _256 = 256
        }

        [Tooltip("Tile size in pixels per dimension, default is 64.")]
        public TileSize tileSize = TileSize._64;
        
        [Range(0, 99)]
        [Tooltip("Maximum allowed lights per tile, 0 means default, which is 30.")]
        public int maxLightsPerTile = 14;
    }
}