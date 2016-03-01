namespace System.Resources
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Security;
    using System.Globalization;
    using System.Configuration.Assemblies;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    internal struct ResourceLocator
    {
        internal Object _value;
        internal int _dataPos;
        internal ResourceLocator(int dataPos, Object value)
        {
            _dataPos = dataPos;
            _value = value;
        }

        internal int DataPosition
        {
            get
            {
                return _dataPos;
            }
        }

        internal Object Value
        {
            get
            {
                return _value;
            }

            set
            {
                _value = value;
            }
        }

        internal static bool CanCache(ResourceTypeCode value)
        {
            Contract.Assert(value >= 0, "negative ResourceTypeCode.  What?");
            return value <= ResourceTypeCode.LastPrimitive;
        }
    }

    public sealed class ResourceReader : IResourceReader
    {
        private const int DefaultFileStreamBufferSize = 4096;
        private BinaryReader _store;
        internal Dictionary<String, ResourceLocator> _resCache;
        private long _nameSectionOffset;
        private long _dataSectionOffset;
        private int[] _nameHashes;
        private unsafe int *_nameHashesPtr;
        private int[] _namePositions;
        private unsafe int *_namePositionsPtr;
        private RuntimeType[] _typeTable;
        private int[] _typeNamePositions;
        private int _numResources;
        private UnmanagedMemoryStream _ums;
        private int _version;
        public ResourceReader(String fileName)
        {
            _resCache = new Dictionary<String, ResourceLocator>(FastResourceComparer.Default);
            _store = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultFileStreamBufferSize, FileOptions.RandomAccess, Path.GetFileName(fileName), false), Encoding.UTF8);
            BCLDebug.Log("RESMGRFILEFORMAT", "ResourceReader .ctor(String).  UnmanagedMemoryStream: " + (_ums != null));
            try
            {
                ReadResources();
            }
            catch
            {
                _store.Close();
                throw;
            }
        }

        public ResourceReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanRead)
                throw new ArgumentException(Environment.GetResourceString("Argument_StreamNotReadable"));
            Contract.EndContractBlock();
            _resCache = new Dictionary<String, ResourceLocator>(FastResourceComparer.Default);
            _store = new BinaryReader(stream, Encoding.UTF8);
            _ums = stream as UnmanagedMemoryStream;
            BCLDebug.Log("RESMGRFILEFORMAT", "ResourceReader .ctor(Stream).  UnmanagedMemoryStream: " + (_ums != null));
            ReadResources();
        }

        internal ResourceReader(Stream stream, Dictionary<String, ResourceLocator> resCache)
        {
            Contract.Requires(stream != null, "Need a stream!");
            Contract.Requires(stream.CanRead, "Stream should be readable!");
            Contract.Requires(resCache != null, "Need a Dictionary!");
            _resCache = resCache;
            _store = new BinaryReader(stream, Encoding.UTF8);
            _ums = stream as UnmanagedMemoryStream;
            BCLDebug.Log("RESMGRFILEFORMAT", "ResourceReader .ctor(Stream, Hashtable).  UnmanagedMemoryStream: " + (_ums != null));
            ReadResources();
        }

        public void Close()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Close();
        }

        private unsafe void Dispose(bool disposing)
        {
            if (_store != null)
            {
                _resCache = null;
                if (disposing)
                {
                    BinaryReader copyOfStore = _store;
                    _store = null;
                    if (copyOfStore != null)
                        copyOfStore.Close();
                }

                _store = null;
                _namePositions = null;
                _nameHashes = null;
                _ums = null;
                _namePositionsPtr = null;
                _nameHashesPtr = null;
            }
        }

        internal static unsafe int ReadUnalignedI4(int *p)
        {
            byte *buffer = (byte *)p;
            return buffer[0] | (buffer[1] << 8) | (buffer[2] << 16) | (buffer[3] << 24);
        }

        private void SkipInt32()
        {
            _store.BaseStream.Seek(4, SeekOrigin.Current);
        }

        private void SkipString()
        {
            int stringLength = _store.Read7BitEncodedInt();
            if (stringLength < 0)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_NegativeStringLength"));
            }

            _store.BaseStream.Seek(stringLength, SeekOrigin.Current);
        }

        private unsafe int GetNameHash(int index)
        {
            Contract.Assert(index >= 0 && index < _numResources, "Bad index into hash array.  index: " + index);
            Contract.Assert((_ums == null && _nameHashes != null && _nameHashesPtr == null) || (_ums != null && _nameHashes == null && _nameHashesPtr != null), "Internal state mangled.");
            if (_ums == null)
                return _nameHashes[index];
            else
                return ReadUnalignedI4(&_nameHashesPtr[index]);
        }

        private unsafe int GetNamePosition(int index)
        {
            Contract.Assert(index >= 0 && index < _numResources, "Bad index into name position array.  index: " + index);
            Contract.Assert((_ums == null && _namePositions != null && _namePositionsPtr == null) || (_ums != null && _namePositions == null && _namePositionsPtr != null), "Internal state mangled.");
            int r;
            if (_ums == null)
                r = _namePositions[index];
            else
                r = ReadUnalignedI4(&_namePositionsPtr[index]);
            if (r < 0 || r > _dataSectionOffset - _nameSectionOffset)
            {
                throw new FormatException(Environment.GetResourceString("BadImageFormat_ResourcesNameInvalidOffset", r));
            }

            return r;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            if (_resCache == null)
                throw new InvalidOperationException(Environment.GetResourceString("ResourceReaderIsClosed"));
            return new ResourceEnumerator(this);
        }

        internal ResourceEnumerator GetEnumeratorInternal()
        {
            return new ResourceEnumerator(this);
        }

        internal int FindPosForResource(String name)
        {
            Contract.Assert(_store != null, "ResourceReader is closed!");
            int hash = FastResourceComparer.HashFunction(name);
            BCLDebug.Log("RESMGRFILEFORMAT", "FindPosForResource for " + name + "  hash: " + hash.ToString("x", CultureInfo.InvariantCulture));
            int lo = 0;
            int hi = _numResources - 1;
            int index = -1;
            bool success = false;
            while (lo <= hi)
            {
                index = (lo + hi) >> 1;
                int currentHash = GetNameHash(index);
                int c;
                if (currentHash == hash)
                    c = 0;
                else if (currentHash < hash)
                    c = -1;
                else
                    c = 1;
                if (c == 0)
                {
                    success = true;
                    break;
                }

                if (c < 0)
                    lo = index + 1;
                else
                    hi = index - 1;
            }

            if (!success)
            {
                return -1;
            }

            if (lo != index)
            {
                lo = index;
                while (lo > 0 && GetNameHash(lo - 1) == hash)
                    lo--;
            }

            if (hi != index)
            {
                hi = index;
                while (hi < _numResources - 1 && GetNameHash(hi + 1) == hash)
                    hi++;
            }

            lock (this)
            {
                for (int i = lo; i <= hi; i++)
                {
                    _store.BaseStream.Seek(_nameSectionOffset + GetNamePosition(i), SeekOrigin.Begin);
                    if (CompareStringEqualsName(name))
                    {
                        int dataPos = _store.ReadInt32();
                        if (dataPos < 0 || dataPos >= _store.BaseStream.Length - _dataSectionOffset)
                        {
                            throw new FormatException(Environment.GetResourceString("BadImageFormat_ResourcesDataInvalidOffset", dataPos));
                        }

                        return dataPos;
                    }
                }
            }

            BCLDebug.Log("RESMGRFILEFORMAT", "FindPosForResource for " + name + ": Found a hash collision, HOWEVER, neither of these collided values equaled the given string.");
            return -1;
        }

        private unsafe bool CompareStringEqualsName(String name)
        {
            Contract.Assert(_store != null, "ResourceReader is closed!");
            int byteLen = _store.Read7BitEncodedInt();
            if (byteLen < 0)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_NegativeStringLength"));
            }

            if (_ums != null)
            {
                byte *bytes = _ums.PositionPointer;
                _ums.Seek(byteLen, SeekOrigin.Current);
                if (_ums.Position > _ums.Length)
                {
                    throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourcesNameTooLong"));
                }

                return FastResourceComparer.CompareOrdinal(bytes, byteLen, name) == 0;
            }
            else
            {
                byte[] bytes = new byte[byteLen];
                int numBytesToRead = byteLen;
                while (numBytesToRead > 0)
                {
                    int n = _store.Read(bytes, byteLen - numBytesToRead, numBytesToRead);
                    if (n == 0)
                        throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourceNameCorrupted"));
                    numBytesToRead -= n;
                }

                return FastResourceComparer.CompareOrdinal(bytes, byteLen / 2, name) == 0;
            }
        }

        private unsafe String AllocateStringForNameIndex(int index, out int dataOffset)
        {
            Contract.Assert(_store != null, "ResourceReader is closed!");
            byte[] bytes;
            int byteLen;
            long nameVA = GetNamePosition(index);
            lock (this)
            {
                _store.BaseStream.Seek(nameVA + _nameSectionOffset, SeekOrigin.Begin);
                byteLen = _store.Read7BitEncodedInt();
                if (byteLen < 0)
                {
                    throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_NegativeStringLength"));
                }

                if (_ums != null)
                {
                    if (_ums.Position > _ums.Length - byteLen)
                        throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourcesIndexTooLong", index));
                    String s = null;
                    char *charPtr = (char *)_ums.PositionPointer;
                    s = new String(charPtr, 0, byteLen / 2);
                    _ums.Position += byteLen;
                    dataOffset = _store.ReadInt32();
                    if (dataOffset < 0 || dataOffset >= _store.BaseStream.Length - _dataSectionOffset)
                    {
                        throw new FormatException(Environment.GetResourceString("BadImageFormat_ResourcesDataInvalidOffset", dataOffset));
                    }

                    return s;
                }

                bytes = new byte[byteLen];
                int count = byteLen;
                while (count > 0)
                {
                    int n = _store.Read(bytes, byteLen - count, count);
                    if (n == 0)
                        throw new EndOfStreamException(Environment.GetResourceString("BadImageFormat_ResourceNameCorrupted_NameIndex", index));
                    count -= n;
                }

                dataOffset = _store.ReadInt32();
                if (dataOffset < 0 || dataOffset >= _store.BaseStream.Length - _dataSectionOffset)
                {
                    throw new FormatException(Environment.GetResourceString("BadImageFormat_ResourcesDataInvalidOffset", dataOffset));
                }
            }

            return Encoding.Unicode.GetString(bytes, 0, byteLen);
        }

        private Object GetValueForNameIndex(int index)
        {
            Contract.Assert(_store != null, "ResourceReader is closed!");
            long nameVA = GetNamePosition(index);
            lock (this)
            {
                _store.BaseStream.Seek(nameVA + _nameSectionOffset, SeekOrigin.Begin);
                SkipString();
                int dataPos = _store.ReadInt32();
                if (dataPos < 0 || dataPos >= _store.BaseStream.Length - _dataSectionOffset)
                {
                    throw new FormatException(Environment.GetResourceString("BadImageFormat_ResourcesDataInvalidOffset", dataPos));
                }

                BCLDebug.Log("RESMGRFILEFORMAT", "GetValueForNameIndex: dataPos: " + dataPos);
                ResourceTypeCode junk;
                if (_version == 1)
                    return LoadObjectV1(dataPos);
                else
                    return LoadObjectV2(dataPos, out junk);
            }
        }

        internal String LoadString(int pos)
        {
            Contract.Assert(_store != null, "ResourceReader is closed!");
            _store.BaseStream.Seek(_dataSectionOffset + pos, SeekOrigin.Begin);
            String s = null;
            int typeIndex = _store.Read7BitEncodedInt();
            if (_version == 1)
            {
                if (typeIndex == -1)
                    return null;
                if (FindType(typeIndex) != typeof (String))
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResourceNotString_Type", FindType(typeIndex).FullName));
                s = _store.ReadString();
            }
            else
            {
                ResourceTypeCode typeCode = (ResourceTypeCode)typeIndex;
                if (typeCode != ResourceTypeCode.String && typeCode != ResourceTypeCode.Null)
                {
                    String typeString;
                    if (typeCode < ResourceTypeCode.StartOfUserTypes)
                        typeString = typeCode.ToString();
                    else
                        typeString = FindType(typeCode - ResourceTypeCode.StartOfUserTypes).FullName;
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ResourceNotString_Type", typeString));
                }

                if (typeCode == ResourceTypeCode.String)
                    s = _store.ReadString();
            }

            BCLDebug.Log("RESMGRFILEFORMAT", "LoadString(" + pos.ToString("x", CultureInfo.InvariantCulture) + " returned " + (s == null ? "[a null string]" : s));
            return s;
        }

        internal Object LoadObject(int pos)
        {
            if (_version == 1)
                return LoadObjectV1(pos);
            ResourceTypeCode typeCode;
            return LoadObjectV2(pos, out typeCode);
        }

        internal Object LoadObject(int pos, out ResourceTypeCode typeCode)
        {
            if (_version == 1)
            {
                Object o = LoadObjectV1(pos);
                typeCode = (o is String) ? ResourceTypeCode.String : ResourceTypeCode.StartOfUserTypes;
                return o;
            }

            return LoadObjectV2(pos, out typeCode);
        }

        internal Object LoadObjectV1(int pos)
        {
            Contract.Assert(_store != null, "ResourceReader is closed!");
            Contract.Assert(_version == 1, ".resources file was not a V1 .resources file!");
            try
            {
                return _LoadObjectV1(pos);
            }
            catch (EndOfStreamException eof)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_TypeMismatch"), eof);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_TypeMismatch"), e);
            }
        }

        private Object _LoadObjectV1(int pos)
        {
            _store.BaseStream.Seek(_dataSectionOffset + pos, SeekOrigin.Begin);
            int typeIndex = _store.Read7BitEncodedInt();
            if (typeIndex == -1)
                return null;
            RuntimeType type = FindType(typeIndex);
            BCLDebug.Log("RESMGRFILEFORMAT", "LoadObject type: " + type.Name + "  pos: 0x" + _store.BaseStream.Position.ToString("x", CultureInfo.InvariantCulture));
            if (type == typeof (String))
                return _store.ReadString();
            else if (type == typeof (Int32))
                return _store.ReadInt32();
            else if (type == typeof (Byte))
                return _store.ReadByte();
            else if (type == typeof (SByte))
                return _store.ReadSByte();
            else if (type == typeof (Int16))
                return _store.ReadInt16();
            else if (type == typeof (Int64))
                return _store.ReadInt64();
            else if (type == typeof (UInt16))
                return _store.ReadUInt16();
            else if (type == typeof (UInt32))
                return _store.ReadUInt32();
            else if (type == typeof (UInt64))
                return _store.ReadUInt64();
            else if (type == typeof (Single))
                return _store.ReadSingle();
            else if (type == typeof (Double))
                return _store.ReadDouble();
            else if (type == typeof (DateTime))
            {
                return new DateTime(_store.ReadInt64());
            }
            else if (type == typeof (TimeSpan))
                return new TimeSpan(_store.ReadInt64());
            else if (type == typeof (Decimal))
            {
                int[] bits = new int[4];
                for (int i = 0; i < bits.Length; i++)
                    bits[i] = _store.ReadInt32();
                return new Decimal(bits);
            }
            else
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ResourceObjectSerialization"));
            }
        }

        internal Object LoadObjectV2(int pos, out ResourceTypeCode typeCode)
        {
            Contract.Assert(_store != null, "ResourceReader is closed!");
            Contract.Assert(_version >= 2, ".resources file was not a V2 (or higher) .resources file!");
            try
            {
                return _LoadObjectV2(pos, out typeCode);
            }
            catch (EndOfStreamException eof)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_TypeMismatch"), eof);
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_TypeMismatch"), e);
            }
        }

        private Object _LoadObjectV2(int pos, out ResourceTypeCode typeCode)
        {
            _store.BaseStream.Seek(_dataSectionOffset + pos, SeekOrigin.Begin);
            typeCode = (ResourceTypeCode)_store.Read7BitEncodedInt();
            BCLDebug.Log("RESMGRFILEFORMAT", "LoadObjectV2 type: " + typeCode + "  pos: 0x" + _store.BaseStream.Position.ToString("x", CultureInfo.InvariantCulture));
            switch (typeCode)
            {
                case ResourceTypeCode.Null:
                    return null;
                case ResourceTypeCode.String:
                    return _store.ReadString();
                case ResourceTypeCode.Boolean:
                    return _store.ReadBoolean();
                case ResourceTypeCode.Char:
                    return (char)_store.ReadUInt16();
                case ResourceTypeCode.Byte:
                    return _store.ReadByte();
                case ResourceTypeCode.SByte:
                    return _store.ReadSByte();
                case ResourceTypeCode.Int16:
                    return _store.ReadInt16();
                case ResourceTypeCode.UInt16:
                    return _store.ReadUInt16();
                case ResourceTypeCode.Int32:
                    return _store.ReadInt32();
                case ResourceTypeCode.UInt32:
                    return _store.ReadUInt32();
                case ResourceTypeCode.Int64:
                    return _store.ReadInt64();
                case ResourceTypeCode.UInt64:
                    return _store.ReadUInt64();
                case ResourceTypeCode.Single:
                    return _store.ReadSingle();
                case ResourceTypeCode.Double:
                    return _store.ReadDouble();
                case ResourceTypeCode.Decimal:
                    return _store.ReadDecimal();
                case ResourceTypeCode.DateTime:
                    Int64 data = _store.ReadInt64();
                    return DateTime.FromBinary(data);
                case ResourceTypeCode.TimeSpan:
                    Int64 ticks = _store.ReadInt64();
                    return new TimeSpan(ticks);
                case ResourceTypeCode.ByteArray:
                {
                    int len = _store.ReadInt32();
                    if (len < 0)
                    {
                        throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourceDataLengthInvalid", len));
                    }

                    if (_ums == null)
                    {
                        if (len > _store.BaseStream.Length)
                        {
                            throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourceDataLengthInvalid", len));
                        }

                        return _store.ReadBytes(len);
                    }

                    if (len > _ums.Length - _ums.Position)
                    {
                        throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourceDataLengthInvalid", len));
                    }

                    byte[] bytes = new byte[len];
                    int r = _ums.Read(bytes, 0, len);
                    Contract.Assert(r == len, "ResourceReader needs to use a blocking read here.  (Call _store.ReadBytes(len)?)");
                    return bytes;
                }

                case ResourceTypeCode.Stream:
                {
                    int len = _store.ReadInt32();
                    if (len < 0)
                    {
                        throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourceDataLengthInvalid", len));
                    }

                    if (_ums == null)
                    {
                        byte[] bytes = _store.ReadBytes(len);
                        return new PinnedBufferMemoryStream(bytes);
                    }

                    if (len > _ums.Length - _ums.Position)
                    {
                        throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourceDataLengthInvalid", len));
                    }

                    unsafe
                    {
                        return new UnmanagedMemoryStream(_ums.PositionPointer, len, len, FileAccess.Read, true);
                    }
                }

                default:
                    if (typeCode < ResourceTypeCode.StartOfUserTypes)
                    {
                        throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_TypeMismatch"));
                    }

                    break;
            }

            throw new NotSupportedException(Environment.GetResourceString("NotSupported_ResourceObjectSerialization"));
        }

        private void ReadResources()
        {
            Contract.Assert(_store != null, "ResourceReader is closed!");
            try
            {
                _ReadResources();
            }
            catch (EndOfStreamException eof)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourcesHeaderCorrupted"), eof);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourcesHeaderCorrupted"), e);
            }
        }

        private void _ReadResources()
        {
            int magicNum = _store.ReadInt32();
            if (magicNum != ResourceManager.MagicNumber)
                throw new ArgumentException(Environment.GetResourceString("Resources_StreamNotValid"));
            int resMgrHeaderVersion = _store.ReadInt32();
            int numBytesToSkip = _store.ReadInt32();
            if (numBytesToSkip < 0 || resMgrHeaderVersion < 0)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourcesHeaderCorrupted"));
            }

            if (resMgrHeaderVersion > 1)
            {
                BCLDebug.Log("RESMGRFILEFORMAT", LogLevel.Status, "ReadResources: Unexpected ResMgr header version: {0}  Skipping ahead {1} bytes.", resMgrHeaderVersion, numBytesToSkip);
                _store.BaseStream.Seek(numBytesToSkip, SeekOrigin.Current);
            }
            else
            {
                BCLDebug.Log("RESMGRFILEFORMAT", "ReadResources: Parsing ResMgr header v1.");
                String readerType = _store.ReadString();
                AssemblyName mscorlib = new AssemblyName(ResourceManager.MscorlibName);
                if (!ResourceManager.CompareNames(readerType, ResourceManager.ResReaderTypeName, mscorlib))
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_WrongResourceReader_Type", readerType));
                SkipString();
            }

            int version = _store.ReadInt32();
            if (version != RuntimeResourceSet.Version && version != 1)
                throw new ArgumentException(Environment.GetResourceString("Arg_ResourceFileUnsupportedVersion", RuntimeResourceSet.Version, version));
            _version = version;
            _numResources = _store.ReadInt32();
            if (_numResources < 0)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourcesHeaderCorrupted"));
            }

            BCLDebug.Log("RESMGRFILEFORMAT", "ReadResources: Expecting " + _numResources + " resources.");
            if (ResourceManager.DEBUG >= 4)
                Console.WriteLine("ResourceReader::ReadResources - Reading in " + _numResources + " resources");
            int numTypes = _store.ReadInt32();
            if (numTypes < 0)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourcesHeaderCorrupted"));
            }

            _typeTable = new RuntimeType[numTypes];
            _typeNamePositions = new int[numTypes];
            for (int i = 0; i < numTypes; i++)
            {
                _typeNamePositions[i] = (int)_store.BaseStream.Position;
                SkipString();
            }

            if (ResourceManager.DEBUG >= 5)
                Console.WriteLine("ResourceReader::ReadResources - Reading in " + numTypes + " type table entries");
            long pos = _store.BaseStream.Position;
            int alignBytes = ((int)pos) & 7;
            if (alignBytes != 0)
            {
                for (int i = 0; i < 8 - alignBytes; i++)
                {
                    _store.ReadByte();
                }
            }

            if (_ums == null)
            {
                _nameHashes = new int[_numResources];
                for (int i = 0; i < _numResources; i++)
                {
                    _nameHashes[i] = _store.ReadInt32();
                }
            }
            else
            {
                int seekPos = unchecked (4 * _numResources);
                if (seekPos < 0)
                {
                    throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourcesHeaderCorrupted"));
                }

                unsafe
                {
                    _nameHashesPtr = (int *)_ums.PositionPointer;
                    _ums.Seek(seekPos, SeekOrigin.Current);
                    byte *junk = _ums.PositionPointer;
                }
            }

            if (_ums == null)
            {
                _namePositions = new int[_numResources];
                for (int i = 0; i < _numResources; i++)
                {
                    int namePosition = _store.ReadInt32();
                    if (namePosition < 0)
                    {
                        throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourcesHeaderCorrupted"));
                    }

                    _namePositions[i] = namePosition;
                }
            }
            else
            {
                int seekPos = unchecked (4 * _numResources);
                if (seekPos < 0)
                {
                    throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourcesHeaderCorrupted"));
                }

                unsafe
                {
                    _namePositionsPtr = (int *)_ums.PositionPointer;
                    _ums.Seek(seekPos, SeekOrigin.Current);
                    byte *junk = _ums.PositionPointer;
                }
            }

            _dataSectionOffset = _store.ReadInt32();
            if (_dataSectionOffset < 0)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourcesHeaderCorrupted"));
            }

            _nameSectionOffset = _store.BaseStream.Position;
            if (_dataSectionOffset < _nameSectionOffset)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ResourcesHeaderCorrupted"));
            }

            BCLDebug.Log("RESMGRFILEFORMAT", String.Format(CultureInfo.InvariantCulture, "ReadResources: _nameOffset = 0x{0:x}  _dataOffset = 0x{1:x}", _nameSectionOffset, _dataSectionOffset));
        }

        private RuntimeType FindType(int typeIndex)
        {
            if (typeIndex < 0 || typeIndex >= _typeTable.Length)
            {
                throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_InvalidType"));
            }

            if (_typeTable[typeIndex] == null)
            {
                long oldPos = _store.BaseStream.Position;
                try
                {
                    _store.BaseStream.Position = _typeNamePositions[typeIndex];
                    String typeName = _store.ReadString();
                    _typeTable[typeIndex] = (RuntimeType)Type.GetType(typeName, true);
                }
                catch (FileNotFoundException)
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_ResourceObjectSerialization"));
                }
                finally
                {
                    _store.BaseStream.Position = oldPos;
                }
            }

            Contract.Assert(_typeTable[typeIndex] != null, "Should have found a type!");
            return _typeTable[typeIndex];
        }

        public void GetResourceData(String resourceName, out String resourceType, out byte[] resourceData)
        {
            if (resourceName == null)
                throw new ArgumentNullException("resourceName");
            Contract.EndContractBlock();
            if (_resCache == null)
                throw new InvalidOperationException(Environment.GetResourceString("ResourceReaderIsClosed"));
            int[] sortedDataPositions = new int[_numResources];
            int dataPos = FindPosForResource(resourceName);
            if (dataPos == -1)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_ResourceNameNotExist", resourceName));
            }

            lock (this)
            {
                for (int i = 0; i < _numResources; i++)
                {
                    _store.BaseStream.Position = _nameSectionOffset + GetNamePosition(i);
                    int numBytesToSkip = _store.Read7BitEncodedInt();
                    if (numBytesToSkip < 0)
                    {
                        throw new FormatException(Environment.GetResourceString("BadImageFormat_ResourcesNameInvalidOffset", numBytesToSkip));
                    }

                    _store.BaseStream.Position += numBytesToSkip;
                    int dPos = _store.ReadInt32();
                    if (dPos < 0 || dPos >= _store.BaseStream.Length - _dataSectionOffset)
                    {
                        throw new FormatException(Environment.GetResourceString("BadImageFormat_ResourcesDataInvalidOffset", dPos));
                    }

                    sortedDataPositions[i] = dPos;
                }

                Array.Sort(sortedDataPositions);
                int index = Array.BinarySearch(sortedDataPositions, dataPos);
                Contract.Assert(index >= 0 && index < _numResources, "Couldn't find data position within sorted data positions array!");
                long nextData = (index < _numResources - 1) ? sortedDataPositions[index + 1] + _dataSectionOffset : _store.BaseStream.Length;
                int len = (int)(nextData - (dataPos + _dataSectionOffset));
                Contract.Assert(len >= 0 && len <= (int)_store.BaseStream.Length - dataPos + _dataSectionOffset, "Length was negative or outside the bounds of the file!");
                _store.BaseStream.Position = _dataSectionOffset + dataPos;
                ResourceTypeCode typeCode = (ResourceTypeCode)_store.Read7BitEncodedInt();
                if (typeCode < 0 || typeCode >= ResourceTypeCode.StartOfUserTypes + _typeTable.Length)
                {
                    throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_InvalidType"));
                }

                resourceType = TypeNameFromTypeCode(typeCode);
                len -= (int)(_store.BaseStream.Position - (_dataSectionOffset + dataPos));
                byte[] bytes = _store.ReadBytes(len);
                if (bytes.Length != len)
                    throw new FormatException(Environment.GetResourceString("BadImageFormat_ResourceNameCorrupted"));
                resourceData = bytes;
            }
        }

        private String TypeNameFromTypeCode(ResourceTypeCode typeCode)
        {
            Contract.Requires(typeCode >= 0, "can't be negative");
            if (typeCode < ResourceTypeCode.StartOfUserTypes)
            {
                Contract.Assert(!String.Equals(typeCode.ToString(), "LastPrimitive"), "Change ResourceTypeCode metadata order so LastPrimitive isn't what Enum.ToString prefers.");
                return "ResourceTypeCode." + typeCode.ToString();
            }
            else
            {
                int typeIndex = typeCode - ResourceTypeCode.StartOfUserTypes;
                Contract.Assert(typeIndex >= 0 && typeIndex < _typeTable.Length, "TypeCode is broken or corrupted!");
                long oldPos = _store.BaseStream.Position;
                try
                {
                    _store.BaseStream.Position = _typeNamePositions[typeIndex];
                    return _store.ReadString();
                }
                finally
                {
                    _store.BaseStream.Position = oldPos;
                }
            }
        }

        internal sealed class ResourceEnumerator : IDictionaryEnumerator
        {
            private const int ENUM_DONE = Int32.MinValue;
            private const int ENUM_NOT_STARTED = -1;
            private ResourceReader _reader;
            private bool _currentIsValid;
            private int _currentName;
            private int _dataPosition;
            internal ResourceEnumerator(ResourceReader reader)
            {
                _currentName = ENUM_NOT_STARTED;
                _reader = reader;
                _dataPosition = -2;
            }

            public bool MoveNext()
            {
                if (_currentName == _reader._numResources - 1 || _currentName == ENUM_DONE)
                {
                    _currentIsValid = false;
                    _currentName = ENUM_DONE;
                    return false;
                }

                _currentIsValid = true;
                _currentName++;
                return true;
            }

            public Object Key
            {
                [System.Security.SecuritySafeCritical]
                get
                {
                    if (_currentName == ENUM_DONE)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                    if (!_currentIsValid)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                    if (_reader._resCache == null)
                        throw new InvalidOperationException(Environment.GetResourceString("ResourceReaderIsClosed"));
                    return _reader.AllocateStringForNameIndex(_currentName, out _dataPosition);
                }
            }

            public Object Current
            {
                get
                {
                    return Entry;
                }
            }

            internal int DataPosition
            {
                get
                {
                    return _dataPosition;
                }
            }

            public DictionaryEntry Entry
            {
                [System.Security.SecuritySafeCritical]
                get
                {
                    if (_currentName == ENUM_DONE)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                    if (!_currentIsValid)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                    if (_reader._resCache == null)
                        throw new InvalidOperationException(Environment.GetResourceString("ResourceReaderIsClosed"));
                    String key;
                    Object value = null;
                    lock (_reader)
                    {
                        lock (_reader._resCache)
                        {
                            key = _reader.AllocateStringForNameIndex(_currentName, out _dataPosition);
                            ResourceLocator locator;
                            if (_reader._resCache.TryGetValue(key, out locator))
                            {
                                value = locator.Value;
                            }

                            if (value == null)
                            {
                                if (_dataPosition == -1)
                                    value = _reader.GetValueForNameIndex(_currentName);
                                else
                                    value = _reader.LoadObject(_dataPosition);
                            }
                        }
                    }

                    return new DictionaryEntry(key, value);
                }
            }

            public Object Value
            {
                get
                {
                    if (_currentName == ENUM_DONE)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                    if (!_currentIsValid)
                        throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                    if (_reader._resCache == null)
                        throw new InvalidOperationException(Environment.GetResourceString("ResourceReaderIsClosed"));
                    return _reader.GetValueForNameIndex(_currentName);
                }
            }

            public void Reset()
            {
                if (_reader._resCache == null)
                    throw new InvalidOperationException(Environment.GetResourceString("ResourceReaderIsClosed"));
                _currentIsValid = false;
                _currentName = ENUM_NOT_STARTED;
            }
        }
    }
}