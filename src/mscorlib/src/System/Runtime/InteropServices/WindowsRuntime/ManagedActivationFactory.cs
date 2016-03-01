using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal interface IManagedActivationFactory
    {
        void RunClassConstructor();
    }

    internal sealed class ManagedActivationFactory : IActivationFactory, IManagedActivationFactory
    {
        private Type m_type;
        internal ManagedActivationFactory(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            if (!(type is RuntimeType) || !type.IsExportedToWindowsRuntime)
                throw new ArgumentException(Environment.GetResourceString("Argument_TypeNotActivatableViaWindowsRuntime", type), "type");
            m_type = type;
        }

        public object ActivateInstance()
        {
            try
            {
                return Activator.CreateInstance(m_type);
            }
            catch (MissingMethodException)
            {
                throw new NotImplementedException();
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        void IManagedActivationFactory.RunClassConstructor()
        {
            RuntimeHelpers.RunClassConstructor(m_type.TypeHandle);
        }
    }
}