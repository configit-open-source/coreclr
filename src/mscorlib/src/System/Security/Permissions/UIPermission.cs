namespace System.Security.Permissions
{
    using System;
    using System.Security;
    using System.Security.Util;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Reflection;
    using System.Collections;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    public enum UIPermissionWindow
    {
        NoWindows = 0x0,
        SafeSubWindows = 0x01,
        SafeTopLevelWindows = 0x02,
        AllWindows = 0x03
    }

    public enum UIPermissionClipboard
    {
        NoClipboard = 0x0,
        OwnClipboard = 0x1,
        AllClipboard = 0x2
    }

    sealed public class UIPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
    {
        private UIPermissionWindow m_windowFlag;
        private UIPermissionClipboard m_clipboardFlag;
        public UIPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                SetUnrestricted(true);
            }
            else if (state == PermissionState.None)
            {
                SetUnrestricted(false);
                Reset();
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }

        public UIPermission(UIPermissionWindow windowFlag, UIPermissionClipboard clipboardFlag)
        {
            VerifyWindowFlag(windowFlag);
            VerifyClipboardFlag(clipboardFlag);
            m_windowFlag = windowFlag;
            m_clipboardFlag = clipboardFlag;
        }

        public UIPermission(UIPermissionWindow windowFlag)
        {
            VerifyWindowFlag(windowFlag);
            m_windowFlag = windowFlag;
        }

        public UIPermission(UIPermissionClipboard clipboardFlag)
        {
            VerifyClipboardFlag(clipboardFlag);
            m_clipboardFlag = clipboardFlag;
        }

        public UIPermissionWindow Window
        {
            set
            {
                VerifyWindowFlag(value);
                m_windowFlag = value;
            }

            get
            {
                return m_windowFlag;
            }
        }

        public UIPermissionClipboard Clipboard
        {
            set
            {
                VerifyClipboardFlag(value);
                m_clipboardFlag = value;
            }

            get
            {
                return m_clipboardFlag;
            }
        }

        private static void VerifyWindowFlag(UIPermissionWindow flag)
        {
            if (flag < UIPermissionWindow.NoWindows || flag > UIPermissionWindow.AllWindows)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)flag));
            }

            Contract.EndContractBlock();
        }

        private static void VerifyClipboardFlag(UIPermissionClipboard flag)
        {
            if (flag < UIPermissionClipboard.NoClipboard || flag > UIPermissionClipboard.AllClipboard)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)flag));
            }

            Contract.EndContractBlock();
        }

        private void Reset()
        {
            m_windowFlag = UIPermissionWindow.NoWindows;
            m_clipboardFlag = UIPermissionClipboard.NoClipboard;
        }

        private void SetUnrestricted(bool unrestricted)
        {
            if (unrestricted)
            {
                m_windowFlag = UIPermissionWindow.AllWindows;
                m_clipboardFlag = UIPermissionClipboard.AllClipboard;
            }
        }

        public bool IsUnrestricted()
        {
            return m_windowFlag == UIPermissionWindow.AllWindows && m_clipboardFlag == UIPermissionClipboard.AllClipboard;
        }

        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return m_windowFlag == UIPermissionWindow.NoWindows && m_clipboardFlag == UIPermissionClipboard.NoClipboard;
            }

            try
            {
                UIPermission operand = (UIPermission)target;
                if (operand.IsUnrestricted())
                    return true;
                else if (this.IsUnrestricted())
                    return false;
                else
                    return this.m_windowFlag <= operand.m_windowFlag && this.m_clipboardFlag <= operand.m_clipboardFlag;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }
        }

        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
            {
                return null;
            }
            else if (!VerifyType(target))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }

            UIPermission operand = (UIPermission)target;
            UIPermissionWindow isectWindowFlags = m_windowFlag < operand.m_windowFlag ? m_windowFlag : operand.m_windowFlag;
            UIPermissionClipboard isectClipboardFlags = m_clipboardFlag < operand.m_clipboardFlag ? m_clipboardFlag : operand.m_clipboardFlag;
            if (isectWindowFlags == UIPermissionWindow.NoWindows && isectClipboardFlags == UIPermissionClipboard.NoClipboard)
                return null;
            else
                return new UIPermission(isectWindowFlags, isectClipboardFlags);
        }

        public override IPermission Union(IPermission target)
        {
            if (target == null)
            {
                return this.Copy();
            }
            else if (!VerifyType(target))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }

            UIPermission operand = (UIPermission)target;
            UIPermissionWindow isectWindowFlags = m_windowFlag > operand.m_windowFlag ? m_windowFlag : operand.m_windowFlag;
            UIPermissionClipboard isectClipboardFlags = m_clipboardFlag > operand.m_clipboardFlag ? m_clipboardFlag : operand.m_clipboardFlag;
            if (isectWindowFlags == UIPermissionWindow.NoWindows && isectClipboardFlags == UIPermissionClipboard.NoClipboard)
                return null;
            else
                return new UIPermission(isectWindowFlags, isectClipboardFlags);
        }

        public override IPermission Copy()
        {
            return new UIPermission(this.m_windowFlag, this.m_clipboardFlag);
        }

        int IBuiltInPermission.GetTokenIndex()
        {
            return UIPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.UIPermissionIndex;
        }
    }
}