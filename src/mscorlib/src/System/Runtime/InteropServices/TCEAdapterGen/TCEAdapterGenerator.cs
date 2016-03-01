namespace System.Runtime.InteropServices.TCEAdapterGen
{
    using System.Runtime.InteropServices;
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Collections;
    using System.Threading;

    internal class TCEAdapterGenerator
    {
        public void Process(ModuleBuilder ModBldr, ArrayList EventItfList)
        {
            m_Module = ModBldr;
            int NumEvItfs = EventItfList.Count;
            for (int cEventItfs = 0; cEventItfs < NumEvItfs; cEventItfs++)
            {
                EventItfInfo CurrEventItf = (EventItfInfo)EventItfList[cEventItfs];
                Type EventItfType = CurrEventItf.GetEventItfType();
                Type SrcItfType = CurrEventItf.GetSrcItfType();
                String EventProviderName = CurrEventItf.GetEventProviderName();
                Type SinkHelperType = new EventSinkHelperWriter(m_Module, SrcItfType, EventItfType).Perform();
                new EventProviderWriter(m_Module, EventProviderName, EventItfType, SrcItfType, SinkHelperType).Perform();
            }
        }

        internal static void SetClassInterfaceTypeToNone(TypeBuilder tb)
        {
            if (s_NoClassItfCABuilder == null)
            {
                Type[] aConsParams = new Type[1];
                aConsParams[0] = typeof (ClassInterfaceType);
                ConstructorInfo Cons = typeof (ClassInterfaceAttribute).GetConstructor(aConsParams);
                Object[] aArgs = new Object[1];
                aArgs[0] = ClassInterfaceType.None;
                s_NoClassItfCABuilder = new CustomAttributeBuilder(Cons, aArgs);
            }

            tb.SetCustomAttribute(s_NoClassItfCABuilder);
        }

        internal static TypeBuilder DefineUniqueType(String strInitFullName, TypeAttributes attrs, Type BaseType, Type[] aInterfaceTypes, ModuleBuilder mb)
        {
            String strFullName = strInitFullName;
            int PostFix = 2;
            for (; mb.GetType(strFullName) != null; strFullName = strInitFullName + "_" + PostFix, PostFix++)
                ;
            return mb.DefineType(strFullName, attrs, BaseType, aInterfaceTypes);
        }

        internal static void SetHiddenAttribute(TypeBuilder tb)
        {
            if (s_HiddenCABuilder == null)
            {
                Type[] aConsParams = new Type[1];
                aConsParams[0] = typeof (TypeLibTypeFlags);
                ConstructorInfo Cons = typeof (TypeLibTypeAttribute).GetConstructor(aConsParams);
                Object[] aArgs = new Object[1];
                aArgs[0] = TypeLibTypeFlags.FHidden;
                s_HiddenCABuilder = new CustomAttributeBuilder(Cons, aArgs);
            }

            tb.SetCustomAttribute(s_HiddenCABuilder);
        }

        internal static MethodInfo[] GetNonPropertyMethods(Type type)
        {
            MethodInfo[] aMethods = type.GetMethods();
            ArrayList methods = new ArrayList(aMethods);
            PropertyInfo[] props = type.GetProperties();
            foreach (PropertyInfo prop in props)
            {
                MethodInfo[] accessors = prop.GetAccessors();
                foreach (MethodInfo accessor in accessors)
                {
                    for (int i = 0; i < methods.Count; i++)
                    {
                        if ((MethodInfo)methods[i] == accessor)
                            methods.RemoveAt(i);
                    }
                }
            }

            MethodInfo[] retMethods = new MethodInfo[methods.Count];
            methods.CopyTo(retMethods);
            return retMethods;
        }

        internal static MethodInfo[] GetPropertyMethods(Type type)
        {
            MethodInfo[] aMethods = type.GetMethods();
            ArrayList methods = new ArrayList();
            PropertyInfo[] props = type.GetProperties();
            foreach (PropertyInfo prop in props)
            {
                MethodInfo[] accessors = prop.GetAccessors();
                foreach (MethodInfo accessor in accessors)
                {
                    methods.Add(accessor);
                }
            }

            MethodInfo[] retMethods = new MethodInfo[methods.Count];
            methods.CopyTo(retMethods);
            return retMethods;
        }

        private ModuleBuilder m_Module = null;
        private Hashtable m_SrcItfToSrcItfInfoMap = new Hashtable();
        private static volatile CustomAttributeBuilder s_NoClassItfCABuilder = null;
        private static volatile CustomAttributeBuilder s_HiddenCABuilder = null;
    }
}