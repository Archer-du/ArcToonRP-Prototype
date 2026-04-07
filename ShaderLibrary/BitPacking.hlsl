#ifndef ARCTOON_BIT_PACKING_INCLUDED
#define ARCTOON_BIT_PACKING_INCLUDED

// =============================================
//  Core: read / write arbitrary bit fields
// =============================================

/// Reads 'bitCount' bits from 'packed' starting at 'bitOffset'.
uint ReadBits(uint packed, int bitOffset, int bitCount)
{
    uint mask = (1u << bitCount) - 1u;
    return (packed >> bitOffset) & mask;
}

/// Reads 'bitCount' bits from a float (via asuint) starting at 'bitOffset'.
uint ReadBitsFromFloat(float packedFloat, int bitOffset, int bitCount)
{
    return ReadBits(asuint(packedFloat), bitOffset, bitCount);
}

// =============================================
//  Convenience: Unpack 2 × 16-bit from float
// =============================================

/// Unpacks two unsigned 16-bit integers from a packed float.
/// lo = bits [0..15], hi = bits [16..31].
void Unpack2x16(float packedFloat, out uint lo, out uint hi)
{
    uint packed = asuint(packedFloat);
    lo = packed & 0xFFFFu;
    hi = packed >> 16u;
}

/// Unpacks two signed integers from a packed float (2×16 layout).
/// lo = bits [0..15], hi = bits [16..31] (as int).
void Unpack2x16(float packedFloat, out int lo, out int hi)
{
    uint packed = asuint(packedFloat);
    lo = (int)(packed & 0xFFFFu);
    hi = (int)(packed >> 16u);
}

// =============================================
//  Convenience: Unpack 4 × 8-bit from float
// =============================================

/// Unpacks four unsigned 8-bit integers from a packed float.
/// b0=[0..7], b1=[8..15], b2=[16..23], b3=[24..31].
void Unpack4x8(float packedFloat, out uint b0, out uint b1, out uint b2, out uint b3)
{
    uint packed = asuint(packedFloat);
    b0 = packed & 0xFFu;
    b1 = (packed >> 8u) & 0xFFu;
    b2 = (packed >> 16u) & 0xFFu;
    b3 = packed >> 24u;
}

#endif
