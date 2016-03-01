namespace System.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Runtime;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Text;
    using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

    public abstract class PropertyInfo : MemberInfo, _PropertyInfo
    {
        protected PropertyInfo()
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
                return System.Reflection.MemberTypes.Property;
            }
        }

        public virtual object GetConstantValue()
        {
            throw new NotImplementedException();
        }

        public virtual object GetRawConstantValue()
        {
            throw new NotImplementedException();
        }

        public abstract Type PropertyType
        {
            get;
        }

        public abstract void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, Object[] index, CultureInfo culture);
        public abstract MethodInfo[] GetAccessors(bool nonPublic);
        public abstract MethodInfo GetGetMethod(bool nonPublic);
        public abstract MethodInfo GetSetMethod(bool nonPublic);
        public abstract ParameterInfo[] GetIndexParameters();
        public abstract PropertyAttributes Attributes
        {
            get;
        }

        public abstract bool CanRead
        {
            get;
        }

        public abstract bool CanWrite
        {
            get;
        }

        public Object GetValue(Object obj)
        {
            return GetValue(obj, null);
        }

        public virtual Object GetValue(Object obj, Object[] index)
        {
            return GetValue(obj, BindingFlags.Default, null, index, null);
        }

        public abstract Object GetValue(Object obj, BindingFlags invokeAttr, Binder binder, Object[] index, CultureInfo culture);
        public void SetValue(Object obj, Object value)
        {
            SetValue(obj, value, null);
        }

        public virtual void SetValue(Object obj, Object value, Object[] index)
        {
            SetValue(obj, value, BindingFlags.Default, null, index, null);
        }

        public virtual Type[] GetRequiredCustomModifiers()
        {
            return EmptyArray<Type>.Value;
        }

        public virtual Type[] GetOptionalCustomModifiers()
        {
            return EmptyArray<Type>.Value;
        }

        public MethodInfo[] GetAccessors()
        {
            return GetAccessors(false);
        }

        public virtual MethodInfo GetMethod
        {
            get
            {
                return GetGetMethod(true);
            }
        }

        public virtual MethodInfo SetMethod
        {
            get
            {
                return GetSetMethod(true);
            }
        }

        public MethodInfo GetGetMethod()
        {
            return GetGetMethod(false);
        }

        public MethodInfo GetSetMethod()
        {
            return GetSetMethod(false);
        }

        public bool IsSpecialName
        {
            get
            {
                return (Attributes & PropertyAttributes.SpecialName) != 0;
            }
        }
    }

    internal unsafe sealed class RuntimePropertyInfo : PropertyInfo, ISerializable
    {
        private int m_token;
        private string m_name;
        private void *m_utf8name;
        private PropertyAttributes m_flags;
        private RuntimeTypeCache m_reflectedTypeCache;
        private RuntimeMethodInfo m_getterMethod;
        private RuntimeMethodInfo m_setterMethod;
        private MethodInfo[] m_otherMethod;
        private RuntimeType m_declaringType;
        private BindingFlags m_bindingFlags;
        private Signature m_signature;
        private ParameterInfo[] m_parameters;
        internal RuntimePropertyInfo(int tkProperty, RuntimeType declaredType, RuntimeTypeCache reflectedTypeCache, out bool isPrivate)
        {
            Contract.Requires(declaredType != null);
            Contract.Requires(reflectedTypeCache != null);
            Contract.Assert(!reflectedTypeCache.IsGlobal);
            MetadataImport scope = declaredType.GetRuntimeModule().MetadataImport;
            m_token = tkProperty;
            m_reflectedTypeCache = reflectedTypeCache;
            m_declaringType = declaredType;
            ConstArray sig;
            scope.GetPropertyProps(tkProperty, out m_utf8name, out m_flags, out sig);
            RuntimeMethodInfo dummy;
            Associates.AssignAssociates(scope, tkProperty, declaredType, reflectedTypeCache.GetRuntimeType(), out dummy, out dummy, out dummy, out m_getterMethod, out m_setterMethod, out m_otherMethod, out isPrivate, out m_bindingFlags);
        }

        internal override bool CacheEquals(object o)
        {
            RuntimePropertyInfo m = o as RuntimePropertyInfo;
            if ((object)m == null)
                return false;
            return m.m_token == m_token && RuntimeTypeHandle.GetModule(m_declaringType).Equals(RuntimeTypeHandle.GetModule(m.m_declaringType));
        }

        internal Signature Signature
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_signature == null)
                {
                    PropertyAttributes flags;
                    ConstArray sig;
                    void *name;
                    GetRuntimeModule().MetadataImport.GetPropertyProps(m_token, out name, out flags, out sig);
                    m_signature = new Signature(sig.Signature.ToPointer(), (int)sig.Length, m_declaringType);
                }

                return m_signature;
            }
        }

        internal bool EqualsSig(RuntimePropertyInfo target)
        {
            Contract.Requires(Name.Equals(target.Name));
            Contract.Requires(this != target);
            Contract.Requires(this.ReflectedType == target.ReflectedType);
            if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                return Signature.CompareSigForAppCompat(this.Signature, this.m_declaringType, target.Signature, target.m_declaringType);
            return Signature.CompareSig(this.Signature, target.Signature);
        }

        internal BindingFlags BindingFlags
        {
            get
            {
                return m_bindingFlags;
            }
        }

        internal bool HasMatchingAccessibility(RuntimePropertyInfo target)
        {
            Contract.Assert(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8);
            bool match = true;
            if (!IsMatchingAccessibility(this.GetGetMethod(true), target.GetGetMethod(true)))
            {
                match = false;
            }
            else if (!IsMatchingAccessibility(this.GetSetMethod(true), target.GetSetMethod(true)))
            {
                match = false;
            }

            return match;
        }

        private bool IsMatchingAccessibility(MethodInfo lhsInfo, MethodInfo rhsInfo)
        {
            if (lhsInfo != null && rhsInfo != null)
            {
                return lhsInfo.IsPublic == rhsInfo.IsPublic;
            }
            else
            {
                return true;
            }
        }

        public override String ToString()
        {
            return FormatNameAndSig(false);
        }

        private string FormatNameAndSig(bool serialization)
        {
            StringBuilder sbName = new StringBuilder(PropertyType.FormatTypeName(serialization));
            sbName.Append(" ");
            sbName.Append(Name);
            RuntimeType[] arguments = Signature.Arguments;
            if (arguments.Length > 0)
            {
                sbName.Append(" [");
                sbName.Append(MethodBase.ConstructParameters(arguments, Signature.CallingConvention, serialization));
                sbName.Append("]");
            }

            return sbName.ToString();
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

        public override MemberTypes MemberType
        {
            get
            {
                return MemberTypes.Property;
            }
        }

        public override String Name
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (m_name == null)
                    m_name = new Utf8String(m_utf8name).ToString();
                return m_name;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return m_declaringType;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return ReflectedTypeInternal;
            }
        }

        private RuntimeType ReflectedTypeInternal
        {
            get
            {
                return m_reflectedTypeCache.GetRuntimeType();
            }
        }

        public override int MetadataToken
        {
            get
            {
                return m_token;
            }
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
            return m_declaringType.GetRuntimeModule();
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return Signature.GetCustomModifiers(0, true);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return Signature.GetCustomModifiers(0, false);
        }

        internal object GetConstantValue(bool raw)
        {
            Object defaultValue = MdConstant.GetValue(GetRuntimeModule().MetadataImport, m_token, PropertyType.GetTypeHandleInternal(), raw);
            if (defaultValue == DBNull.Value)
                throw new InvalidOperationException(Environment.GetResourceString("Arg_EnumLitValueNotFound"));
            return defaultValue;
        }

        public override object GetConstantValue()
        {
            return GetConstantValue(false);
        }

        public override object GetRawConstantValue()
        {
            return GetConstantValue(true);
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            List<MethodInfo> accessorList = new List<MethodInfo>();
            if (Associates.IncludeAccessor(m_getterMethod, nonPublic))
                accessorList.Add(m_getterMethod);
            if (Associates.IncludeAccessor(m_setterMethod, nonPublic))
                accessorList.Add(m_setterMethod);
            if ((object)m_otherMethod != null)
            {
                for (int i = 0; i < m_otherMethod.Length; i++)
                {
                    if (Associates.IncludeAccessor(m_otherMethod[i] as MethodInfo, nonPublic))
                        accessorList.Add(m_otherMethod[i]);
                }
            }

            return accessorList.ToArray();
        }

        public override Type PropertyType
        {
            get
            {
                return Signature.ReturnType;
            }
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            if (!Associates.IncludeAccessor(m_getterMethod, nonPublic))
                return null;
            return m_getterMethod;
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            if (!Associates.IncludeAccessor(m_setterMethod, nonPublic))
                return null;
            return m_setterMethod;
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            ParameterInfo[] indexParams = GetIndexParametersNoCopy();
            int numParams = indexParams.Length;
            if (numParams == 0)
                return indexParams;
            ParameterInfo[] ret = new ParameterInfo[numParams];
            Array.Copy(indexParams, ret, numParams);
            return ret;
        }

        internal ParameterInfo[] GetIndexParametersNoCopy()
        {
            if (m_parameters == null)
            {
                int numParams = 0;
                ParameterInfo[] methParams = null;
                MethodInfo m = GetGetMethod(true);
                if (m != null)
                {
                    methParams = m.GetParametersNoCopy();
                    numParams = methParams.Length;
                }
                else
                {
                    m = GetSetMethod(true);
                    if (m != null)
                    {
                        methParams = m.GetParametersNoCopy();
                        numParams = methParams.Length - 1;
                    }
                }

                ParameterInfo[] propParams = new ParameterInfo[numParams];
                for (int i = 0; i < numParams; i++)
                    propParams[i] = new RuntimeParameterInfo((RuntimeParameterInfo)methParams[i], this);
                m_parameters = propParams;
            }

            return m_parameters;
        }

        public override PropertyAttributes Attributes
        {
            get
            {
                return m_flags;
            }
        }

        public override bool CanRead
        {
            get
            {
                return m_getterMethod != null;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return m_setterMethod != null;
            }
        }

        public override Object GetValue(Object obj, Object[] index)
        {
            return GetValue(obj, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, index, null);
        }

        public override Object GetValue(Object obj, BindingFlags invokeAttr, Binder binder, Object[] index, CultureInfo culture)
        {
            MethodInfo m = GetGetMethod(true);
            if (m == null)
                throw new ArgumentException(System.Environment.GetResourceString("Arg_GetMethNotFnd"));
            return m.Invoke(obj, invokeAttr, binder, index, null);
        }

        public override void SetValue(Object obj, Object value, Object[] index)
        {
            SetValue(obj, value, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, index, null);
        }

        public override void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, Object[] index, CultureInfo culture)
        {
            MethodInfo m = GetSetMethod(true);
            if (m == null)
                throw new ArgumentException(System.Environment.GetResourceString("Arg_SetMethNotFnd"));
            Object[] args = null;
            if (index != null)
            {
                args = new Object[index.Length + 1];
                for (int i = 0; i < index.Length; i++)
                    args[i] = index[i];
                args[index.Length] = value;
            }
            else
            {
                args = new Object[1];
                args[0] = value;
            }

            m.Invoke(obj, invokeAttr, binder, args, culture);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            MemberInfoSerializationHolder.GetSerializationInfo(info, Name, ReflectedTypeInternal, ToString(), SerializationToString(), MemberTypes.Property, null);
        }

        internal string SerializationToString()
        {
            return FormatNameAndSig(true);
        }
    }
}