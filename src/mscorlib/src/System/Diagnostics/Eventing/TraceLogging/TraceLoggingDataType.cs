using System;

namespace System.Diagnostics.Tracing
{
    internal enum TraceLoggingDataType
    {
        Nil = 0,
        Utf16String = 1,
        MbcsString = 2,
        Int8 = 3,
        UInt8 = 4,
        Int16 = 5,
        UInt16 = 6,
        Int32 = 7,
        UInt32 = 8,
        Int64 = 9,
        UInt64 = 10,
        Float = 11,
        Double = 12,
        Boolean32 = 13,
        Binary = 14,
        Guid = 15,
        FileTime = 17,
        SystemTime = 18,
        HexInt32 = 20,
        HexInt64 = 21,
        CountedUtf16String = 22,
        CountedMbcsString = 23,
        Struct = 24,
        Char16 = UInt16 + (EventFieldFormat.String << 8),
        Char8 = UInt8 + (EventFieldFormat.String << 8),
        Boolean8 = UInt8 + (EventFieldFormat.Boolean << 8),
        HexInt8 = UInt8 + (EventFieldFormat.Hexadecimal << 8),
        HexInt16 = UInt16 + (EventFieldFormat.Hexadecimal << 8),
        Utf16Xml = Utf16String + (EventFieldFormat.Xml << 8),
        MbcsXml = MbcsString + (EventFieldFormat.Xml << 8),
        CountedUtf16Xml = CountedUtf16String + (EventFieldFormat.Xml << 8),
        CountedMbcsXml = CountedMbcsString + (EventFieldFormat.Xml << 8),
        Utf16Json = Utf16String + (EventFieldFormat.Json << 8),
        MbcsJson = MbcsString + (EventFieldFormat.Json << 8),
        CountedUtf16Json = CountedUtf16String + (EventFieldFormat.Json << 8),
        CountedMbcsJson = CountedMbcsString + (EventFieldFormat.Json << 8),
        HResult = Int32 + (EventFieldFormat.HResult << 8)}
}