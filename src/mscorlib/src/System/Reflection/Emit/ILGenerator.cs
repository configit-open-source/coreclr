namespace System.Reflection.Emit
{
    using System;
    using TextWriter = System.IO.TextWriter;
    using System.Diagnostics.SymbolStore;
    using System.Runtime.InteropServices;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    public class ILGenerator : _ILGenerator
    {
        private const int defaultSize = 16;
        private const int DefaultFixupArraySize = 8;
        private const int DefaultLabelArraySize = 4;
        private const int DefaultExceptionArraySize = 2;
        internal static int[] EnlargeArray(int[] incoming)
        {
            Contract.Requires(incoming != null);
            Contract.Ensures(Contract.Result<int[]>() != null);
            Contract.Ensures(Contract.Result<int[]>().Length > incoming.Length);
            int[] temp = new int[incoming.Length * 2];
            Array.Copy(incoming, temp, incoming.Length);
            return temp;
        }

        private static byte[] EnlargeArray(byte[] incoming)
        {
            byte[] temp = new byte[incoming.Length * 2];
            Array.Copy(incoming, temp, incoming.Length);
            return temp;
        }

        private static byte[] EnlargeArray(byte[] incoming, int requiredSize)
        {
            byte[] temp = new byte[requiredSize];
            Array.Copy(incoming, temp, incoming.Length);
            return temp;
        }

        private static __FixupData[] EnlargeArray(__FixupData[] incoming)
        {
            __FixupData[] temp = new __FixupData[incoming.Length * 2];
            Array.Copy(incoming, temp, incoming.Length);
            return temp;
        }

        private static __ExceptionInfo[] EnlargeArray(__ExceptionInfo[] incoming)
        {
            __ExceptionInfo[] temp = new __ExceptionInfo[incoming.Length * 2];
            Array.Copy(incoming, temp, incoming.Length);
            return temp;
        }

        private int m_length;
        private byte[] m_ILStream;
        private int[] m_labelList;
        private int m_labelCount;
        private __FixupData[] m_fixupData;
        private int m_fixupCount;
        private int[] m_RelocFixupList;
        private int m_RelocFixupCount;
        private int m_exceptionCount;
        private int m_currExcStackCount;
        private __ExceptionInfo[] m_exceptions;
        private __ExceptionInfo[] m_currExcStack;
        internal ScopeTree m_ScopeTree;
        internal LineNumberInfo m_LineNumberInfo;
        internal MethodInfo m_methodBuilder;
        internal int m_localCount;
        internal SignatureHelper m_localSignature;
        private int m_maxStackSize = 0;
        private int m_maxMidStack = 0;
        private int m_maxMidStackCur = 0;
        internal int CurrExcStackCount
        {
            get
            {
                return m_currExcStackCount;
            }
        }

        internal __ExceptionInfo[] CurrExcStack
        {
            get
            {
                return m_currExcStack;
            }
        }

        internal ILGenerator(MethodInfo methodBuilder): this (methodBuilder, 64)
        {
        }

        internal ILGenerator(MethodInfo methodBuilder, int size)
        {
            Contract.Requires(methodBuilder != null);
            Contract.Requires(methodBuilder is MethodBuilder || methodBuilder is DynamicMethod);
            if (size < defaultSize)
            {
                m_ILStream = new byte[defaultSize];
            }
            else
            {
                m_ILStream = new byte[size];
            }

            m_length = 0;
            m_labelCount = 0;
            m_fixupCount = 0;
            m_labelList = null;
            m_fixupData = null;
            m_exceptions = null;
            m_exceptionCount = 0;
            m_currExcStack = null;
            m_currExcStackCount = 0;
            m_RelocFixupList = null;
            m_RelocFixupCount = 0;
            m_ScopeTree = new ScopeTree();
            m_LineNumberInfo = new LineNumberInfo();
            m_methodBuilder = methodBuilder;
            m_localCount = 0;
            MethodBuilder mb = m_methodBuilder as MethodBuilder;
            if (mb == null)
                m_localSignature = SignatureHelper.GetLocalVarSigHelper(null);
            else
                m_localSignature = SignatureHelper.GetLocalVarSigHelper(mb.GetTypeBuilder().Module);
        }

        internal virtual void RecordTokenFixup()
        {
            if (m_RelocFixupList == null)
                m_RelocFixupList = new int[DefaultFixupArraySize];
            else if (m_RelocFixupList.Length <= m_RelocFixupCount)
                m_RelocFixupList = EnlargeArray(m_RelocFixupList);
            m_RelocFixupList[m_RelocFixupCount++] = m_length;
        }

        internal void InternalEmit(OpCode opcode)
        {
            if (opcode.Size != 1)
            {
                m_ILStream[m_length++] = (byte)(opcode.Value >> 8);
            }

            m_ILStream[m_length++] = (byte)opcode.Value;
            UpdateStackSize(opcode, opcode.StackChange());
        }

        internal void UpdateStackSize(OpCode opcode, int stackchange)
        {
            m_maxMidStackCur += stackchange;
            if (m_maxMidStackCur > m_maxMidStack)
                m_maxMidStack = m_maxMidStackCur;
            else if (m_maxMidStackCur < 0)
                m_maxMidStackCur = 0;
            if (opcode.EndsUncondJmpBlk())
            {
                m_maxStackSize += m_maxMidStack;
                m_maxMidStack = 0;
                m_maxMidStackCur = 0;
            }
        }

        private int GetMethodToken(MethodBase method, Type[] optionalParameterTypes, bool useMethodDef)
        {
            return ((ModuleBuilder)m_methodBuilder.Module).GetMethodTokenInternal(method, optionalParameterTypes, useMethodDef);
        }

        internal virtual SignatureHelper GetMemberRefSignature(CallingConventions call, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes)
        {
            return GetMemberRefSignature(call, returnType, parameterTypes, optionalParameterTypes, 0);
        }

        private SignatureHelper GetMemberRefSignature(CallingConventions call, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes, int cGenericParameters)
        {
            return ((ModuleBuilder)m_methodBuilder.Module).GetMemberRefSignature(call, returnType, parameterTypes, optionalParameterTypes, cGenericParameters);
        }

        internal byte[] BakeByteArray()
        {
            int newSize;
            int updateAddr;
            byte[] newBytes;
            if (m_currExcStackCount != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_UnclosedExceptionBlock"));
            }

            if (m_length == 0)
                return null;
            newSize = m_length;
            newBytes = new byte[newSize];
            Array.Copy(m_ILStream, newBytes, newSize);
            for (int i = 0; i < m_fixupCount; i++)
            {
                updateAddr = GetLabelPos(m_fixupData[i].m_fixupLabel) - (m_fixupData[i].m_fixupPos + m_fixupData[i].m_fixupInstSize);
                if (m_fixupData[i].m_fixupInstSize == 1)
                {
                    if (updateAddr < SByte.MinValue || updateAddr > SByte.MaxValue)
                    {
                        throw new NotSupportedException(Environment.GetResourceString("NotSupported_IllegalOneByteBranch", m_fixupData[i].m_fixupPos, updateAddr));
                    }

                    if (updateAddr < 0)
                    {
                        newBytes[m_fixupData[i].m_fixupPos] = (byte)(256 + updateAddr);
                    }
                    else
                    {
                        newBytes[m_fixupData[i].m_fixupPos] = (byte)updateAddr;
                    }
                }
                else
                {
                    PutInteger4InArray(updateAddr, m_fixupData[i].m_fixupPos, newBytes);
                }
            }

            return newBytes;
        }

        internal __ExceptionInfo[] GetExceptions()
        {
            __ExceptionInfo[] temp;
            if (m_currExcStackCount != 0)
            {
                throw new NotSupportedException(Environment.GetResourceString(ResId.Argument_UnclosedExceptionBlock));
            }

            if (m_exceptionCount == 0)
            {
                return null;
            }

            temp = new __ExceptionInfo[m_exceptionCount];
            Array.Copy(m_exceptions, temp, m_exceptionCount);
            SortExceptions(temp);
            return temp;
        }

        internal void EnsureCapacity(int size)
        {
            if (m_length + size >= m_ILStream.Length)
            {
                if (m_length + size >= 2 * m_ILStream.Length)
                {
                    m_ILStream = EnlargeArray(m_ILStream, m_length + size);
                }
                else
                {
                    m_ILStream = EnlargeArray(m_ILStream);
                }
            }
        }

        internal void PutInteger4(int value)
        {
            m_length = PutInteger4InArray(value, m_length, m_ILStream);
        }

        private static int PutInteger4InArray(int value, int startPos, byte[] array)
        {
            array[startPos++] = (byte)value;
            array[startPos++] = (byte)(value >> 8);
            array[startPos++] = (byte)(value >> 16);
            array[startPos++] = (byte)(value >> 24);
            return startPos;
        }

        private int GetLabelPos(Label lbl)
        {
            int index = lbl.GetLabelValue();
            if (index < 0 || index >= m_labelCount)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadLabel"));
            if (m_labelList[index] < 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadLabelContent"));
            return m_labelList[index];
        }

        private void AddFixup(Label lbl, int pos, int instSize)
        {
            if (m_fixupData == null)
            {
                m_fixupData = new __FixupData[DefaultFixupArraySize];
            }
            else if (m_fixupData.Length <= m_fixupCount)
            {
                m_fixupData = EnlargeArray(m_fixupData);
            }

            m_fixupData[m_fixupCount].m_fixupPos = pos;
            m_fixupData[m_fixupCount].m_fixupLabel = lbl;
            m_fixupData[m_fixupCount].m_fixupInstSize = instSize;
            m_fixupCount++;
        }

        internal int GetMaxStackSize()
        {
            return m_maxStackSize;
        }

        private static void SortExceptions(__ExceptionInfo[] exceptions)
        {
            int least;
            __ExceptionInfo temp;
            int length = exceptions.Length;
            for (int i = 0; i < length; i++)
            {
                least = i;
                for (int j = i + 1; j < length; j++)
                {
                    if (exceptions[least].IsInner(exceptions[j]))
                    {
                        least = j;
                    }
                }

                temp = exceptions[i];
                exceptions[i] = exceptions[least];
                exceptions[least] = temp;
            }
        }

        internal int[] GetTokenFixups()
        {
            if (m_RelocFixupCount == 0)
            {
                Contract.Assert(m_RelocFixupList == null);
                return null;
            }

            int[] narrowTokens = new int[m_RelocFixupCount];
            Array.Copy(m_RelocFixupList, narrowTokens, m_RelocFixupCount);
            return narrowTokens;
        }

        public virtual void Emit(OpCode opcode)
        {
            EnsureCapacity(3);
            InternalEmit(opcode);
        }

        public virtual void Emit(OpCode opcode, byte arg)
        {
            EnsureCapacity(4);
            InternalEmit(opcode);
            m_ILStream[m_length++] = arg;
        }

        public void Emit(OpCode opcode, sbyte arg)
        {
            EnsureCapacity(4);
            InternalEmit(opcode);
            if (arg < 0)
            {
                m_ILStream[m_length++] = (byte)(256 + arg);
            }
            else
            {
                m_ILStream[m_length++] = (byte)arg;
            }
        }

        public virtual void Emit(OpCode opcode, short arg)
        {
            EnsureCapacity(5);
            InternalEmit(opcode);
            m_ILStream[m_length++] = (byte)arg;
            m_ILStream[m_length++] = (byte)(arg >> 8);
        }

        public virtual void Emit(OpCode opcode, int arg)
        {
            EnsureCapacity(7);
            InternalEmit(opcode);
            PutInteger4(arg);
        }

        public virtual void Emit(OpCode opcode, MethodInfo meth)
        {
            if (meth == null)
                throw new ArgumentNullException("meth");
            Contract.EndContractBlock();
            if (opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj))
            {
                EmitCall(opcode, meth, null);
            }
            else
            {
                int stackchange = 0;
                bool useMethodDef = opcode.Equals(OpCodes.Ldtoken) || opcode.Equals(OpCodes.Ldftn) || opcode.Equals(OpCodes.Ldvirtftn);
                int tk = GetMethodToken(meth, null, useMethodDef);
                EnsureCapacity(7);
                InternalEmit(opcode);
                UpdateStackSize(opcode, stackchange);
                RecordTokenFixup();
                PutInteger4(tk);
            }
        }

        public virtual void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes)
        {
            int stackchange = 0;
            SignatureHelper sig;
            if (optionalParameterTypes != null)
            {
                if ((callingConvention & CallingConventions.VarArgs) == 0)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAVarArgCallingConvention"));
                }
            }

            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            sig = GetMemberRefSignature(callingConvention, returnType, parameterTypes, optionalParameterTypes);
            EnsureCapacity(7);
            Emit(OpCodes.Calli);
            if (returnType != typeof (void))
                stackchange++;
            if (parameterTypes != null)
                stackchange -= parameterTypes.Length;
            if (optionalParameterTypes != null)
                stackchange -= optionalParameterTypes.Length;
            if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
                stackchange--;
            stackchange--;
            UpdateStackSize(OpCodes.Calli, stackchange);
            RecordTokenFixup();
            PutInteger4(modBuilder.GetSignatureToken(sig).Token);
        }

        public virtual void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
        {
            int stackchange = 0;
            int cParams = 0;
            int i;
            SignatureHelper sig;
            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            if (parameterTypes != null)
            {
                cParams = parameterTypes.Length;
            }

            sig = SignatureHelper.GetMethodSigHelper(modBuilder, unmanagedCallConv, returnType);
            if (parameterTypes != null)
            {
                for (i = 0; i < cParams; i++)
                {
                    sig.AddArgument(parameterTypes[i]);
                }
            }

            if (returnType != typeof (void))
                stackchange++;
            if (parameterTypes != null)
                stackchange -= cParams;
            stackchange--;
            UpdateStackSize(OpCodes.Calli, stackchange);
            EnsureCapacity(7);
            Emit(OpCodes.Calli);
            RecordTokenFixup();
            PutInteger4(modBuilder.GetSignatureToken(sig).Token);
        }

        public virtual void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes)
        {
            if (methodInfo == null)
                throw new ArgumentNullException("methodInfo");
            if (!(opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj)))
                throw new ArgumentException(Environment.GetResourceString("Argument_NotMethodCallOpcode"), "opcode");
            Contract.EndContractBlock();
            int stackchange = 0;
            int tk = GetMethodToken(methodInfo, optionalParameterTypes, false);
            EnsureCapacity(7);
            InternalEmit(opcode);
            if (methodInfo.ReturnType != typeof (void))
                stackchange++;
            Type[] parameters = methodInfo.GetParameterTypes();
            if (parameters != null)
                stackchange -= parameters.Length;
            if (!(methodInfo is SymbolMethod) && methodInfo.IsStatic == false && !(opcode.Equals(OpCodes.Newobj)))
                stackchange--;
            if (optionalParameterTypes != null)
                stackchange -= optionalParameterTypes.Length;
            UpdateStackSize(opcode, stackchange);
            RecordTokenFixup();
            PutInteger4(tk);
        }

        public virtual void Emit(OpCode opcode, SignatureHelper signature)
        {
            if (signature == null)
                throw new ArgumentNullException("signature");
            Contract.EndContractBlock();
            int stackchange = 0;
            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            SignatureToken sig = modBuilder.GetSignatureToken(signature);
            int tempVal = sig.Token;
            EnsureCapacity(7);
            InternalEmit(opcode);
            if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
            {
                Contract.Assert(opcode.Equals(OpCodes.Calli), "Unexpected opcode encountered for StackBehaviour VarPop.");
                stackchange -= signature.ArgumentCount;
                stackchange--;
                UpdateStackSize(opcode, stackchange);
            }

            RecordTokenFixup();
            PutInteger4(tempVal);
        }

        public virtual void Emit(OpCode opcode, ConstructorInfo con)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            Contract.EndContractBlock();
            int stackchange = 0;
            int tk = GetMethodToken(con, null, true);
            EnsureCapacity(7);
            InternalEmit(opcode);
            if (opcode.StackBehaviourPush == StackBehaviour.Varpush)
            {
                Contract.Assert(opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt), "Unexpected opcode encountered for StackBehaviour of VarPush.");
                stackchange++;
            }

            if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
            {
                Contract.Assert(opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj), "Unexpected opcode encountered for StackBehaviour of VarPop.");
                Type[] parameters = con.GetParameterTypes();
                if (parameters != null)
                    stackchange -= parameters.Length;
            }

            UpdateStackSize(opcode, stackchange);
            RecordTokenFixup();
            PutInteger4(tk);
        }

        public virtual void Emit(OpCode opcode, Type cls)
        {
            int tempVal = 0;
            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            if (opcode == OpCodes.Ldtoken && cls != null && cls.IsGenericTypeDefinition)
            {
                tempVal = modBuilder.GetTypeToken(cls).Token;
            }
            else
            {
                tempVal = modBuilder.GetTypeTokenInternal(cls).Token;
            }

            EnsureCapacity(7);
            InternalEmit(opcode);
            RecordTokenFixup();
            PutInteger4(tempVal);
        }

        public virtual void Emit(OpCode opcode, long arg)
        {
            EnsureCapacity(11);
            InternalEmit(opcode);
            m_ILStream[m_length++] = (byte)arg;
            m_ILStream[m_length++] = (byte)(arg >> 8);
            m_ILStream[m_length++] = (byte)(arg >> 16);
            m_ILStream[m_length++] = (byte)(arg >> 24);
            m_ILStream[m_length++] = (byte)(arg >> 32);
            m_ILStream[m_length++] = (byte)(arg >> 40);
            m_ILStream[m_length++] = (byte)(arg >> 48);
            m_ILStream[m_length++] = (byte)(arg >> 56);
        }

        unsafe public virtual void Emit(OpCode opcode, float arg)
        {
            EnsureCapacity(7);
            InternalEmit(opcode);
            uint tempVal = *(uint *)&arg;
            m_ILStream[m_length++] = (byte)tempVal;
            m_ILStream[m_length++] = (byte)(tempVal >> 8);
            m_ILStream[m_length++] = (byte)(tempVal >> 16);
            m_ILStream[m_length++] = (byte)(tempVal >> 24);
        }

        unsafe public virtual void Emit(OpCode opcode, double arg)
        {
            EnsureCapacity(11);
            InternalEmit(opcode);
            ulong tempVal = *(ulong *)&arg;
            m_ILStream[m_length++] = (byte)tempVal;
            m_ILStream[m_length++] = (byte)(tempVal >> 8);
            m_ILStream[m_length++] = (byte)(tempVal >> 16);
            m_ILStream[m_length++] = (byte)(tempVal >> 24);
            m_ILStream[m_length++] = (byte)(tempVal >> 32);
            m_ILStream[m_length++] = (byte)(tempVal >> 40);
            m_ILStream[m_length++] = (byte)(tempVal >> 48);
            m_ILStream[m_length++] = (byte)(tempVal >> 56);
        }

        public virtual void Emit(OpCode opcode, Label label)
        {
            int tempVal = label.GetLabelValue();
            EnsureCapacity(7);
            InternalEmit(opcode);
            if (OpCodes.TakesSingleByteArgument(opcode))
            {
                AddFixup(label, m_length, 1);
                m_length++;
            }
            else
            {
                AddFixup(label, m_length, 4);
                m_length += 4;
            }
        }

        public virtual void Emit(OpCode opcode, Label[] labels)
        {
            if (labels == null)
                throw new ArgumentNullException("labels");
            Contract.EndContractBlock();
            int i;
            int remaining;
            int count = labels.Length;
            EnsureCapacity(count * 4 + 7);
            InternalEmit(opcode);
            PutInteger4(count);
            for (remaining = count * 4, i = 0; remaining > 0; remaining -= 4, i++)
            {
                AddFixup(labels[i], m_length, remaining);
                m_length += 4;
            }
        }

        public virtual void Emit(OpCode opcode, FieldInfo field)
        {
            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            int tempVal = modBuilder.GetFieldToken(field).Token;
            EnsureCapacity(7);
            InternalEmit(opcode);
            RecordTokenFixup();
            PutInteger4(tempVal);
        }

        public virtual void Emit(OpCode opcode, String str)
        {
            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            int tempVal = modBuilder.GetStringConstant(str).Token;
            EnsureCapacity(7);
            InternalEmit(opcode);
            PutInteger4(tempVal);
        }

        public virtual void Emit(OpCode opcode, LocalBuilder local)
        {
            if (local == null)
            {
                throw new ArgumentNullException("local");
            }

            Contract.EndContractBlock();
            int tempVal = local.GetLocalIndex();
            if (local.GetMethodBuilder() != m_methodBuilder)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_UnmatchedMethodForLocal"), "local");
            }

            if (opcode.Equals(OpCodes.Ldloc))
            {
                switch (tempVal)
                {
                    case 0:
                        opcode = OpCodes.Ldloc_0;
                        break;
                    case 1:
                        opcode = OpCodes.Ldloc_1;
                        break;
                    case 2:
                        opcode = OpCodes.Ldloc_2;
                        break;
                    case 3:
                        opcode = OpCodes.Ldloc_3;
                        break;
                    default:
                        if (tempVal <= 255)
                            opcode = OpCodes.Ldloc_S;
                        break;
                }
            }
            else if (opcode.Equals(OpCodes.Stloc))
            {
                switch (tempVal)
                {
                    case 0:
                        opcode = OpCodes.Stloc_0;
                        break;
                    case 1:
                        opcode = OpCodes.Stloc_1;
                        break;
                    case 2:
                        opcode = OpCodes.Stloc_2;
                        break;
                    case 3:
                        opcode = OpCodes.Stloc_3;
                        break;
                    default:
                        if (tempVal <= 255)
                            opcode = OpCodes.Stloc_S;
                        break;
                }
            }
            else if (opcode.Equals(OpCodes.Ldloca))
            {
                if (tempVal <= 255)
                    opcode = OpCodes.Ldloca_S;
            }

            EnsureCapacity(7);
            InternalEmit(opcode);
            if (opcode.OperandType == OperandType.InlineNone)
                return;
            else if (!OpCodes.TakesSingleByteArgument(opcode))
            {
                m_ILStream[m_length++] = (byte)tempVal;
                m_ILStream[m_length++] = (byte)(tempVal >> 8);
            }
            else
            {
                if (tempVal > Byte.MaxValue)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadInstructionOrIndexOutOfBound"));
                }

                m_ILStream[m_length++] = (byte)tempVal;
            }
        }

        public virtual Label BeginExceptionBlock()
        {
            if (m_exceptions == null)
            {
                m_exceptions = new __ExceptionInfo[DefaultExceptionArraySize];
            }

            if (m_currExcStack == null)
            {
                m_currExcStack = new __ExceptionInfo[DefaultExceptionArraySize];
            }

            if (m_exceptionCount >= m_exceptions.Length)
            {
                m_exceptions = EnlargeArray(m_exceptions);
            }

            if (m_currExcStackCount >= m_currExcStack.Length)
            {
                m_currExcStack = EnlargeArray(m_currExcStack);
            }

            Label endLabel = DefineLabel();
            __ExceptionInfo exceptionInfo = new __ExceptionInfo(m_length, endLabel);
            m_exceptions[m_exceptionCount++] = exceptionInfo;
            m_currExcStack[m_currExcStackCount++] = exceptionInfo;
            return endLabel;
        }

        public virtual void EndExceptionBlock()
        {
            if (m_currExcStackCount == 0)
            {
                throw new NotSupportedException(Environment.GetResourceString("Argument_NotInExceptionBlock"));
            }

            __ExceptionInfo current = m_currExcStack[m_currExcStackCount - 1];
            m_currExcStack[m_currExcStackCount - 1] = null;
            m_currExcStackCount--;
            Label endLabel = current.GetEndLabel();
            int state = current.GetCurrentState();
            if (state == __ExceptionInfo.State_Filter || state == __ExceptionInfo.State_Try)
            {
                throw new InvalidOperationException(Environment.GetResourceString("Argument_BadExceptionCodeGen"));
            }

            if (state == __ExceptionInfo.State_Catch)
            {
                this.Emit(OpCodes.Leave, endLabel);
            }
            else if (state == __ExceptionInfo.State_Finally || state == __ExceptionInfo.State_Fault)
            {
                this.Emit(OpCodes.Endfinally);
            }

            if (m_labelList[endLabel.GetLabelValue()] == -1)
            {
                MarkLabel(endLabel);
            }
            else
            {
                MarkLabel(current.GetFinallyEndLabel());
            }

            current.Done(m_length);
        }

        public virtual void BeginExceptFilterBlock()
        {
            if (m_currExcStackCount == 0)
                throw new NotSupportedException(Environment.GetResourceString("Argument_NotInExceptionBlock"));
            __ExceptionInfo current = m_currExcStack[m_currExcStackCount - 1];
            Label endLabel = current.GetEndLabel();
            this.Emit(OpCodes.Leave, endLabel);
            current.MarkFilterAddr(m_length);
        }

        public virtual void BeginCatchBlock(Type exceptionType)
        {
            if (m_currExcStackCount == 0)
            {
                throw new NotSupportedException(Environment.GetResourceString("Argument_NotInExceptionBlock"));
            }

            __ExceptionInfo current = m_currExcStack[m_currExcStackCount - 1];
            if (current.GetCurrentState() == __ExceptionInfo.State_Filter)
            {
                if (exceptionType != null)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_ShouldNotSpecifyExceptionType"));
                }

                this.Emit(OpCodes.Endfilter);
            }
            else
            {
                if (exceptionType == null)
                {
                    throw new ArgumentNullException("exceptionType");
                }

                Label endLabel = current.GetEndLabel();
                this.Emit(OpCodes.Leave, endLabel);
            }

            current.MarkCatchAddr(m_length, exceptionType);
        }

        public virtual void BeginFaultBlock()
        {
            if (m_currExcStackCount == 0)
            {
                throw new NotSupportedException(Environment.GetResourceString("Argument_NotInExceptionBlock"));
            }

            __ExceptionInfo current = m_currExcStack[m_currExcStackCount - 1];
            Label endLabel = current.GetEndLabel();
            this.Emit(OpCodes.Leave, endLabel);
            current.MarkFaultAddr(m_length);
        }

        public virtual void BeginFinallyBlock()
        {
            if (m_currExcStackCount == 0)
            {
                throw new NotSupportedException(Environment.GetResourceString("Argument_NotInExceptionBlock"));
            }

            __ExceptionInfo current = m_currExcStack[m_currExcStackCount - 1];
            int state = current.GetCurrentState();
            Label endLabel = current.GetEndLabel();
            int catchEndAddr = 0;
            if (state != __ExceptionInfo.State_Try)
            {
                this.Emit(OpCodes.Leave, endLabel);
                catchEndAddr = m_length;
            }

            MarkLabel(endLabel);
            Label finallyEndLabel = this.DefineLabel();
            current.SetFinallyEndLabel(finallyEndLabel);
            this.Emit(OpCodes.Leave, finallyEndLabel);
            if (catchEndAddr == 0)
                catchEndAddr = m_length;
            current.MarkFinallyAddr(m_length, catchEndAddr);
        }

        public virtual Label DefineLabel()
        {
            if (m_labelList == null)
            {
                m_labelList = new int[DefaultLabelArraySize];
            }

            if (m_labelCount >= m_labelList.Length)
            {
                m_labelList = EnlargeArray(m_labelList);
            }

            m_labelList[m_labelCount] = -1;
            return new Label(m_labelCount++);
        }

        public virtual void MarkLabel(Label loc)
        {
            int labelIndex = loc.GetLabelValue();
            if (labelIndex < 0 || labelIndex >= m_labelList.Length)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidLabel"));
            }

            if (m_labelList[labelIndex] != -1)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_RedefinedLabel"));
            }

            m_labelList[labelIndex] = m_length;
        }

        public virtual void ThrowException(Type excType)
        {
            if (excType == null)
            {
                throw new ArgumentNullException("excType");
            }

            if (!excType.IsSubclassOf(typeof (Exception)) && excType != typeof (Exception))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_NotExceptionType"));
            }

            Contract.EndContractBlock();
            ConstructorInfo con = excType.GetConstructor(Type.EmptyTypes);
            if (con == null)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_MissingDefaultConstructor"));
            }

            this.Emit(OpCodes.Newobj, con);
            this.Emit(OpCodes.Throw);
        }

        private static Type GetConsoleType()
        {
            return typeof (Console);
        }

        public virtual void EmitWriteLine(String value)
        {
            Emit(OpCodes.Ldstr, value);
            Type[] parameterTypes = new Type[1];
            parameterTypes[0] = typeof (String);
            MethodInfo mi = GetConsoleType().GetMethod("WriteLine", parameterTypes);
            Emit(OpCodes.Call, mi);
        }

        public virtual void EmitWriteLine(LocalBuilder localBuilder)
        {
            Object cls;
            if (m_methodBuilder == null)
            {
                throw new ArgumentException(Environment.GetResourceString("InvalidOperation_BadILGeneratorUsage"));
            }

            MethodInfo prop = GetConsoleType().GetMethod("get_Out");
            Emit(OpCodes.Call, prop);
            Emit(OpCodes.Ldloc, localBuilder);
            Type[] parameterTypes = new Type[1];
            cls = localBuilder.LocalType;
            if (cls is TypeBuilder || cls is EnumBuilder)
            {
                throw new ArgumentException(Environment.GetResourceString("NotSupported_OutputStreamUsingTypeBuilder"));
            }

            parameterTypes[0] = (Type)cls;
            MethodInfo mi = typeof (TextWriter).GetMethod("WriteLine", parameterTypes);
            if (mi == null)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmitWriteLineType"), "localBuilder");
            }

            Emit(OpCodes.Callvirt, mi);
        }

        public virtual void EmitWriteLine(FieldInfo fld)
        {
            Object cls;
            if (fld == null)
            {
                throw new ArgumentNullException("fld");
            }

            Contract.EndContractBlock();
            MethodInfo prop = GetConsoleType().GetMethod("get_Out");
            Emit(OpCodes.Call, prop);
            if ((fld.Attributes & FieldAttributes.Static) != 0)
            {
                Emit(OpCodes.Ldsfld, fld);
            }
            else
            {
                Emit(OpCodes.Ldarg, (short)0);
                Emit(OpCodes.Ldfld, fld);
            }

            Type[] parameterTypes = new Type[1];
            cls = fld.FieldType;
            if (cls is TypeBuilder || cls is EnumBuilder)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_OutputStreamUsingTypeBuilder"));
            }

            parameterTypes[0] = (Type)cls;
            MethodInfo mi = typeof (TextWriter).GetMethod("WriteLine", parameterTypes);
            if (mi == null)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmitWriteLineType"), "fld");
            }

            Emit(OpCodes.Callvirt, mi);
        }

        public virtual LocalBuilder DeclareLocal(Type localType)
        {
            return DeclareLocal(localType, false);
        }

        public virtual LocalBuilder DeclareLocal(Type localType, bool pinned)
        {
            LocalBuilder localBuilder;
            MethodBuilder methodBuilder = m_methodBuilder as MethodBuilder;
            if (methodBuilder == null)
                throw new NotSupportedException();
            if (methodBuilder.IsTypeCreated())
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_TypeHasBeenCreated"));
            }

            if (localType == null)
            {
                throw new ArgumentNullException("localType");
            }

            if (methodBuilder.m_bIsBaked)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_MethodBaked"));
            }

            m_localSignature.AddArgument(localType, pinned);
            localBuilder = new LocalBuilder(m_localCount, localType, methodBuilder, pinned);
            m_localCount++;
            return localBuilder;
        }

        public virtual void UsingNamespace(String usingNamespace)
        {
            if (usingNamespace == null)
                throw new ArgumentNullException("usingNamespace");
            if (usingNamespace.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyName"), "usingNamespace");
            Contract.EndContractBlock();
            int index;
            MethodBuilder methodBuilder = m_methodBuilder as MethodBuilder;
            if (methodBuilder == null)
                throw new NotSupportedException();
            index = methodBuilder.GetILGenerator().m_ScopeTree.GetCurrentActiveScopeIndex();
            if (index == -1)
            {
                methodBuilder.m_localSymInfo.AddUsingNamespace(usingNamespace);
            }
            else
            {
                m_ScopeTree.AddUsingNamespaceToCurrentScope(usingNamespace);
            }
        }

        public virtual void MarkSequencePoint(ISymbolDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn)
        {
            if (startLine == 0 || startLine < 0 || endLine == 0 || endLine < 0)
            {
                throw new ArgumentOutOfRangeException("startLine");
            }

            Contract.EndContractBlock();
            m_LineNumberInfo.AddLineNumberInfo(document, m_length, startLine, startColumn, endLine, endColumn);
        }

        public virtual void BeginScope()
        {
            m_ScopeTree.AddScopeInfo(ScopeAction.Open, m_length);
        }

        public virtual void EndScope()
        {
            m_ScopeTree.AddScopeInfo(ScopeAction.Close, m_length);
        }

        public virtual int ILOffset
        {
            get
            {
                return m_length;
            }
        }
    }

    internal struct __FixupData
    {
        internal Label m_fixupLabel;
        internal int m_fixupPos;
        internal int m_fixupInstSize;
    }

    internal sealed class __ExceptionInfo
    {
        internal const int None = 0x0000;
        internal const int Filter = 0x0001;
        internal const int Finally = 0x0002;
        internal const int Fault = 0x0004;
        internal const int PreserveStack = 0x0004;
        internal const int State_Try = 0;
        internal const int State_Filter = 1;
        internal const int State_Catch = 2;
        internal const int State_Finally = 3;
        internal const int State_Fault = 4;
        internal const int State_Done = 5;
        internal int m_startAddr;
        internal int[] m_filterAddr;
        internal int[] m_catchAddr;
        internal int[] m_catchEndAddr;
        internal int[] m_type;
        internal Type[] m_catchClass;
        internal Label m_endLabel;
        internal Label m_finallyEndLabel;
        internal int m_endAddr;
        internal int m_endFinally;
        internal int m_currentCatch;
        int m_currentState;
        private __ExceptionInfo()
        {
            m_startAddr = 0;
            m_filterAddr = null;
            m_catchAddr = null;
            m_catchEndAddr = null;
            m_endAddr = 0;
            m_currentCatch = 0;
            m_type = null;
            m_endFinally = -1;
            m_currentState = State_Try;
        }

        internal __ExceptionInfo(int startAddr, Label endLabel)
        {
            m_startAddr = startAddr;
            m_endAddr = -1;
            m_filterAddr = new int[4];
            m_catchAddr = new int[4];
            m_catchEndAddr = new int[4];
            m_catchClass = new Type[4];
            m_currentCatch = 0;
            m_endLabel = endLabel;
            m_type = new int[4];
            m_endFinally = -1;
            m_currentState = State_Try;
        }

        private static Type[] EnlargeArray(Type[] incoming)
        {
            Type[] temp = new Type[incoming.Length * 2];
            Array.Copy(incoming, temp, incoming.Length);
            return temp;
        }

        private void MarkHelper(int catchorfilterAddr, int catchEndAddr, Type catchClass, int type)
        {
            if (m_currentCatch >= m_catchAddr.Length)
            {
                m_filterAddr = ILGenerator.EnlargeArray(m_filterAddr);
                m_catchAddr = ILGenerator.EnlargeArray(m_catchAddr);
                m_catchEndAddr = ILGenerator.EnlargeArray(m_catchEndAddr);
                m_catchClass = __ExceptionInfo.EnlargeArray(m_catchClass);
                m_type = ILGenerator.EnlargeArray(m_type);
            }

            if (type == Filter)
            {
                m_type[m_currentCatch] = type;
                m_filterAddr[m_currentCatch] = catchorfilterAddr;
                m_catchAddr[m_currentCatch] = -1;
                if (m_currentCatch > 0)
                {
                    Contract.Assert(m_catchEndAddr[m_currentCatch - 1] == -1, "m_catchEndAddr[m_currentCatch-1] == -1");
                    m_catchEndAddr[m_currentCatch - 1] = catchorfilterAddr;
                }
            }
            else
            {
                m_catchClass[m_currentCatch] = catchClass;
                if (m_type[m_currentCatch] != Filter)
                {
                    m_type[m_currentCatch] = type;
                }

                m_catchAddr[m_currentCatch] = catchorfilterAddr;
                if (m_currentCatch > 0)
                {
                    if (m_type[m_currentCatch] != Filter)
                    {
                        Contract.Assert(m_catchEndAddr[m_currentCatch - 1] == -1, "m_catchEndAddr[m_currentCatch-1] == -1");
                        m_catchEndAddr[m_currentCatch - 1] = catchEndAddr;
                    }
                }

                m_catchEndAddr[m_currentCatch] = -1;
                m_currentCatch++;
            }

            if (m_endAddr == -1)
            {
                m_endAddr = catchorfilterAddr;
            }
        }

        internal void MarkFilterAddr(int filterAddr)
        {
            m_currentState = State_Filter;
            MarkHelper(filterAddr, filterAddr, null, Filter);
        }

        internal void MarkFaultAddr(int faultAddr)
        {
            m_currentState = State_Fault;
            MarkHelper(faultAddr, faultAddr, null, Fault);
        }

        internal void MarkCatchAddr(int catchAddr, Type catchException)
        {
            m_currentState = State_Catch;
            MarkHelper(catchAddr, catchAddr, catchException, None);
        }

        internal void MarkFinallyAddr(int finallyAddr, int endCatchAddr)
        {
            if (m_endFinally != -1)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_TooManyFinallyClause"));
            }
            else
            {
                m_currentState = State_Finally;
                m_endFinally = finallyAddr;
            }

            MarkHelper(finallyAddr, endCatchAddr, null, Finally);
        }

        internal void Done(int endAddr)
        {
            Contract.Assert(m_currentCatch > 0, "m_currentCatch > 0");
            Contract.Assert(m_catchAddr[m_currentCatch - 1] > 0, "m_catchAddr[m_currentCatch-1] > 0");
            Contract.Assert(m_catchEndAddr[m_currentCatch - 1] == -1, "m_catchEndAddr[m_currentCatch-1] == -1");
            m_catchEndAddr[m_currentCatch - 1] = endAddr;
            m_currentState = State_Done;
        }

        internal int GetStartAddress()
        {
            return m_startAddr;
        }

        internal int GetEndAddress()
        {
            return m_endAddr;
        }

        internal int GetFinallyEndAddress()
        {
            return m_endFinally;
        }

        internal Label GetEndLabel()
        {
            return m_endLabel;
        }

        internal int[] GetFilterAddresses()
        {
            return m_filterAddr;
        }

        internal int[] GetCatchAddresses()
        {
            return m_catchAddr;
        }

        internal int[] GetCatchEndAddresses()
        {
            return m_catchEndAddr;
        }

        internal Type[] GetCatchClass()
        {
            return m_catchClass;
        }

        internal int GetNumberOfCatches()
        {
            return m_currentCatch;
        }

        internal int[] GetExceptionTypes()
        {
            return m_type;
        }

        internal void SetFinallyEndLabel(Label lbl)
        {
            m_finallyEndLabel = lbl;
        }

        internal Label GetFinallyEndLabel()
        {
            return m_finallyEndLabel;
        }

        internal bool IsInner(__ExceptionInfo exc)
        {
            Contract.Requires(exc != null);
            Contract.Assert(m_currentCatch > 0, "m_currentCatch > 0");
            Contract.Assert(exc.m_currentCatch > 0, "exc.m_currentCatch > 0");
            int exclast = exc.m_currentCatch - 1;
            int last = m_currentCatch - 1;
            if (exc.m_catchEndAddr[exclast] < m_catchEndAddr[last])
                return true;
            else if (exc.m_catchEndAddr[exclast] == m_catchEndAddr[last])
            {
                Contract.Assert(exc.GetEndAddress() != GetEndAddress(), "exc.GetEndAddress() != GetEndAddress()");
                if (exc.GetEndAddress() > GetEndAddress())
                    return true;
            }

            return false;
        }

        internal int GetCurrentState()
        {
            return m_currentState;
        }
    }

    enum ScopeAction
    {
        Open = 0x0,
        Close = 0x1
    }

    internal sealed class ScopeTree
    {
        internal ScopeTree()
        {
            m_iOpenScopeCount = 0;
            m_iCount = 0;
        }

        internal int GetCurrentActiveScopeIndex()
        {
            int cClose = 0;
            int i = m_iCount - 1;
            if (m_iCount == 0)
            {
                return -1;
            }

            for (; cClose > 0 || m_ScopeActions[i] == ScopeAction.Close; i--)
            {
                if (m_ScopeActions[i] == ScopeAction.Open)
                {
                    cClose--;
                }
                else
                    cClose++;
            }

            return i;
        }

        internal void AddLocalSymInfoToCurrentScope(String strName, byte[] signature, int slot, int startOffset, int endOffset)
        {
            int i = GetCurrentActiveScopeIndex();
            if (m_localSymInfos[i] == null)
            {
                m_localSymInfos[i] = new LocalSymInfo();
            }

            m_localSymInfos[i].AddLocalSymInfo(strName, signature, slot, startOffset, endOffset);
        }

        internal void AddUsingNamespaceToCurrentScope(String strNamespace)
        {
            int i = GetCurrentActiveScopeIndex();
            if (m_localSymInfos[i] == null)
            {
                m_localSymInfos[i] = new LocalSymInfo();
            }

            m_localSymInfos[i].AddUsingNamespace(strNamespace);
        }

        internal void AddScopeInfo(ScopeAction sa, int iOffset)
        {
            if (sa == ScopeAction.Close && m_iOpenScopeCount <= 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_UnmatchingSymScope"));
            }

            Contract.EndContractBlock();
            EnsureCapacity();
            m_ScopeActions[m_iCount] = sa;
            m_iOffsets[m_iCount] = iOffset;
            m_localSymInfos[m_iCount] = null;
            checked
            {
                m_iCount++;
            }

            if (sa == ScopeAction.Open)
            {
                m_iOpenScopeCount++;
            }
            else
                m_iOpenScopeCount--;
        }

        internal void EnsureCapacity()
        {
            if (m_iCount == 0)
            {
                m_iOffsets = new int[InitialSize];
                m_ScopeActions = new ScopeAction[InitialSize];
                m_localSymInfos = new LocalSymInfo[InitialSize];
            }
            else if (m_iCount == m_iOffsets.Length)
            {
                int newSize = checked (m_iCount * 2);
                int[] temp = new int[newSize];
                Array.Copy(m_iOffsets, temp, m_iCount);
                m_iOffsets = temp;
                ScopeAction[] tempSA = new ScopeAction[newSize];
                Array.Copy(m_ScopeActions, tempSA, m_iCount);
                m_ScopeActions = tempSA;
                LocalSymInfo[] tempLSI = new LocalSymInfo[newSize];
                Array.Copy(m_localSymInfos, tempLSI, m_iCount);
                m_localSymInfos = tempLSI;
            }
        }

        internal void EmitScopeTree(ISymbolWriter symWriter)
        {
            int i;
            for (i = 0; i < m_iCount; i++)
            {
                if (m_ScopeActions[i] == ScopeAction.Open)
                {
                    symWriter.OpenScope(m_iOffsets[i]);
                }
                else
                {
                    symWriter.CloseScope(m_iOffsets[i]);
                }

                if (m_localSymInfos[i] != null)
                {
                    m_localSymInfos[i].EmitLocalSymInfo(symWriter);
                }
            }
        }

        internal int[] m_iOffsets;
        internal ScopeAction[] m_ScopeActions;
        internal int m_iCount;
        internal int m_iOpenScopeCount;
        internal const int InitialSize = 16;
        internal LocalSymInfo[] m_localSymInfos;
    }

    internal sealed class LineNumberInfo
    {
        internal LineNumberInfo()
        {
            m_DocumentCount = 0;
            m_iLastFound = 0;
        }

        internal void AddLineNumberInfo(ISymbolDocumentWriter document, int iOffset, int iStartLine, int iStartColumn, int iEndLine, int iEndColumn)
        {
            int i;
            i = FindDocument(document);
            Contract.Assert(i < m_DocumentCount, "Bad document look up!");
            m_Documents[i].AddLineNumberInfo(document, iOffset, iStartLine, iStartColumn, iEndLine, iEndColumn);
        }

        private int FindDocument(ISymbolDocumentWriter document)
        {
            int i;
            if (m_iLastFound < m_DocumentCount && m_Documents[m_iLastFound].m_document == document)
                return m_iLastFound;
            for (i = 0; i < m_DocumentCount; i++)
            {
                if (m_Documents[i].m_document == document)
                {
                    m_iLastFound = i;
                    return m_iLastFound;
                }
            }

            EnsureCapacity();
            m_iLastFound = m_DocumentCount;
            m_Documents[m_iLastFound] = new REDocument(document);
            checked
            {
                m_DocumentCount++;
            }

            return m_iLastFound;
        }

        private void EnsureCapacity()
        {
            if (m_DocumentCount == 0)
            {
                m_Documents = new REDocument[InitialSize];
            }
            else if (m_DocumentCount == m_Documents.Length)
            {
                REDocument[] temp = new REDocument[m_DocumentCount * 2];
                Array.Copy(m_Documents, temp, m_DocumentCount);
                m_Documents = temp;
            }
        }

        internal void EmitLineNumberInfo(ISymbolWriter symWriter)
        {
            for (int i = 0; i < m_DocumentCount; i++)
                m_Documents[i].EmitLineNumberInfo(symWriter);
        }

        private int m_DocumentCount;
        private REDocument[] m_Documents;
        private const int InitialSize = 16;
        private int m_iLastFound;
    }

    internal sealed class REDocument
    {
        internal REDocument(ISymbolDocumentWriter document)
        {
            m_iLineNumberCount = 0;
            m_document = document;
        }

        internal void AddLineNumberInfo(ISymbolDocumentWriter document, int iOffset, int iStartLine, int iStartColumn, int iEndLine, int iEndColumn)
        {
            Contract.Assert(document == m_document, "Bad document look up!");
            EnsureCapacity();
            m_iOffsets[m_iLineNumberCount] = iOffset;
            m_iLines[m_iLineNumberCount] = iStartLine;
            m_iColumns[m_iLineNumberCount] = iStartColumn;
            m_iEndLines[m_iLineNumberCount] = iEndLine;
            m_iEndColumns[m_iLineNumberCount] = iEndColumn;
            checked
            {
                m_iLineNumberCount++;
            }
        }

        private void EnsureCapacity()
        {
            if (m_iLineNumberCount == 0)
            {
                m_iOffsets = new int[InitialSize];
                m_iLines = new int[InitialSize];
                m_iColumns = new int[InitialSize];
                m_iEndLines = new int[InitialSize];
                m_iEndColumns = new int[InitialSize];
            }
            else if (m_iLineNumberCount == m_iOffsets.Length)
            {
                int newSize = checked (m_iLineNumberCount * 2);
                int[] temp = new int[newSize];
                Array.Copy(m_iOffsets, temp, m_iLineNumberCount);
                m_iOffsets = temp;
                temp = new int[newSize];
                Array.Copy(m_iLines, temp, m_iLineNumberCount);
                m_iLines = temp;
                temp = new int[newSize];
                Array.Copy(m_iColumns, temp, m_iLineNumberCount);
                m_iColumns = temp;
                temp = new int[newSize];
                Array.Copy(m_iEndLines, temp, m_iLineNumberCount);
                m_iEndLines = temp;
                temp = new int[newSize];
                Array.Copy(m_iEndColumns, temp, m_iLineNumberCount);
                m_iEndColumns = temp;
            }
        }

        internal void EmitLineNumberInfo(ISymbolWriter symWriter)
        {
            int[] iOffsetsTemp;
            int[] iLinesTemp;
            int[] iColumnsTemp;
            int[] iEndLinesTemp;
            int[] iEndColumnsTemp;
            if (m_iLineNumberCount == 0)
                return;
            iOffsetsTemp = new int[m_iLineNumberCount];
            Array.Copy(m_iOffsets, iOffsetsTemp, m_iLineNumberCount);
            iLinesTemp = new int[m_iLineNumberCount];
            Array.Copy(m_iLines, iLinesTemp, m_iLineNumberCount);
            iColumnsTemp = new int[m_iLineNumberCount];
            Array.Copy(m_iColumns, iColumnsTemp, m_iLineNumberCount);
            iEndLinesTemp = new int[m_iLineNumberCount];
            Array.Copy(m_iEndLines, iEndLinesTemp, m_iLineNumberCount);
            iEndColumnsTemp = new int[m_iLineNumberCount];
            Array.Copy(m_iEndColumns, iEndColumnsTemp, m_iLineNumberCount);
            symWriter.DefineSequencePoints(m_document, iOffsetsTemp, iLinesTemp, iColumnsTemp, iEndLinesTemp, iEndColumnsTemp);
        }

        private int[] m_iOffsets;
        private int[] m_iLines;
        private int[] m_iColumns;
        private int[] m_iEndLines;
        private int[] m_iEndColumns;
        internal ISymbolDocumentWriter m_document;
        private int m_iLineNumberCount;
        private const int InitialSize = 16;
    }
}