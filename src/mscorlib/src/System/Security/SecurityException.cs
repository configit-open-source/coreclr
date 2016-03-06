
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Security.Policy;

namespace System.Security
{
    public class SecurityException : SystemException
    {
        internal static string GetResString(string sResourceName)
        {
            PermissionSet.s_fullTrust.Assert();
            return Environment.GetResourceString(sResourceName);
        }

        internal static Exception MakeSecurityException(AssemblyName asmName, Evidence asmEvidence, PermissionSet granted, PermissionSet refused, RuntimeMethodHandleInternal rmh, SecurityAction action, Object demand, IPermission permThatFailed)
        {
            return new SecurityException(GetResString("Arg_SecurityException"));
        }

        public SecurityException(): base (GetResString("Arg_SecurityException"))
        {
            SetErrorCode(System.__HResults.COR_E_SECURITY);
        }

        public SecurityException(String message): base (message)
        {
            SetErrorCode(System.__HResults.COR_E_SECURITY);
        }

        public SecurityException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(System.__HResults.COR_E_SECURITY);
        }

        internal SecurityException(PermissionSet grantedSetObj, PermissionSet refusedSetObj): this ()
        {
        }

        internal SecurityException(string message, AssemblyName assemblyName, PermissionSet grant, PermissionSet refused, MethodInfo method, SecurityAction action, Object demanded, IPermission permThatFailed, Evidence evidence): this ()
        {
        }

        internal SecurityException(string message, Object deny, Object permitOnly, MethodInfo method, Object demanded, IPermission permThatFailed): this ()
        {
        }

        public override String ToString()
        {
            return base.ToString();
        }
    }
}