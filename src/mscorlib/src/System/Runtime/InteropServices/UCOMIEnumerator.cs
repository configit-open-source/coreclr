namespace System.Runtime.InteropServices
{
    using System;

    internal interface UCOMIEnumerator
    {
        bool MoveNext();
        Object Current
        {
            get;
        }

        void Reset();
    }
}