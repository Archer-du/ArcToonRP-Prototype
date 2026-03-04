#ifndef ARCTOON_FORWARD_PLUS_INCLUDED
#define ARCTOON_FORWARD_PLUS_INCLUDED

// xy: Screen UV to tile coordinates.
// z: Tiles per row, as integer.
// w: Tile data size, as integer.
float4 _ForwardPlusTileSettings;

StructuredBuffer<int> _ForwardPlusTileData;

struct ForwardPlusTile
{
    int2 coordinates;

    int index;

    int GetTileDataSize()
    {
        return asint(_ForwardPlusTileSettings.w);
    }

    int GetHeaderIndex()
    {
        return index * GetTileDataSize();
    }

    int GetTailIndex()
    {
        return index * GetTileDataSize() + GetTileDataSize() - 1;
    }

    int GetSpotLightCount()
    {
        return _ForwardPlusTileData[GetTailIndex()];
    }

    int GetPointLightCount()
    {
        return _ForwardPlusTileData[GetHeaderIndex()];
    }

    int GetLightCount()
    {
        return GetSpotLightCount() + GetPointLightCount();
    }

    int GetFirstLightIndexInTile()
    {
        return GetHeaderIndex() + 1;
    }

    int GetLightIndex(int lightIndexInTile)
    {
        return _ForwardPlusTileData[lightIndexInTile];
    }

    // debug
    bool IsMinimumEdgePixel(float2 screenUV)
    {
        float2 startUV = coordinates / _ForwardPlusTileSettings.xy;
        return any(screenUV - startUV < (1.0 / _ScreenParams.xy));
    }

    int GetMaxLightsPerTile()
    {
        return GetTileDataSize() - 1;
    }

    int2 GetScreenSize()
    {
        return int2(round(_CameraBufferSize.zw / _ForwardPlusTileSettings.xy));
    }
};

ForwardPlusTile GetForwardPlusTile(float2 screenUV)
{
    ForwardPlusTile tile;
    tile.coordinates = int2(screenUV * _ForwardPlusTileSettings.xy);
    tile.index = tile.coordinates.y * asint(_ForwardPlusTileSettings.z) +
        tile.coordinates.x;
    return tile;
}

#endif
