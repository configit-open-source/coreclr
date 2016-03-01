using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
    public class Object
    {
        public Object()
        {
        }

        public virtual String ToString()
        {
            return GetType().ToString();
        }

        public virtual bool Equals(Object obj)
        {
            return RuntimeHelpers.Equals(this, obj);
        }

        public static bool Equals(Object objA, Object objB)
        {
            if (objA == objB)
            {
                return true;
            }

            if (objA == null || objB == null)
            {
                return false;
            }

            return objA.Equals(objB);
        }

        public static bool ReferenceEquals(Object objA, Object objB)
        {
            return objA == objB;
        }

        public virtual int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        public extern Type GetType();
        ~Object()
        {
        }

        protected extern Object MemberwiseClone();
        private void FieldSetter(String typeName, String fieldName, Object val)
        {
            Contract.Requires(typeName != null);
            Contract.Requires(fieldName != null);
            FieldInfo fldInfo = GetFieldInfo(typeName, fieldName);
            if (fldInfo.IsInitOnly)
                throw new FieldAccessException(Environment.GetResourceString("FieldAccess_InitOnly"));
            Type pt = fldInfo.FieldType;
            if (pt.IsByRef)
            {
                pt = pt.GetElementType();
            }

            if (!pt.IsInstanceOfType(val))
            {
                val = Convert.ChangeType(val, pt, CultureInfo.InvariantCulture);
            }

            fldInfo.SetValue(this, val);
        }

        private void FieldGetter(String typeName, String fieldName, ref Object val)
        {
            Contract.Requires(typeName != null);
            Contract.Requires(fieldName != null);
            FieldInfo fldInfo = GetFieldInfo(typeName, fieldName);
            val = fldInfo.GetValue(this);
        }

        private FieldInfo GetFieldInfo(String typeName, String fieldName)
        {
            Contract.Requires(typeName != null);
            Contract.Requires(fieldName != null);
            Contract.Ensures(Contract.Result<FieldInfo>() != null);
            Type t = GetType();
            while (null != t)
            {
                if (t.FullName.Equals(typeName))
                {
                    break;
                }

                t = t.BaseType;
            }

            if (null == t)
            {
                throw new ArgumentException();
            }

            FieldInfo fldInfo = t.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (null == fldInfo)
            {
                throw new ArgumentException();
            }

            return fldInfo;
        }
    }

    internal class __Canon
    {
    }
}