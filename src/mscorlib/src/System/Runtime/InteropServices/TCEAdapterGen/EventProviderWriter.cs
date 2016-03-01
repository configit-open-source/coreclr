namespace System.Runtime.InteropServices.TCEAdapterGen
{
    using System.Runtime.InteropServices.ComTypes;
    using ubyte = System.Byte;
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Collections;
    using System.Threading;
    using System.Diagnostics.Contracts;

    internal class EventProviderWriter
    {
        private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
        private readonly Type[] MonitorEnterParamTypes = new Type[]{typeof (Object), Type.GetType("System.Boolean&")};
        public EventProviderWriter(ModuleBuilder OutputModule, String strDestTypeName, Type EventItfType, Type SrcItfType, Type SinkHelperType)
        {
            m_OutputModule = OutputModule;
            m_strDestTypeName = strDestTypeName;
            m_EventItfType = EventItfType;
            m_SrcItfType = SrcItfType;
            m_SinkHelperType = SinkHelperType;
        }

        public Type Perform()
        {
            TypeBuilder OutputTypeBuilder = m_OutputModule.DefineType(m_strDestTypeName, TypeAttributes.Sealed | TypeAttributes.NotPublic, typeof (Object), new Type[]{m_EventItfType, typeof (IDisposable)});
            FieldBuilder fbCPC = OutputTypeBuilder.DefineField("m_ConnectionPointContainer", typeof (IConnectionPointContainer), FieldAttributes.Private);
            FieldBuilder fbSinkHelper = OutputTypeBuilder.DefineField("m_aEventSinkHelpers", typeof (ArrayList), FieldAttributes.Private);
            FieldBuilder fbEventCP = OutputTypeBuilder.DefineField("m_ConnectionPoint", typeof (IConnectionPoint), FieldAttributes.Private);
            MethodBuilder InitSrcItfMethodBuilder = DefineInitSrcItfMethod(OutputTypeBuilder, m_SrcItfType, fbSinkHelper, fbEventCP, fbCPC);
            MethodInfo[] aMethods = TCEAdapterGenerator.GetNonPropertyMethods(m_SrcItfType);
            for (int cMethods = 0; cMethods < aMethods.Length; cMethods++)
            {
                if (m_SrcItfType == aMethods[cMethods].DeclaringType)
                {
                    MethodBuilder AddEventMethodBuilder = DefineAddEventMethod(OutputTypeBuilder, aMethods[cMethods], m_SinkHelperType, fbSinkHelper, fbEventCP, InitSrcItfMethodBuilder);
                    MethodBuilder RemoveEventMethodBuilder = DefineRemoveEventMethod(OutputTypeBuilder, aMethods[cMethods], m_SinkHelperType, fbSinkHelper, fbEventCP);
                }
            }

            DefineConstructor(OutputTypeBuilder, fbCPC);
            MethodBuilder FinalizeMethod = DefineFinalizeMethod(OutputTypeBuilder, m_SinkHelperType, fbSinkHelper, fbEventCP);
            DefineDisposeMethod(OutputTypeBuilder, FinalizeMethod);
            return OutputTypeBuilder.CreateType();
        }

        private MethodBuilder DefineAddEventMethod(TypeBuilder OutputTypeBuilder, MethodInfo SrcItfMethod, Type SinkHelperClass, FieldBuilder fbSinkHelperArray, FieldBuilder fbEventCP, MethodBuilder mbInitSrcItf)
        {
            Type[] aParamTypes;
            FieldInfo DelegateField = SinkHelperClass.GetField("m_" + SrcItfMethod.Name + "Delegate");
            Contract.Assert(DelegateField != null, "Unable to find the field m_" + SrcItfMethod.Name + "Delegate on the sink helper");
            FieldInfo CookieField = SinkHelperClass.GetField("m_dwCookie");
            Contract.Assert(CookieField != null, "Unable to find the field m_dwCookie on the sink helper");
            ConstructorInfo SinkHelperCons = SinkHelperClass.GetConstructor(EventProviderWriter.DefaultLookup | BindingFlags.NonPublic, null, Array.Empty<Type>(), null);
            Contract.Assert(SinkHelperCons != null, "Unable to find the constructor for the sink helper");
            MethodInfo CPAdviseMethod = typeof (IConnectionPoint).GetMethod("Advise");
            Contract.Assert(CPAdviseMethod != null, "Unable to find the method ConnectionPoint.Advise");
            aParamTypes = new Type[1];
            aParamTypes[0] = typeof (Object);
            MethodInfo ArrayListAddMethod = typeof (ArrayList).GetMethod("Add", aParamTypes, null);
            Contract.Assert(ArrayListAddMethod != null, "Unable to find the method ArrayList.Add");
            MethodInfo MonitorEnterMethod = typeof (Monitor).GetMethod("Enter", MonitorEnterParamTypes, null);
            Contract.Assert(MonitorEnterMethod != null, "Unable to find the method Monitor.Enter()");
            aParamTypes[0] = typeof (Object);
            MethodInfo MonitorExitMethod = typeof (Monitor).GetMethod("Exit", aParamTypes, null);
            Contract.Assert(MonitorExitMethod != null, "Unable to find the method Monitor.Exit()");
            Type[] parameterTypes;
            parameterTypes = new Type[1];
            parameterTypes[0] = DelegateField.FieldType;
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod("add_" + SrcItfMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual, null, parameterTypes);
            ILGenerator il = Meth.GetILGenerator();
            Label EventCPNonNullLabel = il.DefineLabel();
            LocalBuilder ltSinkHelper = il.DeclareLocal(SinkHelperClass);
            LocalBuilder ltCookie = il.DeclareLocal(typeof (Int32));
            LocalBuilder ltLockTaken = il.DeclareLocal(typeof (bool));
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldloca_S, ltLockTaken);
            il.Emit(OpCodes.Call, MonitorEnterMethod);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbEventCP);
            il.Emit(OpCodes.Brtrue, EventCPNonNullLabel);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Call, mbInitSrcItf);
            il.MarkLabel(EventCPNonNullLabel);
            il.Emit(OpCodes.Newobj, SinkHelperCons);
            il.Emit(OpCodes.Stloc, ltSinkHelper);
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, ltCookie);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbEventCP);
            il.Emit(OpCodes.Ldloc, ltSinkHelper);
            il.Emit(OpCodes.Castclass, typeof (Object));
            il.Emit(OpCodes.Ldloca, ltCookie);
            il.Emit(OpCodes.Callvirt, CPAdviseMethod);
            il.Emit(OpCodes.Ldloc, ltSinkHelper);
            il.Emit(OpCodes.Ldloc, ltCookie);
            il.Emit(OpCodes.Stfld, CookieField);
            il.Emit(OpCodes.Ldloc, ltSinkHelper);
            il.Emit(OpCodes.Ldarg, (short)1);
            il.Emit(OpCodes.Stfld, DelegateField);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbSinkHelperArray);
            il.Emit(OpCodes.Ldloc, ltSinkHelper);
            il.Emit(OpCodes.Castclass, typeof (Object));
            il.Emit(OpCodes.Callvirt, ArrayListAddMethod);
            il.Emit(OpCodes.Pop);
            il.BeginFinallyBlock();
            Label skipExit = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, ltLockTaken);
            il.Emit(OpCodes.Brfalse_S, skipExit);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Call, MonitorExitMethod);
            il.MarkLabel(skipExit);
            il.EndExceptionBlock();
            il.Emit(OpCodes.Ret);
            return Meth;
        }

        private MethodBuilder DefineRemoveEventMethod(TypeBuilder OutputTypeBuilder, MethodInfo SrcItfMethod, Type SinkHelperClass, FieldBuilder fbSinkHelperArray, FieldBuilder fbEventCP)
        {
            Type[] aParamTypes;
            FieldInfo DelegateField = SinkHelperClass.GetField("m_" + SrcItfMethod.Name + "Delegate");
            Contract.Assert(DelegateField != null, "Unable to find the field m_" + SrcItfMethod.Name + "Delegate on the sink helper");
            FieldInfo CookieField = SinkHelperClass.GetField("m_dwCookie");
            Contract.Assert(CookieField != null, "Unable to find the field m_dwCookie on the sink helper");
            aParamTypes = new Type[1];
            aParamTypes[0] = typeof (Int32);
            MethodInfo ArrayListRemoveMethod = typeof (ArrayList).GetMethod("RemoveAt", aParamTypes, null);
            Contract.Assert(ArrayListRemoveMethod != null, "Unable to find the method ArrayList.RemoveAt()");
            PropertyInfo ArrayListItemProperty = typeof (ArrayList).GetProperty("Item");
            Contract.Assert(ArrayListItemProperty != null, "Unable to find the property ArrayList.Item");
            MethodInfo ArrayListItemGetMethod = ArrayListItemProperty.GetGetMethod();
            Contract.Assert(ArrayListItemGetMethod != null, "Unable to find the get method for property ArrayList.Item");
            PropertyInfo ArrayListSizeProperty = typeof (ArrayList).GetProperty("Count");
            Contract.Assert(ArrayListSizeProperty != null, "Unable to find the property ArrayList.Count");
            MethodInfo ArrayListSizeGetMethod = ArrayListSizeProperty.GetGetMethod();
            Contract.Assert(ArrayListSizeGetMethod != null, "Unable to find the get method for property ArrayList.Count");
            aParamTypes[0] = typeof (Delegate);
            MethodInfo DelegateEqualsMethod = typeof (Delegate).GetMethod("Equals", aParamTypes, null);
            Contract.Assert(DelegateEqualsMethod != null, "Unable to find the method Delegate.Equlals()");
            MethodInfo MonitorEnterMethod = typeof (Monitor).GetMethod("Enter", MonitorEnterParamTypes, null);
            Contract.Assert(MonitorEnterMethod != null, "Unable to find the method Monitor.Enter()");
            aParamTypes[0] = typeof (Object);
            MethodInfo MonitorExitMethod = typeof (Monitor).GetMethod("Exit", aParamTypes, null);
            Contract.Assert(MonitorExitMethod != null, "Unable to find the method Monitor.Exit()");
            MethodInfo CPUnadviseMethod = typeof (IConnectionPoint).GetMethod("Unadvise");
            Contract.Assert(CPUnadviseMethod != null, "Unable to find the method ConnectionPoint.Unadvise()");
            MethodInfo ReleaseComObjectMethod = typeof (Marshal).GetMethod("ReleaseComObject");
            Contract.Assert(ReleaseComObjectMethod != null, "Unable to find the method Marshal.ReleaseComObject()");
            Type[] parameterTypes;
            parameterTypes = new Type[1];
            parameterTypes[0] = DelegateField.FieldType;
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod("remove_" + SrcItfMethod.Name, MethodAttributes.Public | MethodAttributes.Virtual, null, parameterTypes);
            ILGenerator il = Meth.GetILGenerator();
            LocalBuilder ltNumSinkHelpers = il.DeclareLocal(typeof (Int32));
            LocalBuilder ltSinkHelperCounter = il.DeclareLocal(typeof (Int32));
            LocalBuilder ltCurrSinkHelper = il.DeclareLocal(SinkHelperClass);
            LocalBuilder ltLockTaken = il.DeclareLocal(typeof (bool));
            Label ForBeginLabel = il.DefineLabel();
            Label ForEndLabel = il.DefineLabel();
            Label FalseIfLabel = il.DefineLabel();
            Label MonitorExitLabel = il.DefineLabel();
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldloca_S, ltLockTaken);
            il.Emit(OpCodes.Call, MonitorEnterMethod);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbSinkHelperArray);
            il.Emit(OpCodes.Brfalse, ForEndLabel);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbSinkHelperArray);
            il.Emit(OpCodes.Callvirt, ArrayListSizeGetMethod);
            il.Emit(OpCodes.Stloc, ltNumSinkHelpers);
            il.Emit(OpCodes.Ldc_I4, 0);
            il.Emit(OpCodes.Stloc, ltSinkHelperCounter);
            il.Emit(OpCodes.Ldc_I4, 0);
            il.Emit(OpCodes.Ldloc, ltNumSinkHelpers);
            il.Emit(OpCodes.Bge, ForEndLabel);
            il.MarkLabel(ForBeginLabel);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbSinkHelperArray);
            il.Emit(OpCodes.Ldloc, ltSinkHelperCounter);
            il.Emit(OpCodes.Callvirt, ArrayListItemGetMethod);
            il.Emit(OpCodes.Castclass, SinkHelperClass);
            il.Emit(OpCodes.Stloc, ltCurrSinkHelper);
            il.Emit(OpCodes.Ldloc, ltCurrSinkHelper);
            il.Emit(OpCodes.Ldfld, DelegateField);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Beq, FalseIfLabel);
            il.Emit(OpCodes.Ldloc, ltCurrSinkHelper);
            il.Emit(OpCodes.Ldfld, DelegateField);
            il.Emit(OpCodes.Ldarg, (short)1);
            il.Emit(OpCodes.Castclass, typeof (Object));
            il.Emit(OpCodes.Callvirt, DelegateEqualsMethod);
            il.Emit(OpCodes.Ldc_I4, 0xff);
            il.Emit(OpCodes.And);
            il.Emit(OpCodes.Ldc_I4, 0);
            il.Emit(OpCodes.Beq, FalseIfLabel);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbSinkHelperArray);
            il.Emit(OpCodes.Ldloc, ltSinkHelperCounter);
            il.Emit(OpCodes.Callvirt, ArrayListRemoveMethod);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbEventCP);
            il.Emit(OpCodes.Ldloc, ltCurrSinkHelper);
            il.Emit(OpCodes.Ldfld, CookieField);
            il.Emit(OpCodes.Callvirt, CPUnadviseMethod);
            il.Emit(OpCodes.Ldloc, ltNumSinkHelpers);
            il.Emit(OpCodes.Ldc_I4, 1);
            il.Emit(OpCodes.Bgt, ForEndLabel);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbEventCP);
            il.Emit(OpCodes.Call, ReleaseComObjectMethod);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stfld, fbEventCP);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stfld, fbSinkHelperArray);
            il.Emit(OpCodes.Br, ForEndLabel);
            il.MarkLabel(FalseIfLabel);
            il.Emit(OpCodes.Ldloc, ltSinkHelperCounter);
            il.Emit(OpCodes.Ldc_I4, 1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, ltSinkHelperCounter);
            il.Emit(OpCodes.Ldloc, ltSinkHelperCounter);
            il.Emit(OpCodes.Ldloc, ltNumSinkHelpers);
            il.Emit(OpCodes.Blt, ForBeginLabel);
            il.MarkLabel(ForEndLabel);
            il.BeginFinallyBlock();
            Label skipExit = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, ltLockTaken);
            il.Emit(OpCodes.Brfalse_S, skipExit);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Call, MonitorExitMethod);
            il.MarkLabel(skipExit);
            il.EndExceptionBlock();
            il.Emit(OpCodes.Ret);
            return Meth;
        }

        private MethodBuilder DefineInitSrcItfMethod(TypeBuilder OutputTypeBuilder, Type SourceInterface, FieldBuilder fbSinkHelperArray, FieldBuilder fbEventCP, FieldBuilder fbCPC)
        {
            ConstructorInfo DefaultArrayListCons = typeof (ArrayList).GetConstructor(EventProviderWriter.DefaultLookup, null, Array.Empty<Type>(), null);
            Contract.Assert(DefaultArrayListCons != null, "Unable to find the constructor for class ArrayList");
            ubyte[] rgByteGuid = new ubyte[16];
            Type[] aParamTypes = new Type[1];
            aParamTypes[0] = typeof (Byte[]);
            ConstructorInfo ByteArrayGUIDCons = typeof (Guid).GetConstructor(EventProviderWriter.DefaultLookup, null, aParamTypes, null);
            Contract.Assert(ByteArrayGUIDCons != null, "Unable to find the constructor for GUID that accepts a string as argument");
            MethodInfo CPCFindCPMethod = typeof (IConnectionPointContainer).GetMethod("FindConnectionPoint");
            Contract.Assert(CPCFindCPMethod != null, "Unable to find the method ConnectionPointContainer.FindConnectionPoint()");
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod("Init", MethodAttributes.Private, null, null);
            ILGenerator il = Meth.GetILGenerator();
            LocalBuilder ltCP = il.DeclareLocal(typeof (IConnectionPoint));
            LocalBuilder ltEvGuid = il.DeclareLocal(typeof (Guid));
            LocalBuilder ltByteArrayGuid = il.DeclareLocal(typeof (Byte[]));
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Stloc, ltCP);
            rgByteGuid = SourceInterface.GUID.ToByteArray();
            il.Emit(OpCodes.Ldc_I4, 0x10);
            il.Emit(OpCodes.Newarr, typeof (Byte));
            il.Emit(OpCodes.Stloc, ltByteArrayGuid);
            for (int i = 0; i < 16; i++)
            {
                il.Emit(OpCodes.Ldloc, ltByteArrayGuid);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldc_I4, (int)(rgByteGuid[i]));
                il.Emit(OpCodes.Stelem_I1);
            }

            il.Emit(OpCodes.Ldloca, ltEvGuid);
            il.Emit(OpCodes.Ldloc, ltByteArrayGuid);
            il.Emit(OpCodes.Call, ByteArrayGUIDCons);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbCPC);
            il.Emit(OpCodes.Ldloca, ltEvGuid);
            il.Emit(OpCodes.Ldloca, ltCP);
            il.Emit(OpCodes.Callvirt, CPCFindCPMethod);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldloc, ltCP);
            il.Emit(OpCodes.Castclass, typeof (IConnectionPoint));
            il.Emit(OpCodes.Stfld, fbEventCP);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Newobj, DefaultArrayListCons);
            il.Emit(OpCodes.Stfld, fbSinkHelperArray);
            il.Emit(OpCodes.Ret);
            return Meth;
        }

        private void DefineConstructor(TypeBuilder OutputTypeBuilder, FieldBuilder fbCPC)
        {
            ConstructorInfo DefaultBaseClsCons = typeof (Object).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, Array.Empty<Type>(), null);
            Contract.Assert(DefaultBaseClsCons != null, "Unable to find the object's public default constructor");
            MethodAttributes ctorAttributes = MethodAttributes.SpecialName | (DefaultBaseClsCons.Attributes & MethodAttributes.MemberAccessMask);
            MethodBuilder Cons = OutputTypeBuilder.DefineMethod(".ctor", ctorAttributes, null, new Type[]{typeof (Object)});
            ILGenerator il = Cons.GetILGenerator();
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Call, DefaultBaseClsCons);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldarg, (short)1);
            il.Emit(OpCodes.Castclass, typeof (IConnectionPointContainer));
            il.Emit(OpCodes.Stfld, fbCPC);
            il.Emit(OpCodes.Ret);
        }

        private MethodBuilder DefineFinalizeMethod(TypeBuilder OutputTypeBuilder, Type SinkHelperClass, FieldBuilder fbSinkHelper, FieldBuilder fbEventCP)
        {
            FieldInfo CookieField = SinkHelperClass.GetField("m_dwCookie");
            Contract.Assert(CookieField != null, "Unable to find the field m_dwCookie on the sink helper");
            PropertyInfo ArrayListItemProperty = typeof (ArrayList).GetProperty("Item");
            Contract.Assert(ArrayListItemProperty != null, "Unable to find the property ArrayList.Item");
            MethodInfo ArrayListItemGetMethod = ArrayListItemProperty.GetGetMethod();
            Contract.Assert(ArrayListItemGetMethod != null, "Unable to find the get method for property ArrayList.Item");
            PropertyInfo ArrayListSizeProperty = typeof (ArrayList).GetProperty("Count");
            Contract.Assert(ArrayListSizeProperty != null, "Unable to find the property ArrayList.Count");
            MethodInfo ArrayListSizeGetMethod = ArrayListSizeProperty.GetGetMethod();
            Contract.Assert(ArrayListSizeGetMethod != null, "Unable to find the get method for property ArrayList.Count");
            MethodInfo CPUnadviseMethod = typeof (IConnectionPoint).GetMethod("Unadvise");
            Contract.Assert(CPUnadviseMethod != null, "Unable to find the method ConnectionPoint.Unadvise()");
            MethodInfo ReleaseComObjectMethod = typeof (Marshal).GetMethod("ReleaseComObject");
            Contract.Assert(ReleaseComObjectMethod != null, "Unable to find the method Marshal.ReleaseComObject()");
            MethodInfo MonitorEnterMethod = typeof (Monitor).GetMethod("Enter", MonitorEnterParamTypes, null);
            Contract.Assert(MonitorEnterMethod != null, "Unable to find the method Monitor.Enter()");
            Type[] aParamTypes = new Type[1];
            aParamTypes[0] = typeof (Object);
            MethodInfo MonitorExitMethod = typeof (Monitor).GetMethod("Exit", aParamTypes, null);
            Contract.Assert(MonitorExitMethod != null, "Unable to find the method Monitor.Exit()");
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod("Finalize", MethodAttributes.Public | MethodAttributes.Virtual, null, null);
            ILGenerator il = Meth.GetILGenerator();
            LocalBuilder ltNumSinkHelpers = il.DeclareLocal(typeof (Int32));
            LocalBuilder ltSinkHelperCounter = il.DeclareLocal(typeof (Int32));
            LocalBuilder ltCurrSinkHelper = il.DeclareLocal(SinkHelperClass);
            LocalBuilder ltLockTaken = il.DeclareLocal(typeof (bool));
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldloca_S, ltLockTaken);
            il.Emit(OpCodes.Call, MonitorEnterMethod);
            Label ForBeginLabel = il.DefineLabel();
            Label ReleaseComObjectLabel = il.DefineLabel();
            Label AfterReleaseComObjectLabel = il.DefineLabel();
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbEventCP);
            il.Emit(OpCodes.Brfalse, AfterReleaseComObjectLabel);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbSinkHelper);
            il.Emit(OpCodes.Callvirt, ArrayListSizeGetMethod);
            il.Emit(OpCodes.Stloc, ltNumSinkHelpers);
            il.Emit(OpCodes.Ldc_I4, 0);
            il.Emit(OpCodes.Stloc, ltSinkHelperCounter);
            il.Emit(OpCodes.Ldc_I4, 0);
            il.Emit(OpCodes.Ldloc, ltNumSinkHelpers);
            il.Emit(OpCodes.Bge, ReleaseComObjectLabel);
            il.MarkLabel(ForBeginLabel);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbSinkHelper);
            il.Emit(OpCodes.Ldloc, ltSinkHelperCounter);
            il.Emit(OpCodes.Callvirt, ArrayListItemGetMethod);
            il.Emit(OpCodes.Castclass, SinkHelperClass);
            il.Emit(OpCodes.Stloc, ltCurrSinkHelper);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbEventCP);
            il.Emit(OpCodes.Ldloc, ltCurrSinkHelper);
            il.Emit(OpCodes.Ldfld, CookieField);
            il.Emit(OpCodes.Callvirt, CPUnadviseMethod);
            il.Emit(OpCodes.Ldloc, ltSinkHelperCounter);
            il.Emit(OpCodes.Ldc_I4, 1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, ltSinkHelperCounter);
            il.Emit(OpCodes.Ldloc, ltSinkHelperCounter);
            il.Emit(OpCodes.Ldloc, ltNumSinkHelpers);
            il.Emit(OpCodes.Blt, ForBeginLabel);
            il.MarkLabel(ReleaseComObjectLabel);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Ldfld, fbEventCP);
            il.Emit(OpCodes.Call, ReleaseComObjectMethod);
            il.Emit(OpCodes.Pop);
            il.MarkLabel(AfterReleaseComObjectLabel);
            il.BeginCatchBlock(typeof (System.Exception));
            il.Emit(OpCodes.Pop);
            il.BeginFinallyBlock();
            Label skipExit = il.DefineLabel();
            il.Emit(OpCodes.Ldloc, ltLockTaken);
            il.Emit(OpCodes.Brfalse_S, skipExit);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Call, MonitorExitMethod);
            il.MarkLabel(skipExit);
            il.EndExceptionBlock();
            il.Emit(OpCodes.Ret);
            return Meth;
        }

        private void DefineDisposeMethod(TypeBuilder OutputTypeBuilder, MethodBuilder FinalizeMethod)
        {
            MethodInfo SuppressFinalizeMethod = typeof (GC).GetMethod("SuppressFinalize");
            Contract.Assert(SuppressFinalizeMethod != null, "Unable to find the GC.SuppressFinalize");
            MethodBuilder Meth = OutputTypeBuilder.DefineMethod("Dispose", MethodAttributes.Public | MethodAttributes.Virtual, null, null);
            ILGenerator il = Meth.GetILGenerator();
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Callvirt, FinalizeMethod);
            il.Emit(OpCodes.Ldarg, (short)0);
            il.Emit(OpCodes.Call, SuppressFinalizeMethod);
            il.Emit(OpCodes.Ret);
        }

        private ModuleBuilder m_OutputModule;
        private String m_strDestTypeName;
        private Type m_EventItfType;
        private Type m_SrcItfType;
        private Type m_SinkHelperType;
    }
}