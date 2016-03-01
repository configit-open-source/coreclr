using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal interface IReference<T> : IPropertyValue
    {
        T Value
        {
            get;
        }
    }

    internal interface IReferenceArray<T> : IPropertyValue
    {
        T[] Value
        {
            get;
        }
    }
}