using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Runtime.ConstrainedExecution;
using System.Runtime.CompilerServices;
using System.Runtime;
using System.Security;

namespace System.Threading
{
    public static class Volatile
    {
        public static bool Read(ref bool location)
        {
            var value = location;
            Thread.MemoryBarrier();
            return value;
        }

        public static sbyte Read(ref sbyte location)
        {
            var value = location;
            Thread.MemoryBarrier();
            return value;
        }

        public static byte Read(ref byte location)
        {
            var value = location;
            Thread.MemoryBarrier();
            return value;
        }

        public static short Read(ref short location)
        {
            var value = location;
            Thread.MemoryBarrier();
            return value;
        }

        public static ushort Read(ref ushort location)
        {
            var value = location;
            Thread.MemoryBarrier();
            return value;
        }

        public static int Read(ref int location)
        {
            var value = location;
            Thread.MemoryBarrier();
            return value;
        }

        public static uint Read(ref uint location)
        {
            var value = location;
            Thread.MemoryBarrier();
            return value;
        }

        public static long Read(ref long location)
        {
            return Interlocked.CompareExchange(ref location, 0, 0);
        }

        public static ulong Read(ref ulong location)
        {
            unsafe
            {
                fixed (ulong *pLocation = &location)
                {
                    return (ulong)Interlocked.CompareExchange(ref *(long *)pLocation, 0, 0);
                }
            }
        }

        public static IntPtr Read(ref IntPtr location)
        {
            var value = location;
            Thread.MemoryBarrier();
            return value;
        }

        public static UIntPtr Read(ref UIntPtr location)
        {
            var value = location;
            Thread.MemoryBarrier();
            return value;
        }

        public static float Read(ref float location)
        {
            var value = location;
            Thread.MemoryBarrier();
            return value;
        }

        public static double Read(ref double location)
        {
            return Interlocked.CompareExchange(ref location, 0, 0);
        }

        public static T Read<T>(ref T location)where T : class
        {
            var value = location;
            Thread.MemoryBarrier();
            return value;
        }

        public static void Write(ref bool location, bool value)
        {
            Thread.MemoryBarrier();
            location = value;
        }

        public static void Write(ref sbyte location, sbyte value)
        {
            Thread.MemoryBarrier();
            location = value;
        }

        public static void Write(ref byte location, byte value)
        {
            Thread.MemoryBarrier();
            location = value;
        }

        public static void Write(ref short location, short value)
        {
            Thread.MemoryBarrier();
            location = value;
        }

        public static void Write(ref ushort location, ushort value)
        {
            Thread.MemoryBarrier();
            location = value;
        }

        public static void Write(ref int location, int value)
        {
            Thread.MemoryBarrier();
            location = value;
        }

        public static void Write(ref uint location, uint value)
        {
            Thread.MemoryBarrier();
            location = value;
        }

        public static void Write(ref long location, long value)
        {
            Interlocked.Exchange(ref location, value);
        }

        public static void Write(ref ulong location, ulong value)
        {
            unsafe
            {
                fixed (ulong *pLocation = &location)
                {
                    Interlocked.Exchange(ref *(long *)pLocation, (long)value);
                }
            }
        }

        public static void Write(ref IntPtr location, IntPtr value)
        {
            Thread.MemoryBarrier();
            location = value;
        }

        public static void Write(ref UIntPtr location, UIntPtr value)
        {
            Thread.MemoryBarrier();
            location = value;
        }

        public static void Write(ref float location, float value)
        {
            Thread.MemoryBarrier();
            location = value;
        }

        public static void Write(ref double location, double value)
        {
            Interlocked.Exchange(ref location, value);
        }

        public static void Write<T>(ref T location, T value)where T : class
        {
            Thread.MemoryBarrier();
            location = value;
        }
    }
}