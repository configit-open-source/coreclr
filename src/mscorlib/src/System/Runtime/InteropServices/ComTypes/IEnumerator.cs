namespace System.Runtime.InteropServices.ComTypes
{
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