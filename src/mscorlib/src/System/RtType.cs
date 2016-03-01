using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;

using MdToken = System.Reflection.MetadataToken;

namespace System
{
    internal delegate void CtorDelegate(Object instance);
    internal enum TypeNameFormatFlags
    {
        FormatBasic = 0x00000000,
        FormatNamespace = 0x00000001,
        FormatFullInst = 0x00000002,
        FormatAssembly = 0x00000004,
        FormatSignature = 0x00000008,
        FormatNoVersion = 0x00000010,
        FormatDebug = 0x00000020,
        FormatAngleBrackets = 0x00000040,
        FormatStubInfo = 0x00000080,
        FormatGenericParam = 0x00000100,
        FormatSerialization = FormatNamespace | FormatGenericParam | FormatFullInst
    }

    internal enum TypeNameKind
    {
        Name,
        ToString,
        SerializationName,
        FullName
    }

    internal class RuntimeType : System.Reflection.TypeInfo, ISerializable, ICloneable
    {
        internal enum MemberListType
        {
            All,
            CaseSensitive,
            CaseInsensitive,
            HandleToInfo
        }

        private struct ListBuilder<T>
            where T : class
        {
            T[] _items;
            T _item;
            int _count;
            int _capacity;
            public ListBuilder(int capacity)
            {
                _items = null;
                _item = null;
                _count = 0;
                _capacity = capacity;
            }

            public T this[int index]
            {
                get
                {
                    Contract.Requires(index < Count);
                    return (_items != null) ? _items[index] : _item;
                }

                set
                {
                    Contract.Requires(index < Count);
                    if (_items != null)
                        _items[index] = value;
                    else
                        _item = value;
                }
            }

            public T[] ToArray()
            {
                if (_count == 0)
                    return EmptyArray<T>.Value;
                if (_count == 1)
                    return new T[1]{_item};
                Array.Resize(ref _items, _count);
                _capacity = _count;
                return _items;
            }

            public void CopyTo(Object[] array, int index)
            {
                if (_count == 0)
                    return;
                if (_count == 1)
                {
                    array[index] = _item;
                    return;
                }

                Array.Copy(_items, 0, array, index, _count);
            }

            public int Count
            {
                get
                {
                    return _count;
                }
            }

            public void Add(T item)
            {
                if (_count == 0)
                {
                    _item = item;
                }
                else
                {
                    if (_count == 1)
                    {
                        if (_capacity < 2)
                            _capacity = 4;
                        _items = new T[_capacity];
                        _items[0] = _item;
                    }
                    else if (_capacity == _count)
                    {
                        int newCapacity = 2 * _capacity;
                        Array.Resize(ref _items, newCapacity);
                        _capacity = newCapacity;
                    }

                    _items[_count] = item;
                }

                _count++;
            }
        }

        internal class RuntimeTypeCache
        {
            private const int MAXNAMELEN = 1024;
            internal enum CacheType
            {
                Method,
                Constructor,
                Field,
                Property,
                Event,
                Interface,
                NestedType
            }

            private struct Filter
            {
                private Utf8String m_name;
                private MemberListType m_listType;
                private uint m_nameHash;
                public unsafe Filter(byte *pUtf8Name, int cUtf8Name, MemberListType listType)
                {
                    this.m_name = new Utf8String((void *)pUtf8Name, cUtf8Name);
                    this.m_listType = listType;
                    this.m_nameHash = 0;
                    if (RequiresStringComparison())
                    {
                        m_nameHash = m_name.HashCaseInsensitive();
                    }
                }

                public bool Match(Utf8String name)
                {
                    bool retVal = true;
                    if (m_listType == MemberListType.CaseSensitive)
                        retVal = m_name.Equals(name);
                    else if (m_listType == MemberListType.CaseInsensitive)
                        retVal = m_name.EqualsCaseInsensitive(name);
                    Contract.Assert(retVal || RequiresStringComparison());
                    return retVal;
                }

                public bool RequiresStringComparison()
                {
                    return (m_listType == MemberListType.CaseSensitive) || (m_listType == MemberListType.CaseInsensitive);
                }

                public bool CaseSensitive()
                {
                    return (m_listType == MemberListType.CaseSensitive);
                }

                public uint GetHashToMatch()
                {
                    Contract.Assert(RequiresStringComparison());
                    return m_nameHash;
                }
            }

            private class MemberInfoCache<T>
                where T : MemberInfo
            {
                private CerHashtable<string, T[]> m_csMemberInfos;
                private CerHashtable<string, T[]> m_cisMemberInfos;
                private T[] m_allMembers;
                private bool m_cacheComplete;
                private List<RuntimePropertyInfo> m_ambiguousProperties;
                private RuntimeTypeCache m_runtimeTypeCache;
                internal MemberInfoCache(RuntimeTypeCache runtimeTypeCache)
                {
                    m_runtimeTypeCache = runtimeTypeCache;
                }

                internal IReadOnlyList<RuntimePropertyInfo> AmbiguousProperties
                {
                    get
                    {
                        return m_ambiguousProperties;
                    }
                }

                private void InitializeAndUpdateAmbiguousPropertiesList(RuntimePropertyInfo parent, RuntimePropertyInfo child)
                {
                    Contract.Assert(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8);
                    if (m_ambiguousProperties == null)
                    {
                        List<RuntimePropertyInfo> newList = new List<RuntimePropertyInfo>();
                        Interlocked.CompareExchange(ref m_ambiguousProperties, newList, null);
                    }

                    lock (m_ambiguousProperties)
                    {
                        Contract.Assert(child.DeclaringType.IsSubclassOf(parent.DeclaringType));
                        m_ambiguousProperties.Add(parent);
                    }
                }

                internal MethodBase AddMethod(RuntimeType declaringType, RuntimeMethodHandleInternal method, CacheType cacheType)
                {
                    T[] list = null;
                    MethodAttributes methodAttributes = RuntimeMethodHandle.GetAttributes(method);
                    bool isPublic = (methodAttributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
                    bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
                    bool isInherited = declaringType != ReflectedType;
                    BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                    switch (cacheType)
                    {
                        case CacheType.Method:
                            list = (T[])(object)new RuntimeMethodInfo[1]{new RuntimeMethodInfo(method, declaringType, m_runtimeTypeCache, methodAttributes, bindingFlags, null)};
                            break;
                        case CacheType.Constructor:
                            list = (T[])(object)new RuntimeConstructorInfo[1]{new RuntimeConstructorInfo(method, declaringType, m_runtimeTypeCache, methodAttributes, bindingFlags)};
                            break;
                    }

                    Insert(ref list, null, MemberListType.HandleToInfo);
                    return (MethodBase)(object)list[0];
                }

                internal FieldInfo AddField(RuntimeFieldHandleInternal field)
                {
                    FieldAttributes fieldAttributes = RuntimeFieldHandle.GetAttributes(field);
                    bool isPublic = (fieldAttributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;
                    bool isStatic = (fieldAttributes & FieldAttributes.Static) != 0;
                    RuntimeType approxDeclaringType = RuntimeFieldHandle.GetApproxDeclaringType(field);
                    bool isInherited = RuntimeFieldHandle.AcquiresContextFromThis(field) ? !RuntimeTypeHandle.CompareCanonicalHandles(approxDeclaringType, ReflectedType) : approxDeclaringType != ReflectedType;
                    BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                    T[] list = (T[])(object)new RuntimeFieldInfo[1]{new RtFieldInfo(field, ReflectedType, m_runtimeTypeCache, bindingFlags)};
                    Insert(ref list, null, MemberListType.HandleToInfo);
                    return (FieldInfo)(object)list[0];
                }

                private unsafe T[] Populate(string name, MemberListType listType, CacheType cacheType)
                {
                    T[] list = null;
                    if (name == null || name.Length == 0 || (cacheType == CacheType.Constructor && name.FirstChar != '.' && name.FirstChar != '*'))
                    {
                        list = GetListByName(null, 0, null, 0, listType, cacheType);
                    }
                    else
                    {
                        int cNameLen = name.Length;
                        fixed (char *pName = name)
                        {
                            int cUtf8Name = Encoding.UTF8.GetByteCount(pName, cNameLen);
                            if (cUtf8Name > MAXNAMELEN)
                            {
                                fixed (byte *pUtf8Name = new byte[cUtf8Name])
                                {
                                    list = GetListByName(pName, cNameLen, pUtf8Name, cUtf8Name, listType, cacheType);
                                }
                            }
                            else
                            {
                                byte *pUtf8Name = stackalloc byte[cUtf8Name];
                                list = GetListByName(pName, cNameLen, pUtf8Name, cUtf8Name, listType, cacheType);
                            }
                        }
                    }

                    Insert(ref list, name, listType);
                    return list;
                }

                private unsafe T[] GetListByName(char *pName, int cNameLen, byte *pUtf8Name, int cUtf8Name, MemberListType listType, CacheType cacheType)
                {
                    if (cNameLen != 0)
                        Encoding.UTF8.GetBytes(pName, cNameLen, pUtf8Name, cUtf8Name);
                    Filter filter = new Filter(pUtf8Name, cUtf8Name, listType);
                    Object list = null;
                    switch (cacheType)
                    {
                        case CacheType.Method:
                            list = PopulateMethods(filter);
                            break;
                        case CacheType.Field:
                            list = PopulateFields(filter);
                            break;
                        case CacheType.Constructor:
                            list = PopulateConstructors(filter);
                            break;
                        case CacheType.Property:
                            list = PopulateProperties(filter);
                            break;
                        case CacheType.Event:
                            list = PopulateEvents(filter);
                            break;
                        case CacheType.NestedType:
                            list = PopulateNestedClasses(filter);
                            break;
                        case CacheType.Interface:
                            list = PopulateInterfaces(filter);
                            break;
                        default:
                            BCLDebug.Assert(true, "Invalid CacheType");
                            break;
                    }

                    return (T[])list;
                }

                internal void Insert(ref T[] list, string name, MemberListType listType)
                {
                    bool lockTaken = false;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        Monitor.Enter(this, ref lockTaken);
                        switch (listType)
                        {
                            case MemberListType.CaseSensitive:
                            {
                                T[] cachedList = m_csMemberInfos[name];
                                if (cachedList == null)
                                {
                                    MergeWithGlobalList(list);
                                    m_csMemberInfos[name] = list;
                                }
                                else
                                    list = cachedList;
                            }

                                break;
                            case MemberListType.CaseInsensitive:
                            {
                                T[] cachedList = m_cisMemberInfos[name];
                                if (cachedList == null)
                                {
                                    MergeWithGlobalList(list);
                                    m_cisMemberInfos[name] = list;
                                }
                                else
                                    list = cachedList;
                            }

                                break;
                            case MemberListType.All:
                                if (!m_cacheComplete)
                                {
                                    MergeWithGlobalList(list);
                                    int memberCount = m_allMembers.Length;
                                    while (memberCount > 0)
                                    {
                                        if (m_allMembers[memberCount - 1] != null)
                                            break;
                                        memberCount--;
                                    }

                                    Array.Resize(ref m_allMembers, memberCount);
                                    Volatile.Write(ref m_cacheComplete, true);
                                }

                                list = m_allMembers;
                                break;
                            default:
                                MergeWithGlobalList(list);
                                break;
                        }
                    }
                    finally
                    {
                        if (lockTaken)
                        {
                            Monitor.Exit(this);
                        }
                    }
                }

                private void MergeWithGlobalList(T[] list)
                {
                    T[] cachedMembers = m_allMembers;
                    if (cachedMembers == null)
                    {
                        m_allMembers = list;
                        return;
                    }

                    int cachedCount = cachedMembers.Length;
                    int freeSlotIndex = 0;
                    for (int i = 0; i < list.Length; i++)
                    {
                        T newMemberInfo = list[i];
                        bool foundInCache = false;
                        int cachedIndex;
                        for (cachedIndex = 0; cachedIndex < cachedCount; cachedIndex++)
                        {
                            T cachedMemberInfo = cachedMembers[cachedIndex];
                            if (cachedMemberInfo == null)
                                break;
                            if (newMemberInfo.CacheEquals(cachedMemberInfo))
                            {
                                list[i] = cachedMemberInfo;
                                foundInCache = true;
                                break;
                            }
                        }

                        if (!foundInCache)
                        {
                            if (freeSlotIndex == 0)
                                freeSlotIndex = cachedIndex;
                            if (freeSlotIndex >= cachedMembers.Length)
                            {
                                int newSize;
                                if (m_cacheComplete)
                                {
                                    Contract.Assert(false);
                                    newSize = cachedMembers.Length + 1;
                                }
                                else
                                {
                                    newSize = Math.Max(Math.Max(4, 2 * cachedMembers.Length), list.Length);
                                }

                                T[] cachedMembers2 = cachedMembers;
                                Array.Resize(ref cachedMembers2, newSize);
                                cachedMembers = cachedMembers2;
                            }

                            Contract.Assert(cachedMembers[freeSlotIndex] == null);
                            cachedMembers[freeSlotIndex] = newMemberInfo;
                            freeSlotIndex++;
                        }
                    }

                    m_allMembers = cachedMembers;
                }

                private unsafe RuntimeMethodInfo[] PopulateMethods(Filter filter)
                {
                    ListBuilder<RuntimeMethodInfo> list = new ListBuilder<RuntimeMethodInfo>();
                    RuntimeType declaringType = ReflectedType;
                    Contract.Assert(declaringType != null);
                    if (RuntimeTypeHandle.IsInterface(declaringType))
                    {
                        foreach (RuntimeMethodHandleInternal methodHandle in RuntimeTypeHandle.GetIntroducedMethods(declaringType))
                        {
                            if (filter.RequiresStringComparison())
                            {
                                if (!RuntimeMethodHandle.MatchesNameHash(methodHandle, filter.GetHashToMatch()))
                                {
                                    Contract.Assert(!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)));
                                    continue;
                                }

                                if (!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)))
                                    continue;
                            }

                            Contract.Assert(!methodHandle.IsNullHandle());
                            Contract.Assert((RuntimeMethodHandle.GetAttributes(methodHandle) & (MethodAttributes.RTSpecialName | MethodAttributes.Abstract | MethodAttributes.Virtual)) == (MethodAttributes.Abstract | MethodAttributes.Virtual) || (RuntimeMethodHandle.GetAttributes(methodHandle) & MethodAttributes.Static) == MethodAttributes.Static || RuntimeMethodHandle.GetName(methodHandle).Equals(".ctor") || RuntimeMethodHandle.GetName(methodHandle).Equals(".cctor") || RuntimeMethodHandle.GetName(methodHandle).StartsWith("IL_STUB", StringComparison.Ordinal));
                            MethodAttributes methodAttributes = RuntimeMethodHandle.GetAttributes(methodHandle);
                            bool isPublic = (methodAttributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
                            bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
                            bool isInherited = false;
                            BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                            if ((methodAttributes & MethodAttributes.RTSpecialName) != 0)
                                continue;
                            RuntimeMethodHandleInternal instantiatedHandle = RuntimeMethodHandle.GetStubIfNeeded(methodHandle, declaringType, null);
                            RuntimeMethodInfo runtimeMethodInfo = new RuntimeMethodInfo(instantiatedHandle, declaringType, m_runtimeTypeCache, methodAttributes, bindingFlags, null);
                            list.Add(runtimeMethodInfo);
                        }
                    }
                    else
                    {
                        while (RuntimeTypeHandle.IsGenericVariable(declaringType))
                            declaringType = declaringType.GetBaseType();
                        bool *overrides = stackalloc bool[RuntimeTypeHandle.GetNumVirtuals(declaringType)];
                        bool isValueType = declaringType.IsValueType;
                        do
                        {
                            int vtableSlots = RuntimeTypeHandle.GetNumVirtuals(declaringType);
                            foreach (RuntimeMethodHandleInternal methodHandle in RuntimeTypeHandle.GetIntroducedMethods(declaringType))
                            {
                                if (filter.RequiresStringComparison())
                                {
                                    if (!RuntimeMethodHandle.MatchesNameHash(methodHandle, filter.GetHashToMatch()))
                                    {
                                        Contract.Assert(!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)));
                                        continue;
                                    }

                                    if (!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)))
                                        continue;
                                }

                                Contract.Assert(!methodHandle.IsNullHandle());
                                MethodAttributes methodAttributes = RuntimeMethodHandle.GetAttributes(methodHandle);
                                MethodAttributes methodAccess = methodAttributes & MethodAttributes.MemberAccessMask;
                                Contract.Assert((RuntimeMethodHandle.GetAttributes(methodHandle) & MethodAttributes.RTSpecialName) == 0 || RuntimeMethodHandle.GetName(methodHandle).Equals(".ctor") || RuntimeMethodHandle.GetName(methodHandle).Equals(".cctor"));
                                if ((methodAttributes & MethodAttributes.RTSpecialName) != 0)
                                    continue;
                                bool isVirtual = false;
                                int methodSlot = 0;
                                if ((methodAttributes & MethodAttributes.Virtual) != 0)
                                {
                                    methodSlot = RuntimeMethodHandle.GetSlot(methodHandle);
                                    isVirtual = (methodSlot < vtableSlots);
                                }

                                bool isInherited = declaringType != ReflectedType;
                                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                                {
                                    bool isPrivate = methodAccess == MethodAttributes.Private;
                                    if (isInherited && isPrivate && !isVirtual)
                                        continue;
                                }

                                if (isVirtual)
                                {
                                    Contract.Assert((methodAttributes & MethodAttributes.Abstract) != 0 || (methodAttributes & MethodAttributes.Virtual) != 0 || RuntimeMethodHandle.GetDeclaringType(methodHandle) != declaringType);
                                    if (overrides[methodSlot] == true)
                                        continue;
                                    overrides[methodSlot] = true;
                                }
                                else if (isValueType)
                                {
                                    if ((methodAttributes & (MethodAttributes.Virtual | MethodAttributes.Abstract)) != 0)
                                        continue;
                                }
                                else
                                {
                                    Contract.Assert((methodAttributes & (MethodAttributes.Virtual | MethodAttributes.Abstract)) == 0);
                                }

                                bool isPublic = methodAccess == MethodAttributes.Public;
                                bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
                                BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                                RuntimeMethodHandleInternal instantiatedHandle = RuntimeMethodHandle.GetStubIfNeeded(methodHandle, declaringType, null);
                                RuntimeMethodInfo runtimeMethodInfo = new RuntimeMethodInfo(instantiatedHandle, declaringType, m_runtimeTypeCache, methodAttributes, bindingFlags, null);
                                list.Add(runtimeMethodInfo);
                            }

                            declaringType = RuntimeTypeHandle.GetBaseType(declaringType);
                        }
                        while (declaringType != null);
                    }

                    return list.ToArray();
                }

                private RuntimeConstructorInfo[] PopulateConstructors(Filter filter)
                {
                    if (ReflectedType.IsGenericParameter)
                    {
                        return EmptyArray<RuntimeConstructorInfo>.Value;
                    }

                    ListBuilder<RuntimeConstructorInfo> list = new ListBuilder<RuntimeConstructorInfo>();
                    RuntimeType declaringType = ReflectedType;
                    foreach (RuntimeMethodHandleInternal methodHandle in RuntimeTypeHandle.GetIntroducedMethods(declaringType))
                    {
                        if (filter.RequiresStringComparison())
                        {
                            if (!RuntimeMethodHandle.MatchesNameHash(methodHandle, filter.GetHashToMatch()))
                            {
                                Contract.Assert(!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)));
                                continue;
                            }

                            if (!filter.Match(RuntimeMethodHandle.GetUtf8Name(methodHandle)))
                                continue;
                        }

                        MethodAttributes methodAttributes = RuntimeMethodHandle.GetAttributes(methodHandle);
                        Contract.Assert(!methodHandle.IsNullHandle());
                        if ((methodAttributes & MethodAttributes.RTSpecialName) == 0)
                            continue;
                        Contract.Assert((methodAttributes & MethodAttributes.Abstract) == 0 && (methodAttributes & MethodAttributes.Virtual) == 0);
                        bool isPublic = (methodAttributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
                        bool isStatic = (methodAttributes & MethodAttributes.Static) != 0;
                        bool isInherited = false;
                        BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                        RuntimeMethodHandleInternal instantiatedHandle = RuntimeMethodHandle.GetStubIfNeeded(methodHandle, declaringType, null);
                        RuntimeConstructorInfo runtimeConstructorInfo = new RuntimeConstructorInfo(instantiatedHandle, ReflectedType, m_runtimeTypeCache, methodAttributes, bindingFlags);
                        list.Add(runtimeConstructorInfo);
                    }

                    return list.ToArray();
                }

                private unsafe RuntimeFieldInfo[] PopulateFields(Filter filter)
                {
                    ListBuilder<RuntimeFieldInfo> list = new ListBuilder<RuntimeFieldInfo>();
                    RuntimeType declaringType = ReflectedType;
                    while (RuntimeTypeHandle.IsGenericVariable(declaringType))
                        declaringType = declaringType.GetBaseType();
                    while (declaringType != null)
                    {
                        PopulateRtFields(filter, declaringType, ref list);
                        PopulateLiteralFields(filter, declaringType, ref list);
                        declaringType = RuntimeTypeHandle.GetBaseType(declaringType);
                    }

                    if (ReflectedType.IsGenericParameter)
                    {
                        Type[] interfaces = ReflectedType.BaseType.GetInterfaces();
                        for (int i = 0; i < interfaces.Length; i++)
                        {
                            PopulateLiteralFields(filter, (RuntimeType)interfaces[i], ref list);
                            PopulateRtFields(filter, (RuntimeType)interfaces[i], ref list);
                        }
                    }
                    else
                    {
                        Type[] interfaces = RuntimeTypeHandle.GetInterfaces(ReflectedType);
                        if (interfaces != null)
                        {
                            for (int i = 0; i < interfaces.Length; i++)
                            {
                                PopulateLiteralFields(filter, (RuntimeType)interfaces[i], ref list);
                                PopulateRtFields(filter, (RuntimeType)interfaces[i], ref list);
                            }
                        }
                    }

                    return list.ToArray();
                }

                private unsafe void PopulateRtFields(Filter filter, RuntimeType declaringType, ref ListBuilder<RuntimeFieldInfo> list)
                {
                    IntPtr*pResult = stackalloc IntPtr[64];
                    int count = 64;
                    if (!RuntimeTypeHandle.GetFields(declaringType, pResult, &count))
                    {
                        fixed (IntPtr*pBigResult = new IntPtr[count])
                        {
                            RuntimeTypeHandle.GetFields(declaringType, pBigResult, &count);
                            PopulateRtFields(filter, pBigResult, count, declaringType, ref list);
                        }
                    }
                    else if (count > 0)
                    {
                        PopulateRtFields(filter, pResult, count, declaringType, ref list);
                    }
                }

                private unsafe void PopulateRtFields(Filter filter, IntPtr*ppFieldHandles, int count, RuntimeType declaringType, ref ListBuilder<RuntimeFieldInfo> list)
                {
                    Contract.Requires(declaringType != null);
                    Contract.Requires(ReflectedType != null);
                    bool needsStaticFieldForGeneric = RuntimeTypeHandle.HasInstantiation(declaringType) && !RuntimeTypeHandle.ContainsGenericVariables(declaringType);
                    bool isInherited = declaringType != ReflectedType;
                    for (int i = 0; i < count; i++)
                    {
                        RuntimeFieldHandleInternal runtimeFieldHandle = new RuntimeFieldHandleInternal(ppFieldHandles[i]);
                        if (filter.RequiresStringComparison())
                        {
                            if (!RuntimeFieldHandle.MatchesNameHash(runtimeFieldHandle, filter.GetHashToMatch()))
                            {
                                Contract.Assert(!filter.Match(RuntimeFieldHandle.GetUtf8Name(runtimeFieldHandle)));
                                continue;
                            }

                            if (!filter.Match(RuntimeFieldHandle.GetUtf8Name(runtimeFieldHandle)))
                                continue;
                        }

                        Contract.Assert(!runtimeFieldHandle.IsNullHandle());
                        FieldAttributes fieldAttributes = RuntimeFieldHandle.GetAttributes(runtimeFieldHandle);
                        FieldAttributes fieldAccess = fieldAttributes & FieldAttributes.FieldAccessMask;
                        if (isInherited)
                        {
                            if (fieldAccess == FieldAttributes.Private)
                                continue;
                        }

                        bool isPublic = fieldAccess == FieldAttributes.Public;
                        bool isStatic = (fieldAttributes & FieldAttributes.Static) != 0;
                        BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                        if (needsStaticFieldForGeneric && isStatic)
                            runtimeFieldHandle = RuntimeFieldHandle.GetStaticFieldForGenericType(runtimeFieldHandle, declaringType);
                        RuntimeFieldInfo runtimeFieldInfo = new RtFieldInfo(runtimeFieldHandle, declaringType, m_runtimeTypeCache, bindingFlags);
                        list.Add(runtimeFieldInfo);
                    }
                }

                private unsafe void PopulateLiteralFields(Filter filter, RuntimeType declaringType, ref ListBuilder<RuntimeFieldInfo> list)
                {
                    Contract.Requires(declaringType != null);
                    Contract.Requires(ReflectedType != null);
                    int tkDeclaringType = RuntimeTypeHandle.GetToken(declaringType);
                    if (MdToken.IsNullToken(tkDeclaringType))
                        return;
                    MetadataImport scope = RuntimeTypeHandle.GetMetadataImport(declaringType);
                    MetadataEnumResult tkFields;
                    scope.EnumFields(tkDeclaringType, out tkFields);
                    for (int i = 0; i < tkFields.Length; i++)
                    {
                        int tkField = tkFields[i];
                        Contract.Assert(MdToken.IsTokenOfType(tkField, MetadataTokenType.FieldDef));
                        Contract.Assert(!MdToken.IsNullToken(tkField));
                        FieldAttributes fieldAttributes;
                        scope.GetFieldDefProps(tkField, out fieldAttributes);
                        FieldAttributes fieldAccess = fieldAttributes & FieldAttributes.FieldAccessMask;
                        if ((fieldAttributes & FieldAttributes.Literal) != 0)
                        {
                            bool isInherited = declaringType != ReflectedType;
                            if (isInherited)
                            {
                                bool isPrivate = fieldAccess == FieldAttributes.Private;
                                if (isPrivate)
                                    continue;
                            }

                            if (filter.RequiresStringComparison())
                            {
                                Utf8String name;
                                name = scope.GetName(tkField);
                                if (!filter.Match(name))
                                    continue;
                            }

                            bool isPublic = fieldAccess == FieldAttributes.Public;
                            bool isStatic = (fieldAttributes & FieldAttributes.Static) != 0;
                            BindingFlags bindingFlags = RuntimeType.FilterPreCalculate(isPublic, isInherited, isStatic);
                            RuntimeFieldInfo runtimeFieldInfo = new MdFieldInfo(tkField, fieldAttributes, declaringType.GetTypeHandleInternal(), m_runtimeTypeCache, bindingFlags);
                            list.Add(runtimeFieldInfo);
                        }
                    }
                }

                private static void AddElementTypes(Type template, IList<Type> types)
                {
                    if (!template.HasElementType)
                        return;
                    AddElementTypes(template.GetElementType(), types);
                    for (int i = 0; i < types.Count; i++)
                    {
                        if (template.IsArray)
                        {
                            if (template.IsSzArray)
                                types[i] = types[i].MakeArrayType();
                            else
                                types[i] = types[i].MakeArrayType(template.GetArrayRank());
                        }
                        else if (template.IsPointer)
                        {
                            types[i] = types[i].MakePointerType();
                        }
                    }
                }

                private void AddSpecialInterface(ref ListBuilder<RuntimeType> list, Filter filter, RuntimeType iList, bool addSubInterface)
                {
                    if (iList.IsAssignableFrom(ReflectedType))
                    {
                        if (filter.Match(RuntimeTypeHandle.GetUtf8Name(iList)))
                            list.Add(iList);
                        if (addSubInterface)
                        {
                            Type[] iFaces = iList.GetInterfaces();
                            for (int j = 0; j < iFaces.Length; j++)
                            {
                                RuntimeType iFace = (RuntimeType)iFaces[j];
                                if (iFace.IsGenericType && filter.Match(RuntimeTypeHandle.GetUtf8Name(iFace)))
                                    list.Add(iFace);
                            }
                        }
                    }
                }

                private RuntimeType[] PopulateInterfaces(Filter filter)
                {
                    ListBuilder<RuntimeType> list = new ListBuilder<RuntimeType>();
                    RuntimeType declaringType = ReflectedType;
                    if (!RuntimeTypeHandle.IsGenericVariable(declaringType))
                    {
                        Type[] ifaces = RuntimeTypeHandle.GetInterfaces(declaringType);
                        if (ifaces != null)
                        {
                            for (int i = 0; i < ifaces.Length; i++)
                            {
                                RuntimeType interfaceType = (RuntimeType)ifaces[i];
                                if (filter.RequiresStringComparison())
                                {
                                    if (!filter.Match(RuntimeTypeHandle.GetUtf8Name(interfaceType)))
                                        continue;
                                }

                                Contract.Assert(interfaceType.IsInterface);
                                list.Add(interfaceType);
                            }
                        }

                        if (ReflectedType.IsSzArray)
                        {
                            RuntimeType arrayType = (RuntimeType)ReflectedType.GetElementType();
                            if (!arrayType.IsPointer)
                            {
                                AddSpecialInterface(ref list, filter, (RuntimeType)typeof (IList<>).MakeGenericType(arrayType), true);
                                AddSpecialInterface(ref list, filter, (RuntimeType)typeof (IReadOnlyList<>).MakeGenericType(arrayType), false);
                                AddSpecialInterface(ref list, filter, (RuntimeType)typeof (IReadOnlyCollection<>).MakeGenericType(arrayType), false);
                            }
                        }
                    }
                    else
                    {
                        List<RuntimeType> al = new List<RuntimeType>();
                        Type[] constraints = declaringType.GetGenericParameterConstraints();
                        for (int i = 0; i < constraints.Length; i++)
                        {
                            RuntimeType constraint = (RuntimeType)constraints[i];
                            if (constraint.IsInterface)
                                al.Add(constraint);
                            Type[] temp = constraint.GetInterfaces();
                            for (int j = 0; j < temp.Length; j++)
                                al.Add(temp[j] as RuntimeType);
                        }

                        Dictionary<RuntimeType, RuntimeType> ht = new Dictionary<RuntimeType, RuntimeType>();
                        for (int i = 0; i < al.Count; i++)
                        {
                            RuntimeType constraint = al[i];
                            if (!ht.ContainsKey(constraint))
                                ht[constraint] = constraint;
                        }

                        RuntimeType[] interfaces = new RuntimeType[ht.Values.Count];
                        ht.Values.CopyTo(interfaces, 0);
                        for (int i = 0; i < interfaces.Length; i++)
                        {
                            if (filter.RequiresStringComparison())
                            {
                                if (!filter.Match(RuntimeTypeHandle.GetUtf8Name(interfaces[i])))
                                    continue;
                            }

                            list.Add(interfaces[i]);
                        }
                    }

                    return list.ToArray();
                }

                private unsafe RuntimeType[] PopulateNestedClasses(Filter filter)
                {
                    RuntimeType declaringType = ReflectedType;
                    while (RuntimeTypeHandle.IsGenericVariable(declaringType))
                    {
                        declaringType = declaringType.GetBaseType();
                    }

                    int tkEnclosingType = RuntimeTypeHandle.GetToken(declaringType);
                    if (MdToken.IsNullToken(tkEnclosingType))
                        return EmptyArray<RuntimeType>.Value;
                    ListBuilder<RuntimeType> list = new ListBuilder<RuntimeType>();
                    RuntimeModule moduleHandle = RuntimeTypeHandle.GetModule(declaringType);
                    MetadataImport scope = ModuleHandle.GetMetadataImport(moduleHandle);
                    MetadataEnumResult tkNestedClasses;
                    scope.EnumNestedTypes(tkEnclosingType, out tkNestedClasses);
                    for (int i = 0; i < tkNestedClasses.Length; i++)
                    {
                        RuntimeType nestedType = null;
                        try
                        {
                            nestedType = ModuleHandle.ResolveTypeHandleInternal(moduleHandle, tkNestedClasses[i], null, null);
                        }
                        catch (System.TypeLoadException)
                        {
                            continue;
                        }

                        if (filter.RequiresStringComparison())
                        {
                            if (!filter.Match(RuntimeTypeHandle.GetUtf8Name(nestedType)))
                                continue;
                        }

                        list.Add(nestedType);
                    }

                    return list.ToArray();
                }

                private unsafe RuntimeEventInfo[] PopulateEvents(Filter filter)
                {
                    Contract.Requires(ReflectedType != null);
                    Dictionary<String, RuntimeEventInfo> csEventInfos = filter.CaseSensitive() ? null : new Dictionary<String, RuntimeEventInfo>();
                    RuntimeType declaringType = ReflectedType;
                    ListBuilder<RuntimeEventInfo> list = new ListBuilder<RuntimeEventInfo>();
                    if (!RuntimeTypeHandle.IsInterface(declaringType))
                    {
                        while (RuntimeTypeHandle.IsGenericVariable(declaringType))
                            declaringType = declaringType.GetBaseType();
                        while (declaringType != null)
                        {
                            PopulateEvents(filter, declaringType, csEventInfos, ref list);
                            declaringType = RuntimeTypeHandle.GetBaseType(declaringType);
                        }
                    }
                    else
                    {
                        PopulateEvents(filter, declaringType, csEventInfos, ref list);
                    }

                    return list.ToArray();
                }

                private unsafe void PopulateEvents(Filter filter, RuntimeType declaringType, Dictionary<String, RuntimeEventInfo> csEventInfos, ref ListBuilder<RuntimeEventInfo> list)
                {
                    int tkDeclaringType = RuntimeTypeHandle.GetToken(declaringType);
                    if (MdToken.IsNullToken(tkDeclaringType))
                        return;
                    MetadataImport scope = RuntimeTypeHandle.GetMetadataImport(declaringType);
                    MetadataEnumResult tkEvents;
                    scope.EnumEvents(tkDeclaringType, out tkEvents);
                    for (int i = 0; i < tkEvents.Length; i++)
                    {
                        int tkEvent = tkEvents[i];
                        bool isPrivate;
                        Contract.Assert(!MdToken.IsNullToken(tkEvent));
                        Contract.Assert(MdToken.IsTokenOfType(tkEvent, MetadataTokenType.Event));
                        if (filter.RequiresStringComparison())
                        {
                            Utf8String name;
                            name = scope.GetName(tkEvent);
                            if (!filter.Match(name))
                                continue;
                        }

                        RuntimeEventInfo eventInfo = new RuntimeEventInfo(tkEvent, declaringType, m_runtimeTypeCache, out isPrivate);
                        if (declaringType != m_runtimeTypeCache.GetRuntimeType() && isPrivate)
                            continue;
                        if (csEventInfos != null)
                        {
                            string name = eventInfo.Name;
                            if (csEventInfos.GetValueOrDefault(name) != null)
                                continue;
                            csEventInfos[name] = eventInfo;
                        }
                        else
                        {
                            if (list.Count > 0)
                                break;
                        }

                        list.Add(eventInfo);
                    }
                }

                private unsafe RuntimePropertyInfo[] PopulateProperties(Filter filter)
                {
                    Contract.Requires(ReflectedType != null);
                    RuntimeType declaringType = ReflectedType;
                    Contract.Assert(declaringType != null);
                    ListBuilder<RuntimePropertyInfo> list = new ListBuilder<RuntimePropertyInfo>();
                    if (!RuntimeTypeHandle.IsInterface(declaringType))
                    {
                        while (RuntimeTypeHandle.IsGenericVariable(declaringType))
                            declaringType = declaringType.GetBaseType();
                        Dictionary<String, List<RuntimePropertyInfo>> csPropertyInfos = filter.CaseSensitive() ? null : new Dictionary<String, List<RuntimePropertyInfo>>();
                        bool[] usedSlots = new bool[RuntimeTypeHandle.GetNumVirtuals(declaringType)];
                        do
                        {
                            PopulateProperties(filter, declaringType, csPropertyInfos, usedSlots, ref list);
                            declaringType = RuntimeTypeHandle.GetBaseType(declaringType);
                        }
                        while (declaringType != null);
                    }
                    else
                    {
                        PopulateProperties(filter, declaringType, null, null, ref list);
                    }

                    return list.ToArray();
                }

                private unsafe void PopulateProperties(Filter filter, RuntimeType declaringType, Dictionary<String, List<RuntimePropertyInfo>> csPropertyInfos, bool[] usedSlots, ref ListBuilder<RuntimePropertyInfo> list)
                {
                    int tkDeclaringType = RuntimeTypeHandle.GetToken(declaringType);
                    if (MdToken.IsNullToken(tkDeclaringType))
                        return;
                    MetadataImport scope = RuntimeTypeHandle.GetMetadataImport(declaringType);
                    MetadataEnumResult tkProperties;
                    scope.EnumProperties(tkDeclaringType, out tkProperties);
                    RuntimeModule declaringModuleHandle = RuntimeTypeHandle.GetModule(declaringType);
                    int numVirtuals = RuntimeTypeHandle.GetNumVirtuals(declaringType);
                    Contract.Assert((declaringType.IsInterface && usedSlots == null && csPropertyInfos == null) || (!declaringType.IsInterface && usedSlots != null && usedSlots.Length >= numVirtuals));
                    for (int i = 0; i < tkProperties.Length; i++)
                    {
                        int tkProperty = tkProperties[i];
                        bool isPrivate;
                        Contract.Assert(!MdToken.IsNullToken(tkProperty));
                        Contract.Assert(MdToken.IsTokenOfType(tkProperty, MetadataTokenType.Property));
                        if (filter.RequiresStringComparison())
                        {
                            if (!ModuleHandle.ContainsPropertyMatchingHash(declaringModuleHandle, tkProperty, filter.GetHashToMatch()))
                            {
                                Contract.Assert(!filter.Match(declaringType.GetRuntimeModule().MetadataImport.GetName(tkProperty)));
                                continue;
                            }

                            Utf8String name;
                            name = declaringType.GetRuntimeModule().MetadataImport.GetName(tkProperty);
                            if (!filter.Match(name))
                                continue;
                        }

                        RuntimePropertyInfo propertyInfo = new RuntimePropertyInfo(tkProperty, declaringType, m_runtimeTypeCache, out isPrivate);
                        if (usedSlots != null)
                        {
                            if (declaringType != ReflectedType && isPrivate)
                                continue;
                            MethodInfo associateMethod = propertyInfo.GetGetMethod();
                            if (associateMethod == null)
                            {
                                associateMethod = propertyInfo.GetSetMethod();
                            }

                            if (associateMethod != null)
                            {
                                int slot = RuntimeMethodHandle.GetSlot((RuntimeMethodInfo)associateMethod);
                                if (slot < numVirtuals)
                                {
                                    Contract.Assert(associateMethod.IsVirtual);
                                    if (usedSlots[slot] == true)
                                        continue;
                                    else
                                        usedSlots[slot] = true;
                                }
                            }

                            if (csPropertyInfos != null)
                            {
                                string name = propertyInfo.Name;
                                List<RuntimePropertyInfo> cache = csPropertyInfos.GetValueOrDefault(name);
                                if (cache == null)
                                {
                                    cache = new List<RuntimePropertyInfo>(1);
                                    csPropertyInfos[name] = cache;
                                }

                                for (int j = 0; j < cache.Count; j++)
                                {
                                    if (propertyInfo.EqualsSig(cache[j]))
                                    {
                                        if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && !propertyInfo.HasMatchingAccessibility(cache[j]))
                                        {
                                            InitializeAndUpdateAmbiguousPropertiesList(propertyInfo, cache[j]);
                                        }
                                        else
                                        {
                                            cache = null;
                                            break;
                                        }
                                    }
                                }

                                if (cache == null)
                                    continue;
                                cache.Add(propertyInfo);
                            }
                            else
                            {
                                bool duplicate = false;
                                for (int j = 0; j < list.Count; j++)
                                {
                                    if (propertyInfo.EqualsSig(list[j]))
                                    {
                                        if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && !propertyInfo.HasMatchingAccessibility(list[j]))
                                        {
                                            InitializeAndUpdateAmbiguousPropertiesList(propertyInfo, list[j]);
                                        }
                                        else
                                        {
                                            duplicate = true;
                                            break;
                                        }
                                    }
                                }

                                if (duplicate)
                                    continue;
                            }
                        }

                        list.Add(propertyInfo);
                    }
                }

                internal T[] GetMemberList(MemberListType listType, string name, CacheType cacheType)
                {
                    T[] list = null;
                    switch (listType)
                    {
                        case MemberListType.CaseSensitive:
                            list = m_csMemberInfos[name];
                            if (list != null)
                                return list;
                            return Populate(name, listType, cacheType);
                        case MemberListType.CaseInsensitive:
                            list = m_cisMemberInfos[name];
                            if (list != null)
                                return list;
                            return Populate(name, listType, cacheType);
                        default:
                            Contract.Assert(listType == MemberListType.All);
                            if (Volatile.Read(ref m_cacheComplete))
                                return m_allMembers;
                            return Populate(null, listType, cacheType);
                    }
                }

                internal RuntimeType ReflectedType
                {
                    get
                    {
                        return m_runtimeTypeCache.GetRuntimeType();
                    }
                }
            }

            private RuntimeType m_runtimeType;
            private RuntimeType m_enclosingType;
            private TypeCode m_typeCode;
            private string m_name;
            private string m_fullname;
            private string m_toString;
            private string m_namespace;
            private string m_serializationname;
            private bool m_isGlobal;
            private bool m_bIsDomainInitialized;
            private MemberInfoCache<RuntimeMethodInfo> m_methodInfoCache;
            private MemberInfoCache<RuntimeConstructorInfo> m_constructorInfoCache;
            private MemberInfoCache<RuntimeFieldInfo> m_fieldInfoCache;
            private MemberInfoCache<RuntimeType> m_interfaceCache;
            private MemberInfoCache<RuntimeType> m_nestedClassesCache;
            private MemberInfoCache<RuntimePropertyInfo> m_propertyInfoCache;
            private MemberInfoCache<RuntimeEventInfo> m_eventInfoCache;
            private static CerHashtable<RuntimeMethodInfo, RuntimeMethodInfo> s_methodInstantiations;
            private static Object s_methodInstantiationsLock;
            private string m_defaultMemberName;
            private Object m_genericCache;
            internal RuntimeTypeCache(RuntimeType runtimeType)
            {
                m_typeCode = TypeCode.Empty;
                m_runtimeType = runtimeType;
                m_isGlobal = RuntimeTypeHandle.GetModule(runtimeType).RuntimeType == runtimeType;
            }

            private string ConstructName(ref string name, TypeNameFormatFlags formatFlags)
            {
                if (name == null)
                {
                    name = new RuntimeTypeHandle(m_runtimeType).ConstructName(formatFlags);
                }

                return name;
            }

            private T[] GetMemberList<T>(ref MemberInfoCache<T> m_cache, MemberListType listType, string name, CacheType cacheType)where T : MemberInfo
            {
                MemberInfoCache<T> existingCache = GetMemberCache<T>(ref m_cache);
                return existingCache.GetMemberList(listType, name, cacheType);
            }

            private T[] GetMemberList<T>(ref MemberInfoCache<T> m_cache, MemberListType listType, string name, CacheType cacheType, out IReadOnlyList<RuntimePropertyInfo> ambiguousProperties)where T : MemberInfo
            {
                Contract.Assert(cacheType == CacheType.Property);
                MemberInfoCache<T> existingCache = GetMemberCache<T>(ref m_cache);
                T[] results = existingCache.GetMemberList(listType, name, cacheType);
                ambiguousProperties = existingCache.AmbiguousProperties;
                Contract.Assert(ambiguousProperties == null || CompatibilitySwitches.IsAppEarlierThanWindowsPhone8);
                return results;
            }

            private MemberInfoCache<T> GetMemberCache<T>(ref MemberInfoCache<T> m_cache)where T : MemberInfo
            {
                MemberInfoCache<T> existingCache = m_cache;
                if (existingCache == null)
                {
                    MemberInfoCache<T> newCache = new MemberInfoCache<T>(this);
                    existingCache = Interlocked.CompareExchange(ref m_cache, newCache, null);
                    if (existingCache == null)
                        existingCache = newCache;
                }

                return existingCache;
            }

            internal Object GenericCache
            {
                get
                {
                    return m_genericCache;
                }

                set
                {
                    m_genericCache = value;
                }
            }

            internal bool DomainInitialized
            {
                get
                {
                    return m_bIsDomainInitialized;
                }

                set
                {
                    m_bIsDomainInitialized = value;
                }
            }

            internal string GetName(TypeNameKind kind)
            {
                switch (kind)
                {
                    case TypeNameKind.Name:
                        return ConstructName(ref m_name, TypeNameFormatFlags.FormatBasic);
                    case TypeNameKind.FullName:
                        if (!m_runtimeType.GetRootElementType().IsGenericTypeDefinition && m_runtimeType.ContainsGenericParameters)
                            return null;
                        return ConstructName(ref m_fullname, TypeNameFormatFlags.FormatNamespace | TypeNameFormatFlags.FormatFullInst);
                    case TypeNameKind.ToString:
                        return ConstructName(ref m_toString, TypeNameFormatFlags.FormatNamespace);
                    case TypeNameKind.SerializationName:
                        return ConstructName(ref m_serializationname, TypeNameFormatFlags.FormatSerialization);
                    default:
                        throw new InvalidOperationException();
                }
            }

            internal unsafe string GetNameSpace()
            {
                if (m_namespace == null)
                {
                    Type type = m_runtimeType;
                    type = type.GetRootElementType();
                    while (type.IsNested)
                        type = type.DeclaringType;
                    m_namespace = RuntimeTypeHandle.GetMetadataImport((RuntimeType)type).GetNamespace(type.MetadataToken).ToString();
                }

                return m_namespace;
            }

            internal TypeCode TypeCode
            {
                get
                {
                    return m_typeCode;
                }

                set
                {
                    m_typeCode = value;
                }
            }

            internal unsafe RuntimeType GetEnclosingType()
            {
                if (m_enclosingType == null)
                {
                    RuntimeType enclosingType = RuntimeTypeHandle.GetDeclaringType(GetRuntimeType());
                    Contract.Assert(enclosingType != typeof (void));
                    m_enclosingType = enclosingType ?? (RuntimeType)typeof (void);
                }

                return (m_enclosingType == typeof (void)) ? null : m_enclosingType;
            }

            internal RuntimeType GetRuntimeType()
            {
                return m_runtimeType;
            }

            internal bool IsGlobal
            {
                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
                get
                {
                    return m_isGlobal;
                }
            }

            internal void InvalidateCachedNestedType()
            {
                m_nestedClassesCache = null;
            }

            internal string GetDefaultMemberName()
            {
                if (m_defaultMemberName == null)
                {
                    CustomAttributeData attr = null;
                    Type DefaultMemberAttrType = typeof (DefaultMemberAttribute);
                    for (RuntimeType t = m_runtimeType; t != null; t = t.GetBaseType())
                    {
                        IList<CustomAttributeData> attrs = CustomAttributeData.GetCustomAttributes(t);
                        for (int i = 0; i < attrs.Count; i++)
                        {
                            if (Object.ReferenceEquals(attrs[i].Constructor.DeclaringType, DefaultMemberAttrType))
                            {
                                attr = attrs[i];
                                break;
                            }
                        }

                        if (attr != null)
                        {
                            m_defaultMemberName = attr.ConstructorArguments[0].Value as string;
                            break;
                        }
                    }
                }

                return m_defaultMemberName;
            }

            internal MethodInfo GetGenericMethodInfo(RuntimeMethodHandleInternal genericMethod)
            {
                LoaderAllocator la = RuntimeMethodHandle.GetLoaderAllocator(genericMethod);
                RuntimeMethodInfo rmi = new RuntimeMethodInfo(genericMethod, RuntimeMethodHandle.GetDeclaringType(genericMethod), this, RuntimeMethodHandle.GetAttributes(genericMethod), (BindingFlags)(-1), la);
                RuntimeMethodInfo crmi;
                if (la != null)
                {
                    crmi = la.m_methodInstantiations[rmi];
                }
                else
                {
                    crmi = s_methodInstantiations[rmi];
                }

                if (crmi != null)
                    return crmi;
                if (s_methodInstantiationsLock == null)
                    Interlocked.CompareExchange(ref s_methodInstantiationsLock, new Object(), null);
                bool lockTaken = false;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    Monitor.Enter(s_methodInstantiationsLock, ref lockTaken);
                    if (la != null)
                    {
                        crmi = la.m_methodInstantiations[rmi];
                        if (crmi != null)
                            return crmi;
                        la.m_methodInstantiations[rmi] = rmi;
                    }
                    else
                    {
                        crmi = s_methodInstantiations[rmi];
                        if (crmi != null)
                            return crmi;
                        s_methodInstantiations[rmi] = rmi;
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(s_methodInstantiationsLock);
                    }
                }

                return rmi;
            }

            internal RuntimeMethodInfo[] GetMethodList(MemberListType listType, string name)
            {
                return GetMemberList<RuntimeMethodInfo>(ref m_methodInfoCache, listType, name, CacheType.Method);
            }

            internal RuntimeConstructorInfo[] GetConstructorList(MemberListType listType, string name)
            {
                return GetMemberList<RuntimeConstructorInfo>(ref m_constructorInfoCache, listType, name, CacheType.Constructor);
            }

            internal RuntimePropertyInfo[] GetPropertyList(MemberListType listType, string name)
            {
                return GetMemberList<RuntimePropertyInfo>(ref m_propertyInfoCache, listType, name, CacheType.Property);
            }

            internal RuntimePropertyInfo[] GetPropertyList(MemberListType listType, string name, out IReadOnlyList<RuntimePropertyInfo> ambiguousProperties)
            {
                return GetMemberList<RuntimePropertyInfo>(ref m_propertyInfoCache, listType, name, CacheType.Property, out ambiguousProperties);
            }

            internal RuntimeEventInfo[] GetEventList(MemberListType listType, string name)
            {
                return GetMemberList<RuntimeEventInfo>(ref m_eventInfoCache, listType, name, CacheType.Event);
            }

            internal RuntimeFieldInfo[] GetFieldList(MemberListType listType, string name)
            {
                return GetMemberList<RuntimeFieldInfo>(ref m_fieldInfoCache, listType, name, CacheType.Field);
            }

            internal RuntimeType[] GetInterfaceList(MemberListType listType, string name)
            {
                return GetMemberList<RuntimeType>(ref m_interfaceCache, listType, name, CacheType.Interface);
            }

            internal RuntimeType[] GetNestedTypeList(MemberListType listType, string name)
            {
                return GetMemberList<RuntimeType>(ref m_nestedClassesCache, listType, name, CacheType.NestedType);
            }

            internal MethodBase GetMethod(RuntimeType declaringType, RuntimeMethodHandleInternal method)
            {
                GetMemberCache<RuntimeMethodInfo>(ref m_methodInfoCache);
                return m_methodInfoCache.AddMethod(declaringType, method, CacheType.Method);
            }

            internal MethodBase GetConstructor(RuntimeType declaringType, RuntimeMethodHandleInternal constructor)
            {
                GetMemberCache<RuntimeConstructorInfo>(ref m_constructorInfoCache);
                return m_constructorInfoCache.AddMethod(declaringType, constructor, CacheType.Constructor);
            }

            internal FieldInfo GetField(RuntimeFieldHandleInternal field)
            {
                GetMemberCache<RuntimeFieldInfo>(ref m_fieldInfoCache);
                return m_fieldInfoCache.AddField(field);
            }
        }

        internal static RuntimeType GetType(String typeName, bool throwOnError, bool ignoreCase, bool reflectionOnly, ref StackCrawlMark stackMark)
        {
            if (typeName == null)
                throw new ArgumentNullException("typeName");
            Contract.EndContractBlock();
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && typeName.Length == 0)
                throw new TypeLoadException(Environment.GetResourceString("Arg_TypeLoadNullStr"));
            return RuntimeTypeHandle.GetTypeByName(typeName, throwOnError, ignoreCase, reflectionOnly, ref stackMark, false);
        }

        internal static MethodBase GetMethodBase(RuntimeModule scope, int typeMetadataToken)
        {
            return GetMethodBase(ModuleHandle.ResolveMethodHandleInternal(scope, typeMetadataToken));
        }

        internal static MethodBase GetMethodBase(IRuntimeMethodInfo methodHandle)
        {
            return GetMethodBase(null, methodHandle);
        }

        internal static MethodBase GetMethodBase(RuntimeType reflectedType, IRuntimeMethodInfo methodHandle)
        {
            MethodBase retval = RuntimeType.GetMethodBase(reflectedType, methodHandle.Value);
            GC.KeepAlive(methodHandle);
            return retval;
        }

        internal unsafe static MethodBase GetMethodBase(RuntimeType reflectedType, RuntimeMethodHandleInternal methodHandle)
        {
            Contract.Assert(!methodHandle.IsNullHandle());
            if (RuntimeMethodHandle.IsDynamicMethod(methodHandle))
            {
                Resolver resolver = RuntimeMethodHandle.GetResolver(methodHandle);
                if (resolver != null)
                    return resolver.GetDynamicMethod();
                return null;
            }

            RuntimeType declaredType = RuntimeMethodHandle.GetDeclaringType(methodHandle);
            RuntimeType[] methodInstantiation = null;
            if (reflectedType == null)
                reflectedType = declaredType as RuntimeType;
            if (reflectedType != declaredType && !reflectedType.IsSubclassOf(declaredType))
            {
                if (reflectedType.IsArray)
                {
                    MethodBase[] methodBases = reflectedType.GetMember(RuntimeMethodHandle.GetName(methodHandle), MemberTypes.Constructor | MemberTypes.Method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) as MethodBase[];
                    bool loaderAssuredCompatible = false;
                    for (int i = 0; i < methodBases.Length; i++)
                    {
                        IRuntimeMethodInfo rmi = (IRuntimeMethodInfo)methodBases[i];
                        if (rmi.Value.Value == methodHandle.Value)
                            loaderAssuredCompatible = true;
                    }

                    if (!loaderAssuredCompatible)
                        throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveMethodHandle"), reflectedType.ToString(), declaredType.ToString()));
                }
                else if (declaredType.IsGenericType)
                {
                    RuntimeType declaringDefinition = (RuntimeType)declaredType.GetGenericTypeDefinition();
                    RuntimeType baseType = reflectedType;
                    while (baseType != null)
                    {
                        RuntimeType baseDefinition = baseType;
                        if (baseDefinition.IsGenericType && !baseType.IsGenericTypeDefinition)
                            baseDefinition = (RuntimeType)baseDefinition.GetGenericTypeDefinition();
                        if (baseDefinition == declaringDefinition)
                            break;
                        baseType = baseType.GetBaseType();
                    }

                    if (baseType == null)
                    {
                        throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveMethodHandle"), reflectedType.ToString(), declaredType.ToString()));
                    }

                    declaredType = baseType;
                    if (!RuntimeMethodHandle.IsGenericMethodDefinition(methodHandle))
                    {
                        methodInstantiation = RuntimeMethodHandle.GetMethodInstantiationInternal(methodHandle);
                    }

                    methodHandle = RuntimeMethodHandle.GetMethodFromCanonical(methodHandle, declaredType);
                }
                else if (!declaredType.IsAssignableFrom(reflectedType))
                {
                    throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveMethodHandle"), reflectedType.ToString(), declaredType.ToString()));
                }
            }

            methodHandle = RuntimeMethodHandle.GetStubIfNeeded(methodHandle, declaredType, methodInstantiation);
            MethodBase retval;
            if (RuntimeMethodHandle.IsConstructor(methodHandle))
            {
                retval = reflectedType.Cache.GetConstructor(declaredType, methodHandle);
            }
            else
            {
                if (RuntimeMethodHandle.HasMethodInstantiation(methodHandle) && !RuntimeMethodHandle.IsGenericMethodDefinition(methodHandle))
                    retval = reflectedType.Cache.GetGenericMethodInfo(methodHandle);
                else
                    retval = reflectedType.Cache.GetMethod(declaredType, methodHandle);
            }

            GC.KeepAlive(methodInstantiation);
            return retval;
        }

        internal Object GenericCache
        {
            get
            {
                return Cache.GenericCache;
            }

            set
            {
                Cache.GenericCache = value;
            }
        }

        internal bool DomainInitialized
        {
            get
            {
                return Cache.DomainInitialized;
            }

            set
            {
                Cache.DomainInitialized = value;
            }
        }

        internal unsafe static FieldInfo GetFieldInfo(IRuntimeFieldInfo fieldHandle)
        {
            return GetFieldInfo(RuntimeFieldHandle.GetApproxDeclaringType(fieldHandle), fieldHandle);
        }

        internal unsafe static FieldInfo GetFieldInfo(RuntimeType reflectedType, IRuntimeFieldInfo field)
        {
            RuntimeFieldHandleInternal fieldHandle = field.Value;
            if (reflectedType == null)
            {
                reflectedType = RuntimeFieldHandle.GetApproxDeclaringType(fieldHandle);
            }
            else
            {
                RuntimeType declaredType = RuntimeFieldHandle.GetApproxDeclaringType(fieldHandle);
                if (reflectedType != declaredType)
                {
                    if (!RuntimeFieldHandle.AcquiresContextFromThis(fieldHandle) || !RuntimeTypeHandle.CompareCanonicalHandles(declaredType, reflectedType))
                    {
                        throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_ResolveFieldHandle"), reflectedType.ToString(), declaredType.ToString()));
                    }
                }
            }

            FieldInfo retVal = reflectedType.Cache.GetField(fieldHandle);
            GC.KeepAlive(field);
            return retVal;
        }

        private unsafe static PropertyInfo GetPropertyInfo(RuntimeType reflectedType, int tkProperty)
        {
            RuntimePropertyInfo property = null;
            RuntimePropertyInfo[] candidates = reflectedType.Cache.GetPropertyList(MemberListType.All, null);
            for (int i = 0; i < candidates.Length; i++)
            {
                property = candidates[i];
                if (property.MetadataToken == tkProperty)
                    return property;
            }

            Contract.Assume(false, "Unreachable code");
            throw new SystemException();
        }

        private static void ThrowIfTypeNeverValidGenericArgument(RuntimeType type)
        {
            if (type.IsPointer || type.IsByRef || type == typeof (void))
                throw new ArgumentException(Environment.GetResourceString("Argument_NeverValidGenericArgument", type.ToString()));
        }

        internal static void SanityCheckGenericArguments(RuntimeType[] genericArguments, RuntimeType[] genericParamters)
        {
            if (genericArguments == null)
                throw new ArgumentNullException();
            Contract.EndContractBlock();
            for (int i = 0; i < genericArguments.Length; i++)
            {
                if (genericArguments[i] == null)
                    throw new ArgumentNullException();
                ThrowIfTypeNeverValidGenericArgument(genericArguments[i]);
            }

            if (genericArguments.Length != genericParamters.Length)
                throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughGenArguments", genericArguments.Length, genericParamters.Length));
        }

        internal static void ValidateGenericArguments(MemberInfo definition, RuntimeType[] genericArguments, Exception e)
        {
            RuntimeType[] typeContext = null;
            RuntimeType[] methodContext = null;
            RuntimeType[] genericParamters = null;
            if (definition is Type)
            {
                RuntimeType genericTypeDefinition = (RuntimeType)definition;
                genericParamters = genericTypeDefinition.GetGenericArgumentsInternal();
                typeContext = genericArguments;
            }
            else
            {
                RuntimeMethodInfo genericMethodDefinition = (RuntimeMethodInfo)definition;
                genericParamters = genericMethodDefinition.GetGenericArgumentsInternal();
                methodContext = genericArguments;
                RuntimeType declaringType = (RuntimeType)genericMethodDefinition.DeclaringType;
                if (declaringType != null)
                {
                    typeContext = declaringType.GetTypeHandleInternal().GetInstantiationInternal();
                }
            }

            for (int i = 0; i < genericArguments.Length; i++)
            {
                Type genericArgument = genericArguments[i];
                Type genericParameter = genericParamters[i];
                if (!RuntimeTypeHandle.SatisfiesConstraints(genericParameter.GetTypeHandleInternal().GetTypeChecked(), typeContext, methodContext, genericArgument.GetTypeHandleInternal().GetTypeChecked()))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_GenConstraintViolation", i.ToString(CultureInfo.CurrentCulture), genericArgument.ToString(), definition.ToString(), genericParameter.ToString()), e);
                }
            }
        }

        private static void SplitName(string fullname, out string name, out string ns)
        {
            name = null;
            ns = null;
            if (fullname == null)
                return;
            int nsDelimiter = fullname.LastIndexOf(".", StringComparison.Ordinal);
            if (nsDelimiter != -1)
            {
                ns = fullname.Substring(0, nsDelimiter);
                int nameLength = fullname.Length - ns.Length - 1;
                if (nameLength != 0)
                    name = fullname.Substring(nsDelimiter + 1, nameLength);
                else
                    name = "";
                Contract.Assert(fullname.Equals(ns + "." + name));
            }
            else
            {
                name = fullname;
            }
        }

        internal static BindingFlags FilterPreCalculate(bool isPublic, bool isInherited, bool isStatic)
        {
            BindingFlags bindingFlags = isPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            if (isInherited)
            {
                bindingFlags |= BindingFlags.DeclaredOnly;
                if (isStatic)
                {
                    bindingFlags |= BindingFlags.Static | BindingFlags.FlattenHierarchy;
                }
                else
                {
                    bindingFlags |= BindingFlags.Instance;
                }
            }
            else
            {
                if (isStatic)
                {
                    bindingFlags |= BindingFlags.Static;
                }
                else
                {
                    bindingFlags |= BindingFlags.Instance;
                }
            }

            return bindingFlags;
        }

        private static void FilterHelper(BindingFlags bindingFlags, ref string name, bool allowPrefixLookup, out bool prefixLookup, out bool ignoreCase, out MemberListType listType)
        {
            prefixLookup = false;
            ignoreCase = false;
            if (name != null)
            {
                if ((bindingFlags & BindingFlags.IgnoreCase) != 0)
                {
                    name = name.ToLower(CultureInfo.InvariantCulture);
                    ignoreCase = true;
                    listType = MemberListType.CaseInsensitive;
                }
                else
                {
                    listType = MemberListType.CaseSensitive;
                }

                if (allowPrefixLookup && name.EndsWith("*", StringComparison.Ordinal))
                {
                    name = name.Substring(0, name.Length - 1);
                    prefixLookup = true;
                    listType = MemberListType.All;
                }
            }
            else
            {
                listType = MemberListType.All;
            }
        }

        private static void FilterHelper(BindingFlags bindingFlags, ref string name, out bool ignoreCase, out MemberListType listType)
        {
            bool prefixLookup;
            FilterHelper(bindingFlags, ref name, false, out prefixLookup, out ignoreCase, out listType);
        }

        private static bool FilterApplyPrefixLookup(MemberInfo memberInfo, string name, bool ignoreCase)
        {
            Contract.Assert(name != null);
            if (ignoreCase)
            {
                if (!memberInfo.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            else
            {
                if (!memberInfo.Name.StartsWith(name, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static bool FilterApplyBase(MemberInfo memberInfo, BindingFlags bindingFlags, bool isPublic, bool isNonProtectedInternal, bool isStatic, string name, bool prefixLookup)
        {
            Contract.Requires(memberInfo != null);
            Contract.Requires(name == null || (bindingFlags & BindingFlags.IgnoreCase) == 0 || (name.ToLower(CultureInfo.InvariantCulture).Equals(name)));
            if (isPublic)
            {
                if ((bindingFlags & BindingFlags.Public) == 0)
                    return false;
            }
            else
            {
                if ((bindingFlags & BindingFlags.NonPublic) == 0)
                    return false;
            }

            bool isInherited = !Object.ReferenceEquals(memberInfo.DeclaringType, memberInfo.ReflectedType);
            if ((bindingFlags & BindingFlags.DeclaredOnly) != 0 && isInherited)
                return false;
            if (memberInfo.MemberType != MemberTypes.TypeInfo && memberInfo.MemberType != MemberTypes.NestedType)
            {
                if (isStatic)
                {
                    if ((bindingFlags & BindingFlags.FlattenHierarchy) == 0 && isInherited)
                        return false;
                    if ((bindingFlags & BindingFlags.Static) == 0)
                        return false;
                }
                else
                {
                    if ((bindingFlags & BindingFlags.Instance) == 0)
                        return false;
                }
            }

            if (prefixLookup == true)
            {
                if (!FilterApplyPrefixLookup(memberInfo, name, (bindingFlags & BindingFlags.IgnoreCase) != 0))
                    return false;
            }

            if (((bindingFlags & BindingFlags.DeclaredOnly) == 0) && isInherited && (isNonProtectedInternal) && ((bindingFlags & BindingFlags.NonPublic) != 0) && (!isStatic) && ((bindingFlags & BindingFlags.Instance) != 0))
            {
                MethodInfo methodInfo = memberInfo as MethodInfo;
                if (methodInfo == null)
                    return false;
                if (!methodInfo.IsVirtual && !methodInfo.IsAbstract)
                    return false;
            }

            return true;
        }

        private static bool FilterApplyType(Type type, BindingFlags bindingFlags, string name, bool prefixLookup, string ns)
        {
            Contract.Requires((object)type != null);
            Contract.Assert(type is RuntimeType);
            bool isPublic = type.IsNestedPublic || type.IsPublic;
            bool isStatic = false;
            if (!RuntimeType.FilterApplyBase(type, bindingFlags, isPublic, type.IsNestedAssembly, isStatic, name, prefixLookup))
                return false;
            if (ns != null && !type.Namespace.Equals(ns))
                return false;
            return true;
        }

        private static bool FilterApplyMethodInfo(RuntimeMethodInfo method, BindingFlags bindingFlags, CallingConventions callConv, Type[] argumentTypes)
        {
            return FilterApplyMethodBase(method, method.BindingFlags, bindingFlags, callConv, argumentTypes);
        }

        private static bool FilterApplyConstructorInfo(RuntimeConstructorInfo constructor, BindingFlags bindingFlags, CallingConventions callConv, Type[] argumentTypes)
        {
            return FilterApplyMethodBase(constructor, constructor.BindingFlags, bindingFlags, callConv, argumentTypes);
        }

        private static bool FilterApplyMethodBase(MethodBase methodBase, BindingFlags methodFlags, BindingFlags bindingFlags, CallingConventions callConv, Type[] argumentTypes)
        {
            Contract.Requires(methodBase != null);
            bindingFlags ^= BindingFlags.DeclaredOnly;
            if ((bindingFlags & methodFlags) != methodFlags)
                return false;
            if ((callConv & CallingConventions.Any) == 0)
            {
                if ((callConv & CallingConventions.VarArgs) != 0 && (methodBase.CallingConvention & CallingConventions.VarArgs) == 0)
                    return false;
                if ((callConv & CallingConventions.Standard) != 0 && (methodBase.CallingConvention & CallingConventions.Standard) == 0)
                    return false;
            }

            if (argumentTypes != null)
            {
                ParameterInfo[] parameterInfos = methodBase.GetParametersNoCopy();
                if (argumentTypes.Length != parameterInfos.Length)
                {
                    if ((bindingFlags & (BindingFlags.InvokeMethod | BindingFlags.CreateInstance | BindingFlags.GetProperty | BindingFlags.SetProperty)) == 0)
                        return false;
                    bool testForParamArray = false;
                    bool excessSuppliedArguments = argumentTypes.Length > parameterInfos.Length;
                    if (excessSuppliedArguments)
                    {
                        if ((methodBase.CallingConvention & CallingConventions.VarArgs) == 0)
                        {
                            testForParamArray = true;
                        }
                        else
                        {
                            Contract.Assert((callConv & CallingConventions.VarArgs) != 0);
                        }
                    }
                    else
                    {
                        if ((bindingFlags & BindingFlags.OptionalParamBinding) == 0)
                        {
                            testForParamArray = true;
                        }
                        else
                        {
                            if (!parameterInfos[argumentTypes.Length].IsOptional)
                                testForParamArray = true;
                        }
                    }

                    if (testForParamArray)
                    {
                        if (parameterInfos.Length == 0)
                            return false;
                        bool shortByMoreThanOneSuppliedArgument = argumentTypes.Length < parameterInfos.Length - 1;
                        if (shortByMoreThanOneSuppliedArgument)
                            return false;
                        ParameterInfo lastParameter = parameterInfos[parameterInfos.Length - 1];
                        if (!lastParameter.ParameterType.IsArray)
                            return false;
                        if (!lastParameter.IsDefined(typeof (ParamArrayAttribute), false))
                            return false;
                    }
                }
                else
                {
                    if ((bindingFlags & BindingFlags.ExactBinding) != 0)
                    {
                        if ((bindingFlags & (BindingFlags.InvokeMethod)) == 0)
                        {
                            for (int i = 0; i < parameterInfos.Length; i++)
                            {
                                if ((object)argumentTypes[i] != null && !Object.ReferenceEquals(parameterInfos[i].ParameterType, argumentTypes[i]))
                                    return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private object m_keepalive;
        private IntPtr m_cache;
        internal IntPtr m_handle;
        private INVOCATION_FLAGS m_invocationFlags;
        internal bool IsNonW8PFrameworkAPI()
        {
            if (IsGenericParameter)
                return false;
            if (HasElementType)
                return ((RuntimeType)GetElementType()).IsNonW8PFrameworkAPI();
            if (IsSimpleTypeNonW8PFrameworkAPI())
                return true;
            if (IsGenericType && !IsGenericTypeDefinition)
            {
                foreach (Type t in GetGenericArguments())
                {
                    if (((RuntimeType)t).IsNonW8PFrameworkAPI())
                        return true;
                }
            }

            return false;
        }

        private bool IsSimpleTypeNonW8PFrameworkAPI()
        {
            RuntimeAssembly rtAssembly = GetRuntimeAssembly();
            if (rtAssembly.IsFrameworkAssembly())
            {
                int ctorToken = rtAssembly.InvocableAttributeCtorToken;
                if (System.Reflection.MetadataToken.IsNullToken(ctorToken) || !CustomAttribute.IsAttributeDefined(GetRuntimeModule(), MetadataToken, ctorToken))
                    return true;
            }

            return false;
        }

        internal INVOCATION_FLAGS InvocationFlags
        {
            get
            {
                if ((m_invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) == 0)
                {
                    INVOCATION_FLAGS invocationFlags = INVOCATION_FLAGS.INVOCATION_FLAGS_UNKNOWN;
                    if (AppDomain.ProfileAPICheck && IsNonW8PFrameworkAPI())
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API;
                    m_invocationFlags = invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
                }

                return m_invocationFlags;
            }
        }

        internal static readonly RuntimeType ValueType = (RuntimeType)typeof (System.ValueType);
        internal static readonly RuntimeType EnumType = (RuntimeType)typeof (System.Enum);
        private static readonly RuntimeType ObjectType = (RuntimeType)typeof (System.Object);
        private static readonly RuntimeType StringType = (RuntimeType)typeof (System.String);
        private static readonly RuntimeType DelegateType = (RuntimeType)typeof (System.Delegate);
        private static Type[] s_SICtorParamTypes;
        internal RuntimeType()
        {
            throw new NotSupportedException();
        }

        internal override bool CacheEquals(object o)
        {
            RuntimeType m = o as RuntimeType;
            if (m == null)
                return false;
            return m.m_handle.Equals(m_handle);
        }

        private RuntimeTypeCache Cache
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_cache.IsNull())
                {
                    IntPtr newgcHandle = new RuntimeTypeHandle(this).GetGCHandle(GCHandleType.WeakTrackResurrection);
                    IntPtr gcHandle = Interlocked.CompareExchange(ref m_cache, newgcHandle, (IntPtr)0);
                    if (!gcHandle.IsNull() && !IsCollectible())
                        GCHandle.InternalFree(newgcHandle);
                }

                RuntimeTypeCache cache = GCHandle.InternalGet(m_cache) as RuntimeTypeCache;
                if (cache == null)
                {
                    cache = new RuntimeTypeCache(this);
                    RuntimeTypeCache existingCache = GCHandle.InternalCompareExchange(m_cache, cache, null, false) as RuntimeTypeCache;
                    if (existingCache != null)
                        cache = existingCache;
                }

                Contract.Assert(cache != null);
                return cache;
            }
        }

        internal bool IsSpecialSerializableType()
        {
            RuntimeType rt = this;
            do
            {
                if (rt == RuntimeType.DelegateType || rt == RuntimeType.EnumType)
                    return true;
                rt = rt.GetBaseType();
            }
            while (rt != null);
            return false;
        }

        private string GetDefaultMemberName()
        {
            return Cache.GetDefaultMemberName();
        }

        private ListBuilder<MethodInfo> GetMethodCandidates(String name, BindingFlags bindingAttr, CallingConventions callConv, Type[] types, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);
            RuntimeMethodInfo[] cache = Cache.GetMethodList(listType, name);
            ListBuilder<MethodInfo> candidates = new ListBuilder<MethodInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeMethodInfo methodInfo = cache[i];
                if (FilterApplyMethodInfo(methodInfo, bindingAttr, callConv, types) && (!prefixLookup || RuntimeType.FilterApplyPrefixLookup(methodInfo, name, ignoreCase)))
                {
                    candidates.Add(methodInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<ConstructorInfo> GetConstructorCandidates(string name, BindingFlags bindingAttr, CallingConventions callConv, Type[] types, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);
            RuntimeConstructorInfo[] cache = Cache.GetConstructorList(listType, name);
            ListBuilder<ConstructorInfo> candidates = new ListBuilder<ConstructorInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeConstructorInfo constructorInfo = cache[i];
                if (FilterApplyConstructorInfo(constructorInfo, bindingAttr, callConv, types) && (!prefixLookup || RuntimeType.FilterApplyPrefixLookup(constructorInfo, name, ignoreCase)))
                {
                    candidates.Add(constructorInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<PropertyInfo> GetPropertyCandidates(String name, BindingFlags bindingAttr, Type[] types, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);
            IReadOnlyList<RuntimePropertyInfo> ambiguousProperties = null;
            RuntimePropertyInfo[] cache = Cache.GetPropertyList(listType, name, out ambiguousProperties);
            bindingAttr ^= BindingFlags.DeclaredOnly;
            ListBuilder<PropertyInfo> candidates = new ListBuilder<PropertyInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimePropertyInfo propertyInfo = cache[i];
                if ((bindingAttr & propertyInfo.BindingFlags) == propertyInfo.BindingFlags && (!prefixLookup || RuntimeType.FilterApplyPrefixLookup(propertyInfo, name, ignoreCase)) && (types == null || (propertyInfo.GetIndexParameters().Length == types.Length)))
                {
                    candidates.Add(propertyInfo);
                }
            }

            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8 && candidates.Count > 1 && ambiguousProperties != null && ambiguousProperties.Count > 0)
            {
                return PruneAmbiguousProperties(candidates, ambiguousProperties);
            }

            return candidates;
        }

        private ListBuilder<PropertyInfo> PruneAmbiguousProperties(ListBuilder<PropertyInfo> candidates, IReadOnlyList<RuntimePropertyInfo> ambiguousProperties)
        {
            Contract.Assert(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8);
            Contract.Assert(candidates.Count > 1);
            Contract.Assert(ambiguousProperties != null && ambiguousProperties.Count > 0);
            ListBuilder<PropertyInfo> newCandidates = candidates;
            int countRemoved = 0;
            lock (ambiguousProperties)
            {
                for (int outerIndex = 0; outerIndex < ambiguousProperties.Count; ++outerIndex)
                {
                    for (int innerIndex = 0; innerIndex < candidates.Count; ++innerIndex)
                    {
                        if (candidates[innerIndex] != null && candidates[innerIndex] == ambiguousProperties[outerIndex])
                        {
                            candidates[innerIndex] = null;
                            ++countRemoved;
                        }
                    }
                }
            }

            Contract.Assert(countRemoved > 0);
            if (countRemoved > 0)
            {
                newCandidates = new ListBuilder<PropertyInfo>(candidates.Count - countRemoved);
                for (int index = 0; index < candidates.Count; ++index)
                {
                    if (candidates[index] != null)
                        newCandidates.Add(candidates[index]);
                }

                Contract.Assert(newCandidates.Count == (candidates.Count - countRemoved));
            }

            return newCandidates;
        }

        private ListBuilder<EventInfo> GetEventCandidates(String name, BindingFlags bindingAttr, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);
            RuntimeEventInfo[] cache = Cache.GetEventList(listType, name);
            bindingAttr ^= BindingFlags.DeclaredOnly;
            ListBuilder<EventInfo> candidates = new ListBuilder<EventInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeEventInfo eventInfo = cache[i];
                if ((bindingAttr & eventInfo.BindingFlags) == eventInfo.BindingFlags && (!prefixLookup || RuntimeType.FilterApplyPrefixLookup(eventInfo, name, ignoreCase)))
                {
                    candidates.Add(eventInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<FieldInfo> GetFieldCandidates(String name, BindingFlags bindingAttr, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            MemberListType listType;
            RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);
            RuntimeFieldInfo[] cache = Cache.GetFieldList(listType, name);
            bindingAttr ^= BindingFlags.DeclaredOnly;
            ListBuilder<FieldInfo> candidates = new ListBuilder<FieldInfo>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeFieldInfo fieldInfo = cache[i];
                if ((bindingAttr & fieldInfo.BindingFlags) == fieldInfo.BindingFlags && (!prefixLookup || FilterApplyPrefixLookup(fieldInfo, name, ignoreCase)))
                {
                    candidates.Add(fieldInfo);
                }
            }

            return candidates;
        }

        private ListBuilder<Type> GetNestedTypeCandidates(String fullname, BindingFlags bindingAttr, bool allowPrefixLookup)
        {
            bool prefixLookup, ignoreCase;
            bindingAttr &= ~BindingFlags.Static;
            string name, ns;
            MemberListType listType;
            SplitName(fullname, out name, out ns);
            RuntimeType.FilterHelper(bindingAttr, ref name, allowPrefixLookup, out prefixLookup, out ignoreCase, out listType);
            RuntimeType[] cache = Cache.GetNestedTypeList(listType, name);
            ListBuilder<Type> candidates = new ListBuilder<Type>(cache.Length);
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType nestedClass = cache[i];
                if (RuntimeType.FilterApplyType(nestedClass, bindingAttr, name, prefixLookup, ns))
                {
                    candidates.Add(nestedClass);
                }
            }

            return candidates;
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return GetMethodCandidates(null, bindingAttr, CallingConventions.Any, null, false).ToArray();
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return GetConstructorCandidates(null, bindingAttr, CallingConventions.Any, null, false).ToArray();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return GetPropertyCandidates(null, bindingAttr, null, false).ToArray();
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return GetEventCandidates(null, bindingAttr, false).ToArray();
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return GetFieldCandidates(null, bindingAttr, false).ToArray();
        }

        public override Type[] GetInterfaces()
        {
            RuntimeType[] candidates = this.Cache.GetInterfaceList(MemberListType.All, null);
            Type[] interfaces = new Type[candidates.Length];
            for (int i = 0; i < candidates.Length; i++)
                JitHelpers.UnsafeSetArrayElement(interfaces, i, candidates[i]);
            return interfaces;
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return GetNestedTypeCandidates(null, bindingAttr, false).ToArray();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            ListBuilder<MethodInfo> methods = GetMethodCandidates(null, bindingAttr, CallingConventions.Any, null, false);
            ListBuilder<ConstructorInfo> constructors = GetConstructorCandidates(null, bindingAttr, CallingConventions.Any, null, false);
            ListBuilder<PropertyInfo> properties = GetPropertyCandidates(null, bindingAttr, null, false);
            ListBuilder<EventInfo> events = GetEventCandidates(null, bindingAttr, false);
            ListBuilder<FieldInfo> fields = GetFieldCandidates(null, bindingAttr, false);
            ListBuilder<Type> nestedTypes = GetNestedTypeCandidates(null, bindingAttr, false);
            MemberInfo[] members = new MemberInfo[methods.Count + constructors.Count + properties.Count + events.Count + fields.Count + nestedTypes.Count];
            int i = 0;
            methods.CopyTo(members, i);
            i += methods.Count;
            constructors.CopyTo(members, i);
            i += constructors.Count;
            properties.CopyTo(members, i);
            i += properties.Count;
            events.CopyTo(members, i);
            i += events.Count;
            fields.CopyTo(members, i);
            i += fields.Count;
            nestedTypes.CopyTo(members, i);
            i += nestedTypes.Count;
            Contract.Assert(i == members.Length);
            return members;
        }

        public override InterfaceMapping GetInterfaceMap(Type ifaceType)
        {
            if (IsGenericParameter)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_GenericParameter"));
            if ((object)ifaceType == null)
                throw new ArgumentNullException("ifaceType");
            Contract.EndContractBlock();
            RuntimeType ifaceRtType = ifaceType as RuntimeType;
            if (ifaceRtType == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "ifaceType");
            RuntimeTypeHandle ifaceRtTypeHandle = ifaceRtType.GetTypeHandleInternal();
            GetTypeHandleInternal().VerifyInterfaceIsImplemented(ifaceRtTypeHandle);
            Contract.Assert(ifaceType.IsInterface);
            Contract.Assert(!IsInterface);
            if (IsSzArray && ifaceType.IsGenericType)
                throw new ArgumentException(Environment.GetResourceString("Argument_ArrayGetInterfaceMap"));
            int ifaceInstanceMethodCount = RuntimeTypeHandle.GetNumVirtuals(ifaceRtType);
            InterfaceMapping im;
            im.InterfaceType = ifaceType;
            im.TargetType = this;
            im.InterfaceMethods = new MethodInfo[ifaceInstanceMethodCount];
            im.TargetMethods = new MethodInfo[ifaceInstanceMethodCount];
            for (int i = 0; i < ifaceInstanceMethodCount; i++)
            {
                RuntimeMethodHandleInternal ifaceRtMethodHandle = RuntimeTypeHandle.GetMethodAt(ifaceRtType, i);
                MethodBase ifaceMethodBase = RuntimeType.GetMethodBase(ifaceRtType, ifaceRtMethodHandle);
                Contract.Assert(ifaceMethodBase is RuntimeMethodInfo);
                im.InterfaceMethods[i] = (MethodInfo)ifaceMethodBase;
                int slot = GetTypeHandleInternal().GetInterfaceMethodImplementationSlot(ifaceRtTypeHandle, ifaceRtMethodHandle);
                if (slot == -1)
                    continue;
                RuntimeMethodHandleInternal classRtMethodHandle = RuntimeTypeHandle.GetMethodAt(this, slot);
                MethodBase rtTypeMethodBase = RuntimeType.GetMethodBase(this, classRtMethodHandle);
                Contract.Assert(rtTypeMethodBase == null || rtTypeMethodBase is RuntimeMethodInfo);
                im.TargetMethods[i] = (MethodInfo)rtTypeMethodBase;
            }

            return im;
        }

        protected override MethodInfo GetMethodImpl(String name, BindingFlags bindingAttr, Binder binder, CallingConventions callConv, Type[] types, ParameterModifier[] modifiers)
        {
            ListBuilder<MethodInfo> candidates = GetMethodCandidates(name, bindingAttr, callConv, types, false);
            if (candidates.Count == 0)
                return null;
            if (types == null || types.Length == 0)
            {
                MethodInfo firstCandidate = candidates[0];
                if (candidates.Count == 1)
                {
                    return firstCandidate;
                }
                else if (types == null)
                {
                    for (int j = 1; j < candidates.Count; j++)
                    {
                        MethodInfo methodInfo = candidates[j];
                        if (!System.DefaultBinder.CompareMethodSigAndName(methodInfo, firstCandidate))
                        {
                            throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
                        }
                    }

                    return System.DefaultBinder.FindMostDerivedNewSlotMeth(candidates.ToArray(), candidates.Count) as MethodInfo;
                }
            }

            if (binder == null)
                binder = DefaultBinder;
            return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as MethodInfo;
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            ListBuilder<ConstructorInfo> candidates = GetConstructorCandidates(null, bindingAttr, CallingConventions.Any, types, false);
            if (candidates.Count == 0)
                return null;
            if (types.Length == 0 && candidates.Count == 1)
            {
                ConstructorInfo firstCandidate = candidates[0];
                ParameterInfo[] parameters = firstCandidate.GetParametersNoCopy();
                if (parameters == null || parameters.Length == 0)
                {
                    return firstCandidate;
                }
            }

            if ((bindingAttr & BindingFlags.ExactBinding) != 0)
                return System.DefaultBinder.ExactBinding(candidates.ToArray(), types, modifiers) as ConstructorInfo;
            if (binder == null)
                binder = DefaultBinder;
            return binder.SelectMethod(bindingAttr, candidates.ToArray(), types, modifiers) as ConstructorInfo;
        }

        protected override PropertyInfo GetPropertyImpl(String name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            if (name == null)
                throw new ArgumentNullException();
            Contract.EndContractBlock();
            ListBuilder<PropertyInfo> candidates = GetPropertyCandidates(name, bindingAttr, types, false);
            if (candidates.Count == 0)
                return null;
            if (types == null || types.Length == 0)
            {
                if (candidates.Count == 1)
                {
                    PropertyInfo firstCandidate = candidates[0];
                    if ((object)returnType != null && !returnType.IsEquivalentTo(firstCandidate.PropertyType))
                        return null;
                    return firstCandidate;
                }
                else
                {
                    if ((object)returnType == null)
                        throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
                }
            }

            if ((bindingAttr & BindingFlags.ExactBinding) != 0)
                return System.DefaultBinder.ExactPropertyBinding(candidates.ToArray(), returnType, types, modifiers);
            if (binder == null)
                binder = DefaultBinder;
            return binder.SelectProperty(bindingAttr, candidates.ToArray(), returnType, types, modifiers);
        }

        public override EventInfo GetEvent(String name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException();
            Contract.EndContractBlock();
            bool ignoreCase;
            MemberListType listType;
            RuntimeType.FilterHelper(bindingAttr, ref name, out ignoreCase, out listType);
            RuntimeEventInfo[] cache = Cache.GetEventList(listType, name);
            EventInfo match = null;
            bindingAttr ^= BindingFlags.DeclaredOnly;
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeEventInfo eventInfo = cache[i];
                if ((bindingAttr & eventInfo.BindingFlags) == eventInfo.BindingFlags)
                {
                    if (match != null)
                        throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
                    match = eventInfo;
                }
            }

            return match;
        }

        public override FieldInfo GetField(String name, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException();
            Contract.EndContractBlock();
            bool ignoreCase;
            MemberListType listType;
            RuntimeType.FilterHelper(bindingAttr, ref name, out ignoreCase, out listType);
            RuntimeFieldInfo[] cache = Cache.GetFieldList(listType, name);
            FieldInfo match = null;
            bindingAttr ^= BindingFlags.DeclaredOnly;
            bool multipleStaticFieldMatches = false;
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeFieldInfo fieldInfo = cache[i];
                if ((bindingAttr & fieldInfo.BindingFlags) == fieldInfo.BindingFlags)
                {
                    if (match != null)
                    {
                        if (Object.ReferenceEquals(fieldInfo.DeclaringType, match.DeclaringType))
                            throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
                        if ((match.DeclaringType.IsInterface == true) && (fieldInfo.DeclaringType.IsInterface == true))
                            multipleStaticFieldMatches = true;
                    }

                    if (match == null || fieldInfo.DeclaringType.IsSubclassOf(match.DeclaringType) || match.DeclaringType.IsInterface)
                        match = fieldInfo;
                }
            }

            if (multipleStaticFieldMatches && match.DeclaringType.IsInterface)
                throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
            return match;
        }

        public override Type GetInterface(String fullname, bool ignoreCase)
        {
            if (fullname == null)
                throw new ArgumentNullException();
            Contract.EndContractBlock();
            BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.NonPublic;
            bindingAttr &= ~BindingFlags.Static;
            if (ignoreCase)
                bindingAttr |= BindingFlags.IgnoreCase;
            string name, ns;
            MemberListType listType;
            SplitName(fullname, out name, out ns);
            RuntimeType.FilterHelper(bindingAttr, ref name, out ignoreCase, out listType);
            RuntimeType[] cache = Cache.GetInterfaceList(listType, name);
            RuntimeType match = null;
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType iface = cache[i];
                if (RuntimeType.FilterApplyType(iface, bindingAttr, name, false, ns))
                {
                    if (match != null)
                        throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
                    match = iface;
                }
            }

            return match;
        }

        public override Type GetNestedType(String fullname, BindingFlags bindingAttr)
        {
            if (fullname == null)
                throw new ArgumentNullException();
            Contract.EndContractBlock();
            bool ignoreCase;
            bindingAttr &= ~BindingFlags.Static;
            string name, ns;
            MemberListType listType;
            SplitName(fullname, out name, out ns);
            RuntimeType.FilterHelper(bindingAttr, ref name, out ignoreCase, out listType);
            RuntimeType[] cache = Cache.GetNestedTypeList(listType, name);
            RuntimeType match = null;
            for (int i = 0; i < cache.Length; i++)
            {
                RuntimeType nestedType = cache[i];
                if (RuntimeType.FilterApplyType(nestedType, bindingAttr, name, false, ns))
                {
                    if (match != null)
                        throw new AmbiguousMatchException(Environment.GetResourceString("Arg_AmbiguousMatchException"));
                    match = nestedType;
                }
            }

            return match;
        }

        public override MemberInfo[] GetMember(String name, MemberTypes type, BindingFlags bindingAttr)
        {
            if (name == null)
                throw new ArgumentNullException();
            Contract.EndContractBlock();
            ListBuilder<MethodInfo> methods = new ListBuilder<MethodInfo>();
            ListBuilder<ConstructorInfo> constructors = new ListBuilder<ConstructorInfo>();
            ListBuilder<PropertyInfo> properties = new ListBuilder<PropertyInfo>();
            ListBuilder<EventInfo> events = new ListBuilder<EventInfo>();
            ListBuilder<FieldInfo> fields = new ListBuilder<FieldInfo>();
            ListBuilder<Type> nestedTypes = new ListBuilder<Type>();
            int totalCount = 0;
            if ((type & MemberTypes.Method) != 0)
            {
                methods = GetMethodCandidates(name, bindingAttr, CallingConventions.Any, null, true);
                if (type == MemberTypes.Method)
                    return methods.ToArray();
                totalCount += methods.Count;
            }

            if ((type & MemberTypes.Constructor) != 0)
            {
                constructors = GetConstructorCandidates(name, bindingAttr, CallingConventions.Any, null, true);
                if (type == MemberTypes.Constructor)
                    return constructors.ToArray();
                totalCount += constructors.Count;
            }

            if ((type & MemberTypes.Property) != 0)
            {
                properties = GetPropertyCandidates(name, bindingAttr, null, true);
                if (type == MemberTypes.Property)
                    return properties.ToArray();
                totalCount += properties.Count;
            }

            if ((type & MemberTypes.Event) != 0)
            {
                events = GetEventCandidates(name, bindingAttr, true);
                if (type == MemberTypes.Event)
                    return events.ToArray();
                totalCount += events.Count;
            }

            if ((type & MemberTypes.Field) != 0)
            {
                fields = GetFieldCandidates(name, bindingAttr, true);
                if (type == MemberTypes.Field)
                    return fields.ToArray();
                totalCount += fields.Count;
            }

            if ((type & (MemberTypes.NestedType | MemberTypes.TypeInfo)) != 0)
            {
                nestedTypes = GetNestedTypeCandidates(name, bindingAttr, true);
                if (type == MemberTypes.NestedType || type == MemberTypes.TypeInfo)
                    return nestedTypes.ToArray();
                totalCount += nestedTypes.Count;
            }

            MemberInfo[] compressMembers = (type == (MemberTypes.Method | MemberTypes.Constructor)) ? new MethodBase[totalCount] : new MemberInfo[totalCount];
            int i = 0;
            methods.CopyTo(compressMembers, i);
            i += methods.Count;
            constructors.CopyTo(compressMembers, i);
            i += constructors.Count;
            properties.CopyTo(compressMembers, i);
            i += properties.Count;
            events.CopyTo(compressMembers, i);
            i += events.Count;
            fields.CopyTo(compressMembers, i);
            i += fields.Count;
            nestedTypes.CopyTo(compressMembers, i);
            i += nestedTypes.Count;
            Contract.Assert(i == compressMembers.Length);
            return compressMembers;
        }

        public override Module Module
        {
            get
            {
                return GetRuntimeModule();
            }
        }

        internal RuntimeModule GetRuntimeModule()
        {
            return RuntimeTypeHandle.GetModule(this);
        }

        public override Assembly Assembly
        {
            get
            {
                return GetRuntimeAssembly();
            }
        }

        internal RuntimeAssembly GetRuntimeAssembly()
        {
            return RuntimeTypeHandle.GetAssembly(this);
        }

        public override RuntimeTypeHandle TypeHandle
        {
            get
            {
                return new RuntimeTypeHandle(this);
            }
        }

        internal sealed override RuntimeTypeHandle GetTypeHandleInternal()
        {
            return new RuntimeTypeHandle(this);
        }

        internal bool IsCollectible()
        {
            return RuntimeTypeHandle.IsCollectible(GetTypeHandleInternal());
        }

        protected override TypeCode GetTypeCodeImpl()
        {
            TypeCode typeCode = Cache.TypeCode;
            if (typeCode != TypeCode.Empty)
                return typeCode;
            CorElementType corElementType = RuntimeTypeHandle.GetCorElementType(this);
            switch (corElementType)
            {
                case CorElementType.Boolean:
                    typeCode = TypeCode.Boolean;
                    break;
                case CorElementType.Char:
                    typeCode = TypeCode.Char;
                    break;
                case CorElementType.I1:
                    typeCode = TypeCode.SByte;
                    break;
                case CorElementType.U1:
                    typeCode = TypeCode.Byte;
                    break;
                case CorElementType.I2:
                    typeCode = TypeCode.Int16;
                    break;
                case CorElementType.U2:
                    typeCode = TypeCode.UInt16;
                    break;
                case CorElementType.I4:
                    typeCode = TypeCode.Int32;
                    break;
                case CorElementType.U4:
                    typeCode = TypeCode.UInt32;
                    break;
                case CorElementType.I8:
                    typeCode = TypeCode.Int64;
                    break;
                case CorElementType.U8:
                    typeCode = TypeCode.UInt64;
                    break;
                case CorElementType.R4:
                    typeCode = TypeCode.Single;
                    break;
                case CorElementType.R8:
                    typeCode = TypeCode.Double;
                    break;
                case CorElementType.String:
                    typeCode = TypeCode.String;
                    break;
                case CorElementType.ValueType:
                    if (this == Convert.ConvertTypes[(int)TypeCode.Decimal])
                        typeCode = TypeCode.Decimal;
                    else if (this == Convert.ConvertTypes[(int)TypeCode.DateTime])
                        typeCode = TypeCode.DateTime;
                    else if (this.IsEnum)
                        typeCode = Type.GetTypeCode(Enum.GetUnderlyingType(this));
                    else
                        typeCode = TypeCode.Object;
                    break;
                default:
                    if (this == Convert.ConvertTypes[(int)TypeCode.DBNull])
                        typeCode = TypeCode.DBNull;
                    else if (this == Convert.ConvertTypes[(int)TypeCode.String])
                        typeCode = TypeCode.String;
                    else
                        typeCode = TypeCode.Object;
                    break;
            }

            Cache.TypeCode = typeCode;
            return typeCode;
        }

        public override MethodBase DeclaringMethod
        {
            get
            {
                if (!IsGenericParameter)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
                Contract.EndContractBlock();
                IRuntimeMethodInfo declaringMethod = RuntimeTypeHandle.GetDeclaringMethod(this);
                if (declaringMethod == null)
                    return null;
                return GetMethodBase(RuntimeMethodHandle.GetDeclaringType(declaringMethod), declaringMethod);
            }
        }

        public override bool IsInstanceOfType(Object o)
        {
            return RuntimeTypeHandle.IsInstanceOfType(this, o);
        }

        public override bool IsSubclassOf(Type type)
        {
            if ((object)type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();
            RuntimeType rtType = type as RuntimeType;
            if (rtType == null)
                return false;
            RuntimeType baseType = GetBaseType();
            while (baseType != null)
            {
                if (baseType == rtType)
                    return true;
                baseType = baseType.GetBaseType();
            }

            if (rtType == RuntimeType.ObjectType && rtType != this)
                return true;
            return false;
        }

        public override bool IsAssignableFrom(System.Reflection.TypeInfo typeInfo)
        {
            if (typeInfo == null)
                return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        public override bool IsAssignableFrom(Type c)
        {
            if ((object)c == null)
                return false;
            if (Object.ReferenceEquals(c, this))
                return true;
            RuntimeType fromType = c.UnderlyingSystemType as RuntimeType;
            if (fromType != null)
            {
                return RuntimeTypeHandle.CanCastTo(fromType, this);
            }

            if (c is System.Reflection.Emit.TypeBuilder)
            {
                if (c.IsSubclassOf(this))
                    return true;
                if (this.IsInterface)
                {
                    return c.ImplementInterface(this);
                }
                else if (this.IsGenericParameter)
                {
                    Type[] constraints = GetGenericParameterConstraints();
                    for (int i = 0; i < constraints.Length; i++)
                        if (!constraints[i].IsAssignableFrom(c))
                            return false;
                    return true;
                }
            }

            return false;
        }

        public override Type BaseType
        {
            get
            {
                return GetBaseType();
            }
        }

        private RuntimeType GetBaseType()
        {
            if (IsInterface)
                return null;
            if (RuntimeTypeHandle.IsGenericVariable(this))
            {
                Type[] constraints = GetGenericParameterConstraints();
                RuntimeType baseType = RuntimeType.ObjectType;
                for (int i = 0; i < constraints.Length; i++)
                {
                    RuntimeType constraint = (RuntimeType)constraints[i];
                    if (constraint.IsInterface)
                        continue;
                    if (constraint.IsGenericParameter)
                    {
                        GenericParameterAttributes special;
                        special = constraint.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
                        if ((special & GenericParameterAttributes.ReferenceTypeConstraint) == 0 && (special & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
                            continue;
                    }

                    baseType = constraint;
                }

                if (baseType == RuntimeType.ObjectType)
                {
                    GenericParameterAttributes special;
                    special = GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask;
                    if ((special & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
                        baseType = RuntimeType.ValueType;
                }

                return baseType;
            }

            return RuntimeTypeHandle.GetBaseType(this);
        }

        public override Type UnderlyingSystemType
        {
            get
            {
                return this;
            }
        }

        public override String FullName
        {
            get
            {
                return GetCachedName(TypeNameKind.FullName);
            }
        }

        public override String AssemblyQualifiedName
        {
            get
            {
                string fullname = FullName;
                if (fullname == null)
                    return null;
                return Assembly.CreateQualifiedName(this.Assembly.FullName, fullname);
            }
        }

        public override String Namespace
        {
            get
            {
                string ns = Cache.GetNameSpace();
                if (ns == null || ns.Length == 0)
                    return null;
                return ns;
            }
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return RuntimeTypeHandle.GetAttributes(this);
        }

        public override Guid GUID
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Guid result = new Guid();
                GetGUID(ref result);
                return result;
            }
        }

        private extern void GetGUID(ref Guid result);
        protected override bool IsContextfulImpl()
        {
            return RuntimeTypeHandle.IsContextful(this);
        }

        protected override bool IsByRefImpl()
        {
            return RuntimeTypeHandle.IsByRef(this);
        }

        protected override bool IsPrimitiveImpl()
        {
            return RuntimeTypeHandle.IsPrimitive(this);
        }

        protected override bool IsPointerImpl()
        {
            return RuntimeTypeHandle.IsPointer(this);
        }

        protected override bool IsCOMObjectImpl()
        {
            return RuntimeTypeHandle.IsComObject(this, false);
        }

        internal override bool IsWindowsRuntimeObjectImpl()
        {
            return IsWindowsRuntimeObjectType(this);
        }

        internal override bool IsExportedToWindowsRuntimeImpl()
        {
            return IsTypeExportedToWindowsRuntime(this);
        }

        private static extern bool IsWindowsRuntimeObjectType(RuntimeType type);
        private static extern bool IsTypeExportedToWindowsRuntime(RuntimeType type);
        internal override bool HasProxyAttributeImpl()
        {
            return RuntimeTypeHandle.HasProxyAttribute(this);
        }

        internal bool IsDelegate()
        {
            return GetBaseType() == typeof (System.MulticastDelegate);
        }

        protected override bool IsValueTypeImpl()
        {
            if (this == typeof (ValueType) || this == typeof (Enum))
                return false;
            return IsSubclassOf(typeof (ValueType));
        }

        protected override bool HasElementTypeImpl()
        {
            return RuntimeTypeHandle.HasElementType(this);
        }

        public override GenericParameterAttributes GenericParameterAttributes
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (!IsGenericParameter)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
                Contract.EndContractBlock();
                GenericParameterAttributes attributes;
                RuntimeTypeHandle.GetMetadataImport(this).GetGenericParamProps(MetadataToken, out attributes);
                return attributes;
            }
        }

        public override bool IsSecurityCritical
        {
            get
            {
                return new RuntimeTypeHandle(this).IsSecurityCritical();
            }
        }

        public override bool IsSecuritySafeCritical
        {
            get
            {
                return new RuntimeTypeHandle(this).IsSecuritySafeCritical();
            }
        }

        public override bool IsSecurityTransparent
        {
            get
            {
                return new RuntimeTypeHandle(this).IsSecurityTransparent();
            }
        }

        internal override bool IsSzArray
        {
            get
            {
                return RuntimeTypeHandle.IsSzArray(this);
            }
        }

        protected override bool IsArrayImpl()
        {
            return RuntimeTypeHandle.IsArray(this);
        }

        public override int GetArrayRank()
        {
            if (!IsArrayImpl())
                throw new ArgumentException(Environment.GetResourceString("Argument_HasToBeArrayClass"));
            return RuntimeTypeHandle.GetArrayRank(this);
        }

        public override Type GetElementType()
        {
            return RuntimeTypeHandle.GetElementType(this);
        }

        public override string[] GetEnumNames()
        {
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            String[] ret = Enum.InternalGetNames(this);
            String[] retVal = new String[ret.Length];
            Array.Copy(ret, retVal, ret.Length);
            return retVal;
        }

        public override Array GetEnumValues()
        {
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            ulong[] values = Enum.InternalGetValues(this);
            Array ret = Array.UnsafeCreateInstance(this, values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                Object val = Enum.ToObject(this, values[i]);
                ret.SetValue(val, i);
            }

            return ret;
        }

        public override Type GetEnumUnderlyingType()
        {
            if (!IsEnum)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnum"), "enumType");
            Contract.EndContractBlock();
            return Enum.InternalGetUnderlyingType(this);
        }

        public override bool IsEnumDefined(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();
            RuntimeType valueType = (RuntimeType)value.GetType();
            if (valueType.IsEnum)
            {
                if (!valueType.IsEquivalentTo(this))
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumAndObjectMustBeSameType", valueType.ToString(), this.ToString()));
                valueType = (RuntimeType)valueType.GetEnumUnderlyingType();
            }

            if (valueType == RuntimeType.StringType)
            {
                string[] names = Enum.InternalGetNames(this);
                if (Array.IndexOf(names, value) >= 0)
                    return true;
                else
                    return false;
            }

            if (Type.IsIntegerType(valueType))
            {
                RuntimeType underlyingType = Enum.InternalGetUnderlyingType(this);
                if (underlyingType != valueType)
                    throw new ArgumentException(Environment.GetResourceString("Arg_EnumUnderlyingTypeAndObjectMustBeSameType", valueType.ToString(), underlyingType.ToString()));
                ulong[] ulValues = Enum.InternalGetValues(this);
                ulong ulValue = Enum.ToUInt64(value);
                return (Array.BinarySearch(ulValues, ulValue) >= 0);
            }
            else if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumUnderlyingTypeAndObjectMustBeSameType", valueType.ToString(), GetEnumUnderlyingType()));
            }
            else
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_UnknownEnumType"));
            }
        }

        public override string GetEnumName(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            Contract.EndContractBlock();
            Type valueType = value.GetType();
            if (!(valueType.IsEnum || IsIntegerType(valueType)))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeEnumBaseTypeOrEnum"), "value");
            ulong[] ulValues = Enum.InternalGetValues(this);
            ulong ulValue = Enum.ToUInt64(value);
            int index = Array.BinarySearch(ulValues, ulValue);
            if (index >= 0)
            {
                string[] names = Enum.InternalGetNames(this);
                return names[index];
            }

            return null;
        }

        internal RuntimeType[] GetGenericArgumentsInternal()
        {
            return GetRootElementType().GetTypeHandleInternal().GetInstantiationInternal();
        }

        public override Type[] GetGenericArguments()
        {
            Type[] types = GetRootElementType().GetTypeHandleInternal().GetInstantiationPublic();
            if (types == null)
                types = EmptyArray<Type>.Value;
            return types;
        }

        public override Type MakeGenericType(Type[] instantiation)
        {
            if (instantiation == null)
                throw new ArgumentNullException("instantiation");
            Contract.EndContractBlock();
            RuntimeType[] instantiationRuntimeType = new RuntimeType[instantiation.Length];
            if (!IsGenericTypeDefinition)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericTypeDefinition", this));
            if (GetGenericArguments().Length != instantiation.Length)
                throw new ArgumentException(Environment.GetResourceString("Argument_GenericArgsCount"), "instantiation");
            for (int i = 0; i < instantiation.Length; i++)
            {
                Type instantiationElem = instantiation[i];
                if (instantiationElem == null)
                    throw new ArgumentNullException();
                RuntimeType rtInstantiationElem = instantiationElem as RuntimeType;
                if (rtInstantiationElem == null)
                {
                    Type[] instantiationCopy = new Type[instantiation.Length];
                    for (int iCopy = 0; iCopy < instantiation.Length; iCopy++)
                        instantiationCopy[iCopy] = instantiation[iCopy];
                    instantiation = instantiationCopy;
                    return System.Reflection.Emit.TypeBuilderInstantiation.MakeGenericType(this, instantiation);
                }

                instantiationRuntimeType[i] = rtInstantiationElem;
            }

            RuntimeType[] genericParameters = GetGenericArgumentsInternal();
            SanityCheckGenericArguments(instantiationRuntimeType, genericParameters);
            Type ret = null;
            try
            {
                ret = new RuntimeTypeHandle(this).Instantiate(instantiationRuntimeType);
            }
            catch (TypeLoadException e)
            {
                ValidateGenericArguments(this, instantiationRuntimeType, e);
                throw e;
            }

            return ret;
        }

        public override bool IsGenericTypeDefinition
        {
            get
            {
                return RuntimeTypeHandle.IsGenericTypeDefinition(this);
            }
        }

        public override bool IsGenericParameter
        {
            get
            {
                return RuntimeTypeHandle.IsGenericVariable(this);
            }
        }

        public override int GenericParameterPosition
        {
            get
            {
                if (!IsGenericParameter)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
                Contract.EndContractBlock();
                return new RuntimeTypeHandle(this).GetGenericVariableIndex();
            }
        }

        public override Type GetGenericTypeDefinition()
        {
            if (!IsGenericType)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotGenericType"));
            Contract.EndContractBlock();
            return RuntimeTypeHandle.GetGenericTypeDefinition(this);
        }

        public override bool IsGenericType
        {
            get
            {
                return RuntimeTypeHandle.HasInstantiation(this);
            }
        }

        public override bool IsConstructedGenericType
        {
            get
            {
                return IsGenericType && !IsGenericTypeDefinition;
            }
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                return GetRootElementType().GetTypeHandleInternal().ContainsGenericVariables();
            }
        }

        public override Type[] GetGenericParameterConstraints()
        {
            if (!IsGenericParameter)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));
            Contract.EndContractBlock();
            Type[] constraints = new RuntimeTypeHandle(this).GetConstraints();
            if (constraints == null)
                constraints = EmptyArray<Type>.Value;
            return constraints;
        }

        public override Type MakePointerType()
        {
            return new RuntimeTypeHandle(this).MakePointer();
        }

        public override Type MakeByRefType()
        {
            return new RuntimeTypeHandle(this).MakeByRef();
        }

        public override Type MakeArrayType()
        {
            return new RuntimeTypeHandle(this).MakeSZArray();
        }

        public override Type MakeArrayType(int rank)
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();
            Contract.EndContractBlock();
            return new RuntimeTypeHandle(this).MakeArray(rank);
        }

        public override StructLayoutAttribute StructLayoutAttribute
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (StructLayoutAttribute)StructLayoutAttribute.GetCustomAttribute(this);
            }
        }

        private const BindingFlags MemberBindingMask = (BindingFlags)0x000000FF;
        private const BindingFlags InvocationMask = (BindingFlags)0x0000FF00;
        private const BindingFlags BinderNonCreateInstance = BindingFlags.InvokeMethod | BinderGetSetField | BinderGetSetProperty;
        private const BindingFlags BinderGetSetProperty = BindingFlags.GetProperty | BindingFlags.SetProperty;
        private const BindingFlags BinderSetInvokeProperty = BindingFlags.InvokeMethod | BindingFlags.SetProperty;
        private const BindingFlags BinderGetSetField = BindingFlags.GetField | BindingFlags.SetField;
        private const BindingFlags BinderSetInvokeField = BindingFlags.SetField | BindingFlags.InvokeMethod;
        private const BindingFlags BinderNonFieldGetSet = (BindingFlags)0x00FFF300;
        private const BindingFlags ClassicBindingMask = BindingFlags.InvokeMethod | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty;
        private static RuntimeType s_typedRef = (RuntimeType)typeof (TypedReference);
        static private extern bool CanValueSpecialCast(RuntimeType valueType, RuntimeType targetType);
        static private extern Object AllocateValueType(RuntimeType type, object value, bool fForceTypeChange);
        internal unsafe Object CheckValue(Object value, Binder binder, CultureInfo culture, BindingFlags invokeAttr)
        {
            if (IsInstanceOfType(value))
            {
                Contract.Assert(!IsGenericParameter);
                Type type = null;
                type = value.GetType();
                if (!Object.ReferenceEquals(type, this) && RuntimeTypeHandle.IsValueType(this))
                {
                    return AllocateValueType(this, value, true);
                }
                else
                {
                    return value;
                }
            }

            bool isByRef = IsByRef;
            if (isByRef)
            {
                RuntimeType elementType = RuntimeTypeHandle.GetElementType(this);
                if (elementType.IsInstanceOfType(value) || value == null)
                {
                    return AllocateValueType(elementType, value, false);
                }
            }
            else if (value == null)
                return value;
            else if (this == s_typedRef)
                return value;
            bool needsSpecialCast = IsPointer || IsEnum || IsPrimitive;
            if (needsSpecialCast)
            {
                RuntimeType valueType;
                Pointer pointer = value as Pointer;
                if (pointer != null)
                    valueType = pointer.GetPointerType();
                else
                    valueType = (RuntimeType)value.GetType();
                if (CanValueSpecialCast(valueType, this))
                {
                    if (pointer != null)
                        return pointer.GetPointerValue();
                    else
                        return value;
                }
            }

            if ((invokeAttr & BindingFlags.ExactBinding) == BindingFlags.ExactBinding)
                throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Arg_ObjObjEx"), value.GetType(), this));
            return TryChangeType(value, binder, culture, needsSpecialCast);
        }

        private Object TryChangeType(Object value, Binder binder, CultureInfo culture, bool needsSpecialCast)
        {
            if (binder != null && binder != Type.DefaultBinder)
            {
                value = binder.ChangeType(value, this, culture);
                if (IsInstanceOfType(value))
                    return value;
                if (IsByRef)
                {
                    RuntimeType elementType = RuntimeTypeHandle.GetElementType(this);
                    if (elementType.IsInstanceOfType(value) || value == null)
                        return AllocateValueType(elementType, value, false);
                }
                else if (value == null)
                    return value;
                if (needsSpecialCast)
                {
                    RuntimeType valueType;
                    Pointer pointer = value as Pointer;
                    if (pointer != null)
                        valueType = pointer.GetPointerType();
                    else
                        valueType = (RuntimeType)value.GetType();
                    if (CanValueSpecialCast(valueType, this))
                    {
                        if (pointer != null)
                            return pointer.GetPointerValue();
                        else
                            return value;
                    }
                }
            }

            throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Arg_ObjObjEx"), value.GetType(), this));
        }

        public override MemberInfo[] GetDefaultMembers()
        {
            MemberInfo[] members = null;
            String defaultMemberName = GetDefaultMemberName();
            if (defaultMemberName != null)
            {
                members = GetMember(defaultMemberName);
            }

            if (members == null)
                members = EmptyArray<MemberInfo>.Value;
            return members;
        }

        public override Object InvokeMember(String name, BindingFlags bindingFlags, Binder binder, Object target, Object[] providedArgs, ParameterModifier[] modifiers, CultureInfo culture, String[] namedParams)
        {
            if (IsGenericParameter)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_GenericParameter"));
            Contract.EndContractBlock();
            if ((bindingFlags & InvocationMask) == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_NoAccessSpec"), "bindingFlags");
            if ((bindingFlags & MemberBindingMask) == 0)
            {
                bindingFlags |= BindingFlags.Instance | BindingFlags.Public;
                if ((bindingFlags & BindingFlags.CreateInstance) == 0)
                    bindingFlags |= BindingFlags.Static;
            }

            if (namedParams != null)
            {
                if (providedArgs != null)
                {
                    if (namedParams.Length > providedArgs.Length)
                        throw new ArgumentException(Environment.GetResourceString("Arg_NamedParamTooBig"), "namedParams");
                }
                else
                {
                    if (namedParams.Length != 0)
                        throw new ArgumentException(Environment.GetResourceString("Arg_NamedParamTooBig"), "namedParams");
                }
            }

            if (namedParams != null && Array.IndexOf(namedParams, null) != -1)
                throw new ArgumentException(Environment.GetResourceString("Arg_NamedParamNull"), "namedParams");
            int argCnt = (providedArgs != null) ? providedArgs.Length : 0;
            if (binder == null)
                binder = DefaultBinder;
            bool bDefaultBinder = (binder == DefaultBinder);
            if ((bindingFlags & BindingFlags.CreateInstance) != 0)
            {
                if ((bindingFlags & BindingFlags.CreateInstance) != 0 && (bindingFlags & BinderNonCreateInstance) != 0)
                    throw new ArgumentException(Environment.GetResourceString("Arg_CreatInstAccess"), "bindingFlags");
                return Activator.CreateInstance(this, bindingFlags, binder, providedArgs, culture);
            }

            if ((bindingFlags & (BindingFlags.PutDispProperty | BindingFlags.PutRefDispProperty)) != 0)
                bindingFlags |= BindingFlags.SetProperty;
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0 || name.Equals(@"[DISPID=0]"))
            {
                name = GetDefaultMemberName();
                if (name == null)
                {
                    name = "ToString";
                }
            }

            bool IsGetField = (bindingFlags & BindingFlags.GetField) != 0;
            bool IsSetField = (bindingFlags & BindingFlags.SetField) != 0;
            if (IsGetField || IsSetField)
            {
                if (IsGetField)
                {
                    if (IsSetField)
                        throw new ArgumentException(Environment.GetResourceString("Arg_FldSetGet"), "bindingFlags");
                    if ((bindingFlags & BindingFlags.SetProperty) != 0)
                        throw new ArgumentException(Environment.GetResourceString("Arg_FldGetPropSet"), "bindingFlags");
                }
                else
                {
                    Contract.Assert(IsSetField);
                    if (providedArgs == null)
                        throw new ArgumentNullException("providedArgs");
                    if ((bindingFlags & BindingFlags.GetProperty) != 0)
                        throw new ArgumentException(Environment.GetResourceString("Arg_FldSetPropGet"), "bindingFlags");
                    if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
                        throw new ArgumentException(Environment.GetResourceString("Arg_FldSetInvoke"), "bindingFlags");
                }

                FieldInfo selFld = null;
                FieldInfo[] flds = GetMember(name, MemberTypes.Field, bindingFlags) as FieldInfo[];
                Contract.Assert(flds != null);
                if (flds.Length == 1)
                {
                    selFld = flds[0];
                }
                else if (flds.Length > 0)
                {
                    selFld = binder.BindToField(bindingFlags, flds, IsGetField ? Empty.Value : providedArgs[0], culture);
                }

                if (selFld != null)
                {
                    if (selFld.FieldType.IsArray || Object.ReferenceEquals(selFld.FieldType, typeof (System.Array)))
                    {
                        int idxCnt;
                        if ((bindingFlags & BindingFlags.GetField) != 0)
                        {
                            idxCnt = argCnt;
                        }
                        else
                        {
                            idxCnt = argCnt - 1;
                        }

                        if (idxCnt > 0)
                        {
                            int[] idx = new int[idxCnt];
                            for (int i = 0; i < idxCnt; i++)
                            {
                                try
                                {
                                    idx[i] = ((IConvertible)providedArgs[i]).ToInt32(null);
                                }
                                catch (InvalidCastException)
                                {
                                    throw new ArgumentException(Environment.GetResourceString("Arg_IndexMustBeInt"));
                                }
                            }

                            Array a = (Array)selFld.GetValue(target);
                            if ((bindingFlags & BindingFlags.GetField) != 0)
                            {
                                return a.GetValue(idx);
                            }
                            else
                            {
                                a.SetValue(providedArgs[idxCnt], idx);
                                return null;
                            }
                        }
                    }

                    if (IsGetField)
                    {
                        if (argCnt != 0)
                            throw new ArgumentException(Environment.GetResourceString("Arg_FldGetArgErr"), "bindingFlags");
                        return selFld.GetValue(target);
                    }
                    else
                    {
                        if (argCnt != 1)
                            throw new ArgumentException(Environment.GetResourceString("Arg_FldSetArgErr"), "bindingFlags");
                        selFld.SetValue(target, providedArgs[0], bindingFlags, binder, culture);
                        return null;
                    }
                }

                if ((bindingFlags & BinderNonFieldGetSet) == 0)
                    throw new MissingFieldException(FullName, name);
            }

            bool isGetProperty = (bindingFlags & BindingFlags.GetProperty) != 0;
            bool isSetProperty = (bindingFlags & BindingFlags.SetProperty) != 0;
            if (isGetProperty || isSetProperty)
            {
                if (isGetProperty)
                {
                    Contract.Assert(!IsSetField);
                    if (isSetProperty)
                        throw new ArgumentException(Environment.GetResourceString("Arg_PropSetGet"), "bindingFlags");
                }
                else
                {
                    Contract.Assert(isSetProperty);
                    Contract.Assert(!IsGetField);
                    if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
                        throw new ArgumentException(Environment.GetResourceString("Arg_PropSetInvoke"), "bindingFlags");
                }
            }

            MethodInfo[] finalists = null;
            MethodInfo finalist = null;
            if ((bindingFlags & BindingFlags.InvokeMethod) != 0)
            {
                MethodInfo[] semiFinalists = GetMember(name, MemberTypes.Method, bindingFlags) as MethodInfo[];
                List<MethodInfo> results = null;
                for (int i = 0; i < semiFinalists.Length; i++)
                {
                    MethodInfo semiFinalist = semiFinalists[i];
                    Contract.Assert(semiFinalist != null);
                    if (!FilterApplyMethodInfo((RuntimeMethodInfo)semiFinalist, bindingFlags, CallingConventions.Any, new Type[argCnt]))
                        continue;
                    if (finalist == null)
                    {
                        finalist = semiFinalist;
                    }
                    else
                    {
                        if (results == null)
                        {
                            results = new List<MethodInfo>(semiFinalists.Length);
                            results.Add(finalist);
                        }

                        results.Add(semiFinalist);
                    }
                }

                if (results != null)
                {
                    Contract.Assert(results.Count > 1);
                    finalists = new MethodInfo[results.Count];
                    results.CopyTo(finalists);
                }
            }

            Contract.Assert(finalists == null || finalist != null);
            if (finalist == null && isGetProperty || isSetProperty)
            {
                PropertyInfo[] semiFinalists = GetMember(name, MemberTypes.Property, bindingFlags) as PropertyInfo[];
                List<MethodInfo> results = null;
                for (int i = 0; i < semiFinalists.Length; i++)
                {
                    MethodInfo semiFinalist = null;
                    if (isSetProperty)
                    {
                        semiFinalist = semiFinalists[i].GetSetMethod(true);
                    }
                    else
                    {
                        semiFinalist = semiFinalists[i].GetGetMethod(true);
                    }

                    if (semiFinalist == null)
                        continue;
                    if (!FilterApplyMethodInfo((RuntimeMethodInfo)semiFinalist, bindingFlags, CallingConventions.Any, new Type[argCnt]))
                        continue;
                    if (finalist == null)
                    {
                        finalist = semiFinalist;
                    }
                    else
                    {
                        if (results == null)
                        {
                            results = new List<MethodInfo>(semiFinalists.Length);
                            results.Add(finalist);
                        }

                        results.Add(semiFinalist);
                    }
                }

                if (results != null)
                {
                    Contract.Assert(results.Count > 1);
                    finalists = new MethodInfo[results.Count];
                    results.CopyTo(finalists);
                }
            }

            if (finalist != null)
            {
                if (finalists == null && argCnt == 0 && finalist.GetParametersNoCopy().Length == 0 && (bindingFlags & BindingFlags.OptionalParamBinding) == 0)
                {
                    return finalist.Invoke(target, bindingFlags, binder, providedArgs, culture);
                }

                if (finalists == null)
                    finalists = new MethodInfo[]{finalist};
                if (providedArgs == null)
                    providedArgs = EmptyArray<Object>.Value;
                Object state = null;
                MethodBase invokeMethod = null;
                try
                {
                    invokeMethod = binder.BindToMethod(bindingFlags, finalists, ref providedArgs, modifiers, culture, namedParams, out state);
                }
                catch (MissingMethodException)
                {
                }

                if (invokeMethod == null)
                    throw new MissingMethodException(FullName, name);
                Object result = ((MethodInfo)invokeMethod).Invoke(target, bindingFlags, binder, providedArgs, culture);
                if (state != null)
                    binder.ReorderArgumentArray(ref providedArgs, state);
                return result;
            }

            throw new MissingMethodException(FullName, name);
        }

        public override bool Equals(object obj)
        {
            return obj == (object)this;
        }

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        public override String ToString()
        {
            return GetCachedName(TypeNameKind.ToString);
        }

        public Object Clone()
        {
            return this;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            UnitySerializationHolder.GetUnitySerializationInfo(info, this);
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, RuntimeType.ObjectType, inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if ((object)attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if ((object)attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.IsDefined(this, attributeRuntimeType, inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }

        public override String Name
        {
            get
            {
                return GetCachedName(TypeNameKind.Name);
            }
        }

        internal override string FormatTypeName(bool serialization)
        {
            if (serialization)
            {
                return GetCachedName(TypeNameKind.SerializationName);
            }
            else
            {
                Type elementType = GetRootElementType();
                if (elementType.IsNested)
                    return Name;
                string typeName = ToString();
                if (elementType.IsPrimitive || elementType == typeof (void) || elementType == typeof (TypedReference))
                {
                    typeName = typeName.Substring(@"System.".Length);
                }

                return typeName;
            }
        }

        private string GetCachedName(TypeNameKind kind)
        {
            return Cache.GetName(kind);
        }

        public override MemberTypes MemberType
        {
            get
            {
                if (this.IsPublic || this.IsNotPublic)
                    return MemberTypes.TypeInfo;
                else
                    return MemberTypes.NestedType;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return Cache.GetEnclosingType();
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return DeclaringType;
            }
        }

        public override int MetadataToken
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return RuntimeTypeHandle.GetToken(this);
            }
        }

        private void CreateInstanceCheckThis()
        {
            if (this is ReflectionOnlyType)
                throw new ArgumentException(Environment.GetResourceString("Arg_ReflectionOnlyInvoke"));
            if (ContainsGenericParameters)
                throw new ArgumentException(Environment.GetResourceString("Acc_CreateGenericEx", this));
            Contract.EndContractBlock();
            Type elementType = this.GetRootElementType();
            if (Object.ReferenceEquals(elementType, typeof (ArgIterator)))
                throw new NotSupportedException(Environment.GetResourceString("Acc_CreateArgIterator"));
            if (Object.ReferenceEquals(elementType, typeof (void)))
                throw new NotSupportedException(Environment.GetResourceString("Acc_CreateVoid"));
        }

        internal Object CreateInstanceImpl(BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, ref StackCrawlMark stackMark)
        {
            CreateInstanceCheckThis();
            Object server = null;
            try
            {
                try
                {
                    if (args == null)
                        args = EmptyArray<Object>.Value;
                    int argCnt = args.Length;
                    if (binder == null)
                        binder = DefaultBinder;
                    if (argCnt == 0 && (bindingAttr & BindingFlags.Public) != 0 && (bindingAttr & BindingFlags.Instance) != 0 && (IsGenericCOMObjectImpl() || IsValueType))
                    {
                        server = CreateInstanceDefaultCtor((bindingAttr & BindingFlags.NonPublic) == 0, false, true, ref stackMark);
                    }
                    else
                    {
                        ConstructorInfo[] candidates = GetConstructors(bindingAttr);
                        List<MethodBase> matches = new List<MethodBase>(candidates.Length);
                        Type[] argsType = new Type[argCnt];
                        for (int i = 0; i < argCnt; i++)
                        {
                            if (args[i] != null)
                            {
                                argsType[i] = args[i].GetType();
                            }
                        }

                        for (int i = 0; i < candidates.Length; i++)
                        {
                            if (FilterApplyConstructorInfo((RuntimeConstructorInfo)candidates[i], bindingAttr, CallingConventions.Any, argsType))
                                matches.Add(candidates[i]);
                        }

                        MethodBase[] cons = new MethodBase[matches.Count];
                        matches.CopyTo(cons);
                        if (cons != null && cons.Length == 0)
                            cons = null;
                        if (cons == null)
                        {
                            throw new MissingMethodException(Environment.GetResourceString("MissingConstructor_Name", FullName));
                        }

                        MethodBase invokeMethod;
                        Object state = null;
                        try
                        {
                            invokeMethod = binder.BindToMethod(bindingAttr, cons, ref args, null, culture, null, out state);
                        }
                        catch (MissingMethodException)
                        {
                            invokeMethod = null;
                        }

                        if (invokeMethod == null)
                        {
                            throw new MissingMethodException(Environment.GetResourceString("MissingConstructor_Name", FullName));
                        }

                        if (RuntimeType.DelegateType.IsAssignableFrom(invokeMethod.DeclaringType))
                        {
                            try
                            {
                                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                            }
                            catch
                            {
                                throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("NotSupported_DelegateCreationFromPT")));
                            }
                        }

                        if (invokeMethod.GetParametersNoCopy().Length == 0)
                        {
                            if (args.Length != 0)
                            {
                                Contract.Assert((invokeMethod.CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs);
                                throw new NotSupportedException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("NotSupported_CallToVarArg")));
                            }

                            server = Activator.CreateInstance(this, true);
                        }
                        else
                        {
                            server = ((ConstructorInfo)invokeMethod).Invoke(bindingAttr, binder, args, culture);
                            if (state != null)
                                binder.ReorderArgumentArray(ref args, state);
                        }
                    }
                }
                finally
                {
                }
            }
            catch (Exception)
            {
                throw;
            }

            return server;
        }

        class ActivatorCacheEntry
        {
            internal readonly RuntimeType m_type;
            internal volatile CtorDelegate m_ctor;
            internal readonly RuntimeMethodHandleInternal m_hCtorMethodHandle;
            internal readonly MethodAttributes m_ctorAttributes;
            internal readonly bool m_bNeedSecurityCheck;
            internal volatile bool m_bFullyInitialized;
            internal ActivatorCacheEntry(RuntimeType t, RuntimeMethodHandleInternal rmh, bool bNeedSecurityCheck)
            {
                m_type = t;
                m_bNeedSecurityCheck = bNeedSecurityCheck;
                m_hCtorMethodHandle = rmh;
                if (!m_hCtorMethodHandle.IsNullHandle())
                    m_ctorAttributes = RuntimeMethodHandle.GetAttributes(m_hCtorMethodHandle);
            }
        }

        class ActivatorCache
        {
            const int CACHE_SIZE = 16;
            volatile int hash_counter;
            readonly ActivatorCacheEntry[] cache = new ActivatorCacheEntry[CACHE_SIZE];
            volatile ConstructorInfo delegateCtorInfo;
            volatile PermissionSet delegateCreatePermissions;
            private void InitializeDelegateCreator()
            {
                PermissionSet ps = new PermissionSet(PermissionState.None);
                ps.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess));
                ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.UnmanagedCode));
                delegateCreatePermissions = ps;
                ConstructorInfo ctorInfo = typeof (CtorDelegate).GetConstructor(new Type[]{typeof (Object), typeof (IntPtr)});
                delegateCtorInfo = ctorInfo;
            }

            private void InitializeCacheEntry(ActivatorCacheEntry ace)
            {
                if (!ace.m_type.IsValueType)
                {
                    Contract.Assert(!ace.m_hCtorMethodHandle.IsNullHandle(), "Expected the default ctor method handle for a reference type.");
                    if (delegateCtorInfo == null)
                        InitializeDelegateCreator();
                    delegateCreatePermissions.Assert();
                    CtorDelegate ctor = (CtorDelegate)delegateCtorInfo.Invoke(new Object[]{null, RuntimeMethodHandle.GetFunctionPointer(ace.m_hCtorMethodHandle)});
                    ace.m_ctor = ctor;
                }

                ace.m_bFullyInitialized = true;
            }

            internal ActivatorCacheEntry GetEntry(RuntimeType t)
            {
                int index = hash_counter;
                for (int i = 0; i < CACHE_SIZE; i++)
                {
                    ActivatorCacheEntry ace = Volatile.Read(ref cache[index]);
                    if (ace != null && ace.m_type == t)
                    {
                        if (!ace.m_bFullyInitialized)
                            InitializeCacheEntry(ace);
                        return ace;
                    }

                    index = (index + 1) & (ActivatorCache.CACHE_SIZE - 1);
                }

                return null;
            }

            internal void SetEntry(ActivatorCacheEntry ace)
            {
                int index = (hash_counter - 1) & (ActivatorCache.CACHE_SIZE - 1);
                hash_counter = index;
                Volatile.Write(ref cache[index], ace);
            }
        }

        private static volatile ActivatorCache s_ActivatorCache;
        internal Object CreateInstanceSlow(bool publicOnly, bool skipCheckThis, bool fillCache, ref StackCrawlMark stackMark)
        {
            RuntimeMethodHandleInternal runtime_ctor = default (RuntimeMethodHandleInternal);
            bool bNeedSecurityCheck = true;
            bool bCanBeCached = false;
            bool bSecurityCheckOff = false;
            if (!skipCheckThis)
                CreateInstanceCheckThis();
            if (!fillCache)
                bSecurityCheckOff = true;
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
            {
                RuntimeAssembly caller = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (caller != null && !caller.IsSafeForReflection())
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", this.FullName));
                bSecurityCheckOff = false;
                bCanBeCached = false;
            }

            Object instance = RuntimeTypeHandle.CreateInstance(this, publicOnly, bSecurityCheckOff, ref bCanBeCached, ref runtime_ctor, ref bNeedSecurityCheck);
            if (bCanBeCached && fillCache)
            {
                ActivatorCache activatorCache = s_ActivatorCache;
                if (activatorCache == null)
                {
                    activatorCache = new ActivatorCache();
                    s_ActivatorCache = activatorCache;
                }

                ActivatorCacheEntry ace = new ActivatorCacheEntry(this, runtime_ctor, bNeedSecurityCheck);
                activatorCache.SetEntry(ace);
            }

            return instance;
        }

        internal Object CreateInstanceDefaultCtor(bool publicOnly, bool skipCheckThis, bool fillCache, ref StackCrawlMark stackMark)
        {
            if (GetType() == typeof (ReflectionOnlyType))
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInReflectionOnly"));
            ActivatorCache activatorCache = s_ActivatorCache;
            if (activatorCache != null)
            {
                ActivatorCacheEntry ace = activatorCache.GetEntry(this);
                if (ace != null)
                {
                    if (publicOnly)
                    {
                        if (ace.m_ctor != null && (ace.m_ctorAttributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
                        {
                            throw new MissingMethodException(Environment.GetResourceString("Arg_NoDefCTor"));
                        }
                    }

                    Object instance = RuntimeTypeHandle.Allocate(this);
                    Contract.Assert(ace.m_ctor != null || this.IsValueType);
                    if (ace.m_ctor != null)
                    {
                        if (ace.m_bNeedSecurityCheck)
                            RuntimeMethodHandle.PerformSecurityCheck(instance, ace.m_hCtorMethodHandle, this, (uint)INVOCATION_FLAGS.INVOCATION_FLAGS_CONSTRUCTOR_INVOKE);
                        try
                        {
                            ace.m_ctor(instance);
                        }
                        catch (Exception e)
                        {
                            throw new TargetInvocationException(e);
                        }
                    }

                    return instance;
                }
            }

            return CreateInstanceSlow(publicOnly, skipCheckThis, fillCache, ref stackMark);
        }

        internal void InvalidateCachedNestedType()
        {
            Cache.InvalidateCachedNestedType();
        }

        internal bool IsGenericCOMObjectImpl()
        {
            return RuntimeTypeHandle.IsComObject(this, true);
        }

        private static extern Object _CreateEnum(RuntimeType enumType, long value);
        internal static Object CreateEnum(RuntimeType enumType, long value)
        {
            return _CreateEnum(enumType, value);
        }

        private extern Object InvokeDispMethod(String name, BindingFlags invokeAttr, Object target, Object[] args, bool[] byrefModifiers, int culture, String[] namedParameters);
        internal static extern Type GetTypeFromProgIDImpl(String progID, String server, bool throwOnError);
        internal static extern Type GetTypeFromCLSIDImpl(Guid clsid, String server, bool throwOnError);
    }

    internal class ReflectionOnlyType : RuntimeType
    {
        private ReflectionOnlyType()
        {
        }

        public override RuntimeTypeHandle TypeHandle
        {
            get
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInReflectionOnly"));
            }
        }
    }

    internal unsafe struct Utf8String
    {
        private static extern unsafe bool EqualsCaseSensitive(void *szLhs, void *szRhs, int cSz);
        private static extern unsafe bool EqualsCaseInsensitive(void *szLhs, void *szRhs, int cSz);
        private static extern unsafe uint HashCaseInsensitive(void *sz, int cSz);
        private static int GetUtf8StringByteLength(void *pUtf8String)
        {
            int len = 0;
            unsafe
            {
                byte *pItr = (byte *)pUtf8String;
                while (*pItr != 0)
                {
                    len++;
                    pItr++;
                }
            }

            return len;
        }

        private void *m_pStringHeap;
        private int m_StringHeapByteLength;
        internal Utf8String(void *pStringHeap)
        {
            m_pStringHeap = pStringHeap;
            if (pStringHeap != null)
            {
                m_StringHeapByteLength = GetUtf8StringByteLength(pStringHeap);
            }
            else
            {
                m_StringHeapByteLength = 0;
            }
        }

        internal unsafe Utf8String(void *pUtf8String, int cUtf8String)
        {
            m_pStringHeap = pUtf8String;
            m_StringHeapByteLength = cUtf8String;
        }

        internal unsafe bool Equals(Utf8String s)
        {
            if (m_pStringHeap == null)
            {
                return s.m_StringHeapByteLength == 0;
            }

            if ((s.m_StringHeapByteLength == m_StringHeapByteLength) && (m_StringHeapByteLength != 0))
            {
                return Utf8String.EqualsCaseSensitive(s.m_pStringHeap, m_pStringHeap, m_StringHeapByteLength);
            }

            return false;
        }

        internal unsafe bool EqualsCaseInsensitive(Utf8String s)
        {
            if (m_pStringHeap == null)
            {
                return s.m_StringHeapByteLength == 0;
            }

            if ((s.m_StringHeapByteLength == m_StringHeapByteLength) && (m_StringHeapByteLength != 0))
            {
                return Utf8String.EqualsCaseInsensitive(s.m_pStringHeap, m_pStringHeap, m_StringHeapByteLength);
            }

            return false;
        }

        internal unsafe uint HashCaseInsensitive()
        {
            return Utf8String.HashCaseInsensitive(m_pStringHeap, m_StringHeapByteLength);
        }

        public override string ToString()
        {
            unsafe
            {
                byte *buf = stackalloc byte[m_StringHeapByteLength];
                byte *pItr = (byte *)m_pStringHeap;
                for (int currentPos = 0; currentPos < m_StringHeapByteLength; currentPos++)
                {
                    buf[currentPos] = *pItr;
                    pItr++;
                }

                if (m_StringHeapByteLength == 0)
                    return "";
                int cResult = Encoding.UTF8.GetCharCount(buf, m_StringHeapByteLength);
                char *result = stackalloc char[cResult];
                Encoding.UTF8.GetChars(buf, m_StringHeapByteLength, result, cResult);
                return new string (result, 0, cResult);
            }
        }
    }
}

namespace System.Reflection
{
    internal struct CerHashtable<K, V>
        where K : class
    {
        private class Table
        {
            internal K[] m_keys;
            internal V[] m_values;
            internal int m_count;
            internal Table(int size)
            {
                size = HashHelpers.GetPrime(size);
                m_keys = new K[size];
                m_values = new V[size];
            }

            internal void Insert(K key, V value)
            {
                int hashcode = GetHashCodeHelper(key);
                if (hashcode < 0)
                    hashcode = ~hashcode;
                K[] keys = m_keys;
                int index = hashcode % keys.Length;
                while (true)
                {
                    K hit = keys[index];
                    if (hit == null)
                    {
                        m_count++;
                        m_values[index] = value;
                        Volatile.Write(ref keys[index], key);
                        break;
                    }
                    else
                    {
                        Contract.Assert(!hit.Equals(key), "Key was already in CerHashtable!  Potential race condition (or bug) in the Reflection cache?");
                        index++;
                        if (index >= keys.Length)
                            index -= keys.Length;
                    }
                }
            }
        }

        private Table m_Table;
        private const int MinSize = 7;
        private static int GetHashCodeHelper(K key)
        {
            string sKey = key as string;
            if (sKey == null)
            {
                return key.GetHashCode();
            }
            else
            {
                return sKey.GetLegacyNonRandomizedHashCode();
            }
        }

        private void Rehash(int newSize)
        {
            Table newTable = new Table(newSize);
            Table oldTable = m_Table;
            if (oldTable != null)
            {
                K[] keys = oldTable.m_keys;
                V[] values = oldTable.m_values;
                for (int i = 0; i < keys.Length; i++)
                {
                    K key = keys[i];
                    if (key != null)
                    {
                        newTable.Insert(key, values[i]);
                    }
                }
            }

            Volatile.Write(ref m_Table, newTable);
        }

        internal V this[K key]
        {
            set
            {
                Table table = m_Table;
                if (table != null)
                {
                    int requiredSize = 2 * (table.m_count + 1);
                    if (requiredSize >= table.m_keys.Length)
                        Rehash(requiredSize);
                }
                else
                {
                    Rehash(MinSize);
                }

                m_Table.Insert(key, value);
            }

            get
            {
                Table table = Volatile.Read(ref m_Table);
                if (table == null)
                    return default (V);
                int hashcode = GetHashCodeHelper(key);
                if (hashcode < 0)
                    hashcode = ~hashcode;
                K[] keys = table.m_keys;
                int index = hashcode % keys.Length;
                while (true)
                {
                    K hit = Volatile.Read(ref keys[index]);
                    if (hit != null)
                    {
                        if (hit.Equals(key))
                            return table.m_values[index];
                        index++;
                        if (index >= keys.Length)
                            index -= keys.Length;
                    }
                    else
                    {
                        return default (V);
                    }
                }
            }
        }
    }
}