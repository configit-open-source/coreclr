using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal sealed class CustomPropertyImpl : ICustomProperty
    {
        private PropertyInfo m_property;
        public CustomPropertyImpl(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException("propertyInfo");
            m_property = propertyInfo;
        }

        public string Name
        {
            get
            {
                return m_property.Name;
            }
        }

        public bool CanRead
        {
            get
            {
                return m_property.GetGetMethod() != null;
            }
        }

        public bool CanWrite
        {
            get
            {
                return m_property.GetSetMethod() != null;
            }
        }

        public object GetValue(object target)
        {
            return InvokeInternal(target, null, true);
        }

        public object GetValue(object target, object indexValue)
        {
            return InvokeInternal(target, new object[]{indexValue}, true);
        }

        public void SetValue(object target, object value)
        {
            InvokeInternal(target, new object[]{value}, false);
        }

        public void SetValue(object target, object value, object indexValue)
        {
            InvokeInternal(target, new object[]{indexValue, value}, false);
        }

        private object InvokeInternal(object target, object[] args, bool getValue)
        {
            IGetProxyTarget proxy = target as IGetProxyTarget;
            if (proxy != null)
            {
                target = proxy.GetTarget();
            }

            MethodInfo accessor = getValue ? m_property.GetGetMethod(true) : m_property.GetSetMethod(true);
            if (accessor == null)
                throw new ArgumentException(System.Environment.GetResourceString(getValue ? "Arg_GetMethNotFnd" : "Arg_SetMethNotFnd"));
            if (!accessor.IsPublic)
                throw new MethodAccessException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Arg_MethodAccessException_WithMethodName"), accessor.ToString(), accessor.DeclaringType.FullName));
            RuntimeMethodInfo rtMethod = accessor as RuntimeMethodInfo;
            if (rtMethod == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"));
            Contract.Assert(AppDomain.CurrentDomain.PermissionSet.IsUnrestricted());
            return rtMethod.UnsafeInvoke(target, BindingFlags.Default, null, args, null);
        }

        public Type Type
        {
            get
            {
                return m_property.PropertyType;
            }
        }
    }
}