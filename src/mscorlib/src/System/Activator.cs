using System.Configuration.Assemblies;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Security.Policy;
using System.Threading;

namespace System
{
    public sealed class Activator : _Activator
    {
        internal const int LookupMask = 0x000000FF;
        internal const BindingFlags ConLookup = (BindingFlags)(BindingFlags.Instance | BindingFlags.Public);
        internal const BindingFlags ConstructorDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;
        private Activator()
        {
        }

        static public Object CreateInstance(Type type, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture)
        {
            return CreateInstance(type, bindingAttr, binder, args, culture, null);
        }

        static public Object CreateInstance(Type type, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes)
        {
            if ((object)type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();
            if (type is System.Reflection.Emit.TypeBuilder)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_CreateInstanceWithTypeBuilder"));
            if ((bindingAttr & (BindingFlags)LookupMask) == 0)
                bindingAttr |= Activator.ConstructorDefault;
            if (activationAttributes != null && activationAttributes.Length > 0)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_ActivAttrOnNonMBR"));
            }

            RuntimeType rt = type.UnderlyingSystemType as RuntimeType;
            if (rt == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "type");
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return rt.CreateInstanceImpl(bindingAttr, binder, args, culture, activationAttributes, ref stackMark);
        }

        static public Object CreateInstance(Type type, params Object[] args)
        {
            return CreateInstance(type, Activator.ConstructorDefault, null, args, null, null);
        }

        static public Object CreateInstance(Type type, Object[] args, Object[] activationAttributes)
        {
            return CreateInstance(type, Activator.ConstructorDefault, null, args, null, activationAttributes);
        }

        static public Object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type, false);
        }

        static public ObjectHandle CreateInstance(String assemblyName, String typeName)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstance(assemblyName, typeName, false, Activator.ConstructorDefault, null, null, null, null, null, ref stackMark);
        }

        static public ObjectHandle CreateInstance(String assemblyName, String typeName, Object[] activationAttributes)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstance(assemblyName, typeName, false, Activator.ConstructorDefault, null, null, null, activationAttributes, null, ref stackMark);
        }

        static public Object CreateInstance(Type type, bool nonPublic)
        {
            if ((object)type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();
            RuntimeType rt = type.UnderlyingSystemType as RuntimeType;
            if (rt == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "type");
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return rt.CreateInstanceDefaultCtor(!nonPublic, false, true, ref stackMark);
        }

        static public T CreateInstance<T>()
        {
            RuntimeType rt = typeof (T) as RuntimeType;
            if (rt.HasElementType)
                throw new MissingMethodException(Environment.GetResourceString("Arg_NoDefCTor"));
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            if (CompatibilitySwitches.IsAppEarlierThanSilverlight4 || CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                return (T)rt.CreateInstanceSlow(true, true, false, ref stackMark);
            else
                return (T)rt.CreateInstanceDefaultCtor(true, true, true, ref stackMark);
        }

        static public ObjectHandle CreateInstanceFrom(String assemblyFile, String typeName)
        {
            return CreateInstanceFrom(assemblyFile, typeName, null);
        }

        static public ObjectHandle CreateInstanceFrom(String assemblyFile, String typeName, Object[] activationAttributes)
        {
            return CreateInstanceFrom(assemblyFile, typeName, false, Activator.ConstructorDefault, null, null, null, activationAttributes);
        }

        static public ObjectHandle CreateInstance(String assemblyName, String typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityInfo)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityInfo, ref stackMark);
        }

        public static ObjectHandle CreateInstance(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstance(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, null, ref stackMark);
        }

        static internal ObjectHandle CreateInstance(String assemblyString, String typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityInfo, ref StackCrawlMark stackMark)
        {
            Type type = null;
            Assembly assembly = null;
            if (assemblyString == null)
            {
                assembly = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
            }
            else
            {
                RuntimeAssembly assemblyFromResolveEvent;
                AssemblyName assemblyName = RuntimeAssembly.CreateAssemblyName(assemblyString, false, out assemblyFromResolveEvent);
                if (assemblyFromResolveEvent != null)
                {
                    assembly = assemblyFromResolveEvent;
                }
                else if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime)
                {
                    type = Type.GetType(typeName + ", " + assemblyString, true, ignoreCase);
                }
                else
                {
                    assembly = RuntimeAssembly.InternalLoadAssemblyName(assemblyName, securityInfo, null, ref stackMark, true, false, false);
                }
            }

            if (type == null)
            {
                Log(assembly != null, "CreateInstance:: ", "Loaded " + assembly.FullName, "Failed to Load: " + assemblyString);
                if (assembly == null)
                    return null;
                type = assembly.GetType(typeName, true, ignoreCase);
            }

            Object o = Activator.CreateInstance(type, bindingAttr, binder, args, culture, activationAttributes);
            Log(o != null, "CreateInstance:: ", "Created Instance of class " + typeName, "Failed to create instance of class " + typeName);
            if (o == null)
                return null;
            else
            {
                ObjectHandle Handle = new ObjectHandle(o);
                return Handle;
            }
        }

        static public ObjectHandle CreateInstanceFrom(String assemblyFile, String typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityInfo)
        {
            return CreateInstanceFromInternal(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityInfo);
        }

        public static ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            return CreateInstanceFromInternal(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, null);
        }

        private static ObjectHandle CreateInstanceFromInternal(String assemblyFile, String typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityInfo)
        {
            Assembly assembly = Assembly.LoadFrom(assemblyFile, securityInfo);
            Type t = assembly.GetType(typeName, true, ignoreCase);
            Object o = Activator.CreateInstance(t, bindingAttr, binder, args, culture, activationAttributes);
            Log(o != null, "CreateInstanceFrom:: ", "Created Instance of class " + typeName, "Failed to create instance of class " + typeName);
            if (o == null)
                return null;
            else
            {
                ObjectHandle Handle = new ObjectHandle(o);
                return Handle;
            }
        }

        public static ObjectHandle CreateInstance(AppDomain domain, string assemblyName, string typeName)
        {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();
            return domain.InternalCreateInstanceWithNoSecurity(assemblyName, typeName);
        }

        public static ObjectHandle CreateInstance(AppDomain domain, string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityAttributes)
        {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();
            return domain.InternalCreateInstanceWithNoSecurity(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
        }

        public static ObjectHandle CreateInstance(AppDomain domain, string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();
            return domain.InternalCreateInstanceWithNoSecurity(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, null);
        }

        public static ObjectHandle CreateInstanceFrom(AppDomain domain, string assemblyFile, string typeName)
        {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();
            return domain.InternalCreateInstanceFromWithNoSecurity(assemblyFile, typeName);
        }

        public static ObjectHandle CreateInstanceFrom(AppDomain domain, string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, Object[] args, CultureInfo culture, Object[] activationAttributes, Evidence securityAttributes)
        {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();
            return domain.InternalCreateInstanceFromWithNoSecurity(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
        }

        public static ObjectHandle CreateInstanceFrom(AppDomain domain, string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();
            return domain.InternalCreateInstanceFromWithNoSecurity(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, null);
        }

        public static ObjectHandle CreateComInstanceFrom(String assemblyName, String typeName)
        {
            return CreateComInstanceFrom(assemblyName, typeName, null, AssemblyHashAlgorithm.None);
        }

        public static ObjectHandle CreateComInstanceFrom(String assemblyName, String typeName, byte[] hashValue, AssemblyHashAlgorithm hashAlgorithm)
        {
            Assembly assembly = Assembly.LoadFrom(assemblyName, hashValue, hashAlgorithm);
            Type t = assembly.GetType(typeName, true, false);
            Object[] Attr = t.GetCustomAttributes(typeof (ComVisibleAttribute), false);
            if (Attr.Length > 0)
            {
                if (((ComVisibleAttribute)Attr[0]).Value == false)
                    throw new TypeLoadException(Environment.GetResourceString("Argument_TypeMustBeVisibleFromCom"));
            }

            Log(assembly != null, "CreateInstance:: ", "Loaded " + assembly.FullName, "Failed to Load: " + assemblyName);
            if (assembly == null)
                return null;
            Object o = Activator.CreateInstance(t, Activator.ConstructorDefault, null, null, null, null);
            Log(o != null, "CreateInstance:: ", "Created Instance of class " + typeName, "Failed to create instance of class " + typeName);
            if (o == null)
                return null;
            else
            {
                ObjectHandle Handle = new ObjectHandle(o);
                return Handle;
            }
        }

        private static void Log(bool test, string title, string success, string failure)
        {
        }
    }
}