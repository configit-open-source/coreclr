namespace System
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;

    public abstract class ValueType
    {
        public override bool Equals(Object obj)
        {
            BCLDebug.Perf(false, "ValueType::Equals is not fast.  " + this.GetType().FullName + " should override Equals(Object)");
            if (null == obj)
            {
                return false;
            }

            RuntimeType thisType = (RuntimeType)this.GetType();
            RuntimeType thatType = (RuntimeType)obj.GetType();
            if (thatType != thisType)
            {
                return false;
            }

            Object thisObj = (Object)this;
            Object thisResult, thatResult;
            if (CanCompareBits(this))
                return FastEqualsCheck(thisObj, obj);
            FieldInfo[] thisFields = thisType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < thisFields.Length; i++)
            {
                thisResult = ((RtFieldInfo)thisFields[i]).UnsafeGetValue(thisObj);
                thatResult = ((RtFieldInfo)thisFields[i]).UnsafeGetValue(obj);
                if (thisResult == null)
                {
                    if (thatResult != null)
                        return false;
                }
                else if (!thisResult.Equals(thatResult))
                {
                    return false;
                }
            }

            return true;
        }

        private static extern bool CanCompareBits(Object obj);
        private static extern bool FastEqualsCheck(Object a, Object b);
        public extern override int GetHashCode();
        internal static extern int GetHashCodeOfPtr(IntPtr ptr);
        public override String ToString()
        {
            return this.GetType().ToString();
        }
    }
}