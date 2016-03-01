using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

namespace System.Runtime.Serialization
{
    public static class FormatterServices
    {
        public static Object GetUninitializedObject(Type type)
        {
            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }

            Contract.EndContractBlock();
            if (!(type is RuntimeType))
            {
                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidType", type.ToString()));
            }

            return nativeGetUninitializedObject((RuntimeType)type);
        }

        public static Object GetSafeUninitializedObject(Type type)
        {
            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }

            Contract.EndContractBlock();
            if (!(type is RuntimeType))
            {
                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidType", type.ToString()));
            }

            try
            {
                return nativeGetSafeUninitializedObject((RuntimeType)type);
            }
            catch (SecurityException e)
            {
                throw new SerializationException(Environment.GetResourceString("Serialization_Security", type.FullName), e);
            }
        }

        private static extern Object nativeGetSafeUninitializedObject(RuntimeType type);
        private static extern Object nativeGetUninitializedObject(RuntimeType type);
        private static Binder s_binder = Type.DefaultBinder;
        internal static void SerializationSetValue(MemberInfo fi, Object target, Object value)
        {
            Contract.Requires(fi != null);
            RtFieldInfo rtField = fi as RtFieldInfo;
            if (rtField != null)
            {
                rtField.CheckConsistency(target);
                rtField.UnsafeSetValue(target, value, BindingFlags.Default, s_binder, null);
                return;
            }

            SerializationFieldInfo serField = fi as SerializationFieldInfo;
            if (serField != null)
            {
                serField.InternalSetValue(target, value, BindingFlags.Default, s_binder, null);
                return;
            }

            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFieldInfo"));
        }

        public static Object PopulateObjectMembers(Object obj, MemberInfo[] members, Object[] data)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            if (members == null)
            {
                throw new ArgumentNullException("members");
            }

            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (members.Length != data.Length)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_DataLengthDifferent"));
            }

            Contract.EndContractBlock();
            MemberInfo mi;
            BCLDebug.Trace("SER", "[PopulateObjectMembers]Enter.");
            for (int i = 0; i < members.Length; i++)
            {
                mi = members[i];
                if (mi == null)
                {
                    throw new ArgumentNullException("members", Environment.GetResourceString("ArgumentNull_NullMember", i));
                }

                if (data[i] != null)
                {
                    if (mi.MemberType == MemberTypes.Field)
                    {
                        SerializationSetValue(mi, obj, data[i]);
                    }
                    else
                    {
                        throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMemberInfo"));
                    }

                    BCLDebug.Trace("SER", "[PopulateObjectMembers]\tType:", obj.GetType(), "\tMember:", members[i].Name, " with member type: ", ((FieldInfo)members[i]).FieldType);
                }
            }

            BCLDebug.Trace("SER", "[PopulateObjectMembers]Leave.");
            return obj;
        }

        public static Object[] GetObjectData(Object obj, MemberInfo[] members)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            if (members == null)
            {
                throw new ArgumentNullException("members");
            }

            Contract.EndContractBlock();
            int numberOfMembers = members.Length;
            Object[] data = new Object[numberOfMembers];
            MemberInfo mi;
            for (int i = 0; i < numberOfMembers; i++)
            {
                mi = members[i];
                if (mi == null)
                {
                    throw new ArgumentNullException("members", Environment.GetResourceString("ArgumentNull_NullMember", i));
                }

                if (mi.MemberType == MemberTypes.Field)
                {
                    Contract.Assert(mi is RuntimeFieldInfo || mi is SerializationFieldInfo, "[FormatterServices.GetObjectData]mi is RuntimeFieldInfo || mi is SerializationFieldInfo.");
                    RtFieldInfo rfi = mi as RtFieldInfo;
                    if (rfi != null)
                    {
                        rfi.CheckConsistency(obj);
                        data[i] = rfi.UnsafeGetValue(obj);
                    }
                    else
                    {
                        data[i] = ((SerializationFieldInfo)mi).InternalGetValue(obj);
                    }
                }
                else
                {
                    throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMemberInfo"));
                }
            }

            return data;
        }

        public static ISerializationSurrogate GetSurrogateForCyclicalReference(ISerializationSurrogate innerSurrogate)
        {
            if (innerSurrogate == null)
                throw new ArgumentNullException("innerSurrogate");
            Contract.EndContractBlock();
            return new SurrogateForCyclicalReference(innerSurrogate);
        }

        public static Type GetTypeFromAssembly(Assembly assem, String name)
        {
            if (assem == null)
                throw new ArgumentNullException("assem");
            Contract.EndContractBlock();
            return assem.GetType(name, false, false);
        }

        internal static Assembly LoadAssemblyFromString(String assemblyName)
        {
            BCLDebug.Trace("SER", "[LoadAssemblyFromString]Looking for assembly: ", assemblyName);
            Assembly found = Assembly.Load(assemblyName);
            return found;
        }

        internal static Assembly LoadAssemblyFromStringNoThrow(String assemblyName)
        {
            try
            {
                return LoadAssemblyFromString(assemblyName);
            }
            catch (Exception e)
            {
                BCLDebug.Trace("SER", "[LoadAssemblyFromString]", e.ToString());
            }

            return null;
        }

        internal static string GetClrAssemblyName(Type type, out bool hasTypeForwardedFrom)
        {
            if ((object)type == null)
            {
                throw new ArgumentNullException("type");
            }

            object[] typeAttributes = type.GetCustomAttributes(typeof (TypeForwardedFromAttribute), false);
            if (typeAttributes != null && typeAttributes.Length > 0)
            {
                hasTypeForwardedFrom = true;
                TypeForwardedFromAttribute typeForwardedFromAttribute = (TypeForwardedFromAttribute)typeAttributes[0];
                return typeForwardedFromAttribute.AssemblyFullName;
            }
            else
            {
                hasTypeForwardedFrom = false;
                return type.Assembly.FullName;
            }
        }

        internal static string GetClrTypeFullName(Type type)
        {
            if (type.IsArray)
            {
                return GetClrTypeFullNameForArray(type);
            }
            else
            {
                return GetClrTypeFullNameForNonArrayTypes(type);
            }
        }

        static string GetClrTypeFullNameForArray(Type type)
        {
            int rank = type.GetArrayRank();
            if (rank == 1)
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}{1}", GetClrTypeFullName(type.GetElementType()), "[]");
            }
            else
            {
                StringBuilder builder = new StringBuilder(GetClrTypeFullName(type.GetElementType())).Append("[");
                for (int commaIndex = 1; commaIndex < rank; commaIndex++)
                {
                    builder.Append(",");
                }

                builder.Append("]");
                return builder.ToString();
            }
        }

        static string GetClrTypeFullNameForNonArrayTypes(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName;
            }

            Type[] genericArguments = type.GetGenericArguments();
            StringBuilder builder = new StringBuilder(type.GetGenericTypeDefinition().FullName).Append("[");
            bool hasTypeForwardedFrom;
            foreach (Type genericArgument in genericArguments)
            {
                builder.Append("[").Append(GetClrTypeFullName(genericArgument)).Append(", ");
                builder.Append(GetClrAssemblyName(genericArgument, out hasTypeForwardedFrom)).Append("],");
            }

            return builder.Remove(builder.Length - 1, 1).Append("]").ToString();
        }
    }

    internal sealed class SurrogateForCyclicalReference : ISerializationSurrogate
    {
        ISerializationSurrogate innerSurrogate;
        internal SurrogateForCyclicalReference(ISerializationSurrogate innerSurrogate)
        {
            if (innerSurrogate == null)
                throw new ArgumentNullException("innerSurrogate");
            this.innerSurrogate = innerSurrogate;
        }

        public void GetObjectData(Object obj, SerializationInfo info, StreamingContext context)
        {
            innerSurrogate.GetObjectData(obj, info, context);
        }

        public Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            return innerSurrogate.SetObjectData(obj, info, context, selector);
        }
    }
}