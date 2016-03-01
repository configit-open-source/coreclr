using System.Collections;
using System.Diagnostics.Contracts;

namespace System.Diagnostics
{
    internal delegate void LogMessageEventHandler(LoggingLevels level, LogSwitch category, String message, StackTrace location);
    internal delegate void LogSwitchLevelHandler(LogSwitch ls, LoggingLevels newLevel);
    internal static class Log
    {
        internal static Hashtable m_Hashtable;
        private static volatile bool m_fConsoleDeviceEnabled;
        private static LogMessageEventHandler _LogMessageEventHandler;
        private static volatile LogSwitchLevelHandler _LogSwitchLevelHandler;
        private static Object locker;
        public static readonly LogSwitch GlobalSwitch;
        static Log()
        {
            m_Hashtable = new Hashtable();
            m_fConsoleDeviceEnabled = false;
            locker = new Object();
            GlobalSwitch = new LogSwitch("Global", "Global Switch for this log");
            GlobalSwitch.MinimumLevel = LoggingLevels.ErrorLevel;
        }

        public static void AddOnLogMessage(LogMessageEventHandler handler)
        {
            lock (locker)
                _LogMessageEventHandler = (LogMessageEventHandler)MulticastDelegate.Combine(_LogMessageEventHandler, handler);
        }

        public static void RemoveOnLogMessage(LogMessageEventHandler handler)
        {
            lock (locker)
                _LogMessageEventHandler = (LogMessageEventHandler)MulticastDelegate.Remove(_LogMessageEventHandler, handler);
        }

        public static void AddOnLogSwitchLevel(LogSwitchLevelHandler handler)
        {
            lock (locker)
                _LogSwitchLevelHandler = (LogSwitchLevelHandler)MulticastDelegate.Combine(_LogSwitchLevelHandler, handler);
        }

        public static void RemoveOnLogSwitchLevel(LogSwitchLevelHandler handler)
        {
            lock (locker)
                _LogSwitchLevelHandler = (LogSwitchLevelHandler)MulticastDelegate.Remove(_LogSwitchLevelHandler, handler);
        }

        internal static void InvokeLogSwitchLevelHandlers(LogSwitch ls, LoggingLevels newLevel)
        {
            LogSwitchLevelHandler handler = _LogSwitchLevelHandler;
            if (handler != null)
                handler(ls, newLevel);
        }

        public static bool IsConsoleEnabled
        {
            get
            {
                return m_fConsoleDeviceEnabled;
            }

            set
            {
                m_fConsoleDeviceEnabled = value;
            }
        }

        public static void LogMessage(LoggingLevels level, String message)
        {
            LogMessage(level, GlobalSwitch, message);
        }

        public static void LogMessage(LoggingLevels level, LogSwitch logswitch, String message)
        {
            if (logswitch == null)
                throw new ArgumentNullException("LogSwitch");
            if (level < 0)
                throw new ArgumentOutOfRangeException("level", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            if (logswitch.CheckLevel(level) == true)
            {
                Debugger.Log((int)level, logswitch.strName, message);
                if (m_fConsoleDeviceEnabled)
                {
                    Console.Write(message);
                }
            }
        }

        public static void Trace(LogSwitch logswitch, String message)
        {
            LogMessage(LoggingLevels.TraceLevel0, logswitch, message);
        }

        public static void Trace(String switchname, String message)
        {
            LogSwitch ls;
            ls = LogSwitch.GetSwitch(switchname);
            LogMessage(LoggingLevels.TraceLevel0, ls, message);
        }

        public static void Trace(String message)
        {
            LogMessage(LoggingLevels.TraceLevel0, GlobalSwitch, message);
        }

        public static void Status(LogSwitch logswitch, String message)
        {
            LogMessage(LoggingLevels.StatusLevel0, logswitch, message);
        }

        public static void Status(String switchname, String message)
        {
            LogSwitch ls;
            ls = LogSwitch.GetSwitch(switchname);
            LogMessage(LoggingLevels.StatusLevel0, ls, message);
        }

        public static void Status(String message)
        {
            LogMessage(LoggingLevels.StatusLevel0, GlobalSwitch, message);
        }

        public static void Warning(LogSwitch logswitch, String message)
        {
            LogMessage(LoggingLevels.WarningLevel, logswitch, message);
        }

        public static void Warning(String switchname, String message)
        {
            LogSwitch ls;
            ls = LogSwitch.GetSwitch(switchname);
            LogMessage(LoggingLevels.WarningLevel, ls, message);
        }

        public static void Warning(String message)
        {
            LogMessage(LoggingLevels.WarningLevel, GlobalSwitch, message);
        }

        public static void Error(LogSwitch logswitch, String message)
        {
            LogMessage(LoggingLevels.ErrorLevel, logswitch, message);
        }

        public static void Error(String switchname, String message)
        {
            LogSwitch ls;
            ls = LogSwitch.GetSwitch(switchname);
            LogMessage(LoggingLevels.ErrorLevel, ls, message);
        }

        public static void Error(String message)
        {
            LogMessage(LoggingLevels.ErrorLevel, GlobalSwitch, message);
        }

        public static void Panic(String message)
        {
            LogMessage(LoggingLevels.PanicLevel, GlobalSwitch, message);
        }

        internal static extern void AddLogSwitch(LogSwitch logSwitch);
        internal static extern void ModifyLogSwitch(int iNewLevel, String strSwitchName, String strParentName);
    }
}