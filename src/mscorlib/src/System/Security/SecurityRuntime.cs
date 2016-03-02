using System.Reflection;
using System.Threading;

namespace System.Security
{
    internal class SecurityRuntime
    {
        private SecurityRuntime()
        {
        }

        internal static extern FrameSecurityDescriptor GetSecurityObjectForFrame(ref StackCrawlMark stackMark, bool create);
        internal const bool StackContinue = true;
        internal const bool StackHalt = false;
        internal static MethodInfo GetMethodInfo(RuntimeMethodHandleInternal rmh)
        {
            if (rmh.IsNullHandle())
                return null;
            try
            {
                PermissionSet.s_fullTrust.Assert();
                return (System.RuntimeType.GetMethodBase(RuntimeMethodHandle.GetDeclaringType(rmh), rmh) as MethodInfo);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool FrameDescSetHelper(FrameSecurityDescriptor secDesc, PermissionSet demandSet, out PermissionSet alteredDemandSet, RuntimeMethodHandleInternal rmh)
        {
            return secDesc.CheckSetDemand(demandSet, out alteredDemandSet, rmh);
        }

        private static bool FrameDescHelper(FrameSecurityDescriptor secDesc, IPermission demandIn, PermissionToken permToken, RuntimeMethodHandleInternal rmh)
        {
            return secDesc.CheckDemand((CodeAccessPermission)demandIn, permToken, rmh);
        }

        internal static void Assert(PermissionSet permSet, ref StackCrawlMark stackMark)
        {
        }

        internal static void AssertAllPossible(ref StackCrawlMark stackMark)
        {
        }

        internal static void Deny(PermissionSet permSet, ref StackCrawlMark stackMark)
        {
        }

        internal static void PermitOnly(PermissionSet permSet, ref StackCrawlMark stackMark)
        {
        }

        internal static void RevertAssert(ref StackCrawlMark stackMark)
        {
        }

        internal static void RevertDeny(ref StackCrawlMark stackMark)
        {
        }

        internal static void RevertPermitOnly(ref StackCrawlMark stackMark)
        {
        }

        internal static void RevertAll(ref StackCrawlMark stackMark)
        {
        }
    }
}