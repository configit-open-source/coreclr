namespace System.Runtime.InteropServices.ComTypes
{
    using System;

    internal interface IEnumerator
    {
        bool MoveNext();
        Object Current
        {
            get;
        }

        void Reset();
    }
}