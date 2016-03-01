using System;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace System
{
    internal sealed class DelegateSerializationHolder : IObjectReference, ISerializable
    {
        internal static DelegateEntry GetDelegateSerializationInfo(SerializationInfo info, Type delegateType, Object target, MethodInfo method, int targetIndex)
        {
            if (method == null)
                throw new ArgumentNullException("method");
            Contract.EndContractBlock();
            if (!method.IsPublic || (method.DeclaringType != null && !method.DeclaringType.IsVisible))
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
            Type c = delegateType.BaseType;
            if (c == null || (c != typeof (Delegate) && c != typeof (MulticastDelegate)))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDelegate"), "type");
            if (method.DeclaringType == null)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_GlobalMethodSerialization"));
            DelegateEntry de = new DelegateEntry(delegateType.FullName, delegateType.Module.Assembly.FullName, target, method.ReflectedType.Module.Assembly.FullName, method.ReflectedType.FullName, method.Name);
            if (info.MemberCount == 0)
            {
                info.SetType(typeof (DelegateSerializationHolder));
                info.AddValue("Delegate", de, typeof (DelegateEntry));
            }

            if (target != null)
            {
                String targetName = "target" + targetIndex;
                info.AddValue(targetName, de.target);
                de.target = targetName;
            }

            String methodInfoName = "method" + targetIndex;
            info.AddValue(methodInfoName, method);
            return de;
        }

        internal class DelegateEntry
        {
            internal String type;
            internal String assembly;
            internal Object target;
            internal String targetTypeAssembly;
            internal String targetTypeName;
            internal String methodName;
            internal DelegateEntry delegateEntry;
            internal DelegateEntry(String type, String assembly, Object target, String targetTypeAssembly, String targetTypeName, String methodName)
            {
                this.type = type;
                this.assembly = assembly;
                this.target = target;
                this.targetTypeAssembly = targetTypeAssembly;
                this.targetTypeName = targetTypeName;
                this.methodName = methodName;
            }

            internal DelegateEntry Entry
            {
                get
                {
                    return delegateEntry;
                }

                set
                {
                    delegateEntry = value;
                }
            }
        }

        private DelegateEntry m_delegateEntry;
        private MethodInfo[] m_methods;
        private DelegateSerializationHolder(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            bool bNewWire = true;
            try
            {
                m_delegateEntry = (DelegateEntry)info.GetValue("Delegate", typeof (DelegateEntry));
            }
            catch
            {
                m_delegateEntry = OldDelegateWireFormat(info, context);
                bNewWire = false;
            }

            if (bNewWire)
            {
                DelegateEntry deiter = m_delegateEntry;
                int count = 0;
                while (deiter != null)
                {
                    if (deiter.target != null)
                    {
                        string stringTarget = deiter.target as string;
                        if (stringTarget != null)
                            deiter.target = info.GetValue(stringTarget, typeof (Object));
                    }

                    count++;
                    deiter = deiter.delegateEntry;
                }

                MethodInfo[] methods = new MethodInfo[count];
                int i;
                for (i = 0; i < count; i++)
                {
                    String methodInfoName = "method" + i;
                    methods[i] = (MethodInfo)info.GetValueNoThrow(methodInfoName, typeof (MethodInfo));
                    if (methods[i] == null)
                        break;
                }

                if (i == count)
                    m_methods = methods;
            }
        }

        private void ThrowInsufficientState(string field)
        {
            throw new SerializationException(Environment.GetResourceString("Serialization_InsufficientDeserializationState", field));
        }

        private DelegateEntry OldDelegateWireFormat(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            String delegateType = info.GetString("DelegateType");
            String delegateAssembly = info.GetString("DelegateAssembly");
            Object target = info.GetValue("Target", typeof (Object));
            String targetTypeAssembly = info.GetString("TargetTypeAssembly");
            String targetTypeName = info.GetString("TargetTypeName");
            String methodName = info.GetString("MethodName");
            return new DelegateEntry(delegateType, delegateAssembly, target, targetTypeAssembly, targetTypeName, methodName);
        }

        private Delegate GetDelegate(DelegateEntry de, int index)
        {
            Delegate d;
            try
            {
                if (de.methodName == null || de.methodName.Length == 0)
                    ThrowInsufficientState("MethodName");
                if (de.assembly == null || de.assembly.Length == 0)
                    ThrowInsufficientState("DelegateAssembly");
                if (de.targetTypeName == null || de.targetTypeName.Length == 0)
                    ThrowInsufficientState("TargetTypeName");
                RuntimeType type = (RuntimeType)Assembly.GetType_Compat(de.assembly, de.type);
                RuntimeType targetType = (RuntimeType)Assembly.GetType_Compat(de.targetTypeAssembly, de.targetTypeName);
                if (m_methods != null)
                {
                    if (!targetType.IsInstanceOfType(de.target))
                        throw new InvalidCastException();
                    Object target = de.target;
                    d = Delegate.CreateDelegateNoSecurityCheck(type, target, m_methods[index]);
                }
                else
                {
                    if (de.target != null)
                    {
                        if (!targetType.IsInstanceOfType(de.target))
                            throw new InvalidCastException();
                        d = Delegate.CreateDelegate(type, de.target, de.methodName);
                    }
                    else
                        d = Delegate.CreateDelegate(type, targetType, de.methodName);
                }

                if ((d.Method != null && !d.Method.IsPublic) || (d.Method.DeclaringType != null && !d.Method.DeclaringType.IsVisible))
                    new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
            }
            catch (Exception e)
            {
                if (e is SerializationException)
                    throw e;
                throw new SerializationException(e.Message, e);
            }

            return d;
        }

        public Object GetRealObject(StreamingContext context)
        {
            int count = 0;
            for (DelegateEntry de = m_delegateEntry; de != null; de = de.Entry)
                count++;
            int maxindex = count - 1;
            if (count == 1)
            {
                return GetDelegate(m_delegateEntry, 0);
            }
            else
            {
                object[] invocationList = new object[count];
                for (DelegateEntry de = m_delegateEntry; de != null; de = de.Entry)
                {
                    --count;
                    invocationList[count] = GetDelegate(de, maxindex - count);
                }

                return ((MulticastDelegate)invocationList[0]).NewMulticastDelegate(invocationList, invocationList.Length);
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DelegateSerHolderSerial"));
        }
    }
}