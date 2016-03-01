using System;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace System.Reflection
{
    internal class MemberInfoSerializationHolder : ISerializable, IObjectReference
    {
        public static void GetSerializationInfo(SerializationInfo info, String name, RuntimeType reflectedClass, String signature, MemberTypes type)
        {
            GetSerializationInfo(info, name, reflectedClass, signature, null, type, null);
        }

        public static void GetSerializationInfo(SerializationInfo info, String name, RuntimeType reflectedClass, String signature, String signature2, MemberTypes type, Type[] genericArguments)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            String assemblyName = reflectedClass.Module.Assembly.FullName;
            String typeName = reflectedClass.FullName;
            info.SetType(typeof (MemberInfoSerializationHolder));
            info.AddValue("Name", name, typeof (String));
            info.AddValue("AssemblyName", assemblyName, typeof (String));
            info.AddValue("ClassName", typeName, typeof (String));
            info.AddValue("Signature", signature, typeof (String));
            info.AddValue("Signature2", signature2, typeof (String));
            info.AddValue("MemberType", (int)type);
            info.AddValue("GenericArguments", genericArguments, typeof (Type[]));
        }

        private String m_memberName;
        private RuntimeType m_reflectedType;
        private String m_signature;
        private String m_signature2;
        private MemberTypes m_memberType;
        private SerializationInfo m_info;
        internal MemberInfoSerializationHolder(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            String assemblyName = info.GetString("AssemblyName");
            String typeName = info.GetString("ClassName");
            if (assemblyName == null || typeName == null)
                throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientState"));
            Assembly assem = FormatterServices.LoadAssemblyFromString(assemblyName);
            m_reflectedType = assem.GetType(typeName, true, false) as RuntimeType;
            m_memberName = info.GetString("Name");
            m_signature = info.GetString("Signature");
            m_signature2 = (string)info.GetValueNoThrow("Signature2", typeof (string));
            m_memberType = (MemberTypes)info.GetInt32("MemberType");
            m_info = info;
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException(Environment.GetResourceString(ResId.NotSupported_Method));
        }

        public virtual Object GetRealObject(StreamingContext context)
        {
            if (m_memberName == null || m_reflectedType == null || m_memberType == 0)
                throw new SerializationException(Environment.GetResourceString(ResId.Serialization_InsufficientState));
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.OptionalParamBinding;
            switch (m_memberType)
            {
                case MemberTypes.Field:
                {
                    FieldInfo[] fields = m_reflectedType.GetMember(m_memberName, MemberTypes.Field, bindingFlags) as FieldInfo[];
                    if (fields.Length == 0)
                        throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMember", m_memberName));
                    return fields[0];
                }

                case MemberTypes.Event:
                {
                    EventInfo[] events = m_reflectedType.GetMember(m_memberName, MemberTypes.Event, bindingFlags) as EventInfo[];
                    if (events.Length == 0)
                        throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMember", m_memberName));
                    return events[0];
                }

                case MemberTypes.Property:
                {
                    PropertyInfo[] properties = m_reflectedType.GetMember(m_memberName, MemberTypes.Property, bindingFlags) as PropertyInfo[];
                    if (properties.Length == 0)
                        throw new SerializationException(Environment.GetResourceString("Serialization_UnknownMember", m_memberName));
                    if (properties.Length == 1)
                        return properties[0];
                    if (properties.Length > 1)
                    {
                        for (int i = 0; i < properties.Length; i++)
                        {
                            if (m_signature2 != null)
                            {
                                if (((RuntimePropertyInfo)properties[i]).SerializationToString().Equals(m_signature2))
                                    return properties[i];
                            }
                            else
                            {
                                if ((properties[i]).ToString().Equals(m_signature))
                                    return properties[i];
                            }
                        }
                    }

                    throw new SerializationException(Environment.GetResourceString(ResId.Serialization_UnknownMember, m_memberName));
                }

                case MemberTypes.Constructor:
                {
                    if (m_signature == null)
                        throw new SerializationException(Environment.GetResourceString(ResId.Serialization_NullSignature));
                    ConstructorInfo[] constructors = m_reflectedType.GetMember(m_memberName, MemberTypes.Constructor, bindingFlags) as ConstructorInfo[];
                    if (constructors.Length == 1)
                        return constructors[0];
                    if (constructors.Length > 1)
                    {
                        for (int i = 0; i < constructors.Length; i++)
                        {
                            if (m_signature2 != null)
                            {
                                if (((RuntimeConstructorInfo)constructors[i]).SerializationToString().Equals(m_signature2))
                                    return constructors[i];
                            }
                            else
                            {
                                if (constructors[i].ToString().Equals(m_signature))
                                    return constructors[i];
                            }
                        }
                    }

                    throw new SerializationException(Environment.GetResourceString(ResId.Serialization_UnknownMember, m_memberName));
                }

                case MemberTypes.Method:
                {
                    MethodInfo methodInfo = null;
                    if (m_signature == null)
                        throw new SerializationException(Environment.GetResourceString(ResId.Serialization_NullSignature));
                    Type[] genericArguments = m_info.GetValueNoThrow("GenericArguments", typeof (Type[])) as Type[];
                    MethodInfo[] methods = m_reflectedType.GetMember(m_memberName, MemberTypes.Method, bindingFlags) as MethodInfo[];
                    if (methods.Length == 1)
                        methodInfo = methods[0];
                    else if (methods.Length > 1)
                    {
                        for (int i = 0; i < methods.Length; i++)
                        {
                            if (m_signature2 != null)
                            {
                                if (((RuntimeMethodInfo)methods[i]).SerializationToString().Equals(m_signature2))
                                {
                                    methodInfo = methods[i];
                                    break;
                                }
                            }
                            else
                            {
                                if (methods[i].ToString().Equals(m_signature))
                                {
                                    methodInfo = methods[i];
                                    break;
                                }
                            }

                            if (genericArguments != null && methods[i].IsGenericMethod)
                            {
                                if (methods[i].GetGenericArguments().Length == genericArguments.Length)
                                {
                                    MethodInfo candidateMethod = methods[i].MakeGenericMethod(genericArguments);
                                    if (m_signature2 != null)
                                    {
                                        if (((RuntimeMethodInfo)candidateMethod).SerializationToString().Equals(m_signature2))
                                        {
                                            methodInfo = candidateMethod;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (candidateMethod.ToString().Equals(m_signature))
                                        {
                                            methodInfo = candidateMethod;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (methodInfo == null)
                        throw new SerializationException(Environment.GetResourceString(ResId.Serialization_UnknownMember, m_memberName));
                    if (!methodInfo.IsGenericMethodDefinition)
                        return methodInfo;
                    if (genericArguments == null)
                        return methodInfo;
                    if (genericArguments[0] == null)
                        return null;
                    return methodInfo.MakeGenericMethod(genericArguments);
                }

                default:
                    throw new ArgumentException(Environment.GetResourceString("Serialization_MemberTypeNotRecognized"));
            }
        }
    }
}