namespace System.Runtime.InteropServices
{
    using System;
    using System.Collections;

    internal interface UCOMIEnumerable
    {
        IEnumerator GetEnumerator();
    }
}