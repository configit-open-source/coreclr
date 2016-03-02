namespace System.Collections
{
    public interface IDictionaryEnumerator : IEnumerator
    {
        Object Key
        {
            get;
        }

        Object Value
        {
            get;
        }

        DictionaryEntry Entry
        {
            get;
        }
    }
}