namespace System.Collections
{
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