using System.Collections;

namespace System.Runtime.InteropServices
{
    internal interface UCOMIEnumerable
    {
        IEnumerator GetEnumerator();
    }
}