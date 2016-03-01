namespace System.Collections.Generic
{
    using System;
    using System.Runtime.InteropServices;

    public interface IEnumerator<out T> : IDisposable, IEnumerator
    {
        new T Current
        {
            get;
        }
    }
}