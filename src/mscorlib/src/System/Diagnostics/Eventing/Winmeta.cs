namespace System.Diagnostics.Tracing
{
    public enum EventLevel
    {
        LogAlways = 0,
        Critical,
        Error,
        Warning,
        Informational,
        Verbose
    }

    public enum EventTask
    {
        None = 0
    }

    public enum EventOpcode
    {
        Info = 0,
        Start,
        Stop,
        DataCollectionStart,
        DataCollectionStop,
        Extension,
        Reply,
        Resume,
        Suspend,
        Send,
        Receive = 240
    }

    public enum EventChannel : byte
    {
        None = 0,
        Admin = 16,
        Operational = 17,
        Analytic = 18,
        Debug = 19
    }

    ;
    [Flags]
    public enum EventKeywords : long
    {
        None = 0x0,
        All = ~0,
        MicrosoftTelemetry = 0x02000000000000,
        WdiContext = 0x02000000000000,
        WdiDiagnostic = 0x04000000000000,
        Sqm = 0x08000000000000,
        AuditFailure = 0x10000000000000,
        AuditSuccess = 0x20000000000000,
        CorrelationHint = 0x10000000000000,
        EventLogClassic = 0x80000000000000
    }
}