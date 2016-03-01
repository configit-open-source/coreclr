using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Reflection
{
    public abstract class FieldInfo : MemberInfo, _FieldInfo
    {
        public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHandle"));
            FieldInfo f = RuntimeType.GetFieldInfo(handle.GetRuntimeFieldInfo());
            Type declaringType = f.DeclaringType;
            if (declaringType != null && declaringType.IsGenericType)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_FieldDeclaringTypeGeneric"), f.Name, declaringType.GetGenericTypeDefinition()));
            return f;
        }

        public static FieldInfo GetFieldFromHandle(RuntimeFieldHandle handle, RuntimeTypeHandle declaringType)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidHandle"));
            return RuntimeType.GetFieldInfo(declaringType.GetRuntimeType(), handle.GetRuntimeFieldInfo());
        }

        protected FieldInfo()
        {
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override MemberTypes MemberType
        {
            get
            {
                return System.Reflection.MemberTypes.Field;
            }
        }

        public virtual Type[] GetRequiredCustomModifiers()
        {
            throw new NotImplementedException();
        }

        public virtual Type[] GetOptionalCustomModifiers()
        {
            throw new NotImplementedException();
        }

        public virtual void SetValueDirect(TypedReference obj, Object value)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_AbstractNonCLS"));
        }

        public virtual Object GetValueDirect(TypedReference obj)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_AbstractNonCLS"));
        }

        public abstract RuntimeFieldHandle FieldHandle
        {
            get;
        }

        public abstract Type FieldType
        {
            get;
        }

        public abstract Object GetValue(Object obj);
        public virtual Object GetRawConstantValue()
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_AbstractNonCLS"));
        }

        public abstract void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture);
        public abstract FieldAttributes Attributes
        {
            get;
        }

        public void SetValue(Object obj, Object value)
        {
            SetValue(obj, value, BindingFlags.Default, Type.DefaultBinder, null);
        }

        public bool IsPublic
        {
            get
            {
                return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public;
            }
        }

        public bool IsPrivate
        {
            get
            {
                return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Private;
            }
        }

        public bool IsFamily
        {
            get
            {
                return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Family;
            }
        }

        public bool IsAssembly
        {
            get
            {
                return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Assembly;
            }
        }

        public bool IsFamilyAndAssembly
        {
            get
            {
                return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamANDAssem;
            }
        }

        public bool IsFamilyOrAssembly
        {
            get
            {
                return (Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.FamORAssem;
            }
        }

        public bool IsStatic
        {
            get
            {
                return (Attributes & FieldAttributes.Static) != 0;
            }
        }

        public bool IsInitOnly
        {
            get
            {
                return (Attributes & FieldAttributes.InitOnly) != 0;
            }
        }

        public bool IsLiteral
        {
            get
            {
                return (Attributes & FieldAttributes.Literal) != 0;
            }
        }

        public bool IsNotSerialized
        {
            get
            {
                return (Attributes & FieldAttributes.NotSerialized) != 0;
            }
        }

        public bool IsSpecialName
        {
            get
            {
                return (Attributes & FieldAttributes.SpecialName) != 0;
            }
        }

        public bool IsPinvokeImpl
        {
            get
            {
                return (Attributes & FieldAttributes.PinvokeImpl) != 0;
            }
        }

        public virtual bool IsSecurityCritical
        {
            get
            {
                return FieldHandle.IsSecurityCritical();
            }
        }

        public virtual bool IsSecuritySafeCritical
        {
            get
            {
                return FieldHandle.IsSecuritySafeCritical();
            }
        }

        public virtual bool IsSecurityTransparent
        {
            get
            {
                return FieldHandle.IsSecurityTransparent();
            }
        }
    }

    internal abstract class RuntimeFieldInfo : FieldInfo, ISerializable
    {
        private BindingFlags m_bindingFlags;
        protected RuntimeType.RuntimeTypeCache m_reflectedTypeCache;
        protected RuntimeType m_declaringType;
        protected RuntimeFieldInfo()
        {
        }

        protected RuntimeFieldInfo(RuntimeType.RuntimeTypeCache reflectedTypeCache, RuntimeType declaringType, BindingFlags bindingFlags)
        {
            m_bindingFlags = bindingFlags;
            m_declaringType = declaringType;
            m_reflectedTypeCache = reflectedTypeCache;
        }

        internal BindingFlags BindingFlags
        {
            get
            {
                return m_bindingFlags;
            }
        }

        private RuntimeType ReflectedTypeInternal
        {
            get
            {
                return m_reflectedTypeCache.GetRuntimeType();
            }
        }

        internal RuntimeType GetDeclaringTypeInternal()
        {
            return m_declaringType;
        }

        internal RuntimeType GetRuntimeType()
        {
            return m_declaringType;
        }

        internal abstract RuntimeModule GetRuntimeModule();
        public override MemberTypes MemberType
        {
            get
            {
                return MemberTypes.Field;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return m_reflectedTypeCache.IsGlobal ? null : ReflectedTypeInternal;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return m_reflectedTypeCache.IsGlobal ? null : m_declaringType;
            }
        }

        public override Module Module
        {
            get
            {
                return GetRuntimeModule();
            }
        }

        public unsafe override String ToString()
        {
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                return FieldType.ToString() + " " + Name;
            else
                return FieldType.FormatTypeName() + " " + Name;
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, typeof (object) as RuntimeType);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            MemberInfoSerializationHolder.GetSerializationInfo(info, Name, ReflectedTypeInternal, ToString(), MemberTypes.Field);
        }
    }

    internal unsafe sealed class RtFieldInfo : RuntimeFieldInfo, IRuntimeFieldInfo
    {
        static private extern void PerformVisibilityCheckOnField(IntPtr field, Object target, RuntimeType declaringType, FieldAttributes attr, uint invocationFlags);
        private IntPtr m_fieldHandle;
        private FieldAttributes m_fieldAttributes;
        private string m_name;
        private RuntimeType m_fieldType;
        private INVOCATION_FLAGS m_invocationFlags;
        private bool IsNonW8PFrameworkAPI()
        {
            if (GetRuntimeType().IsNonW8PFrameworkAPI())
                return true;
            if (m_declaringType.IsEnum)
                return false;
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
                    Type declaringType = DeclaringType;
                    bool fIsReflectionOnlyType = (declaringType is ReflectionOnlyType);
                    INVOCATION_FLAGS invocationFlags = 0;
                    if ((declaringType != null && declaringType.ContainsGenericParameters) || (declaringType == null && Module.Assembly.ReflectionOnly) || (fIsReflectionOnlyType))
                    {
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE;
                    }

                    if (invocationFlags == 0)
                    {
                        if ((m_fieldAttributes & FieldAttributes.InitOnly) != (FieldAttributes)0)
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_SPECIAL_FIELD;
                        if ((m_fieldAttributes & FieldAttributes.HasFieldRVA) != (FieldAttributes)0)
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_SPECIAL_FIELD;
                        bool needsTransparencySecurityCheck = IsSecurityCritical && !IsSecuritySafeCritical;
                        bool needsVisibilitySecurityCheck = ((m_fieldAttributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public) || (declaringType != null && declaringType.NeedsReflectionSecurityCheck);
                        if (needsTransparencySecurityCheck || needsVisibilitySecurityCheck)
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;
                        Type fieldType = FieldType;
                        if (fieldType.IsPointer || fieldType.IsEnum || fieldType.IsPrimitive)
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_FIELD_SPECIAL_CAST;
                    }

                    if (AppDomain.ProfileAPICheck && IsNonW8PFrameworkAPI())
                        invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API;
                    m_invocationFlags = invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
                }

                return m_invocationFlags;
            }
        }

        private RuntimeAssembly GetRuntimeAssembly()
        {
            return m_declaringType.GetRuntimeAssembly();
        }

        internal RtFieldInfo(RuntimeFieldHandleInternal handle, RuntimeType declaringType, RuntimeType.RuntimeTypeCache reflectedTypeCache, BindingFlags bindingFlags): base (reflectedTypeCache, declaringType, bindingFlags)
        {
            m_fieldHandle = handle.Value;
            m_fieldAttributes = RuntimeFieldHandle.GetAttributes(handle);
        }

        RuntimeFieldHandleInternal IRuntimeFieldInfo.Value
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return new RuntimeFieldHandleInternal(m_fieldHandle);
            }
        }

        internal void CheckConsistency(Object target)
        {
            if ((m_fieldAttributes & FieldAttributes.Static) != FieldAttributes.Static)
            {
                if (!m_declaringType.IsInstanceOfType(target))
                {
                    if (target == null)
                    {
                        if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                            throw new ArgumentNullException(Environment.GetResourceString("RFLCT.Targ_StatFldReqTarg"));
                        else
                            throw new TargetException(Environment.GetResourceString("RFLCT.Targ_StatFldReqTarg"));
                    }
                    else
                    {
                        throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Arg_FieldDeclTarget"), Name, m_declaringType, target.GetType()));
                    }
                }
            }
        }

        internal override bool CacheEquals(object o)
        {
            RtFieldInfo m = o as RtFieldInfo;
            if ((object)m == null)
                return false;
            return m.m_fieldHandle == m_fieldHandle;
        }

        internal void InternalSetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture, ref StackCrawlMark stackMark)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            RuntimeType declaringType = DeclaringType as RuntimeType;
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
            {
                if (declaringType != null && declaringType.ContainsGenericParameters)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_UnboundGenField"));
                if ((declaringType == null && Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_ReflectionOnlyField"));
                throw new FieldAccessException();
            }

            CheckConsistency(obj);
            RuntimeType fieldType = (RuntimeType)FieldType;
            value = fieldType.CheckValue(value, binder, culture, invokeAttr);
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
            {
                RuntimeAssembly caller = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (caller != null && !caller.IsSafeForReflection())
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", FullName));
            }

            if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_SPECIAL_FIELD | INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY)) != 0)
                PerformVisibilityCheckOnField(m_fieldHandle, obj, m_declaringType, m_fieldAttributes, (uint)m_invocationFlags);
            bool domainInitialized = false;
            if (declaringType == null)
            {
                RuntimeFieldHandle.SetValue(this, obj, value, fieldType, m_fieldAttributes, null, ref domainInitialized);
            }
            else
            {
                domainInitialized = declaringType.DomainInitialized;
                RuntimeFieldHandle.SetValue(this, obj, value, fieldType, m_fieldAttributes, declaringType, ref domainInitialized);
                declaringType.DomainInitialized = domainInitialized;
            }
        }

        internal void UnsafeSetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            RuntimeType declaringType = DeclaringType as RuntimeType;
            RuntimeType fieldType = (RuntimeType)FieldType;
            value = fieldType.CheckValue(value, binder, culture, invokeAttr);
            bool domainInitialized = false;
            if (declaringType == null)
            {
                RuntimeFieldHandle.SetValue(this, obj, value, fieldType, m_fieldAttributes, null, ref domainInitialized);
            }
            else
            {
                domainInitialized = declaringType.DomainInitialized;
                RuntimeFieldHandle.SetValue(this, obj, value, fieldType, m_fieldAttributes, declaringType, ref domainInitialized);
                declaringType.DomainInitialized = domainInitialized;
            }
        }

        internal Object InternalGetValue(Object obj, ref StackCrawlMark stackMark)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            RuntimeType declaringType = DeclaringType as RuntimeType;
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
            {
                if (declaringType != null && DeclaringType.ContainsGenericParameters)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_UnboundGenField"));
                if ((declaringType == null && Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
                    throw new InvalidOperationException(Environment.GetResourceString("Arg_ReflectionOnlyField"));
                throw new FieldAccessException();
            }

            CheckConsistency(obj);
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NON_W8P_FX_API) != 0)
            {
                RuntimeAssembly caller = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
                if (caller != null && !caller.IsSafeForReflection())
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_APIInvalidForCurrentContext", FullName));
            }

            RuntimeType fieldType = (RuntimeType)FieldType;
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY) != 0)
                PerformVisibilityCheckOnField(m_fieldHandle, obj, m_declaringType, m_fieldAttributes, (uint)(m_invocationFlags & ~INVOCATION_FLAGS.INVOCATION_FLAGS_SPECIAL_FIELD));
            return UnsafeGetValue(obj);
        }

        internal Object UnsafeGetValue(Object obj)
        {
            RuntimeType declaringType = DeclaringType as RuntimeType;
            RuntimeType fieldType = (RuntimeType)FieldType;
            bool domainInitialized = false;
            if (declaringType == null)
            {
                return RuntimeFieldHandle.GetValue(this, obj, fieldType, null, ref domainInitialized);
            }
            else
            {
                domainInitialized = declaringType.DomainInitialized;
                object retVal = RuntimeFieldHandle.GetValue(this, obj, fieldType, declaringType, ref domainInitialized);
                declaringType.DomainInitialized = domainInitialized;
                return retVal;
            }
        }

        public override String Name
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_name == null)
                    m_name = RuntimeFieldHandle.GetName(this);
                return m_name;
            }
        }

        internal String FullName
        {
            get
            {
                return String.Format("{0}.{1}", DeclaringType.FullName, Name);
            }
        }

        public override int MetadataToken
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return RuntimeFieldHandle.GetToken(this);
            }
        }

        internal override RuntimeModule GetRuntimeModule()
        {
            return RuntimeTypeHandle.GetModule(RuntimeFieldHandle.GetApproxDeclaringType(this));
        }

        public override Object GetValue(Object obj)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalGetValue(obj, ref stackMark);
        }

        public override object GetRawConstantValue()
        {
            throw new InvalidOperationException();
        }

        public override Object GetValueDirect(TypedReference obj)
        {
            if (obj.IsNull)
                throw new ArgumentException(Environment.GetResourceString("Arg_TypedReference_Null"));
            Contract.EndContractBlock();
            unsafe
            {
                return RuntimeFieldHandle.GetValueDirect(this, (RuntimeType)FieldType, &obj, (RuntimeType)DeclaringType);
            }
        }

        public override void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            InternalSetValue(obj, value, invokeAttr, binder, culture, ref stackMark);
        }

        public override void SetValueDirect(TypedReference obj, Object value)
        {
            if (obj.IsNull)
                throw new ArgumentException(Environment.GetResourceString("Arg_TypedReference_Null"));
            Contract.EndContractBlock();
            unsafe
            {
                RuntimeFieldHandle.SetValueDirect(this, (RuntimeType)FieldType, &obj, value, (RuntimeType)DeclaringType);
            }
        }

        public override RuntimeFieldHandle FieldHandle
        {
            get
            {
                Type declaringType = DeclaringType;
                if ((declaringType == null && Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInReflectionOnly"));
                return new RuntimeFieldHandle(this);
            }
        }

        internal IntPtr GetFieldHandle()
        {
            return m_fieldHandle;
        }

        public override FieldAttributes Attributes
        {
            get
            {
                return m_fieldAttributes;
            }
        }

        public override Type FieldType
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_fieldType == null)
                    m_fieldType = new Signature(this, m_declaringType).FieldType;
                return m_fieldType;
            }
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return new Signature(this, m_declaringType).GetCustomModifiers(1, true);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return new Signature(this, m_declaringType).GetCustomModifiers(1, false);
        }
    }

    internal sealed unsafe class MdFieldInfo : RuntimeFieldInfo, ISerializable
    {
        private int m_tkField;
        private string m_name;
        private RuntimeType m_fieldType;
        private FieldAttributes m_fieldAttributes;
        internal MdFieldInfo(int tkField, FieldAttributes fieldAttributes, RuntimeTypeHandle declaringTypeHandle, RuntimeType.RuntimeTypeCache reflectedTypeCache, BindingFlags bindingFlags): base (reflectedTypeCache, declaringTypeHandle.GetRuntimeType(), bindingFlags)
        {
            m_tkField = tkField;
            m_name = null;
            m_fieldAttributes = fieldAttributes;
        }

        internal override bool CacheEquals(object o)
        {
            MdFieldInfo m = o as MdFieldInfo;
            if ((object)m == null)
                return false;
            return m.m_tkField == m_tkField && m_declaringType.GetTypeHandleInternal().GetModuleHandle().Equals(m.m_declaringType.GetTypeHandleInternal().GetModuleHandle());
        }

        public override String Name
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_name == null)
                    m_name = GetRuntimeModule().MetadataImport.GetName(m_tkField).ToString();
                return m_name;
            }
        }

        public override int MetadataToken
        {
            get
            {
                return m_tkField;
            }
        }

        internal override RuntimeModule GetRuntimeModule()
        {
            return m_declaringType.GetRuntimeModule();
        }

        public override RuntimeFieldHandle FieldHandle
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override FieldAttributes Attributes
        {
            get
            {
                return m_fieldAttributes;
            }
        }

        public override bool IsSecurityCritical
        {
            get
            {
                return DeclaringType.IsSecurityCritical;
            }
        }

        public override bool IsSecuritySafeCritical
        {
            get
            {
                return DeclaringType.IsSecuritySafeCritical;
            }
        }

        public override bool IsSecurityTransparent
        {
            get
            {
                return DeclaringType.IsSecurityTransparent;
            }
        }

        public override Object GetValueDirect(TypedReference obj)
        {
            return GetValue(null);
        }

        public override void SetValueDirect(TypedReference obj, Object value)
        {
            throw new FieldAccessException(Environment.GetResourceString("Acc_ReadOnly"));
        }

        public unsafe override Object GetValue(Object obj)
        {
            return GetValue(false);
        }

        public unsafe override Object GetRawConstantValue()
        {
            return GetValue(true);
        }

        private unsafe Object GetValue(bool raw)
        {
            Object value = MdConstant.GetValue(GetRuntimeModule().MetadataImport, m_tkField, FieldType.GetTypeHandleInternal(), raw);
            if (value == DBNull.Value)
                throw new NotSupportedException(Environment.GetResourceString("Arg_EnumLitValueNotFound"));
            return value;
        }

        public override void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            throw new FieldAccessException(Environment.GetResourceString("Acc_ReadOnly"));
        }

        public override Type FieldType
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_fieldType == null)
                {
                    ConstArray fieldMarshal = GetRuntimeModule().MetadataImport.GetSigOfFieldDef(m_tkField);
                    m_fieldType = new Signature(fieldMarshal.Signature.ToPointer(), (int)fieldMarshal.Length, m_declaringType).FieldType;
                }

                return m_fieldType;
            }
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return EmptyArray<Type>.Value;
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return EmptyArray<Type>.Value;
        }
    }
}