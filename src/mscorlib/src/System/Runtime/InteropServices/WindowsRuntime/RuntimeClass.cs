namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal interface IStringable
    {
        string ToString();
    }

    internal class IStringableHelper
    {
        internal static string ToString(object obj)
        {
            IGetProxyTarget proxy = obj as IGetProxyTarget;
            if (proxy != null)
                obj = proxy.GetTarget();
            IStringable stringableType = obj as IStringable;
            if (stringableType != null)
            {
                return stringableType.ToString();
            }

            return obj.ToString();
        }
    }

    internal abstract class RuntimeClass : __ComObject
    {
        internal extern IntPtr GetRedirectedGetHashCodeMD();
        internal extern int RedirectGetHashCode(IntPtr pMD);
        public override int GetHashCode()
        {
            IntPtr pMD = GetRedirectedGetHashCodeMD();
            if (pMD == IntPtr.Zero)
                return base.GetHashCode();
            return RedirectGetHashCode(pMD);
        }

        internal extern IntPtr GetRedirectedToStringMD();
        internal extern string RedirectToString(IntPtr pMD);
        public override string ToString()
        {
            IStringable stringableType = this as IStringable;
            if (stringableType != null)
            {
                return stringableType.ToString();
            }
            else
            {
                IntPtr pMD = GetRedirectedToStringMD();
                if (pMD == IntPtr.Zero)
                    return base.ToString();
                return RedirectToString(pMD);
            }
        }

        internal extern IntPtr GetRedirectedEqualsMD();
        internal extern bool RedirectEquals(object obj, IntPtr pMD);
        public override bool Equals(object obj)
        {
            IntPtr pMD = GetRedirectedEqualsMD();
            if (pMD == IntPtr.Zero)
                return base.Equals(obj);
            return RedirectEquals(obj, pMD);
        }
    }
}