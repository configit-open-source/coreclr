namespace System.Runtime.InteropServices
{
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