using System;
using System.Diagnostics;

namespace System.Runtime.Versioning
{
    sealed class NonVersionableAttribute : Attribute
    {
        public NonVersionableAttribute()
        {
        }
    }
}