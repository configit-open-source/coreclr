using System.Diagnostics.Contracts;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Runtime.InteropServices.TCEAdapterGen
{
    internal class EventSinkHelperWriter
    {
        public static readonly String GeneratedTypeNamePostfix = "_SinkHelper";
        public EventSinkHelperWriter(ModuleBuilder OutputModule, Type InputType, Type EventItfType)
        {
            m_InputType = InputType;
            m_OutputModule = OutputModule;
            m_EventItfType = EventItfType;
        }

        public Type Perform()
        {
            Type[] aInterfaces = new Type[1];
            aInterfaces[0] = m_InputType;
            String strFullName = null;
            String strNameSpace = NameSpaceExtractor.ExtractNameSpace(m_EventItfType.FullName);
            if (strNameSpace != "")
                strFullName = strNameSpace + ".";
            strFullName += m_InputType.Name + GeneratedTypeNamePostfix;
            TypeBuilder OutputTypeBuilder = TCEAdapterGenerator.DefineUniqueType(strFullName, TypeAttributes.Sealed | TypeAttributes.Public, null, aInterfaces, m_OutputModule);
            TCEAdapterGenerator.SetHiddenAttribute(OutputTypeBuilder);
            TCEAdapterGenerator.SetClassInterfaceTypeToNone(OutputTypeBuilder);
            MethodInfo[] pMethods = TCEAdapterGenerator.GetPropertyMethods(m_InputType);
            foreach (MethodInfo method in pMethods)
            {
                DefineBlankMethod(OutputTypeBuilder, method);
            }

            MethodInfo[] aMethods = TCEAdapterGenerator.GetNonPropertyMethods(m_InputType);
            FieldBuilder[] afbDelegates = new FieldBuilder[aMethods.Length];
            for (int cMethods = 0; cMethods < aMethods.Length; cMethods++)
            {
                if (m_InputType == aMethods[cMethods].DeclaringType)
                {
                    MethodInfo AddMeth = m_EventItfType.GetMethod("add_" + aMethods[cMethods].Name);
                    ParameterInfo[] aParams = AddMeth.GetParameters();
                    Contract.Assert(aParams.Length == 1, "All event interface methods must take a single delegate derived type and have a void return type");
                    Type DelegateCls = aParams[0].ParameterType;
                    afbDelegates[cMethods] = OutputTypeBuilder.DefineField("m_" + aMethods[cMethods].Name + "Delegate", DelegateCls, FieldAttributes.Public);
                    DefineEventMethod(OutputTypeBuilder, aMethods[cMethods], DelegateCls, afbDelegates[cMethods]);
                }
            }

            FieldBuilder fbCookie = OutputTypeBuilder.DefineField("m_dwCookie", typeof (Int32), FieldAttributes.Public);
            DefineConstructor(OutputTypeBuilder, fbCookie, afbDelegates);
            return OutputTypeBuilder.CreateType();
        }

        private void DefineBlankMethod(TypeBuilder OutputTypeBuilder, MethodInfo Method)
        {
            ParameterInfo[] PIs = Method.GetParameters();
            Type[] parameters = new Type[PIs.Length];
            for (int i = 0; i < PIs.Length; i++)
            {
                parameters[i] = PIs[i].ParameterType;
            }

            MethodBuilder Meth = OutputTypeBuilder.DefineMethod(Method.Name, Method.Attributes & ~MethodAttributes.Abstract, Method.CallingConvention, Method.ReturnType, parameters);
            ILGenerator il = Meth.GetILGenerator();
            AddReturn(Method.ReturnType, il, Meth);
            il.Emit(OpCodes.Ret);
        }

        private void DefineEventMethod(TypeBuilder OutputTypeBuilder, MethodInfo Method, Type DelegateCls, FieldBuilder fbDelegate)
        {
            MethodInfo DelegateInvokeMethod = DelegateCls.GetMethod("Invoke");
            Contract.Assert(DelegateInvokeMethod != null, "Unable to find method Delegate.Invoke()");
            Type ReturnType = Method.ReturnType;
            ParameterInfo[] paramInfos = Method.GetParameters();
            Type[] parameterTypes;
            if (paramInfos != null)
            {
                parameterTypes = new Type[paramInfos.Length];
                for (int i = 0; i < paramInfos.Length; i++)
                {
                    parameterTypes[i] = paramInfos[i].ParameterType;
                }
            }
            else
                parameterTypes = null;
            MethodAttributes attr = MethodAttributes.Public | MethodAttributes.Virtual;
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod(Method.Name, attr, CallingConventions.Standard, ReturnType, parameterTypes);
            ILGenerator il = Meth.GetILGenerator();
            Label ExitLabel = il.DefineLabel();
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbDelegate);
            il.Emit(OpCodes.Brfalse, ExitLabel);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbDelegate);
            ParameterInfo[] aParams = Method.GetParameters();
            for (int cParams = 0; cParams < aParams.Length; cParams++)
            {
                il.Emit(OpCodes.Ldarg, (short)(cParams + 1));
            }

            il.Emit(OpCodes.Callvirt, DelegateInvokeMethod);
            il.Emit(OpCodes.Ret);
            il.MarkLabel(ExitLabel);
            AddReturn(ReturnType, il, Meth);
            il.Emit(OpCodes.Ret);
        }

        private void AddReturn(Type ReturnType, ILGenerator il, MethodBuilder Meth)
        {
            if (ReturnType == typeof (void))
            {
            }
            else if (ReturnType.IsPrimitive)
            {
                switch (System.Type.GetTypeCode(ReturnType))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Char:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        il.Emit(OpCodes.Ldc_I4_0);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        il.Emit(OpCodes.Ldc_I4_0);
                        il.Emit(OpCodes.Conv_I8);
                        break;
                    case TypeCode.Single:
                        il.Emit(OpCodes.Ldc_R4, 0);
                        break;
                    case TypeCode.Double:
                        il.Emit(OpCodes.Ldc_R4, 0);
                        il.Emit(OpCodes.Conv_R8);
                        break;
                    default:
                        if (ReturnType == typeof (IntPtr))
                            il.Emit(OpCodes.Ldc_I4_0);
                        else
                            Contract.Assert(false, "Unexpected type for Primitive type.");
                        break;
                }
            }
            else if (ReturnType.IsValueType)
            {
                Meth.InitLocals = true;
                LocalBuilder ltRetVal = il.DeclareLocal(ReturnType);
                il.Emit(OpCodes.Ldloc_S, ltRetVal);
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }
        }

        private void DefineConstructor(TypeBuilder OutputTypeBuilder, FieldBuilder fbCookie, FieldBuilder[] afbDelegates)
        {
            ConstructorInfo DefaultBaseClsCons = typeof (Object).GetConstructor(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, null, Array.Empty<Type>(), null);
            Contract.Assert(DefaultBaseClsCons != null, "Unable to find the constructor for class " + m_InputType.Name);
            MethodBuilder Cons = OutputTypeBuilder.DefineMethod(".ctor", MethodAttributes.Assembly | MethodAttributes.SpecialName, CallingConventions.Standard, null, null);
            ILGenerator il = Cons.GetILGenerator();
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Call, DefaultBaseClsCons);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldc_I4, 0);
            il.Emit(OpCodes.Stfld, fbCookie);
            for (int cDelegates = 0; cDelegates < afbDelegates.Length; cDelegates++)
            {
                if (afbDelegates[cDelegates] != null)
                {
                    il.Emit(OpCodes.Ldarg, (short)0);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Stfld, afbDelegates[cDelegates]);
                }
            }

            il.Emit(OpCodes.Ret);
        }

        private Type m_InputType;
        private Type m_EventItfType;
        private ModuleBuilder m_OutputModule;
    }
}