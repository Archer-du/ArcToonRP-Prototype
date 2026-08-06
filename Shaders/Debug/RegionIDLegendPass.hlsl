#ifndef ARCTOON_REGION_ID_LEGEND_PASS_INCLUDED
#define ARCTOON_REGION_ID_LEGEND_PASS_INCLUDED

// Region ID legend fragment. Draws a compact swatch + index-number column for the
// region debug palette (_RegionDebugColors in RegionID.hlsl) at the bottom-left of
// the screen, so each index can be identified at a glance. Index digits use the
// SRP core debug font (5x9 per digit, spaced 1px). A semi-transparent backdrop
// strip is drawn first so the swatches stay readable over any scene content.
//
// Requires: Common.hlsl (DefaultPassVertex / Varyings_Default via the shader's
// HLSLINCLUDE), Debug.hlsl (SampleDebugFontNumberAllDigits), RegionID.hlsl
// (REGION_MAX_COUNT, GetRegionDebugColor).
//
// Layout (screen pixels, origin bottom-left):
//   x: [margin] [swatch] [gap] [digit]   |  y: swatch centered on each row, rows
//                                         |     stacked upward from the bottom margin.
// Rows are 1-based: index 0 is the bottom row, index N-1 the top row.

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"
#include "Packages/com.arctoon.render-pipeline/ShaderLibrary/RegionID.hlsl"

#define REGION_LEGEND_MARGIN 12.0
#define REGION_LEGEND_SWATCH_SIZE 14.0
#define REGION_LEGEND_DIGIT_WIDTH 6.0
#define REGION_LEGEND_DIGIT_HEIGHT 9.0
#define REGION_LEGEND_GAP 6.0

// Full backing strip rect (pixels, bottom-left origin) set by the C# pass; pixels
// inside it draw the semi-transparent backdrop, pixels outside pass through.
// Defaults to a zero-size rect so the backdrop is inert if the uniform is missing.
float4 _RegionLegendBackdrop = float4(0, 0, 0, 0);

float4 RegionIDLegendFragment(Varyings_Default input) : SV_TARGET
{
    // Convert to attachment pixels with a bottom-left origin. DefaultPassVertex
    // outputs screenUV with y=0 at the bottom (verified by ScreenDebug's use of
    // input.screenUV * _CameraBufferSize.zw as bottom-origin pixels), and
    // _CameraBufferSize.zw is the attachment size (SetupPass). No Y flip needed.
    float2 p = input.screenUV * _CameraBufferSize.zw;

    // Backdrop: semi-transparent black under the whole legend strip. Pixels
    // outside it are transparent and skip the content pass-through below.
    float3 baseColor = 0.0;
    float baseAlpha = 0.0;
    if (all(p >= _RegionLegendBackdrop.xy) && all(p < _RegionLegendBackdrop.xy + _RegionLegendBackdrop.zw))
    {
        baseColor = 0.0;
        baseAlpha = 0.6;
    }

    p -= REGION_LEGEND_MARGIN;

    // Region index this pixel row maps to (only rendered when within the row count).
    float row = floor(p.y / REGION_LEGEND_SWATCH_SIZE);
    int region = (int)row;
    if (region >= 0 && region < REGION_MAX_COUNT && p.x >= 0)
    {
        // cell is relative to the row origin: x within the content columns
        // (swatch | gap | digit), y within the row. p.x is already past the
        // left margin after the p -= REGION_LEGEND_MARGIN above.
        float2 cell = p - float2(0, row * REGION_LEGEND_SWATCH_SIZE);

        float3 swatchColor = GetRegionDebugColor(region);
        // Digits always white: the backdrop under the digit column is the dark
        // semi-transparent strip, so black digits would be invisible there.
        float3 textColor = 1.0;

        // Swatch column.
        if (cell.x < REGION_LEGEND_SWATCH_SIZE)
        {
            baseColor = swatchColor;
            baseAlpha = 0.95;
        }
        // Digit column: sample the region index with the SRP core debug font.
        // The digit column spans cell.x in [14, 20) (after swatch 14 + gap 6);
        // shift it to a 0-based [0, 6) so the font's 5px glyph + 1px spacing
        // aligns. SampleDebugFontNumberAllDigits internally shifts the digit
        // right by 6px (offset = int2(6 * digitCount, 0)) and down by 4px
        // (pixCoord.y -= 4), so add both shifts here to keep the digit inside
        // the column's visible band. The font expects bottom-up coordinates
        // matching cell.y (y = 0 is the row bottom), as OverlayHeatMap feeds it
        // bottom-origin pixels and renders upright.
        else
        {
            cell.x -= REGION_LEGEND_SWATCH_SIZE + REGION_LEGEND_GAP;
            if (cell.x < REGION_LEGEND_DIGIT_WIDTH && cell.y < REGION_LEGEND_DIGIT_HEIGHT)
            {
                int2 pixCoord = int2((int)cell.x + 6, (int)cell.y + 4);
                if (SampleDebugFontNumberAllDigits(pixCoord, (uint)region))
                {
                    baseColor = textColor;
                    baseAlpha = 0.95;
                }
            }
        }
    }

    return float4(baseColor, baseAlpha);
}

#endif
