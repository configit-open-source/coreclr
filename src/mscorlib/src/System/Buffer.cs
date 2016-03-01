using System.Diagnostics.Contracts;

namespace System
{
    public static class Buffer
    {
        public static extern void BlockCopy(Array src, int srcOffset, Array dst, int dstOffset, int count);
        internal static extern void InternalBlockCopy(Array src, int srcOffsetBytes, Array dst, int dstOffsetBytes, int byteCount);
        internal unsafe static int IndexOfByte(byte *src, byte value, int index, int count)
        {
            Contract.Assert(src != null, "src should not be null");
            byte *pByte = src + index;
            while (((int)pByte & 3) != 0)
            {
                if (count == 0)
                    return -1;
                else if (*pByte == value)
                    return (int)(pByte - src);
                count--;
                pByte++;
            }

            uint comparer = (((uint)value << 8) + (uint)value);
            comparer = (comparer << 16) + comparer;
            while (count > 3)
            {
                uint t1 = *(uint *)pByte;
                t1 = t1 ^ comparer;
                uint t2 = 0x7efefeff + t1;
                t1 = t1 ^ 0xffffffff;
                t1 = t1 ^ t2;
                t1 = t1 & 0x81010100;
                if (t1 != 0)
                {
                    int foundIndex = (int)(pByte - src);
                    if (pByte[0] == value)
                        return foundIndex;
                    else if (pByte[1] == value)
                        return foundIndex + 1;
                    else if (pByte[2] == value)
                        return foundIndex + 2;
                    else if (pByte[3] == value)
                        return foundIndex + 3;
                }

                count -= 4;
                pByte += 4;
            }

            while (count > 0)
            {
                if (*pByte == value)
                    return (int)(pByte - src);
                count--;
                pByte++;
            }

            return -1;
        }

        private static extern bool IsPrimitiveTypeArray(Array array);
        private static extern byte _GetByte(Array array, int index);
        public static byte GetByte(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (!IsPrimitiveTypeArray(array))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePrimArray"), "array");
            if (index < 0 || index >= _ByteLength(array))
                throw new ArgumentOutOfRangeException("index");
            return _GetByte(array, index);
        }

        private static extern void _SetByte(Array array, int index, byte value);
        public static void SetByte(Array array, int index, byte value)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (!IsPrimitiveTypeArray(array))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePrimArray"), "array");
            if (index < 0 || index >= _ByteLength(array))
                throw new ArgumentOutOfRangeException("index");
            _SetByte(array, index, value);
        }

        private static extern int _ByteLength(Array array);
        public static int ByteLength(Array array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (!IsPrimitiveTypeArray(array))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBePrimArray"), "array");
            return _ByteLength(array);
        }

        internal unsafe static void ZeroMemory(byte *src, long len)
        {
            while (len-- > 0)
                *(src + len) = 0;
        }

        internal unsafe static void Memcpy(byte[] dest, int destIndex, byte *src, int srcIndex, int len)
        {
            Contract.Assert((srcIndex >= 0) && (destIndex >= 0) && (len >= 0), "Index and length must be non-negative!");
            Contract.Assert(dest.Length - destIndex >= len, "not enough bytes in dest");
            if (len == 0)
                return;
            fixed (byte *pDest = dest)
            {
                Memcpy(pDest + destIndex, src + srcIndex, len);
            }
        }

        internal unsafe static void Memcpy(byte *pDest, int destIndex, byte[] src, int srcIndex, int len)
        {
            Contract.Assert((srcIndex >= 0) && (destIndex >= 0) && (len >= 0), "Index and length must be non-negative!");
            Contract.Assert(src.Length - srcIndex >= len, "not enough bytes in src");
            if (len == 0)
                return;
            fixed (byte *pSrc = src)
            {
                Memcpy(pDest + destIndex, pSrc + srcIndex, len);
            }
        }

        internal unsafe static void Memcpy(byte *dest, byte *src, int len)
        {
            Contract.Assert(len >= 0, "Negative length in memcopy!");
            Memmove(dest, src, (uint)len);
        }

        internal unsafe static void Memmove(byte *dest, byte *src, ulong len)
        {
            if ((ulong)dest - (ulong)src < len)
                goto PInvoke;
            switch (len)
            {
                case 0:
                    return;
                case 1:
                    *dest = *src;
                    return;
                case 2:
                    *(short *)dest = *(short *)src;
                    return;
                case 3:
                    *(short *)dest = *(short *)src;
                    *(dest + 2) = *(src + 2);
                    return;
                case 4:
                    *(int *)dest = *(int *)src;
                    return;
                case 5:
                    *(int *)dest = *(int *)src;
                    *(dest + 4) = *(src + 4);
                    return;
                case 6:
                    *(int *)dest = *(int *)src;
                    *(short *)(dest + 4) = *(short *)(src + 4);
                    return;
                case 7:
                    *(int *)dest = *(int *)src;
                    *(short *)(dest + 4) = *(short *)(src + 4);
                    *(dest + 6) = *(src + 6);
                    return;
                case 8:
                    *(long *)dest = *(long *)src;
                    return;
                case 9:
                    *(long *)dest = *(long *)src;
                    *(dest + 8) = *(src + 8);
                    return;
                case 10:
                    *(long *)dest = *(long *)src;
                    *(short *)(dest + 8) = *(short *)(src + 8);
                    return;
                case 11:
                    *(long *)dest = *(long *)src;
                    *(short *)(dest + 8) = *(short *)(src + 8);
                    *(dest + 10) = *(src + 10);
                    return;
                case 12:
                    *(long *)dest = *(long *)src;
                    *(int *)(dest + 8) = *(int *)(src + 8);
                    return;
                case 13:
                    *(long *)dest = *(long *)src;
                    *(int *)(dest + 8) = *(int *)(src + 8);
                    *(dest + 12) = *(src + 12);
                    return;
                case 14:
                    *(long *)dest = *(long *)src;
                    *(int *)(dest + 8) = *(int *)(src + 8);
                    *(short *)(dest + 12) = *(short *)(src + 12);
                    return;
                case 15:
                    *(long *)dest = *(long *)src;
                    *(int *)(dest + 8) = *(int *)(src + 8);
                    *(short *)(dest + 12) = *(short *)(src + 12);
                    *(dest + 14) = *(src + 14);
                    return;
                case 16:
                    *(long *)dest = *(long *)src;
                    *(long *)(dest + 8) = *(long *)(src + 8);
                    return;
                default:
                    break;
            }

            if (len >= 512)
                goto PInvoke;
            if (((int)dest & 3) != 0)
            {
                if (((int)dest & 1) != 0)
                {
                    *dest = *src;
                    src++;
                    dest++;
                    len--;
                    if (((int)dest & 2) == 0)
                        goto Aligned;
                }

                *(short *)dest = *(short *)src;
                src += 2;
                dest += 2;
                len -= 2;
                Aligned:
                    ;
            }

            if (((int)dest & 4) != 0)
            {
                *(int *)dest = *(int *)src;
                src += 4;
                dest += 4;
                len -= 4;
            }

            ulong count = len / 16;
            while (count > 0)
            {
                ((long *)dest)[0] = ((long *)src)[0];
                ((long *)dest)[1] = ((long *)src)[1];
                dest += 16;
                src += 16;
                count--;
            }

            if ((len & 8) != 0)
            {
                ((long *)dest)[0] = ((long *)src)[0];
                dest += 8;
                src += 8;
            }

            if ((len & 4) != 0)
            {
                ((int *)dest)[0] = ((int *)src)[0];
                dest += 4;
                src += 4;
            }

            if ((len & 2) != 0)
            {
                ((short *)dest)[0] = ((short *)src)[0];
                dest += 2;
                src += 2;
            }

            if ((len & 1) != 0)
                *dest = *src;
            return;
            PInvoke:
                _Memmove(dest, src, len);
        }

        private unsafe static void _Memmove(byte *dest, byte *src, ulong len)
        {
            __Memmove(dest, src, len);
        }

        extern private unsafe static void __Memmove(byte *dest, byte *src, ulong len);
        public static unsafe void MemoryCopy(void *source, void *destination, long destinationSizeInBytes, long sourceBytesToCopy)
        {
            if (sourceBytesToCopy > destinationSizeInBytes)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sourceBytesToCopy);
            }

            Memmove((byte *)destination, (byte *)source, checked ((ulong)sourceBytesToCopy));
        }

        public static unsafe void MemoryCopy(void *source, void *destination, ulong destinationSizeInBytes, ulong sourceBytesToCopy)
        {
            if (sourceBytesToCopy > destinationSizeInBytes)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sourceBytesToCopy);
            }

            Memmove((byte *)destination, (byte *)source, sourceBytesToCopy);
        }
    }
}