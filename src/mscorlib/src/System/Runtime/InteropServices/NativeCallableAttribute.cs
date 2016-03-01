using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public sealed class NativeCallableAttribute : Attribute
    {
        public NativeCallableAttribute()
        {
        }

        public CallingConvention CallingConvention;
        public string EntryPoint;
    }
}