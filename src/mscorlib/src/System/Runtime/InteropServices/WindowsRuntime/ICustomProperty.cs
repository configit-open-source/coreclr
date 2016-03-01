using System;
using System.Diagnostics.Contracts;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal interface ICustomProperty
    {
        Type Type
        {
            [Pure]
            get;
        }

        string Name
        {
            [Pure]
            get;
        }

        object GetValue(object target);
        void SetValue(object target, object value);
        object GetValue(object target, object indexValue);
        void SetValue(object target, object value, object indexValue);
        bool CanWrite
        {
            [Pure]
            get;
        }

        bool CanRead
        {
            [Pure]
            get;
        }
    }
}