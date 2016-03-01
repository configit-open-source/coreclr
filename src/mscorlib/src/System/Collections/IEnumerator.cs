namespace System.Collections
{
    using System;
    using System.Runtime.InteropServices;

    public interface IEnumerator
    {
        bool MoveNext();
        Object Current
        {
            get;
        }

        void Reset();
    }
}