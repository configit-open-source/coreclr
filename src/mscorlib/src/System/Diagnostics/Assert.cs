namespace System.Diagnostics
{
    internal static class Assert
    {
        internal const int COR_E_FAILFAST = unchecked ((int)0x80131623);
        private static AssertFilter Filter;
        static Assert()
        {
            Filter = new DefaultFilter();
        }

        internal static void Check(bool condition, String conditionString, String message)
        {
            if (!condition)
            {
                Fail(conditionString, message, null, COR_E_FAILFAST);
            }
        }

        internal static void Check(bool condition, String conditionString, String message, int exitCode)
        {
            if (!condition)
            {
                Fail(conditionString, message, null, exitCode);
            }
        }

        internal static void Fail(String conditionString, String message)
        {
            Fail(conditionString, message, null, COR_E_FAILFAST);
        }

        internal static void Fail(String conditionString, String message, String windowTitle, int exitCode)
        {
            Fail(conditionString, message, windowTitle, exitCode, StackTrace.TraceFormat.Normal, 0);
        }

        internal static void Fail(String conditionString, String message, int exitCode, StackTrace.TraceFormat stackTraceFormat)
        {
            Fail(conditionString, message, null, exitCode, stackTraceFormat, 0);
        }

        internal static void Fail(String conditionString, String message, String windowTitle, int exitCode, StackTrace.TraceFormat stackTraceFormat, int numStackFramesToSkip)
        {
            StackTrace st = new StackTrace(numStackFramesToSkip, true);
            AssertFilters iResult = Filter.AssertFailure(conditionString, message, st, stackTraceFormat, windowTitle);
            if (iResult == AssertFilters.FailDebug)
            {
                if (Debugger.IsAttached == true)
                    Debugger.Break();
                else
                {
                    if (Debugger.Launch() == false)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DebuggerLaunchFailed"));
                    }
                }
            }
            else if (iResult == AssertFilters.FailTerminate)
            {
                Environment._Exit(exitCode);
            }
        }

        internal extern static int ShowDefaultAssertDialog(String conditionString, String message, String stackTrace, String windowTitle);
    }
}