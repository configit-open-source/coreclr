namespace System.Reflection.Emit
{
    using System.Text;
    using System;
    using System.Diagnostics.Contracts;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security.Permissions;

    public sealed class SignatureHelper : _SignatureHelper
    {
        private const int NO_SIZE_IN_SIG = -1;
        public static SignatureHelper GetMethodSigHelper(Module mod, Type returnType, Type[] parameterTypes)
        {
            return GetMethodSigHelper(mod, CallingConventions.Standard, returnType, null, null, parameterTypes, null, null);
        }

        internal static SignatureHelper GetMethodSigHelper(Module mod, CallingConventions callingConvention, Type returnType, int cGenericParam)
        {
            return GetMethodSigHelper(mod, callingConvention, cGenericParam, returnType, null, null, null, null, null);
        }

        public static SignatureHelper GetMethodSigHelper(Module mod, CallingConventions callingConvention, Type returnType)
        {
            return GetMethodSigHelper(mod, callingConvention, returnType, null, null, null, null, null);
        }

        internal static SignatureHelper GetMethodSpecSigHelper(Module scope, Type[] inst)
        {
            SignatureHelper sigHelp = new SignatureHelper(scope, MdSigCallingConvention.GenericInst);
            sigHelp.AddData(inst.Length);
            foreach (Type t in inst)
                sigHelp.AddArgument(t);
            return sigHelp;
        }

        internal static SignatureHelper GetMethodSigHelper(Module scope, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
        {
            return GetMethodSigHelper(scope, callingConvention, 0, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
        }

        internal static SignatureHelper GetMethodSigHelper(Module scope, CallingConventions callingConvention, int cGenericParam, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
        {
            SignatureHelper sigHelp;
            MdSigCallingConvention intCall;
            if (returnType == null)
            {
                returnType = typeof (void);
            }

            intCall = MdSigCallingConvention.Default;
            if ((callingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
                intCall = MdSigCallingConvention.Vararg;
            if (cGenericParam > 0)
            {
                intCall |= MdSigCallingConvention.Generic;
            }

            if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
                intCall |= MdSigCallingConvention.HasThis;
            sigHelp = new SignatureHelper(scope, intCall, cGenericParam, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers);
            sigHelp.AddArguments(parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
            return sigHelp;
        }

        public static SignatureHelper GetMethodSigHelper(Module mod, CallingConvention unmanagedCallConv, Type returnType)
        {
            SignatureHelper sigHelp;
            MdSigCallingConvention intCall;
            if (returnType == null)
                returnType = typeof (void);
            if (unmanagedCallConv == CallingConvention.Cdecl)
            {
                intCall = MdSigCallingConvention.C;
            }
            else if (unmanagedCallConv == CallingConvention.StdCall || unmanagedCallConv == CallingConvention.Winapi)
            {
                intCall = MdSigCallingConvention.StdCall;
            }
            else if (unmanagedCallConv == CallingConvention.ThisCall)
            {
                intCall = MdSigCallingConvention.ThisCall;
            }
            else if (unmanagedCallConv == CallingConvention.FastCall)
            {
                intCall = MdSigCallingConvention.FastCall;
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_UnknownUnmanagedCallConv"), "unmanagedCallConv");
            }

            sigHelp = new SignatureHelper(mod, intCall, returnType, null, null);
            return sigHelp;
        }

        public static SignatureHelper GetLocalVarSigHelper()
        {
            return GetLocalVarSigHelper(null);
        }

        public static SignatureHelper GetMethodSigHelper(CallingConventions callingConvention, Type returnType)
        {
            return GetMethodSigHelper(null, callingConvention, returnType);
        }

        public static SignatureHelper GetMethodSigHelper(CallingConvention unmanagedCallingConvention, Type returnType)
        {
            return GetMethodSigHelper(null, unmanagedCallingConvention, returnType);
        }

        public static SignatureHelper GetLocalVarSigHelper(Module mod)
        {
            return new SignatureHelper(mod, MdSigCallingConvention.LocalSig);
        }

        public static SignatureHelper GetFieldSigHelper(Module mod)
        {
            return new SignatureHelper(mod, MdSigCallingConvention.Field);
        }

        public static SignatureHelper GetPropertySigHelper(Module mod, Type returnType, Type[] parameterTypes)
        {
            return GetPropertySigHelper(mod, returnType, null, null, parameterTypes, null, null);
        }

        public static SignatureHelper GetPropertySigHelper(Module mod, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
        {
            return GetPropertySigHelper(mod, (CallingConventions)0, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers, parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
        }

        public static SignatureHelper GetPropertySigHelper(Module mod, CallingConventions callingConvention, Type returnType, Type[] requiredReturnTypeCustomModifiers, Type[] optionalReturnTypeCustomModifiers, Type[] parameterTypes, Type[][] requiredParameterTypeCustomModifiers, Type[][] optionalParameterTypeCustomModifiers)
        {
            SignatureHelper sigHelp;
            if (returnType == null)
            {
                returnType = typeof (void);
            }

            MdSigCallingConvention intCall = MdSigCallingConvention.Property;
            if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
                intCall |= MdSigCallingConvention.HasThis;
            sigHelp = new SignatureHelper(mod, intCall, returnType, requiredReturnTypeCustomModifiers, optionalReturnTypeCustomModifiers);
            sigHelp.AddArguments(parameterTypes, requiredParameterTypeCustomModifiers, optionalParameterTypeCustomModifiers);
            return sigHelp;
        }

        internal static SignatureHelper GetTypeSigToken(Module mod, Type type)
        {
            if (mod == null)
                throw new ArgumentNullException("module");
            if (type == null)
                throw new ArgumentNullException("type");
            return new SignatureHelper(mod, type);
        }

        private byte[] m_signature;
        private int m_currSig;
        private int m_sizeLoc;
        private ModuleBuilder m_module;
        private bool m_sigDone;
        private int m_argCount;
        private SignatureHelper(Module mod, MdSigCallingConvention callingConvention)
        {
            Init(mod, callingConvention);
        }

        private SignatureHelper(Module mod, MdSigCallingConvention callingConvention, int cGenericParameters, Type returnType, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
        {
            Init(mod, callingConvention, cGenericParameters);
            if (callingConvention == MdSigCallingConvention.Field)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadFieldSig"));
            AddOneArgTypeHelper(returnType, requiredCustomModifiers, optionalCustomModifiers);
        }

        private SignatureHelper(Module mod, MdSigCallingConvention callingConvention, Type returnType, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers): this (mod, callingConvention, 0, returnType, requiredCustomModifiers, optionalCustomModifiers)
        {
        }

        private SignatureHelper(Module mod, Type type)
        {
            Init(mod);
            AddOneArgTypeHelper(type);
        }

        private void Init(Module mod)
        {
            m_signature = new byte[32];
            m_currSig = 0;
            m_module = mod as ModuleBuilder;
            m_argCount = 0;
            m_sigDone = false;
            m_sizeLoc = NO_SIZE_IN_SIG;
            if (m_module == null && mod != null)
                throw new ArgumentException(Environment.GetResourceString("NotSupported_MustBeModuleBuilder"));
        }

        private void Init(Module mod, MdSigCallingConvention callingConvention)
        {
            Init(mod, callingConvention, 0);
        }

        private void Init(Module mod, MdSigCallingConvention callingConvention, int cGenericParam)
        {
            Init(mod);
            AddData((byte)callingConvention);
            if (callingConvention == MdSigCallingConvention.Field || callingConvention == MdSigCallingConvention.GenericInst)
            {
                m_sizeLoc = NO_SIZE_IN_SIG;
            }
            else
            {
                if (cGenericParam > 0)
                    AddData(cGenericParam);
                m_sizeLoc = m_currSig++;
            }
        }

        private void AddOneArgTypeHelper(Type argument, bool pinned)
        {
            if (pinned)
                AddElementType(CorElementType.Pinned);
            AddOneArgTypeHelper(argument);
        }

        private void AddOneArgTypeHelper(Type clsArgument, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
        {
            Contract.Requires(clsArgument != null);
            Contract.Requires((optionalCustomModifiers == null && requiredCustomModifiers == null) || !clsArgument.ContainsGenericParameters);
            if (optionalCustomModifiers != null)
            {
                for (int i = 0; i < optionalCustomModifiers.Length; i++)
                {
                    Type t = optionalCustomModifiers[i];
                    if (t == null)
                        throw new ArgumentNullException("optionalCustomModifiers");
                    if (t.HasElementType)
                        throw new ArgumentException(Environment.GetResourceString("Argument_ArraysInvalid"), "optionalCustomModifiers");
                    if (t.ContainsGenericParameters)
                        throw new ArgumentException(Environment.GetResourceString("Argument_GenericsInvalid"), "optionalCustomModifiers");
                    AddElementType(CorElementType.CModOpt);
                    int token = m_module.GetTypeToken(t).Token;
                    Contract.Assert(!MetadataToken.IsNullToken(token));
                    AddToken(token);
                }
            }

            if (requiredCustomModifiers != null)
            {
                for (int i = 0; i < requiredCustomModifiers.Length; i++)
                {
                    Type t = requiredCustomModifiers[i];
                    if (t == null)
                        throw new ArgumentNullException("requiredCustomModifiers");
                    if (t.HasElementType)
                        throw new ArgumentException(Environment.GetResourceString("Argument_ArraysInvalid"), "requiredCustomModifiers");
                    if (t.ContainsGenericParameters)
                        throw new ArgumentException(Environment.GetResourceString("Argument_GenericsInvalid"), "requiredCustomModifiers");
                    AddElementType(CorElementType.CModReqd);
                    int token = m_module.GetTypeToken(t).Token;
                    Contract.Assert(!MetadataToken.IsNullToken(token));
                    AddToken(token);
                }
            }

            AddOneArgTypeHelper(clsArgument);
        }

        private void AddOneArgTypeHelper(Type clsArgument)
        {
            AddOneArgTypeHelperWorker(clsArgument, false);
        }

        private void AddOneArgTypeHelperWorker(Type clsArgument, bool lastWasGenericInst)
        {
            if (clsArgument.IsGenericParameter)
            {
                if (clsArgument.DeclaringMethod != null)
                    AddElementType(CorElementType.MVar);
                else
                    AddElementType(CorElementType.Var);
                AddData(clsArgument.GenericParameterPosition);
            }
            else if (clsArgument.IsGenericType && (!clsArgument.IsGenericTypeDefinition || !lastWasGenericInst))
            {
                AddElementType(CorElementType.GenericInst);
                AddOneArgTypeHelperWorker(clsArgument.GetGenericTypeDefinition(), true);
                Type[] args = clsArgument.GetGenericArguments();
                AddData(args.Length);
                foreach (Type t in args)
                    AddOneArgTypeHelper(t);
            }
            else if (clsArgument is TypeBuilder)
            {
                TypeBuilder clsBuilder = (TypeBuilder)clsArgument;
                TypeToken tkType;
                if (clsBuilder.Module.Equals(m_module))
                {
                    tkType = clsBuilder.TypeToken;
                }
                else
                {
                    tkType = m_module.GetTypeToken(clsArgument);
                }

                if (clsArgument.IsValueType)
                {
                    InternalAddTypeToken(tkType, CorElementType.ValueType);
                }
                else
                {
                    InternalAddTypeToken(tkType, CorElementType.Class);
                }
            }
            else if (clsArgument is EnumBuilder)
            {
                TypeBuilder clsBuilder = ((EnumBuilder)clsArgument).m_typeBuilder;
                TypeToken tkType;
                if (clsBuilder.Module.Equals(m_module))
                {
                    tkType = clsBuilder.TypeToken;
                }
                else
                {
                    tkType = m_module.GetTypeToken(clsArgument);
                }

                if (clsArgument.IsValueType)
                {
                    InternalAddTypeToken(tkType, CorElementType.ValueType);
                }
                else
                {
                    InternalAddTypeToken(tkType, CorElementType.Class);
                }
            }
            else if (clsArgument.IsByRef)
            {
                AddElementType(CorElementType.ByRef);
                clsArgument = clsArgument.GetElementType();
                AddOneArgTypeHelper(clsArgument);
            }
            else if (clsArgument.IsPointer)
            {
                AddElementType(CorElementType.Ptr);
                AddOneArgTypeHelper(clsArgument.GetElementType());
            }
            else if (clsArgument.IsArray)
            {
                if (clsArgument.IsSzArray)
                {
                    AddElementType(CorElementType.SzArray);
                    AddOneArgTypeHelper(clsArgument.GetElementType());
                }
                else
                {
                    AddElementType(CorElementType.Array);
                    AddOneArgTypeHelper(clsArgument.GetElementType());
                    int rank = clsArgument.GetArrayRank();
                    AddData(rank);
                    AddData(0);
                    AddData(rank);
                    for (int i = 0; i < rank; i++)
                        AddData(0);
                }
            }
            else
            {
                CorElementType type = CorElementType.Max;
                if (clsArgument is RuntimeType)
                {
                    type = RuntimeTypeHandle.GetCorElementType((RuntimeType)clsArgument);
                    if (type == CorElementType.Class)
                    {
                        if (clsArgument == typeof (object))
                            type = CorElementType.Object;
                        else if (clsArgument == typeof (string))
                            type = CorElementType.String;
                    }
                }

                if (IsSimpleType(type))
                {
                    AddElementType(type);
                }
                else if (m_module == null)
                {
                    InternalAddRuntimeType(clsArgument);
                }
                else if (clsArgument.IsValueType)
                {
                    InternalAddTypeToken(m_module.GetTypeToken(clsArgument), CorElementType.ValueType);
                }
                else
                {
                    InternalAddTypeToken(m_module.GetTypeToken(clsArgument), CorElementType.Class);
                }
            }
        }

        private void AddData(int data)
        {
            if (m_currSig + 4 > m_signature.Length)
            {
                m_signature = ExpandArray(m_signature);
            }

            if (data <= 0x7F)
            {
                m_signature[m_currSig++] = (byte)(data & 0xFF);
            }
            else if (data <= 0x3FFF)
            {
                m_signature[m_currSig++] = (byte)((data >> 8) | 0x80);
                m_signature[m_currSig++] = (byte)(data & 0xFF);
            }
            else if (data <= 0x1FFFFFFF)
            {
                m_signature[m_currSig++] = (byte)((data >> 24) | 0xC0);
                m_signature[m_currSig++] = (byte)((data >> 16) & 0xFF);
                m_signature[m_currSig++] = (byte)((data >> 8) & 0xFF);
                m_signature[m_currSig++] = (byte)((data) & 0xFF);
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_LargeInteger"));
            }
        }

        private void AddData(uint data)
        {
            if (m_currSig + 4 > m_signature.Length)
            {
                m_signature = ExpandArray(m_signature);
            }

            m_signature[m_currSig++] = (byte)((data) & 0xFF);
            m_signature[m_currSig++] = (byte)((data >> 8) & 0xFF);
            m_signature[m_currSig++] = (byte)((data >> 16) & 0xFF);
            m_signature[m_currSig++] = (byte)((data >> 24) & 0xFF);
        }

        private void AddData(ulong data)
        {
            if (m_currSig + 8 > m_signature.Length)
            {
                m_signature = ExpandArray(m_signature);
            }

            m_signature[m_currSig++] = (byte)((data) & 0xFF);
            m_signature[m_currSig++] = (byte)((data >> 8) & 0xFF);
            m_signature[m_currSig++] = (byte)((data >> 16) & 0xFF);
            m_signature[m_currSig++] = (byte)((data >> 24) & 0xFF);
            m_signature[m_currSig++] = (byte)((data >> 32) & 0xFF);
            m_signature[m_currSig++] = (byte)((data >> 40) & 0xFF);
            m_signature[m_currSig++] = (byte)((data >> 48) & 0xFF);
            m_signature[m_currSig++] = (byte)((data >> 56) & 0xFF);
        }

        private void AddElementType(CorElementType cvt)
        {
            if (m_currSig + 1 > m_signature.Length)
                m_signature = ExpandArray(m_signature);
            m_signature[m_currSig++] = (byte)cvt;
        }

        private void AddToken(int token)
        {
            int rid = (token & 0x00FFFFFF);
            MetadataTokenType type = (MetadataTokenType)(token & unchecked ((int)0xFF000000));
            if (rid > 0x3FFFFFF)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_LargeInteger"));
            }

            rid = (rid << 2);
            if (type == MetadataTokenType.TypeRef)
            {
                rid |= 0x1;
            }
            else if (type == MetadataTokenType.TypeSpec)
            {
                rid |= 0x2;
            }

            AddData(rid);
        }

        private void InternalAddTypeToken(TypeToken clsToken, CorElementType CorType)
        {
            AddElementType(CorType);
            AddToken(clsToken.Token);
        }

        private unsafe void InternalAddRuntimeType(Type type)
        {
            AddElementType(CorElementType.Internal);
            IntPtr handle = type.GetTypeHandleInternal().Value;
            if (m_currSig + sizeof (void *) > m_signature.Length)
                m_signature = ExpandArray(m_signature);
            byte *phandle = (byte *)&handle;
            for (int i = 0; i < sizeof (void *); i++)
                m_signature[m_currSig++] = phandle[i];
        }

        private byte[] ExpandArray(byte[] inArray)
        {
            return ExpandArray(inArray, inArray.Length * 2);
        }

        private byte[] ExpandArray(byte[] inArray, int requiredLength)
        {
            if (requiredLength < inArray.Length)
                requiredLength = inArray.Length * 2;
            byte[] outArray = new byte[requiredLength];
            Array.Copy(inArray, outArray, inArray.Length);
            return outArray;
        }

        private void IncrementArgCounts()
        {
            if (m_sizeLoc == NO_SIZE_IN_SIG)
            {
                return;
            }

            m_argCount++;
        }

        private void SetNumberOfSignatureElements(bool forceCopy)
        {
            byte[] temp;
            int newSigSize;
            int currSigHolder = m_currSig;
            if (m_sizeLoc == NO_SIZE_IN_SIG)
                return;
            if (m_argCount < 0x80 && !forceCopy)
            {
                m_signature[m_sizeLoc] = (byte)m_argCount;
                return;
            }

            if (m_argCount < 0x80)
            {
                newSigSize = 1;
            }
            else if (m_argCount < 0x4000)
            {
                newSigSize = 2;
            }
            else
            {
                newSigSize = 4;
            }

            temp = new byte[m_currSig + newSigSize - 1];
            temp[0] = m_signature[0];
            Array.Copy(m_signature, m_sizeLoc + 1, temp, m_sizeLoc + newSigSize, currSigHolder - (m_sizeLoc + 1));
            m_signature = temp;
            m_currSig = m_sizeLoc;
            AddData(m_argCount);
            m_currSig = currSigHolder + (newSigSize - 1);
        }

        internal int ArgumentCount
        {
            get
            {
                return m_argCount;
            }
        }

        internal static bool IsSimpleType(CorElementType type)
        {
            if (type <= CorElementType.String)
                return true;
            if (type == CorElementType.TypedByRef || type == CorElementType.I || type == CorElementType.U || type == CorElementType.Object)
                return true;
            return false;
        }

        internal byte[] InternalGetSignature(out int length)
        {
            if (!m_sigDone)
            {
                m_sigDone = true;
                SetNumberOfSignatureElements(false);
            }

            length = m_currSig;
            return m_signature;
        }

        internal byte[] InternalGetSignatureArray()
        {
            int argCount = m_argCount;
            int currSigLength = m_currSig;
            int newSigSize = currSigLength;
            if (argCount < 0x7F)
                newSigSize += 1;
            else if (argCount < 0x3FFF)
                newSigSize += 2;
            else
                newSigSize += 4;
            byte[] temp = new byte[newSigSize];
            int sigCopyIndex = 0;
            temp[sigCopyIndex++] = m_signature[0];
            if (argCount <= 0x7F)
                temp[sigCopyIndex++] = (byte)(argCount & 0xFF);
            else if (argCount <= 0x3FFF)
            {
                temp[sigCopyIndex++] = (byte)((argCount >> 8) | 0x80);
                temp[sigCopyIndex++] = (byte)(argCount & 0xFF);
            }
            else if (argCount <= 0x1FFFFFFF)
            {
                temp[sigCopyIndex++] = (byte)((argCount >> 24) | 0xC0);
                temp[sigCopyIndex++] = (byte)((argCount >> 16) & 0xFF);
                temp[sigCopyIndex++] = (byte)((argCount >> 8) & 0xFF);
                temp[sigCopyIndex++] = (byte)((argCount) & 0xFF);
            }
            else
                throw new ArgumentException(Environment.GetResourceString("Argument_LargeInteger"));
            Array.Copy(m_signature, 2, temp, sigCopyIndex, currSigLength - 2);
            temp[newSigSize - 1] = (byte)CorElementType.End;
            return temp;
        }

        public void AddArgument(Type clsArgument)
        {
            AddArgument(clsArgument, null, null);
        }

        public void AddArgument(Type argument, bool pinned)
        {
            if (argument == null)
                throw new ArgumentNullException("argument");
            IncrementArgCounts();
            AddOneArgTypeHelper(argument, pinned);
        }

        public void AddArguments(Type[] arguments, Type[][] requiredCustomModifiers, Type[][] optionalCustomModifiers)
        {
            if (requiredCustomModifiers != null && (arguments == null || requiredCustomModifiers.Length != arguments.Length))
                throw new ArgumentException(Environment.GetResourceString("Argument_MismatchedArrays", "requiredCustomModifiers", "arguments"));
            if (optionalCustomModifiers != null && (arguments == null || optionalCustomModifiers.Length != arguments.Length))
                throw new ArgumentException(Environment.GetResourceString("Argument_MismatchedArrays", "optionalCustomModifiers", "arguments"));
            if (arguments != null)
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    AddArgument(arguments[i], requiredCustomModifiers == null ? null : requiredCustomModifiers[i], optionalCustomModifiers == null ? null : optionalCustomModifiers[i]);
                }
            }
        }

        public void AddArgument(Type argument, Type[] requiredCustomModifiers, Type[] optionalCustomModifiers)
        {
            if (m_sigDone)
                throw new ArgumentException(Environment.GetResourceString("Argument_SigIsFinalized"));
            if (argument == null)
                throw new ArgumentNullException("argument");
            IncrementArgCounts();
            AddOneArgTypeHelper(argument, requiredCustomModifiers, optionalCustomModifiers);
        }

        public void AddSentinel()
        {
            AddElementType(CorElementType.Sentinel);
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is SignatureHelper))
            {
                return false;
            }

            SignatureHelper temp = (SignatureHelper)obj;
            if (!temp.m_module.Equals(m_module) || temp.m_currSig != m_currSig || temp.m_sizeLoc != m_sizeLoc || temp.m_sigDone != m_sigDone)
            {
                return false;
            }

            for (int i = 0; i < m_currSig; i++)
            {
                if (m_signature[i] != temp.m_signature[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int HashCode = m_module.GetHashCode() + m_currSig + m_sizeLoc;
            if (m_sigDone)
                HashCode += 1;
            for (int i = 0; i < m_currSig; i++)
                HashCode += m_signature[i].GetHashCode();
            return HashCode;
        }

        public byte[] GetSignature()
        {
            return GetSignature(false);
        }

        internal byte[] GetSignature(bool appendEndOfSig)
        {
            if (!m_sigDone)
            {
                if (appendEndOfSig)
                    AddElementType(CorElementType.End);
                SetNumberOfSignatureElements(true);
                m_sigDone = true;
            }

            if (m_signature.Length > m_currSig)
            {
                byte[] temp = new byte[m_currSig];
                Array.Copy(m_signature, temp, m_currSig);
                m_signature = temp;
            }

            return m_signature;
        }

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Length: " + m_currSig + Environment.NewLine);
            if (m_sizeLoc != -1)
            {
                sb.Append("Arguments: " + m_signature[m_sizeLoc] + Environment.NewLine);
            }
            else
            {
                sb.Append("Field Signature" + Environment.NewLine);
            }

            sb.Append("Signature: " + Environment.NewLine);
            for (int i = 0; i <= m_currSig; i++)
            {
                sb.Append(m_signature[i] + "  ");
            }

            sb.Append(Environment.NewLine);
            return sb.ToString();
        }
    }
}