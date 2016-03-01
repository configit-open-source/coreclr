

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal interface IPropertyValue
    {
        PropertyType Type
        {
            
            get;
        }

        bool IsNumericScalar
        {
            
            get;
        }

        Byte GetUInt8();
        Int16 GetInt16();
        UInt16 GetUInt16();
        Int32 GetInt32();
        UInt32 GetUInt32();
        Int64 GetInt64();
        UInt64 GetUInt64();
        Single GetSingle();
        Double GetDouble();
        char GetChar16();
        Boolean GetBoolean();
        String GetString();
        Guid GetGuid();
        DateTimeOffset GetDateTime();
        TimeSpan GetTimeSpan();
        Point GetPoint();
        Size GetSize();
        Rect GetRect();
        Byte[] GetUInt8Array();
        Int16[] GetInt16Array();
        UInt16[] GetUInt16Array();
        Int32[] GetInt32Array();
        UInt32[] GetUInt32Array();
        Int64[] GetInt64Array();
        UInt64[] GetUInt64Array();
        Single[] GetSingleArray();
        Double[] GetDoubleArray();
        char[] GetChar16Array();
        Boolean[] GetBooleanArray();
        String[] GetStringArray();
        object[] GetInspectableArray();
        Guid[] GetGuidArray();
        DateTimeOffset[] GetDateTimeArray();
        TimeSpan[] GetTimeSpanArray();
        Point[] GetPointArray();
        Size[] GetSizeArray();
        Rect[] GetRectArray();
    }

    internal struct Point
    {
    }

    internal struct Size
    {
    }

    internal struct Rect
    {
    }
}