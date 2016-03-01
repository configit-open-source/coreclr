using System.Security.Permissions;

namespace System.Security
{
    public sealed class NamedPermissionSet : PermissionSet
    {
        internal static PermissionSet GetBuiltInSet(string name)
        {
            if (name == null)
                return null;
            else if (name.Equals("FullTrust"))
                return CreateFullTrustSet();
            else if (name.Equals("Nothing"))
                return CreateNothingSet();
            else if (name.Equals("Execution"))
                return CreateExecutionSet();
            else if (name.Equals("SkipVerification"))
                return CreateSkipVerificationSet();
            else if (name.Equals("Internet"))
                return CreateInternetSet();
            else
                return null;
        }

        private static PermissionSet CreateFullTrustSet()
        {
            return new PermissionSet(PermissionState.Unrestricted);
        }

        private static PermissionSet CreateNothingSet()
        {
            return new PermissionSet(PermissionState.None);
        }

        private static PermissionSet CreateExecutionSet()
        {
            PermissionSet permSet = new PermissionSet(PermissionState.None);
            permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            return permSet;
        }

        private static PermissionSet CreateSkipVerificationSet()
        {
            PermissionSet permSet = new PermissionSet(PermissionState.None);
            permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.SkipVerification));
            return permSet;
        }

        private static PermissionSet CreateInternetSet()
        {
            PermissionSet permSet = new PermissionSet(PermissionState.None);
            permSet.AddPermission(new FileDialogPermission(FileDialogPermissionAccess.Open));
            permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            permSet.AddPermission(new UIPermission(UIPermissionWindow.SafeTopLevelWindows, UIPermissionClipboard.OwnClipboard));
            return permSet;
        }
    }
}