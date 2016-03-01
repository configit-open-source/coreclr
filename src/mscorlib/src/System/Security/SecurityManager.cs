namespace System.Security
{
    using System;
    using System.Security.Util;
    using System.Security.Policy;
    using System.Security.Permissions;
    using System.Collections;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Reflection;
    using System.IO;
    using System.Globalization;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    public enum PolicyLevelType
    {
        User = 0,
        Machine = 1,
        Enterprise = 2,
        AppDomain = 3
    }

    static public class SecurityManager
    {
        private static int[][] s_BuiltInPermissionIndexMap = {new int[]{BuiltInPermissionIndex.EnvironmentPermissionIndex, (int)PermissionType.EnvironmentPermission}, new int[]{BuiltInPermissionIndex.FileDialogPermissionIndex, (int)PermissionType.FileDialogPermission}, new int[]{BuiltInPermissionIndex.FileIOPermissionIndex, (int)PermissionType.FileIOPermission}, new int[]{BuiltInPermissionIndex.ReflectionPermissionIndex, (int)PermissionType.ReflectionPermission}, new int[]{BuiltInPermissionIndex.SecurityPermissionIndex, (int)PermissionType.SecurityPermission}, new int[]{BuiltInPermissionIndex.UIPermissionIndex, (int)PermissionType.UIPermission}};
        private static CodeAccessPermission[] s_UnrestrictedSpecialPermissionMap = {new EnvironmentPermission(PermissionState.Unrestricted), new FileDialogPermission(PermissionState.Unrestricted), new FileIOPermission(PermissionState.Unrestricted), new ReflectionPermission(PermissionState.Unrestricted), new SecurityPermission(PermissionState.Unrestricted), new UIPermission(PermissionState.Unrestricted)};
        internal static int GetSpecialFlags(PermissionSet grantSet, PermissionSet deniedSet)
        {
            if ((grantSet != null && grantSet.IsUnrestricted()) && (deniedSet == null || deniedSet.IsEmpty()))
            {
                return -1;
            }
            else
            {
                SecurityPermission securityPermission = null;
                SecurityPermissionFlag securityPermissionFlags = SecurityPermissionFlag.NoFlags;
                ReflectionPermission reflectionPermission = null;
                ReflectionPermissionFlag reflectionPermissionFlags = ReflectionPermissionFlag.NoFlags;
                CodeAccessPermission[] specialPermissions = new CodeAccessPermission[6];
                if (grantSet != null)
                {
                    if (grantSet.IsUnrestricted())
                    {
                        securityPermissionFlags = SecurityPermissionFlag.AllFlags;
                        reflectionPermissionFlags = ReflectionPermission.AllFlagsAndMore;
                        for (int i = 0; i < specialPermissions.Length; i++)
                        {
                            specialPermissions[i] = s_UnrestrictedSpecialPermissionMap[i];
                        }
                    }
                    else
                    {
                        securityPermission = grantSet.GetPermission(BuiltInPermissionIndex.SecurityPermissionIndex) as SecurityPermission;
                        if (securityPermission != null)
                            securityPermissionFlags = securityPermission.Flags;
                        reflectionPermission = grantSet.GetPermission(BuiltInPermissionIndex.ReflectionPermissionIndex) as ReflectionPermission;
                        if (reflectionPermission != null)
                            reflectionPermissionFlags = reflectionPermission.Flags;
                        for (int i = 0; i < specialPermissions.Length; i++)
                        {
                            specialPermissions[i] = grantSet.GetPermission(s_BuiltInPermissionIndexMap[i][0]) as CodeAccessPermission;
                        }
                    }
                }

                if (deniedSet != null)
                {
                    if (deniedSet.IsUnrestricted())
                    {
                        securityPermissionFlags = SecurityPermissionFlag.NoFlags;
                        reflectionPermissionFlags = ReflectionPermissionFlag.NoFlags;
                        for (int i = 0; i < s_BuiltInPermissionIndexMap.Length; i++)
                        {
                            specialPermissions[i] = null;
                        }
                    }
                    else
                    {
                        securityPermission = deniedSet.GetPermission(BuiltInPermissionIndex.SecurityPermissionIndex) as SecurityPermission;
                        if (securityPermission != null)
                            securityPermissionFlags &= ~securityPermission.Flags;
                        reflectionPermission = deniedSet.GetPermission(BuiltInPermissionIndex.ReflectionPermissionIndex) as ReflectionPermission;
                        if (reflectionPermission != null)
                            reflectionPermissionFlags &= ~reflectionPermission.Flags;
                        for (int i = 0; i < s_BuiltInPermissionIndexMap.Length; i++)
                        {
                            CodeAccessPermission deniedSpecialPermission = deniedSet.GetPermission(s_BuiltInPermissionIndexMap[i][0]) as CodeAccessPermission;
                            if (deniedSpecialPermission != null && !deniedSpecialPermission.IsSubsetOf(null))
                                specialPermissions[i] = null;
                        }
                    }
                }

                int flags = MapToSpecialFlags(securityPermissionFlags, reflectionPermissionFlags);
                if (flags != -1)
                {
                    for (int i = 0; i < specialPermissions.Length; i++)
                    {
                        if (specialPermissions[i] != null && ((IUnrestrictedPermission)specialPermissions[i]).IsUnrestricted())
                            flags |= (1 << (int)s_BuiltInPermissionIndexMap[i][1]);
                    }
                }

                return flags;
            }
        }

        private static int MapToSpecialFlags(SecurityPermissionFlag securityPermissionFlags, ReflectionPermissionFlag reflectionPermissionFlags)
        {
            int flags = 0;
            if ((securityPermissionFlags & SecurityPermissionFlag.UnmanagedCode) == SecurityPermissionFlag.UnmanagedCode)
                flags |= (1 << (int)PermissionType.SecurityUnmngdCodeAccess);
            if ((securityPermissionFlags & SecurityPermissionFlag.SkipVerification) == SecurityPermissionFlag.SkipVerification)
                flags |= (1 << (int)PermissionType.SecuritySkipVerification);
            if ((securityPermissionFlags & SecurityPermissionFlag.Assertion) == SecurityPermissionFlag.Assertion)
                flags |= (1 << (int)PermissionType.SecurityAssert);
            if ((securityPermissionFlags & SecurityPermissionFlag.SerializationFormatter) == SecurityPermissionFlag.SerializationFormatter)
                flags |= (1 << (int)PermissionType.SecuritySerialization);
            if ((securityPermissionFlags & SecurityPermissionFlag.BindingRedirects) == SecurityPermissionFlag.BindingRedirects)
                flags |= (1 << (int)PermissionType.SecurityBindingRedirects);
            if ((securityPermissionFlags & SecurityPermissionFlag.ControlEvidence) == SecurityPermissionFlag.ControlEvidence)
                flags |= (1 << (int)PermissionType.SecurityControlEvidence);
            if ((securityPermissionFlags & SecurityPermissionFlag.ControlPrincipal) == SecurityPermissionFlag.ControlPrincipal)
                flags |= (1 << (int)PermissionType.SecurityControlPrincipal);
            if ((reflectionPermissionFlags & ReflectionPermissionFlag.RestrictedMemberAccess) == ReflectionPermissionFlag.RestrictedMemberAccess)
                flags |= (1 << (int)PermissionType.ReflectionRestrictedMemberAccess);
            if ((reflectionPermissionFlags & ReflectionPermissionFlag.MemberAccess) == ReflectionPermissionFlag.MemberAccess)
                flags |= (1 << (int)PermissionType.ReflectionMemberAccess);
            return flags;
        }

        internal static extern bool IsSameType(String strLeft, String strRight);
        internal static extern bool _SetThreadSecurity(bool bThreadSecurity);
        internal static extern void GetGrantedPermissions(ObjectHandleOnStack retGranted, ObjectHandleOnStack retDenied, StackCrawlMarkHandle stackMark);
    }
}