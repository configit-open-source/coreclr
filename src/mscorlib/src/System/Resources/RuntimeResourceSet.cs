using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

namespace System.Resources
{
    internal sealed class RuntimeResourceSet : ResourceSet, IEnumerable
    {
        internal const int Version = 2;
        private Dictionary<String, ResourceLocator> _resCache;
        private ResourceReader _defaultReader;
        private Dictionary<String, ResourceLocator> _caseInsensitiveTable;
        private bool _haveReadFromReader;
        internal RuntimeResourceSet(String fileName): base (false)
        {
            BCLDebug.Log("RESMGRFILEFORMAT", "RuntimeResourceSet .ctor(String)");
            _resCache = new Dictionary<String, ResourceLocator>(FastResourceComparer.Default);
            Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            _defaultReader = new ResourceReader(stream, _resCache);
            Reader = _defaultReader;
        }

        internal RuntimeResourceSet(Stream stream): base (false)
        {
            BCLDebug.Log("RESMGRFILEFORMAT", "RuntimeResourceSet .ctor(Stream)");
            _resCache = new Dictionary<String, ResourceLocator>(FastResourceComparer.Default);
            _defaultReader = new ResourceReader(stream, _resCache);
            Reader = _defaultReader;
        }

        protected override void Dispose(bool disposing)
        {
            if (Reader == null)
                return;
            if (disposing)
            {
                lock (Reader)
                {
                    _resCache = null;
                    if (_defaultReader != null)
                    {
                        _defaultReader.Close();
                        _defaultReader = null;
                    }

                    _caseInsensitiveTable = null;
                    base.Dispose(disposing);
                }
            }
            else
            {
                _resCache = null;
                _caseInsensitiveTable = null;
                _defaultReader = null;
                base.Dispose(disposing);
            }
        }

        public override IDictionaryEnumerator GetEnumerator()
        {
            return GetEnumeratorHelper();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorHelper();
        }

        private IDictionaryEnumerator GetEnumeratorHelper()
        {
            IResourceReader copyOfReader = Reader;
            if (copyOfReader == null || _resCache == null)
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_ResourceSet"));
            return copyOfReader.GetEnumerator();
        }

        public override String GetString(String key)
        {
            Object o = GetObject(key, false, true);
            return (String)o;
        }

        public override String GetString(String key, bool ignoreCase)
        {
            Object o = GetObject(key, ignoreCase, true);
            return (String)o;
        }

        public override Object GetObject(String key)
        {
            return GetObject(key, false, false);
        }

        public override Object GetObject(String key, bool ignoreCase)
        {
            return GetObject(key, ignoreCase, false);
        }

        private Object GetObject(String key, bool ignoreCase, bool isString)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            if (Reader == null || _resCache == null)
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_ResourceSet"));
            Contract.EndContractBlock();
            Object value = null;
            ResourceLocator resLocation;
            lock (Reader)
            {
                if (Reader == null)
                    throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_ResourceSet"));
                if (_defaultReader != null)
                {
                    BCLDebug.Log("RESMGRFILEFORMAT", "Going down fast path in RuntimeResourceSet::GetObject");
                    int dataPos = -1;
                    if (_resCache.TryGetValue(key, out resLocation))
                    {
                        value = resLocation.Value;
                        dataPos = resLocation.DataPosition;
                    }

                    if (dataPos == -1 && value == null)
                    {
                        dataPos = _defaultReader.FindPosForResource(key);
                    }

                    if (dataPos != -1 && value == null)
                    {
                        Contract.Assert(dataPos >= 0, "data section offset cannot be negative!");
                        ResourceTypeCode typeCode;
                        if (isString)
                        {
                            value = _defaultReader.LoadString(dataPos);
                            typeCode = ResourceTypeCode.String;
                        }
                        else
                        {
                            value = _defaultReader.LoadObject(dataPos, out typeCode);
                        }

                        resLocation = new ResourceLocator(dataPos, (ResourceLocator.CanCache(typeCode)) ? value : null);
                        lock (_resCache)
                        {
                            _resCache[key] = resLocation;
                        }
                    }

                    if (value != null || !ignoreCase)
                    {
                        return value;
                    }
                }

                if (!_haveReadFromReader)
                {
                    if (ignoreCase && _caseInsensitiveTable == null)
                    {
                        _caseInsensitiveTable = new Dictionary<String, ResourceLocator>(StringComparer.OrdinalIgnoreCase);
                    }

                    BCLDebug.Perf(!ignoreCase, "Using case-insensitive lookups is bad perf-wise.  Consider capitalizing " + key + " correctly in your source");
                    if (_defaultReader == null)
                    {
                        IDictionaryEnumerator en = Reader.GetEnumerator();
                        while (en.MoveNext())
                        {
                            DictionaryEntry entry = en.Entry;
                            String readKey = (String)entry.Key;
                            ResourceLocator resLoc = new ResourceLocator(-1, entry.Value);
                            _resCache.Add(readKey, resLoc);
                            if (ignoreCase)
                                _caseInsensitiveTable.Add(readKey, resLoc);
                        }

                        if (!ignoreCase)
                            Reader.Close();
                    }
                    else
                    {
                        Contract.Assert(ignoreCase, "This should only happen for case-insensitive lookups");
                        ResourceReader.ResourceEnumerator en = _defaultReader.GetEnumeratorInternal();
                        while (en.MoveNext())
                        {
                            String currentKey = (String)en.Key;
                            int dataPos = en.DataPosition;
                            ResourceLocator resLoc = new ResourceLocator(dataPos, null);
                            _caseInsensitiveTable.Add(currentKey, resLoc);
                        }
                    }

                    _haveReadFromReader = true;
                }

                Object obj = null;
                bool found = false;
                bool keyInWrongCase = false;
                if (_defaultReader != null)
                {
                    if (_resCache.TryGetValue(key, out resLocation))
                    {
                        found = true;
                        obj = ResolveResourceLocator(resLocation, key, _resCache, keyInWrongCase);
                    }
                }

                if (!found && ignoreCase)
                {
                    if (_caseInsensitiveTable.TryGetValue(key, out resLocation))
                    {
                        found = true;
                        keyInWrongCase = true;
                        obj = ResolveResourceLocator(resLocation, key, _resCache, keyInWrongCase);
                    }
                }

                return obj;
            }
        }

        private Object ResolveResourceLocator(ResourceLocator resLocation, String key, Dictionary<String, ResourceLocator> copyOfCache, bool keyInWrongCase)
        {
            Object value = resLocation.Value;
            if (value == null)
            {
                ResourceTypeCode typeCode;
                lock (Reader)
                {
                    value = _defaultReader.LoadObject(resLocation.DataPosition, out typeCode);
                }

                if (!keyInWrongCase && ResourceLocator.CanCache(typeCode))
                {
                    resLocation.Value = value;
                    copyOfCache[key] = resLocation;
                }
            }

            return value;
        }
    }
}