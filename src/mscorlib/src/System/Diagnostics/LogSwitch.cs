namespace System.Diagnostics
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.CodeAnalysis;

    internal class LogSwitch
    {
        internal String strName;
        internal String strDescription;
        private LogSwitch ParentSwitch;
        internal volatile LoggingLevels iLevel;
        internal volatile LoggingLevels iOldLevel;
        private LogSwitch()
        {
        }

        public LogSwitch(String name, String description, LogSwitch parent)
        {
            if (name != null && name.Length == 0)
                throw new ArgumentOutOfRangeException("Name", Environment.GetResourceString("Argument_StringZeroLength"));
            Contract.EndContractBlock();
            if ((name != null) && (parent != null))
            {
                strName = name;
                strDescription = description;
                iLevel = LoggingLevels.ErrorLevel;
                iOldLevel = iLevel;
                ParentSwitch = parent;
                Log.m_Hashtable.Add(strName, this);
                Log.AddLogSwitch(this);
            }
            else
                throw new ArgumentNullException((name == null ? "name" : "parent"));
        }

        internal LogSwitch(String name, String description)
        {
            strName = name;
            strDescription = description;
            iLevel = LoggingLevels.ErrorLevel;
            iOldLevel = iLevel;
            ParentSwitch = null;
            Log.m_Hashtable.Add(strName, this);
            Log.AddLogSwitch(this);
        }

        public virtual String Name
        {
            get
            {
                return strName;
            }
        }

        public virtual String Description
        {
            get
            {
                return strDescription;
            }
        }

        public virtual LogSwitch Parent
        {
            get
            {
                return ParentSwitch;
            }
        }

        public virtual LoggingLevels MinimumLevel
        {
            get
            {
                return iLevel;
            }

            [System.Security.SecuritySafeCritical]
            set
            {
                iLevel = value;
                iOldLevel = value;
                String strParentName = ParentSwitch != null ? ParentSwitch.Name : "";
                if (Debugger.IsAttached)
                    Log.ModifyLogSwitch((int)iLevel, strName, strParentName);
                Log.InvokeLogSwitchLevelHandlers(this, iLevel);
            }
        }

        public virtual bool CheckLevel(LoggingLevels level)
        {
            if (iLevel > level)
            {
                if (this.ParentSwitch == null)
                    return false;
                else
                    return this.ParentSwitch.CheckLevel(level);
            }
            else
                return true;
        }

        public static LogSwitch GetSwitch(String name)
        {
            return (LogSwitch)Log.m_Hashtable[name];
        }
    }
}