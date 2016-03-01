namespace System.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;
    using System.Threading;
    using MdToken = System.Reflection.MetadataToken;

    public class ParameterInfo : _ParameterInfo, ICustomAttributeProvider, IObjectReference
    {
        protected String NameImpl;
        protected Type ClassImpl;
        protected int PositionImpl;
        protected ParameterAttributes AttrsImpl;
        protected Object DefaultValueImpl;
        protected MemberInfo MemberImpl;
        private IntPtr _importer;
        private int _token;
        private bool bExtraConstChecked;
        protected ParameterInfo()
        {
        }

        internal void SetName(String name)
        {
            NameImpl = name;
        }

        internal void SetAttributes(ParameterAttributes attributes)
        {
            AttrsImpl = attributes;
        }

        public virtual Type ParameterType
        {
            get
            {
                return ClassImpl;
            }
        }

        public virtual String Name
        {
            get
            {
                return NameImpl;
            }
        }

        public virtual bool HasDefaultValue
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual Object DefaultValue
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual Object RawDefaultValue
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual int Position
        {
            get
            {
                return PositionImpl;
            }
        }

        public virtual ParameterAttributes Attributes
        {
            get
            {
                return AttrsImpl;
            }
        }

        public virtual MemberInfo Member
        {
            get
            {
                Contract.Ensures(Contract.Result<MemberInfo>() != null);
                return MemberImpl;
            }
        }

        public bool IsIn
        {
            get
            {
                return ((Attributes & ParameterAttributes.In) != 0);
            }
        }

        public bool IsOut
        {
            get
            {
                return ((Attributes & ParameterAttributes.Out) != 0);
            }
        }

        public bool IsRetval
        {
            get
            {
                return ((Attributes & ParameterAttributes.Retval) != 0);
            }
        }

        public bool IsOptional
        {
            get
            {
                return ((Attributes & ParameterAttributes.Optional) != 0);
            }
        }

        public virtual int MetadataToken
        {
            get
            {
                RuntimeParameterInfo rtParam = this as RuntimeParameterInfo;
                if (rtParam != null)
                    return rtParam.MetadataToken;
                return (int)MetadataTokenType.ParamDef;
            }
        }

        public virtual Type[] GetRequiredCustomModifiers()
        {
            return EmptyArray<Type>.Value;
        }

        public virtual Type[] GetOptionalCustomModifiers()
        {
            return EmptyArray<Type>.Value;
        }

        public override String ToString()
        {
            return ParameterType.FormatTypeName() + " " + Name;
        }

        public virtual IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return GetCustomAttributesData();
            }
        }

        public virtual Object[] GetCustomAttributes(bool inherit)
        {
            return EmptyArray<Object>.Value;
        }

        public virtual Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            return EmptyArray<Object>.Value;
        }

        public virtual bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            return false;
        }

        public virtual IList<CustomAttributeData> GetCustomAttributesData()
        {
            throw new NotImplementedException();
        }

        public object GetRealObject(StreamingContext context)
        {
            Contract.Ensures(Contract.Result<Object>() != null);
            if (MemberImpl == null)
                throw new SerializationException(Environment.GetResourceString(ResId.Serialization_InsufficientState));
            ParameterInfo[] args = null;
            switch (MemberImpl.MemberType)
            {
                case MemberTypes.Constructor:
                case MemberTypes.Method:
                    if (PositionImpl == -1)
                    {
                        if (MemberImpl.MemberType == MemberTypes.Method)
                            return ((MethodInfo)MemberImpl).ReturnParameter;
                        else
                            throw new SerializationException(Environment.GetResourceString(ResId.Serialization_BadParameterInfo));
                    }
                    else
                    {
                        args = ((MethodBase)MemberImpl).GetParametersNoCopy();
                        if (args != null && PositionImpl < args.Length)
                            return args[PositionImpl];
                        else
                            throw new SerializationException(Environment.GetResourceString(ResId.Serialization_BadParameterInfo));
                    }

                case MemberTypes.Property:
                    args = ((RuntimePropertyInfo)MemberImpl).GetIndexParametersNoCopy();
                    if (args != null && PositionImpl > -1 && PositionImpl < args.Length)
                        return args[PositionImpl];
                    else
                        throw new SerializationException(Environment.GetResourceString(ResId.Serialization_BadParameterInfo));
                default:
                    throw new SerializationException(Environment.GetResourceString(ResId.Serialization_NoParameterInfo));
            }
        }
    }

    internal unsafe sealed class RuntimeParameterInfo : ParameterInfo, ISerializable
    {
        internal unsafe static ParameterInfo[] GetParameters(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
        {
            Contract.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);
            ParameterInfo dummy;
            return GetParameters(method, member, sig, out dummy, false);
        }

        internal unsafe static ParameterInfo GetReturnParameter(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
        {
            Contract.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);
            ParameterInfo returnParameter;
            GetParameters(method, member, sig, out returnParameter, true);
            return returnParameter;
        }

        internal unsafe static ParameterInfo[] GetParameters(IRuntimeMethodInfo methodHandle, MemberInfo member, Signature sig, out ParameterInfo returnParameter, bool fetchReturnParameter)
        {
            returnParameter = null;
            int sigArgCount = sig.Arguments.Length;
            ParameterInfo[] args = fetchReturnParameter ? null : new ParameterInfo[sigArgCount];
            int tkMethodDef = RuntimeMethodHandle.GetMethodDef(methodHandle);
            int cParamDefs = 0;
            if (!MdToken.IsNullToken(tkMethodDef))
            {
                MetadataImport scope = RuntimeTypeHandle.GetMetadataImport(RuntimeMethodHandle.GetDeclaringType(methodHandle));
                MetadataEnumResult tkParamDefs;
                scope.EnumParams(tkMethodDef, out tkParamDefs);
                cParamDefs = tkParamDefs.Length;
                if (cParamDefs > sigArgCount + 1)
                    throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ParameterSignatureMismatch"));
                for (int i = 0; i < cParamDefs; i++)
                {
                    ParameterAttributes attr;
                    int position, tkParamDef = tkParamDefs[i];
                    scope.GetParamDefProps(tkParamDef, out position, out attr);
                    position--;
                    if (fetchReturnParameter == true && position == -1)
                    {
                        if (returnParameter != null)
                            throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ParameterSignatureMismatch"));
                        returnParameter = new RuntimeParameterInfo(sig, scope, tkParamDef, position, attr, member);
                    }
                    else if (fetchReturnParameter == false && position >= 0)
                    {
                        if (position >= sigArgCount)
                            throw new BadImageFormatException(Environment.GetResourceString("BadImageFormat_ParameterSignatureMismatch"));
                        args[position] = new RuntimeParameterInfo(sig, scope, tkParamDef, position, attr, member);
                    }
                }
            }

            if (fetchReturnParameter)
            {
                if (returnParameter == null)
                {
                    returnParameter = new RuntimeParameterInfo(sig, MetadataImport.EmptyImport, 0, -1, (ParameterAttributes)0, member);
                }
            }
            else
            {
                if (cParamDefs < args.Length + 1)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i] != null)
                            continue;
                        args[i] = new RuntimeParameterInfo(sig, MetadataImport.EmptyImport, 0, i, (ParameterAttributes)0, member);
                    }
                }
            }

            return args;
        }

        private static readonly Type s_DecimalConstantAttributeType = typeof (DecimalConstantAttribute);
        private static readonly Type s_CustomConstantAttributeType = typeof (CustomConstantAttribute);
        private int m_tkParamDef;
        private MetadataImport m_scope;
        private Signature m_signature;
        private volatile bool m_nameIsCached = false;
        private readonly bool m_noMetadata = false;
        private bool m_noDefaultValue = false;
        private MethodBase m_originalMember = null;
        internal MethodBase DefiningMethod
        {
            get
            {
                MethodBase result = m_originalMember != null ? m_originalMember : MemberImpl as MethodBase;
                Contract.Assert(result != null);
                return result;
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            info.SetType(typeof (ParameterInfo));
            info.AddValue("AttrsImpl", Attributes);
            info.AddValue("ClassImpl", ParameterType);
            info.AddValue("DefaultValueImpl", DefaultValue);
            info.AddValue("MemberImpl", Member);
            info.AddValue("NameImpl", Name);
            info.AddValue("PositionImpl", Position);
            info.AddValue("_token", m_tkParamDef);
        }

        internal RuntimeParameterInfo(RuntimeParameterInfo accessor, RuntimePropertyInfo property): this (accessor, (MemberInfo)property)
        {
            m_signature = property.Signature;
        }

        private RuntimeParameterInfo(RuntimeParameterInfo accessor, MemberInfo member)
        {
            MemberImpl = member;
            m_originalMember = accessor.MemberImpl as MethodBase;
            Contract.Assert(m_originalMember != null);
            NameImpl = accessor.Name;
            m_nameIsCached = true;
            ClassImpl = accessor.ParameterType;
            PositionImpl = accessor.Position;
            AttrsImpl = accessor.Attributes;
            m_tkParamDef = MdToken.IsNullToken(accessor.MetadataToken) ? (int)MetadataTokenType.ParamDef : accessor.MetadataToken;
            m_scope = accessor.m_scope;
        }

        private RuntimeParameterInfo(Signature signature, MetadataImport scope, int tkParamDef, int position, ParameterAttributes attributes, MemberInfo member)
        {
            Contract.Requires(member != null);
            Contract.Assert(MdToken.IsNullToken(tkParamDef) == scope.Equals(MetadataImport.EmptyImport));
            Contract.Assert(MdToken.IsNullToken(tkParamDef) || MdToken.IsTokenOfType(tkParamDef, MetadataTokenType.ParamDef));
            PositionImpl = position;
            MemberImpl = member;
            m_signature = signature;
            m_tkParamDef = MdToken.IsNullToken(tkParamDef) ? (int)MetadataTokenType.ParamDef : tkParamDef;
            m_scope = scope;
            AttrsImpl = attributes;
            ClassImpl = null;
            NameImpl = null;
        }

        internal RuntimeParameterInfo(MethodInfo owner, String name, Type parameterType, int position)
        {
            MemberImpl = owner;
            NameImpl = name;
            m_nameIsCached = true;
            m_noMetadata = true;
            ClassImpl = parameterType;
            PositionImpl = position;
            AttrsImpl = ParameterAttributes.None;
            m_tkParamDef = (int)MetadataTokenType.ParamDef;
            m_scope = MetadataImport.EmptyImport;
        }

        public override Type ParameterType
        {
            get
            {
                if (ClassImpl == null)
                {
                    RuntimeType parameterType;
                    if (PositionImpl == -1)
                        parameterType = m_signature.ReturnType;
                    else
                        parameterType = m_signature.Arguments[PositionImpl];
                    Contract.Assert(parameterType != null);
                    ClassImpl = parameterType;
                }

                return ClassImpl;
            }
        }

        public override String Name
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (!m_nameIsCached)
                {
                    if (!MdToken.IsNullToken(m_tkParamDef))
                    {
                        string name;
                        name = m_scope.GetName(m_tkParamDef).ToString();
                        NameImpl = name;
                    }

                    m_nameIsCached = true;
                }

                return NameImpl;
            }
        }

        public override bool HasDefaultValue
        {
            get
            {
                if (m_noMetadata || m_noDefaultValue)
                    return false;
                object defaultValue = GetDefaultValueInternal(false);
                return (defaultValue != DBNull.Value);
            }
        }

        public override Object DefaultValue
        {
            get
            {
                return GetDefaultValue(false);
            }
        }

        public override Object RawDefaultValue
        {
            get
            {
                return GetDefaultValue(true);
            }
        }

        private Object GetDefaultValue(bool raw)
        {
            if (m_noMetadata)
                return null;
            object defaultValue = GetDefaultValueInternal(raw);
            if (defaultValue == DBNull.Value)
            {
                if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                    defaultValue = null;
                else if (IsOptional)
                {
                    defaultValue = Type.Missing;
                }
            }

            return defaultValue;
        }

        private Object GetDefaultValueInternal(bool raw)
        {
            Contract.Assert(!m_noMetadata);
            if (m_noDefaultValue)
                return DBNull.Value;
            object defaultValue = null;
            if (ParameterType == typeof (DateTime))
            {
                if (raw)
                {
                    CustomAttributeTypedArgument value = CustomAttributeData.Filter(CustomAttributeData.GetCustomAttributes(this), typeof (DateTimeConstantAttribute), 0);
                    if (value.ArgumentType != null)
                        return new DateTime((long)value.Value);
                }
                else
                {
                    object[] dt = GetCustomAttributes(typeof (DateTimeConstantAttribute), false);
                    if (dt != null && dt.Length != 0)
                        return ((DateTimeConstantAttribute)dt[0]).Value;
                }
            }

            if (!MdToken.IsNullToken(m_tkParamDef))
            {
                defaultValue = MdConstant.GetValue(m_scope, m_tkParamDef, ParameterType.GetTypeHandleInternal(), raw);
            }

            if (defaultValue == DBNull.Value)
            {
                if (raw)
                {
                    foreach (CustomAttributeData attr in CustomAttributeData.GetCustomAttributes(this))
                    {
                        Type attrType = attr.Constructor.DeclaringType;
                        if (attrType == typeof (DateTimeConstantAttribute))
                        {
                            defaultValue = DateTimeConstantAttribute.GetRawDateTimeConstant(attr);
                        }
                        else if (attrType == typeof (DecimalConstantAttribute))
                        {
                            defaultValue = DecimalConstantAttribute.GetRawDecimalConstant(attr);
                        }
                        else if (attrType.IsSubclassOf(s_CustomConstantAttributeType))
                        {
                            defaultValue = CustomConstantAttribute.GetRawConstant(attr);
                        }
                    }
                }
                else
                {
                    Object[] CustomAttrs = GetCustomAttributes(s_CustomConstantAttributeType, false);
                    if (CustomAttrs.Length != 0)
                    {
                        defaultValue = ((CustomConstantAttribute)CustomAttrs[0]).Value;
                    }
                    else
                    {
                        CustomAttrs = GetCustomAttributes(s_DecimalConstantAttributeType, false);
                        if (CustomAttrs.Length != 0)
                        {
                            defaultValue = ((DecimalConstantAttribute)CustomAttrs[0]).Value;
                        }
                    }
                }
            }

            if (defaultValue == DBNull.Value)
                m_noDefaultValue = true;
            return defaultValue;
        }

        internal RuntimeModule GetRuntimeModule()
        {
            RuntimeMethodInfo method = Member as RuntimeMethodInfo;
            RuntimeConstructorInfo constructor = Member as RuntimeConstructorInfo;
            RuntimePropertyInfo property = Member as RuntimePropertyInfo;
            if (method != null)
                return method.GetRuntimeModule();
            else if (constructor != null)
                return constructor.GetRuntimeModule();
            else if (property != null)
                return property.GetRuntimeModule();
            else
                return null;
        }

        public override int MetadataToken
        {
            get
            {
                return m_tkParamDef;
            }
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return m_signature.GetCustomModifiers(PositionImpl + 1, true);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return m_signature.GetCustomModifiers(PositionImpl + 1, false);
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            if (MdToken.IsNullToken(m_tkParamDef))
                return EmptyArray<Object>.Value;
            return CustomAttribute.GetCustomAttributes(this, typeof (object) as RuntimeType);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");
            Contract.EndContractBlock();
            if (MdToken.IsNullToken(m_tkParamDef))
                return EmptyArray<Object>.Value;
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
            if (MdToken.IsNullToken(m_tkParamDef))
                return false;
            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;
            if (attributeRuntimeType == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "attributeType");
            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }
    }
}