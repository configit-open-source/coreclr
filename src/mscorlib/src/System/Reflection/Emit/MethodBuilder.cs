using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Diagnostics.SymbolStore;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Reflection.Emit
{
    public sealed class MethodBuilder : MethodInfo, _MethodBuilder
    {
        internal String m_strName;
        private MethodToken m_tkMethod;
        private ModuleBuilder m_module;
        internal TypeBuilder m_containingType;
        private int[] m_mdMethodFixups;
        private byte[] m_localSignature;
        internal LocalSymInfo m_localSymInfo;
        internal ILGenerator m_ilGenerator;
        private byte[] m_ubBody;
        private ExceptionHandler[] m_exceptions;
        private const int DefaultMaxStack = 16;
        private int m_maxStack = DefaultMaxStack;
        internal bool m_bIsBaked;
        private bool m_bIsGlobalMethod;
        private bool m_fInitLocals;
        private MethodAttributes m_iAttributes;
        private CallingConventions m_callingConvention;
        private MethodImplAttributes m_dwMethodImplFlags;
        private SignatureHelper m_signature;
        internal Type[] m_parameterTypes;
        private ParameterBuilder m_retParam;
        private Type m_returnType;
        private Type[] m_returnTypeRequiredCustomModifiers;
        private Type[] m_returnTypeOptionalCustomModifiers;
        private Type[][] m_parameterTypeRequiredCustomModifiers;
        private Type[][] m_parameterTypeOptionalCustomModifiers;
        private GenericTypeParameterBuilder[] m_inst;
        private bool m_bIsGenMethDef;
        internal MethodBuilder(String name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, ModuleBuilder mod, TypeBuilder type, bool bIsGlobalMethod)
        {
            Init(name, attributes, callingConvention, returnType, null, null, parameterTypes, null, null, mod, type, bIsGlobalMethod);
        }

        internal MethodBuilder(String name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers, ModuleBuilder mod, TypeBuilder type, bool bIsGlobalMethod)
        {
            Init(name, attributes, callingConvention, returnType, returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes, parameterTypeRequiredCustomModifiers, parameterTypeOptionalCustomModifiers, mod, type, bIsGlobalMethod);
        }

        private void Init(String name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers, ModuleBuilder mod, TypeBuilder type, bool bIsGlobalMethod)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "name");
            if (name[0] == '\0')
                throw new ArgumentException(Environment.GetResourceString("Argument_IllegalName"), "name");
            if (mod == null)
                throw new ArgumentNullException("mod");
            Contract.EndContractBlock();
            if (parameterTypes != null)
            {
                foreach (Type t in parameterTypes)
                {
                    if (t == null)
                        throw new ArgumentNullException("parameterTypes");
                }
            }

            m_strName = name;
            m_module = mod;
            m_containingType = type;
            {
                m_returnType = returnType;
            }

            if ((attributes & MethodAttributes.Static) == 0)
            {
                callingConvention = callingConvention | CallingConventions.HasThis;
            }
            else if ((attributes & MethodAttributes.Virtual) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_NoStaticVirtual"));
            }

            if ((attributes & MethodAttributes.SpecialName) != MethodAttributes.SpecialName)
            {
                if ((type.Attributes & TypeAttributes.Interface) == TypeAttributes.Interface)
                {
                    if ((attributes & (MethodAttributes.Abstract | MethodAttributes.Virtual)) != (MethodAttributes.Abstract | MethodAttributes.Virtual) && (attributes & MethodAttributes.Static) == 0)
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadAttributeOnInterfaceMethod"));
                }
            }

            m_callingConvention = callingConvention;
            if (parameterTypes != null)
            {
                m_parameterTypes = new Type[parameterTypes.Length];
                Array.Copy(parameterTypes, m_parameterTypes, parameterTypes.Length);
            }
            else
            {
                m_parameterTypes = null;
            }

            m_returnTypeRequiredCustomModifiers = returnTypeRequiredCustomModifiers;
            m_returnTypeOptionalCustomModifiers = returnTypeOptionalCustomModifiers;
            m_parameterTypeRequiredCustomModifiers = parameterTypeRequiredCustomModifiers;
            m_parameterTypeOptionalCustomModifiers = parameterTypeOptionalCustomModifiers;
            m_iAttributes = attributes;
            m_bIsGlobalMethod = bIsGlobalMethod;
            m_bIsBaked = false;
            m_fInitLocals = true;
            m_localSymInfo = new LocalSymInfo();
            m_ubBody = null;
            m_ilGenerator = null;
            m_dwMethodImplFlags = MethodImplAttributes.IL;
        }

        internal void CheckContext(params Type[][] typess)
        {
            m_module.CheckContext(typess);
        }

        internal void CheckContext(params Type[] types)
        {
            m_module.CheckContext(types);
        }

        internal void CreateMethodBodyHelper(ILGenerator il)
        {
            if (il == null)
            {
                throw new ArgumentNullException("il");
            }

            Contract.EndContractBlock();
            __ExceptionInfo[] excp;
            int counter = 0;
            int[] filterAddrs;
            int[] catchAddrs;
            int[] catchEndAddrs;
            Type[] catchClass;
            int[] type;
            int numCatch;
            int start, end;
            ModuleBuilder dynMod = (ModuleBuilder)m_module;
            m_containingType.ThrowIfCreated();
            if (m_bIsBaked)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MethodHasBody"));
            }

            if (il.m_methodBuilder != this && il.m_methodBuilder != null)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadILGeneratorUsage"));
            }

            ThrowIfShouldNotHaveBody();
            if (il.m_ScopeTree.m_iOpenScopeCount != 0)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_OpenLocalVariableScope"));
            }

            m_ubBody = il.BakeByteArray();
            m_mdMethodFixups = il.GetTokenFixups();
            excp = il.GetExceptions();
            int numExceptions = CalculateNumberOfExceptions(excp);
            if (numExceptions > 0)
            {
                m_exceptions = new ExceptionHandler[numExceptions];
                for (int i = 0; i < excp.Length; i++)
                {
                    filterAddrs = excp[i].GetFilterAddresses();
                    catchAddrs = excp[i].GetCatchAddresses();
                    catchEndAddrs = excp[i].GetCatchEndAddresses();
                    catchClass = excp[i].GetCatchClass();
                    numCatch = excp[i].GetNumberOfCatches();
                    start = excp[i].GetStartAddress();
                    end = excp[i].GetEndAddress();
                    type = excp[i].GetExceptionTypes();
                    for (int j = 0; j < numCatch; j++)
                    {
                        int tkExceptionClass = 0;
                        if (catchClass[j] != null)
                        {
                            tkExceptionClass = dynMod.GetTypeTokenInternal(catchClass[j]).Token;
                        }

                        switch (type[j])
                        {
                            case __ExceptionInfo.None:
                            case __ExceptionInfo.Fault:
                            case __ExceptionInfo.Filter:
                                m_exceptions[counter++] = new ExceptionHandler(start, end, filterAddrs[j], catchAddrs[j], catchEndAddrs[j], type[j], tkExceptionClass);
                                break;
                            case __ExceptionInfo.Finally:
                                m_exceptions[counter++] = new ExceptionHandler(start, excp[i].GetFinallyEndAddress(), filterAddrs[j], catchAddrs[j], catchEndAddrs[j], type[j], tkExceptionClass);
                                break;
                        }
                    }
                }
            }

            m_bIsBaked = true;
            if (dynMod.GetSymWriter() != null)
            {
                SymbolToken tk = new SymbolToken(MetadataTokenInternal);
                ISymbolWriter symWriter = dynMod.GetSymWriter();
                symWriter.OpenMethod(tk);
                symWriter.OpenScope(0);
                if (m_symCustomAttrs != null)
                {
                    foreach (SymCustomAttr symCustomAttr in m_symCustomAttrs)
                        dynMod.GetSymWriter().SetSymAttribute(new SymbolToken(MetadataTokenInternal), symCustomAttr.m_name, symCustomAttr.m_data);
                }

                if (m_localSymInfo != null)
                    m_localSymInfo.EmitLocalSymInfo(symWriter);
                il.m_ScopeTree.EmitScopeTree(symWriter);
                il.m_LineNumberInfo.EmitLineNumberInfo(symWriter);
                symWriter.CloseScope(il.ILOffset);
                symWriter.CloseMethod();
            }
        }

        internal void ReleaseBakedStructures()
        {
            if (!m_bIsBaked)
            {
                return;
            }

            m_ubBody = null;
            m_localSymInfo = null;
            m_mdMethodFixups = null;
            m_localSignature = null;
            m_exceptions = null;
        }

        internal override Type[] GetParameterTypes()
        {
            if (m_parameterTypes == null)
                m_parameterTypes = EmptyArray<Type>.Value;
            return m_parameterTypes;
        }

        internal static Type GetMethodBaseReturnType(MethodBase method)
        {
            MethodInfo mi = null;
            ConstructorInfo ci = null;
            if ((mi = method as MethodInfo) != null)
            {
                return mi.ReturnType;
            }
            else if ((ci = method as ConstructorInfo) != null)
            {
                return ci.GetReturnType();
            }
            else
            {
                Contract.Assert(false, "We should never get here!");
                return null;
            }
        }

        internal void SetToken(MethodToken token)
        {
            m_tkMethod = token;
        }

        internal byte[] GetBody()
        {
            return m_ubBody;
        }

        internal int[] GetTokenFixups()
        {
            return m_mdMethodFixups;
        }

        internal SignatureHelper GetMethodSignature()
        {
            if (m_parameterTypes == null)
                m_parameterTypes = EmptyArray<Type>.Value;
            m_signature = SignatureHelper.GetMethodSigHelper(m_module, m_callingConvention, m_inst != null ? m_inst.Length : 0, m_returnType == null ? typeof (void) : m_returnType, m_returnTypeRequiredCustomModifiers, m_returnTypeOptionalCustomModifiers, m_parameterTypes, m_parameterTypeRequiredCustomModifiers, m_parameterTypeOptionalCustomModifiers);
            return m_signature;
        }

        internal byte[] GetLocalSignature(out int signatureLength)
        {
            if (m_localSignature != null)
            {
                signatureLength = m_localSignature.Length;
                return m_localSignature;
            }

            if (m_ilGenerator != null)
            {
                if (m_ilGenerator.m_localCount != 0)
                {
                    return m_ilGenerator.m_localSignature.InternalGetSignature(out signatureLength);
                }
            }

            return SignatureHelper.GetLocalVarSigHelper(m_module).InternalGetSignature(out signatureLength);
        }

        internal int GetMaxStack()
        {
            if (m_ilGenerator != null)
            {
                return m_ilGenerator.GetMaxStackSize() + ExceptionHandlerCount;
            }
            else
            {
                return m_maxStack;
            }
        }

        internal ExceptionHandler[] GetExceptionHandlers()
        {
            return m_exceptions;
        }

        internal int ExceptionHandlerCount
        {
            get
            {
                return m_exceptions != null ? m_exceptions.Length : 0;
            }
        }

        internal int CalculateNumberOfExceptions(__ExceptionInfo[] excp)
        {
            int num = 0;
            if (excp == null)
            {
                return 0;
            }

            for (int i = 0; i < excp.Length; i++)
            {
                num += excp[i].GetNumberOfCatches();
            }

            return num;
        }

        internal bool IsTypeCreated()
        {
            return (m_containingType != null && m_containingType.IsCreated());
        }

        internal TypeBuilder GetTypeBuilder()
        {
            return m_containingType;
        }

        internal ModuleBuilder GetModuleBuilder()
        {
            return m_module;
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is MethodBuilder))
            {
                return false;
            }

            if (!(this.m_strName.Equals(((MethodBuilder)obj).m_strName)))
            {
                return false;
            }

            if (m_iAttributes != (((MethodBuilder)obj).m_iAttributes))
            {
                return false;
            }

            SignatureHelper thatSig = ((MethodBuilder)obj).GetMethodSignature();
            if (thatSig.Equals(GetMethodSignature()))
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return this.m_strName.GetHashCode();
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder(1000);
            sb.Append("Name: " + m_strName + " " + Environment.NewLine);
            sb.Append("Attributes: " + (int)m_iAttributes + Environment.NewLine);
            sb.Append("Method Signature: " + GetMethodSignature() + Environment.NewLine);
            sb.Append(Environment.NewLine);
            return sb.ToString();
        }

        public override String Name
        {
            get
            {
                return m_strName;
            }
        }

        internal int MetadataTokenInternal
        {
            get
            {
                return GetToken().Token;
            }
        }

        public override Module Module
        {
            get
            {
                return m_containingType.Module;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                if (m_containingType.m_isHiddenGlobalType == true)
                    return null;
                return m_containingType;
            }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get
            {
                return null;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return DeclaringType;
            }
        }

        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return m_dwMethodImplFlags;
        }

        public override MethodAttributes Attributes
        {
            get
            {
                return m_iAttributes;
            }
        }

        public override CallingConventions CallingConvention
        {
            get
            {
                return m_callingConvention;
            }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
            }
        }

        public override bool IsSecurityCritical
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
            }
        }

        public override bool IsSecuritySafeCritical
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
            }
        }

        public override bool IsSecurityTransparent
        {
            get
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
            }
        }

        public override MethodInfo GetBaseDefinition()
        {
            return this;
        }

        public override Type ReturnType
        {
            get
            {
                return m_returnType;
            }
        }

        public override ParameterInfo[] GetParameters()
        {
            if (!m_bIsBaked || m_containingType == null || m_containingType.BakedRuntimeType == null)
                throw new NotSupportedException(Environment.GetResourceString("InvalidOperation_TypeNotCreated"));
            MethodInfo rmi = m_containingType.GetMethod(m_strName, m_parameterTypes);
            return rmi.GetParameters();
        }

        public override ParameterInfo ReturnParameter
        {
            get
            {
                if (!m_bIsBaked || m_containingType == null || m_containingType.BakedRuntimeType == null)
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TypeNotCreated"));
                MethodInfo rmi = m_containingType.GetMethod(m_strName, m_parameterTypes);
                return rmi.ReturnParameter;
            }
        }

        public override Object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicModule"));
        }

        public override bool IsGenericMethodDefinition
        {
            get
            {
                return m_bIsGenMethDef;
            }
        }

        public override bool ContainsGenericParameters
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override MethodInfo GetGenericMethodDefinition()
        {
            if (!IsGenericMethod)
                throw new InvalidOperationException();
            return this;
        }

        public override bool IsGenericMethod
        {
            get
            {
                return m_inst != null;
            }
        }

        public override Type[] GetGenericArguments()
        {
            return m_inst;
        }

        public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            return MethodBuilderInstantiation.MakeGenericMethod(this, typeArguments);
        }

        public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        {
            if (names == null)
                throw new ArgumentNullException("names");
            if (names.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EmptyArray"), "names");
            Contract.EndContractBlock();
            if (m_inst != null)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_GenericParametersAlreadySet"));
            for (int i = 0; i < names.Length; i++)
                if (names[i] == null)
                    throw new ArgumentNullException("names");
            if (m_tkMethod.Token != 0)
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MethodBuilderBaked"));
            m_bIsGenMethDef = true;
            m_inst = new GenericTypeParameterBuilder[names.Length];
            for (int i = 0; i < names.Length; i++)
                m_inst[i] = new GenericTypeParameterBuilder(new TypeBuilder(names[i], i, this));
            return m_inst;
        }

        internal void ThrowIfGeneric()
        {
            if (IsGenericMethod && !IsGenericMethodDefinition)
                throw new InvalidOperationException();
        }

        public MethodToken GetToken()
        {
            if (m_tkMethod.Token != 0)
            {
                return m_tkMethod;
            }

            MethodBuilder currentMethod = null;
            MethodToken currentToken = new MethodToken(0);
            int i;
            lock (m_containingType.m_listMethods)
            {
                if (m_tkMethod.Token != 0)
                {
                    return m_tkMethod;
                }

                for (i = m_containingType.m_lastTokenizedMethod + 1; i < m_containingType.m_listMethods.Count; ++i)
                {
                    currentMethod = m_containingType.m_listMethods[i];
                    currentToken = currentMethod.GetTokenNoLock();
                    if (currentMethod == this)
                        break;
                }

                m_containingType.m_lastTokenizedMethod = i;
            }

            Contract.Assert(currentMethod == this, "We should have found this method in m_containingType.m_listMethods");
            Contract.Assert(currentToken.Token != 0, "The token should not be 0");
            return currentToken;
        }

        private MethodToken GetTokenNoLock()
        {
            Contract.Assert(m_tkMethod.Token == 0, "m_tkMethod should not have been initialized");
            int sigLength;
            byte[] sigBytes = GetMethodSignature().InternalGetSignature(out sigLength);
            int token = TypeBuilder.DefineMethod(m_module.GetNativeHandle(), m_containingType.MetadataTokenInternal, m_strName, sigBytes, sigLength, Attributes);
            m_tkMethod = new MethodToken(token);
            if (m_inst != null)
                foreach (GenericTypeParameterBuilder tb in m_inst)
                    if (!tb.m_type.IsCreated())
                        tb.m_type.CreateType();
            TypeBuilder.SetMethodImpl(m_module.GetNativeHandle(), token, m_dwMethodImplFlags);
            return m_tkMethod;
        }

        public void SetParameters(params Type[] parameterTypes)
        {
            CheckContext(parameterTypes);
            SetSignature(null, null, null, parameterTypes, null, null);
        }

        public void SetReturnType(Type returnType)
        {
            CheckContext(returnType);
            SetSignature(returnType, null, null, null, null, null);
        }

        public void SetSignature(Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
            if (m_tkMethod.Token != 0)
                return;
            CheckContext(returnType);
            CheckContext(returnTypeRequiredCustomModifiers, returnTypeOptionalCustomModifiers, parameterTypes);
            CheckContext(parameterTypeRequiredCustomModifiers);
            CheckContext(parameterTypeOptionalCustomModifiers);
            ThrowIfGeneric();
            if (returnType != null)
            {
                m_returnType = returnType;
            }

            if (parameterTypes != null)
            {
                m_parameterTypes = new Type[parameterTypes.Length];
                Array.Copy(parameterTypes, m_parameterTypes, parameterTypes.Length);
            }

            m_returnTypeRequiredCustomModifiers = returnTypeRequiredCustomModifiers;
            m_returnTypeOptionalCustomModifiers = returnTypeOptionalCustomModifiers;
            m_parameterTypeRequiredCustomModifiers = parameterTypeRequiredCustomModifiers;
            m_parameterTypeOptionalCustomModifiers = parameterTypeOptionalCustomModifiers;
        }

        public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, String strParamName)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_ParamSequence"));
            Contract.EndContractBlock();
            ThrowIfGeneric();
            m_containingType.ThrowIfCreated();
            if (position > 0 && (m_parameterTypes == null || position > m_parameterTypes.Length))
                throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_ParamSequence"));
            attributes = attributes & ~ParameterAttributes.ReservedMask;
            return new ParameterBuilder(this, position, attributes, strParamName);
        }

        public void SetMarshal(UnmanagedMarshal unmanagedMarshal)
        {
            ThrowIfGeneric();
            m_containingType.ThrowIfCreated();
            if (m_retParam == null)
            {
                m_retParam = new ParameterBuilder(this, 0, 0, null);
            }

            m_retParam.SetMarshal(unmanagedMarshal);
        }

        private List<SymCustomAttr> m_symCustomAttrs;
        private struct SymCustomAttr
        {
            public SymCustomAttr(String name, byte[] data)
            {
                m_name = name;
                m_data = data;
            }

            public String m_name;
            public byte[] m_data;
        }

        public void SetSymCustomAttribute(String name, byte[] data)
        {
            ThrowIfGeneric();
            m_containingType.ThrowIfCreated();
            ModuleBuilder dynMod = (ModuleBuilder)m_module;
            if (dynMod.GetSymWriter() == null)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotADebugModule"));
            }

            if (m_symCustomAttrs == null)
                m_symCustomAttrs = new List<SymCustomAttr>();
            m_symCustomAttrs.Add(new SymCustomAttr(name, data));
        }

        public void SetMethodBody(byte[] il, int maxStack, byte[] localSignature, IEnumerable<ExceptionHandler> exceptionHandlers, IEnumerable<int> tokenFixups)
        {
            if (il == null)
            {
                throw new ArgumentNullException("il", Environment.GetResourceString("ArgumentNull_Array"));
            }

            if (maxStack < 0)
            {
                throw new ArgumentOutOfRangeException("maxStack", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            Contract.EndContractBlock();
            if (m_bIsBaked)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MethodBaked"));
            }

            m_containingType.ThrowIfCreated();
            ThrowIfGeneric();
            byte[] newLocalSignature = null;
            ExceptionHandler[] newHandlers = null;
            int[] newTokenFixups = null;
            byte[] newIL = (byte[])il.Clone();
            if (localSignature != null)
            {
                newLocalSignature = (byte[])localSignature.Clone();
            }

            if (exceptionHandlers != null)
            {
                newHandlers = ToArray(exceptionHandlers);
                CheckExceptionHandlerRanges(newHandlers, newIL.Length);
            }

            if (tokenFixups != null)
            {
                newTokenFixups = ToArray(tokenFixups);
                int maxTokenOffset = newIL.Length - 4;
                for (int i = 0; i < newTokenFixups.Length; i++)
                {
                    if (newTokenFixups[i] < 0 || newTokenFixups[i] > maxTokenOffset)
                    {
                        throw new ArgumentOutOfRangeException("tokenFixups[" + i + "]", Environment.GetResourceString("ArgumentOutOfRange_Range", 0, maxTokenOffset));
                    }
                }
            }

            m_ubBody = newIL;
            m_localSignature = newLocalSignature;
            m_exceptions = newHandlers;
            m_mdMethodFixups = newTokenFixups;
            m_maxStack = maxStack;
            m_ilGenerator = null;
            m_bIsBaked = true;
        }

        private static T[] ToArray<T>(IEnumerable<T> sequence)
        {
            T[] array = sequence as T[];
            if (array != null)
            {
                return (T[])array.Clone();
            }

            return new List<T>(sequence).ToArray();
        }

        private static void CheckExceptionHandlerRanges(ExceptionHandler[] exceptionHandlers, int maxOffset)
        {
            for (int i = 0; i < exceptionHandlers.Length; i++)
            {
                var handler = exceptionHandlers[i];
                if (handler.m_filterOffset > maxOffset || handler.m_tryEndOffset > maxOffset || handler.m_handlerEndOffset > maxOffset)
                {
                    throw new ArgumentOutOfRangeException("exceptionHandlers[" + i + "]", Environment.GetResourceString("ArgumentOutOfRange_Range", 0, maxOffset));
                }

                if (handler.Kind == ExceptionHandlingClauseOptions.Clause && handler.ExceptionTypeToken == 0)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTypeToken", handler.ExceptionTypeToken), "exceptionHandlers[" + i + "]");
                }
            }
        }

        public void CreateMethodBody(byte[] il, int count)
        {
            ThrowIfGeneric();
            if (m_bIsBaked)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MethodBaked"));
            }

            m_containingType.ThrowIfCreated();
            if (il != null && (count < 0 || count > il.Length))
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (il == null)
            {
                m_ubBody = null;
                return;
            }

            m_ubBody = new byte[count];
            Array.Copy(il, m_ubBody, count);
            m_localSignature = null;
            m_exceptions = null;
            m_mdMethodFixups = null;
            m_maxStack = DefaultMaxStack;
            m_bIsBaked = true;
        }

        public void SetImplementationFlags(MethodImplAttributes attributes)
        {
            ThrowIfGeneric();
            m_containingType.ThrowIfCreated();
            m_dwMethodImplFlags = attributes;
            m_canBeRuntimeImpl = true;
            TypeBuilder.SetMethodImpl(m_module.GetNativeHandle(), MetadataTokenInternal, attributes);
        }

        public ILGenerator GetILGenerator()
        {
            Contract.Ensures(Contract.Result<ILGenerator>() != null);
            ThrowIfGeneric();
            ThrowIfShouldNotHaveBody();
            if (m_ilGenerator == null)
                m_ilGenerator = new ILGenerator(this);
            return m_ilGenerator;
        }

        public ILGenerator GetILGenerator(int size)
        {
            Contract.Ensures(Contract.Result<ILGenerator>() != null);
            ThrowIfGeneric();
            ThrowIfShouldNotHaveBody();
            if (m_ilGenerator == null)
                m_ilGenerator = new ILGenerator(this, size);
            return m_ilGenerator;
        }

        private void ThrowIfShouldNotHaveBody()
        {
            if ((m_dwMethodImplFlags & MethodImplAttributes.CodeTypeMask) != MethodImplAttributes.IL || (m_dwMethodImplFlags & MethodImplAttributes.Unmanaged) != 0 || (m_iAttributes & MethodAttributes.PinvokeImpl) != 0 || m_isDllImport)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ShouldNotHaveMethodBody"));
            }
        }

        public bool InitLocals
        {
            get
            {
                ThrowIfGeneric();
                return m_fInitLocals;
            }

            set
            {
                ThrowIfGeneric();
                m_fInitLocals = value;
            }
        }

        public Module GetModule()
        {
            return GetModuleBuilder();
        }

        public String Signature
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return GetMethodSignature().ToString();
            }
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            if (binaryAttribute == null)
                throw new ArgumentNullException("binaryAttribute");
            Contract.EndContractBlock();
            ThrowIfGeneric();
            TypeBuilder.DefineCustomAttribute(m_module, MetadataTokenInternal, ((ModuleBuilder)m_module).GetConstructorToken(con).Token, binaryAttribute, false, false);
            if (IsKnownCA(con))
                ParseCA(con, binaryAttribute);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
                throw new ArgumentNullException("customBuilder");
            Contract.EndContractBlock();
            ThrowIfGeneric();
            customBuilder.CreateCustomAttribute((ModuleBuilder)m_module, MetadataTokenInternal);
            if (IsKnownCA(customBuilder.m_con))
                ParseCA(customBuilder.m_con, customBuilder.m_blob);
        }

        private bool IsKnownCA(ConstructorInfo con)
        {
            Type caType = con.DeclaringType;
            if (caType == typeof (System.Runtime.CompilerServices.MethodImplAttribute))
                return true;
            else if (caType == typeof (DllImportAttribute))
                return true;
            else
                return false;
        }

        private void ParseCA(ConstructorInfo con, byte[] blob)
        {
            Type caType = con.DeclaringType;
            if (caType == typeof (System.Runtime.CompilerServices.MethodImplAttribute))
            {
                m_canBeRuntimeImpl = true;
            }
            else if (caType == typeof (DllImportAttribute))
            {
                m_canBeRuntimeImpl = true;
                m_isDllImport = true;
            }
        }

        internal bool m_canBeRuntimeImpl = false;
        internal bool m_isDllImport = false;
    }

    internal class LocalSymInfo
    {
        internal String[] m_strName;
        internal byte[][] m_ubSignature;
        internal int[] m_iLocalSlot;
        internal int[] m_iStartOffset;
        internal int[] m_iEndOffset;
        internal int m_iLocalSymCount;
        internal String[] m_namespace;
        internal int m_iNameSpaceCount;
        internal const int InitialSize = 16;
        internal LocalSymInfo()
        {
            m_iLocalSymCount = 0;
            m_iNameSpaceCount = 0;
        }

        private void EnsureCapacityNamespace()
        {
            if (m_iNameSpaceCount == 0)
            {
                m_namespace = new String[InitialSize];
            }
            else if (m_iNameSpaceCount == m_namespace.Length)
            {
                String[] strTemp = new String[checked (m_iNameSpaceCount * 2)];
                Array.Copy(m_namespace, strTemp, m_iNameSpaceCount);
                m_namespace = strTemp;
            }
        }

        private void EnsureCapacity()
        {
            if (m_iLocalSymCount == 0)
            {
                m_strName = new String[InitialSize];
                m_ubSignature = new byte[InitialSize][];
                m_iLocalSlot = new int[InitialSize];
                m_iStartOffset = new int[InitialSize];
                m_iEndOffset = new int[InitialSize];
            }
            else if (m_iLocalSymCount == m_strName.Length)
            {
                int newSize = checked (m_iLocalSymCount * 2);
                int[] temp = new int[newSize];
                Array.Copy(m_iLocalSlot, temp, m_iLocalSymCount);
                m_iLocalSlot = temp;
                temp = new int[newSize];
                Array.Copy(m_iStartOffset, temp, m_iLocalSymCount);
                m_iStartOffset = temp;
                temp = new int[newSize];
                Array.Copy(m_iEndOffset, temp, m_iLocalSymCount);
                m_iEndOffset = temp;
                String[] strTemp = new String[newSize];
                Array.Copy(m_strName, strTemp, m_iLocalSymCount);
                m_strName = strTemp;
                byte[][] ubTemp = new byte[newSize][];
                Array.Copy(m_ubSignature, ubTemp, m_iLocalSymCount);
                m_ubSignature = ubTemp;
            }
        }

        internal void AddLocalSymInfo(String strName, byte[] signature, int slot, int startOffset, int endOffset)
        {
            EnsureCapacity();
            m_iStartOffset[m_iLocalSymCount] = startOffset;
            m_iEndOffset[m_iLocalSymCount] = endOffset;
            m_iLocalSlot[m_iLocalSymCount] = slot;
            m_strName[m_iLocalSymCount] = strName;
            m_ubSignature[m_iLocalSymCount] = signature;
            checked
            {
                m_iLocalSymCount++;
            }
        }

        internal void AddUsingNamespace(String strNamespace)
        {
            EnsureCapacityNamespace();
            m_namespace[m_iNameSpaceCount] = strNamespace;
            checked
            {
                m_iNameSpaceCount++;
            }
        }

        internal virtual void EmitLocalSymInfo(ISymbolWriter symWriter)
        {
            int i;
            for (i = 0; i < m_iLocalSymCount; i++)
            {
                symWriter.DefineLocalVariable(m_strName[i], FieldAttributes.PrivateScope, m_ubSignature[i], SymAddressKind.ILOffset, m_iLocalSlot[i], 0, 0, m_iStartOffset[i], m_iEndOffset[i]);
            }

            for (i = 0; i < m_iNameSpaceCount; i++)
            {
                symWriter.UsingNamespace(m_namespace[i]);
            }
        }
    }

    public struct ExceptionHandler : IEquatable<ExceptionHandler>
    {
        internal readonly int m_exceptionClass;
        internal readonly int m_tryStartOffset;
        internal readonly int m_tryEndOffset;
        internal readonly int m_filterOffset;
        internal readonly int m_handlerStartOffset;
        internal readonly int m_handlerEndOffset;
        internal readonly ExceptionHandlingClauseOptions m_kind;
        public int ExceptionTypeToken
        {
            get
            {
                return m_exceptionClass;
            }
        }

        public int TryOffset
        {
            get
            {
                return m_tryStartOffset;
            }
        }

        public int TryLength
        {
            get
            {
                return m_tryEndOffset - m_tryStartOffset;
            }
        }

        public int FilterOffset
        {
            get
            {
                return m_filterOffset;
            }
        }

        public int HandlerOffset
        {
            get
            {
                return m_handlerStartOffset;
            }
        }

        public int HandlerLength
        {
            get
            {
                return m_handlerEndOffset - m_handlerStartOffset;
            }
        }

        public ExceptionHandlingClauseOptions Kind
        {
            get
            {
                return m_kind;
            }
        }

        public ExceptionHandler(int tryOffset, int tryLength, int filterOffset, int handlerOffset, int handlerLength, ExceptionHandlingClauseOptions kind, int exceptionTypeToken)
        {
            if (tryOffset < 0)
            {
                throw new ArgumentOutOfRangeException("tryOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (tryLength < 0)
            {
                throw new ArgumentOutOfRangeException("tryLength", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (filterOffset < 0)
            {
                throw new ArgumentOutOfRangeException("filterOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (handlerOffset < 0)
            {
                throw new ArgumentOutOfRangeException("handlerOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if (handlerLength < 0)
            {
                throw new ArgumentOutOfRangeException("handlerLength", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            if ((long)tryOffset + tryLength > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("tryLength", Environment.GetResourceString("ArgumentOutOfRange_Range", 0, Int32.MaxValue - tryOffset));
            }

            if ((long)handlerOffset + handlerLength > Int32.MaxValue)
            {
                throw new ArgumentOutOfRangeException("handlerLength", Environment.GetResourceString("ArgumentOutOfRange_Range", 0, Int32.MaxValue - handlerOffset));
            }

            if (kind == ExceptionHandlingClauseOptions.Clause && (exceptionTypeToken & 0x00FFFFFF) == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTypeToken", exceptionTypeToken), "exceptionTypeToken");
            }

            Contract.EndContractBlock();
            if (!IsValidKind(kind))
            {
                throw new ArgumentOutOfRangeException("kind", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
            }

            m_tryStartOffset = tryOffset;
            m_tryEndOffset = tryOffset + tryLength;
            m_filterOffset = filterOffset;
            m_handlerStartOffset = handlerOffset;
            m_handlerEndOffset = handlerOffset + handlerLength;
            m_kind = kind;
            m_exceptionClass = exceptionTypeToken;
        }

        internal ExceptionHandler(int tryStartOffset, int tryEndOffset, int filterOffset, int handlerStartOffset, int handlerEndOffset, int kind, int exceptionTypeToken)
        {
            Contract.Assert(tryStartOffset >= 0);
            Contract.Assert(tryEndOffset >= 0);
            Contract.Assert(filterOffset >= 0);
            Contract.Assert(handlerStartOffset >= 0);
            Contract.Assert(handlerEndOffset >= 0);
            Contract.Assert(IsValidKind((ExceptionHandlingClauseOptions)kind));
            Contract.Assert(kind != (int)ExceptionHandlingClauseOptions.Clause || (exceptionTypeToken & 0x00FFFFFF) != 0);
            m_tryStartOffset = tryStartOffset;
            m_tryEndOffset = tryEndOffset;
            m_filterOffset = filterOffset;
            m_handlerStartOffset = handlerStartOffset;
            m_handlerEndOffset = handlerEndOffset;
            m_kind = (ExceptionHandlingClauseOptions)kind;
            m_exceptionClass = exceptionTypeToken;
        }

        private static bool IsValidKind(ExceptionHandlingClauseOptions kind)
        {
            switch (kind)
            {
                case ExceptionHandlingClauseOptions.Clause:
                case ExceptionHandlingClauseOptions.Filter:
                case ExceptionHandlingClauseOptions.Finally:
                case ExceptionHandlingClauseOptions.Fault:
                    return true;
                default:
                    return false;
            }
        }

        public override int GetHashCode()
        {
            return m_exceptionClass ^ m_tryStartOffset ^ m_tryEndOffset ^ m_filterOffset ^ m_handlerStartOffset ^ m_handlerEndOffset ^ (int)m_kind;
        }

        public override bool Equals(Object obj)
        {
            return obj is ExceptionHandler && Equals((ExceptionHandler)obj);
        }

        public bool Equals(ExceptionHandler other)
        {
            return other.m_exceptionClass == m_exceptionClass && other.m_tryStartOffset == m_tryStartOffset && other.m_tryEndOffset == m_tryEndOffset && other.m_filterOffset == m_filterOffset && other.m_handlerStartOffset == m_handlerStartOffset && other.m_handlerEndOffset == m_handlerEndOffset && other.m_kind == m_kind;
        }

        public static bool operator ==(ExceptionHandler left, ExceptionHandler right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ExceptionHandler left, ExceptionHandler right)
        {
            return !left.Equals(right);
        }
    }
}