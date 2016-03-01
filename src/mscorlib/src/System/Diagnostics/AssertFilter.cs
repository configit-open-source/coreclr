namespace System.Diagnostics
{
    using System;
    using System.Runtime.Versioning;

    abstract internal class AssertFilter
    {
        abstract public AssertFilters AssertFailure(String condition, String message, StackTrace location, StackTrace.TraceFormat stackTraceFormat, String windowTitle);
    }

    internal class DefaultFilter : AssertFilter
    {
        internal DefaultFilter()
        {
        }

        public override AssertFilters AssertFailure(String condition, String message, StackTrace location, StackTrace.TraceFormat stackTraceFormat, String windowTitle)
        {
            return (AssertFilters)Assert.ShowDefaultAssertDialog(condition, message, location.ToString(stackTraceFormat), windowTitle);
        }
    }
}