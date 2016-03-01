using System.Diagnostics.Contracts;
using System.Reflection;
using System.Security.Permissions;
using System.Security.Policy;
using System.Threading;

namespace System.Security
{
    internal enum PermissionType
    {
        SecurityUnmngdCodeAccess = 0,
        SecuritySkipVerification = 1,
        ReflectionTypeInfo = 2,
        SecurityAssert = 3,
        ReflectionMemberAccess = 4,
        SecuritySerialization = 5,
        ReflectionRestrictedMemberAccess = 6,
        FullTrust = 7,
        SecurityBindingRedirects = 8,
        UIPermission = 9,
        EnvironmentPermission = 10,
        FileDialogPermission = 11,
        FileIOPermission = 12,
        ReflectionPermission = 13,
        SecurityPermission = 14,
        SecurityControlEvidence = 16,
        SecurityControlPrincipal = 17
    }

    internal static class CodeAccessSecurityEngine
    {
        internal static SecurityPermission AssertPermission;
        internal static PermissionToken AssertPermissionToken;
        internal static extern void SpecialDemand(PermissionType whatPermission, ref StackCrawlMark stackMark);
        private static void DEBUG_OUT(String str)
        {
            if (debug)
            {
                Console.WriteLine(str);
            }
        }

        private static bool debug = false;
        private const String file = "d:\\foo\\debug.txt";
        static CodeAccessSecurityEngine()
        {
            AssertPermission = new SecurityPermission(SecurityPermissionFlag.Assertion);
            AssertPermissionToken = PermissionToken.GetToken(AssertPermission);
        }

        private static void ThrowSecurityException(RuntimeAssembly asm, PermissionSet granted, PermissionSet refused, RuntimeMethodHandleInternal rmh, SecurityAction action, Object demand, IPermission permThatFailed)
        {
            AssemblyName asmName = null;
            Evidence asmEvidence = null;
            if (asm != null)
            {
                PermissionSet.s_fullTrust.Assert();
                asmName = asm.GetName();
            }

            throw SecurityException.MakeSecurityException(asmName, asmEvidence, granted, refused, rmh, action, demand, permThatFailed);
        }

        private static void ThrowSecurityException(Object assemblyOrString, PermissionSet granted, PermissionSet refused, RuntimeMethodHandleInternal rmh, SecurityAction action, Object demand, IPermission permThatFailed)
        {
            Contract.Assert((assemblyOrString == null || assemblyOrString is RuntimeAssembly || assemblyOrString is String), "Must pass in an Assembly object or String object here");
            if (assemblyOrString == null || assemblyOrString is RuntimeAssembly)
                ThrowSecurityException((RuntimeAssembly)assemblyOrString, granted, refused, rmh, action, demand, permThatFailed);
            else
            {
                AssemblyName asmName = new AssemblyName((String)assemblyOrString);
                throw SecurityException.MakeSecurityException(asmName, null, granted, refused, rmh, action, demand, permThatFailed);
            }
        }

        internal static void CheckSetHelper(Object notUsed, PermissionSet grants, PermissionSet refused, PermissionSet demands, RuntimeMethodHandleInternal rmh, RuntimeAssembly asm, SecurityAction action)
        {
            Contract.Assert(notUsed == null, "Should not reach here with a non-null first arg which is the CompressedStack");
            CheckSetHelper(grants, refused, demands, rmh, (Object)asm, action, true);
        }

        internal static bool CheckSetHelper(PermissionSet grants, PermissionSet refused, PermissionSet demands, RuntimeMethodHandleInternal rmh, Object assemblyOrString, SecurityAction action, bool throwException)
        {
            Contract.Assert(demands != null, "Should not reach here with a null demand set");
            IPermission permThatFailed = null;
            if (grants != null)
                grants.CheckDecoded(demands);
            if (refused != null)
                refused.CheckDecoded(demands);
            bool bThreadSecurity = SecurityManager._SetThreadSecurity(false);
            try
            {
                if (!demands.CheckDemand(grants, out permThatFailed))
                {
                    if (throwException)
                        ThrowSecurityException(assemblyOrString, grants, refused, rmh, action, demands, permThatFailed);
                    else
                        return false;
                }

                if (!demands.CheckDeny(refused, out permThatFailed))
                {
                    if (throwException)
                        ThrowSecurityException(assemblyOrString, grants, refused, rmh, action, demands, permThatFailed);
                    else
                        return false;
                }
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (Exception)
            {
                if (throwException)
                    ThrowSecurityException(assemblyOrString, grants, refused, rmh, action, demands, permThatFailed);
                else
                    return false;
            }
            finally
            {
                if (bThreadSecurity)
                    SecurityManager._SetThreadSecurity(true);
            }

            return true;
        }

        internal static void CheckHelper(Object notUsed, PermissionSet grantedSet, PermissionSet refusedSet, CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh, RuntimeAssembly asm, SecurityAction action)
        {
            Contract.Assert(notUsed == null, "Should not reach here with a non-null first arg which is the CompressedStack");
            CheckHelper(grantedSet, refusedSet, demand, permToken, rmh, (Object)asm, action, true);
        }

        internal static bool CheckHelper(PermissionSet grantedSet, PermissionSet refusedSet, CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh, Object assemblyOrString, SecurityAction action, bool throwException)
        {
            Contract.Assert(demand != null, "Should not reach here with a null demand");
            if (permToken == null)
                permToken = PermissionToken.GetToken(demand);
            if (grantedSet != null)
                grantedSet.CheckDecoded(permToken.m_index);
            if (refusedSet != null)
                refusedSet.CheckDecoded(permToken.m_index);
            bool bThreadSecurity = SecurityManager._SetThreadSecurity(false);
            try
            {
                if (grantedSet == null)
                {
                    if (throwException)
                        ThrowSecurityException(assemblyOrString, grantedSet, refusedSet, rmh, action, demand, demand);
                    else
                        return false;
                }
                else if (!grantedSet.IsUnrestricted())
                {
                    Contract.Assert(demand != null, "demand != null");
                    CodeAccessPermission grantedPerm = (CodeAccessPermission)grantedSet.GetPermission(permToken);
                    if (!demand.CheckDemand(grantedPerm))
                    {
                        if (throwException)
                            ThrowSecurityException(assemblyOrString, grantedSet, refusedSet, rmh, action, demand, demand);
                        else
                            return false;
                    }
                }

                if (refusedSet != null)
                {
                    CodeAccessPermission refusedPerm = (CodeAccessPermission)refusedSet.GetPermission(permToken);
                    if (refusedPerm != null)
                    {
                        if (!refusedPerm.CheckDeny(demand))
                        {
                            if (debug)
                                DEBUG_OUT("Permission found in refused set");
                            if (throwException)
                                ThrowSecurityException(assemblyOrString, grantedSet, refusedSet, rmh, action, demand, demand);
                            else
                                return false;
                        }
                    }

                    if (refusedSet.IsUnrestricted())
                    {
                        if (throwException)
                            ThrowSecurityException(assemblyOrString, grantedSet, refusedSet, rmh, action, demand, demand);
                        else
                            return false;
                    }
                }
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (Exception)
            {
                if (throwException)
                    ThrowSecurityException(assemblyOrString, grantedSet, refusedSet, rmh, action, demand, demand);
                else
                    return false;
            }
            finally
            {
                if (bThreadSecurity)
                    SecurityManager._SetThreadSecurity(true);
            }

            DEBUG_OUT("Check passed");
            return true;
        }

        internal static void Check(CodeAccessPermission cap, ref StackCrawlMark stackMark)
        {
        }

        internal static void Check(PermissionSet permSet, ref StackCrawlMark stackMark)
        {
        }

        internal static extern FrameSecurityDescriptor CheckNReturnSO(PermissionToken permToken, CodeAccessPermission demand, ref StackCrawlMark stackMark, int create);
        internal static void Assert(CodeAccessPermission cap, ref StackCrawlMark stackMark)
        {
        }

        internal static void Deny(CodeAccessPermission cap, ref StackCrawlMark stackMark)
        {
        }

        internal static void PermitOnly(CodeAccessPermission cap, ref StackCrawlMark stackMark)
        {
        }
    }
}