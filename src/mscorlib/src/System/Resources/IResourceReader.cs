namespace System.Resources
{
    using System;
    using System.IO;
    using System.Collections;

    public interface IResourceReader : IEnumerable, IDisposable
    {
        void Close();
        new IDictionaryEnumerator GetEnumerator();
    }
}