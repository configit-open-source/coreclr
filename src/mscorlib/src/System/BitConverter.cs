

namespace System
{
    public static class BitConverter
    {
        public static readonly bool IsLittleEndian = true;
        public static byte[] GetBytes(bool value)
        {
                                    byte[] r = new byte[1];
            r[0] = (value ? (byte)Boolean.True : (byte)Boolean.False);
            return r;
        }

        public static byte[] GetBytes(char value)
        {
                                    return GetBytes((short)value);
        }

        public unsafe static byte[] GetBytes(short value)
        {
                                    byte[] bytes = new byte[2];
            fixed (byte *b = bytes)
                *((short *)b) = value;
            return bytes;
        }

        public unsafe static byte[] GetBytes(int value)
        {
                                    byte[] bytes = new byte[4];
            fixed (byte *b = bytes)
                *((int *)b) = value;
            return bytes;
        }

        public unsafe static byte[] GetBytes(long value)
        {
                                    byte[] bytes = new byte[8];
            fixed (byte *b = bytes)
                *((long *)b) = value;
            return bytes;
        }

        public static byte[] GetBytes(ushort value)
        {
                                    return GetBytes((short)value);
        }

        public static byte[] GetBytes(uint value)
        {
                                    return GetBytes((int)value);
        }

        public static byte[] GetBytes(ulong value)
        {
                                    return GetBytes((long)value);
        }

        public unsafe static byte[] GetBytes(float value)
        {
                                    return GetBytes(*(int *)&value);
        }

        public unsafe static byte[] GetBytes(double value)
        {
                                    return GetBytes(*(long *)&value);
        }

        public static char ToChar(byte[] value, int startIndex)
        {
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            if ((uint)startIndex >= value.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            }

            if (startIndex > value.Length - 2)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

                        return (char)ToInt16(value, startIndex);
        }

        public static unsafe short ToInt16(byte[] value, int startIndex)
        {
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            if ((uint)startIndex >= value.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            }

            if (startIndex > value.Length - 2)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

                        fixed (byte *pbyte = &value[startIndex])
            {
                if (startIndex % 2 == 0)
                {
                    return *((short *)pbyte);
                }
                else
                {
                    if (IsLittleEndian)
                    {
                        return (short)((*pbyte) | (*(pbyte + 1) << 8));
                    }
                    else
                    {
                        return (short)((*pbyte << 8) | (*(pbyte + 1)));
                    }
                }
            }
        }

        public static unsafe int ToInt32(byte[] value, int startIndex)
        {
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            if ((uint)startIndex >= value.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            }

            if (startIndex > value.Length - 4)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

                        fixed (byte *pbyte = &value[startIndex])
            {
                if (startIndex % 4 == 0)
                {
                    return *((int *)pbyte);
                }
                else
                {
                    if (IsLittleEndian)
                    {
                        return (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                    }
                    else
                    {
                        return (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                    }
                }
            }
        }

        public static unsafe long ToInt64(byte[] value, int startIndex)
        {
            if (value == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            if ((uint)startIndex >= value.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            }

            if (startIndex > value.Length - 8)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

                        fixed (byte *pbyte = &value[startIndex])
            {
                if (startIndex % 8 == 0)
                {
                    return *((long *)pbyte);
                }
                else
                {
                    if (IsLittleEndian)
                    {
                        int i1 = (*pbyte) | (*(pbyte + 1) << 8) | (*(pbyte + 2) << 16) | (*(pbyte + 3) << 24);
                        int i2 = (*(pbyte + 4)) | (*(pbyte + 5) << 8) | (*(pbyte + 6) << 16) | (*(pbyte + 7) << 24);
                        return (uint)i1 | ((long)i2 << 32);
                    }
                    else
                    {
                        int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                        int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                        return (uint)i2 | ((long)i1 << 32);
                    }
                }
            }
        }

        public static ushort ToUInt16(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            if ((uint)startIndex >= value.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            if (startIndex > value.Length - 2)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                        return (ushort)ToInt16(value, startIndex);
        }

        public static uint ToUInt32(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            if ((uint)startIndex >= value.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            if (startIndex > value.Length - 4)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                        return (uint)ToInt32(value, startIndex);
        }

        public static ulong ToUInt64(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            if ((uint)startIndex >= value.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            if (startIndex > value.Length - 8)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                        return (ulong)ToInt64(value, startIndex);
        }

        unsafe public static float ToSingle(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            if ((uint)startIndex >= value.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            if (startIndex > value.Length - 4)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                        int val = ToInt32(value, startIndex);
            return *(float *)&val;
        }

        unsafe public static double ToDouble(byte[] value, int startIndex)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            if ((uint)startIndex >= value.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.startIndex, ExceptionResource.ArgumentOutOfRange_Index);
            if (startIndex > value.Length - 8)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
                        long val = ToInt64(value, startIndex);
            return *(double *)&val;
        }

        private static char GetHexValue(int i)
        {
                        if (i < 10)
            {
                return (char)(i + '0');
            }

            return (char)(i - 10 + 'A');
        }

        public static String ToString(byte[] value, int startIndex, int length)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (startIndex < 0 || startIndex >= value.Length && startIndex > 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_StartIndex"));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_GenericPositive"));
            }

            if (startIndex > value.Length - length)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_ArrayPlusOffTooSmall"));
            }

                        if (length == 0)
            {
                return string.Empty;
            }

            if (length > (Int32.MaxValue / 3))
            {
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_LengthTooLarge", (Int32.MaxValue / 3)));
            }

            int chArrayLength = length * 3;
            char[] chArray = new char[chArrayLength];
            int i = 0;
            int index = startIndex;
            for (i = 0; i < chArrayLength; i += 3)
            {
                byte b = value[index++];
                chArray[i] = GetHexValue(b / 16);
                chArray[i + 1] = GetHexValue(b % 16);
                chArray[i + 2] = '-';
            }

            return new String(chArray, 0, chArray.Length - 1);
        }

        public static String ToString(byte[] value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
                                    return ToString(value, 0, value.Length);
        }

        public static String ToString(byte[] value, int startIndex)
        {
            if (value == null)
                throw new ArgumentNullException("value");
                                    return ToString(value, startIndex, value.Length - startIndex);
        }

        public static bool ToBoolean(byte[] value, int startIndex)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (startIndex > value.Length - 1)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                        return (value[startIndex] == 0) ? false : true;
        }

        public static unsafe long DoubleToInt64Bits(double value)
        {
            return *((long *)&value);
        }

        public static unsafe double Int64BitsToDouble(long value)
        {
            return *((double *)&value);
        }
    }
}