using System.Runtime.CompilerServices;

namespace ArcToon.Runtime.Utils.Extensions
{
    /// <summary>
    /// Bit-level packing utilities for storing integers in float channels.
    /// All methods produce values that can be unpacked in HLSL via asuint().
    /// Paired HLSL functions are in ShaderLibrary/BitPacking.hlsl.
    /// </summary>
    public static class BitPackingExtensions
    {
        // ========================================================
        //  Core: write / read arbitrary bit fields within a uint
        // ========================================================

        /// <summary>
        /// Writes <paramref name="value"/> into <paramref name="packed"/> at bit position
        /// [<paramref name="bitOffset"/> .. <paramref name="bitOffset"/>+<paramref name="bitCount"/>-1].
        /// Previous bits in that range are cleared first.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBits(ref uint packed, int value, int bitOffset, int bitCount)
        {
            uint mask = (1u << bitCount) - 1u;
            packed = (packed & ~(mask << bitOffset)) | (((uint)value & mask) << bitOffset);
        }

        /// <summary>
        /// Reads <paramref name="bitCount"/> bits from <paramref name="packed"/>
        /// starting at <paramref name="bitOffset"/>, returned as a signed int.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadBits(uint packed, int bitOffset, int bitCount)
        {
            uint mask = (1u << bitCount) - 1u;
            return (int)((packed >> bitOffset) & mask);
        }

        // ========================================================
        //  Convenience: Pack 2 × 16-bit into one float
        // ========================================================

        /// <summary>
        /// Packs two unsigned 16-bit integers (lo, hi) into a single float via bit reinterpretation.
        /// lo occupies bits [0..15], hi occupies bits [16..31].
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Pack2x16(int lo, int hi)
        {
            uint packed = ((uint)lo & 0xFFFFu) | (((uint)hi & 0xFFFFu) << 16);
            return ((int)packed).ReinterpretAsFloat();
        }

        // ========================================================
        //  Convenience: Pack 4 × 8-bit into one float
        // ========================================================

        /// <summary>
        /// Packs four unsigned 8-bit integers (b0, b1, b2, b3) into a single float.
        /// b0=[0..7], b1=[8..15], b2=[16..23], b3=[24..31].
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Pack4x8(int b0, int b1, int b2, int b3)
        {
            uint packed = ((uint)b0 & 0xFFu)
                        | (((uint)b1 & 0xFFu) << 8)
                        | (((uint)b2 & 0xFFu) << 16)
                        | (((uint)b3 & 0xFFu) << 24);
            return ((int)packed).ReinterpretAsFloat();
        }

        // ========================================================
        //  Convenience: Generic pack with arbitrary bit layout
        // ========================================================

        /// <summary>
        /// Packs multiple integer values into a single float using a fluent builder pattern.
        /// Usage: BitPacker.Begin().Write(value, bitCount).Write(value, bitCount).PackAsFloat();
        /// </summary>
        public struct BitPacker
        {
            private uint packed;
            private int cursor;

            public static BitPacker Begin()
            {
                return new BitPacker { packed = 0u, cursor = 0 };
            }

            /// <summary>
            /// Writes <paramref name="value"/> using the next <paramref name="bitCount"/> bits, then advances the cursor.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public BitPacker Write(int value, int bitCount)
            {
                WriteBits(ref packed, value, cursor, bitCount);
                cursor += bitCount;
                return this;
            }

            /// <summary>
            /// Returns the packed result as a float via bit reinterpretation.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float PackAsFloat()
            {
                return ((int)packed).ReinterpretAsFloat();
            }

            /// <summary>
            /// Returns the packed result as a raw uint.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint PackAsUInt()
            {
                return packed;
            }
        }
    }
}
