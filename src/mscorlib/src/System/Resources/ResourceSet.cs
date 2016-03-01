using System.Collections;
using System.Diagnostics.Contracts;
using System.IO;

namespace System.Resources
{
    public class ResourceSet : IDisposable, IEnumerable
    {
        protected IResourceReader Reader;
        internal Hashtable Table;
        private Hashtable _caseInsensitiveTable;
        protected ResourceSet()
        {
            CommonInit();
        }

        internal ResourceSet(bool junk)
        {
        }

        public ResourceSet(String fileName)
        {
            Reader = new ResourceReader(fileName);
            CommonInit();
            ReadResources();
        }

        public ResourceSet(Stream stream)
        {
            Reader = new ResourceReader(stream);
            CommonInit();
            ReadResources();
        }

        public ResourceSet(IResourceReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");
            Contract.EndContractBlock();
            Reader = reader;
            CommonInit();
            ReadResources();
        }

        private void CommonInit()
        {
            Table = new Hashtable();
        }

        public virtual void Close()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                IResourceReader copyOfReader = Reader;
                Reader = null;
                if (copyOfReader != null)
                    copyOfReader.Close();
            }

            Reader = null;
            _caseInsensitiveTable = null;
            Table = null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual Type GetDefaultReader()
        {
            return typeof (ResourceReader);
        }

        public virtual IDictionaryEnumerator GetEnumerator()
        {
            return GetEnumeratorHelper();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorHelper();
        }

        private IDictionaryEnumerator GetEnumeratorHelper()
        {
            Hashtable copyOfTable = Table;
            if (copyOfTable == null)
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_ResourceSet"));
            return copyOfTable.GetEnumerator();
        }

        public virtual String GetString(String name)
        {
            Object obj = GetObjectInternal(name);
            try
            {
                return (String)obj;
            }
            catch (InvalidCastException)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResourceNotString_Name", name));
            }
        }

        public virtual String GetString(String name, bool ignoreCase)
        {
            Object obj;
            String s;
            obj = GetObjectInternal(name);
            try
            {
                s = (String)obj;
            }
            catch (InvalidCastException)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResourceNotString_Name", name));
            }

            if (s != null || !ignoreCase)
            {
                return s;
            }

            obj = GetCaseInsensitiveObjectInternal(name);
            try
            {
                return (String)obj;
            }
            catch (InvalidCastException)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResourceNotString_Name", name));
            }
        }

        public virtual Object GetObject(String name)
        {
            return GetObjectInternal(name);
        }

        public virtual Object GetObject(String name, bool ignoreCase)
        {
            Object obj = GetObjectInternal(name);
            if (obj != null || !ignoreCase)
                return obj;
            return GetCaseInsensitiveObjectInternal(name);
        }

        protected virtual void ReadResources()
        {
            IDictionaryEnumerator en = Reader.GetEnumerator();
            while (en.MoveNext())
            {
                Object value = en.Value;
                Table.Add(en.Key, value);
            }
        }

        private Object GetObjectInternal(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            Contract.EndContractBlock();
            Hashtable copyOfTable = Table;
            if (copyOfTable == null)
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_ResourceSet"));
            return copyOfTable[name];
        }

        private Object GetCaseInsensitiveObjectInternal(String name)
        {
            Hashtable copyOfTable = Table;
            if (copyOfTable == null)
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_ResourceSet"));
            Hashtable caseTable = _caseInsensitiveTable;
            if (caseTable == null)
            {
                caseTable = new Hashtable(StringComparer.OrdinalIgnoreCase);
                BCLDebug.Perf(false, "Using case-insensitive lookups is bad perf-wise.  Consider capitalizing " + name + " correctly in your source");
                IDictionaryEnumerator en = copyOfTable.GetEnumerator();
                while (en.MoveNext())
                {
                    caseTable.Add(en.Key, en.Value);
                }

                _caseInsensitiveTable = caseTable;
            }

            return caseTable[name];
        }
    }
}