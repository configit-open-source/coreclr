using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Reflection;
using Microsoft.Win32;

namespace System.Diagnostics.Tracing
{
    public partial class EventSource
    {
        public static void SetCurrentThreadActivityId(Guid activityId)
        {
            if (TplEtwProvider.Log != null)
                TplEtwProvider.Log.SetActivityId(activityId);
            if (UnsafeNativeMethods.ManifestEtw.EventActivityIdControl(UnsafeNativeMethods.ManifestEtw.ActivityControl.EVENT_ACTIVITY_CTRL_GET_SET_ID, ref activityId) == 0)
            {
            }
        }

        public static void SetCurrentThreadActivityId(Guid activityId, out Guid oldActivityThatWillContinue)
        {
            oldActivityThatWillContinue = activityId;
            UnsafeNativeMethods.ManifestEtw.EventActivityIdControl(UnsafeNativeMethods.ManifestEtw.ActivityControl.EVENT_ACTIVITY_CTRL_GET_SET_ID, ref oldActivityThatWillContinue);
            if (TplEtwProvider.Log != null)
                TplEtwProvider.Log.SetActivityId(activityId);
        }

        public static Guid CurrentThreadActivityId
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Guid retVal = new Guid();
                UnsafeNativeMethods.ManifestEtw.EventActivityIdControl(UnsafeNativeMethods.ManifestEtw.ActivityControl.EVENT_ACTIVITY_CTRL_GET_ID, ref retVal);
                return retVal;
            }
        }

        private int GetParameterCount(EventMetadata eventData)
        {
            return eventData.Parameters.Length;
        }

        private Type GetDataType(EventMetadata eventData, int parameterId)
        {
            return eventData.Parameters[parameterId].ParameterType;
        }

        private static string GetResourceString(string key, params object[] args)
        {
            return Environment.GetResourceString(key, args);
        }

        private static readonly bool m_EventSourcePreventRecursion = false;
    }

    internal partial class ManifestBuilder
    {
        private string GetTypeNameHelper(Type type)
        {
            switch (type.GetTypeCode())
            {
                case TypeCode.Boolean:
                    return "win:Boolean";
                case TypeCode.Byte:
                    return "win:UInt8";
                case TypeCode.Char:
                case TypeCode.UInt16:
                    return "win:UInt16";
                case TypeCode.UInt32:
                    return "win:UInt32";
                case TypeCode.UInt64:
                    return "win:UInt64";
                case TypeCode.SByte:
                    return "win:Int8";
                case TypeCode.Int16:
                    return "win:Int16";
                case TypeCode.Int32:
                    return "win:Int32";
                case TypeCode.Int64:
                    return "win:Int64";
                case TypeCode.String:
                    return "win:UnicodeString";
                case TypeCode.Single:
                    return "win:Float";
                case TypeCode.Double:
                    return "win:Double";
                case TypeCode.DateTime:
                    return "win:FILETIME";
                default:
                    if (type == typeof (Guid))
                        return "win:GUID";
                    else if (type == typeof (IntPtr))
                        return "win:Pointer";
                    else if ((type.IsArray || type.IsPointer) && type.GetElementType() == typeof (byte))
                        return "win:Binary";
                    ManifestError(Environment.GetResourceString("EventSource_UnsupportedEventTypeInManifest", type.Name), true);
                    return string.Empty;
            }
        }
    }

    internal partial class EventProvider
    {
        internal unsafe int SetInformation(UnsafeNativeMethods.ManifestEtw.EVENT_INFO_CLASS eventInfoClass, IntPtr data, uint dataSize)
        {
            int status = UnsafeNativeMethods.ManifestEtw.ERROR_NOT_SUPPORTED;
            if (!m_setInformationMissing)
            {
                try
                {
                    status = UnsafeNativeMethods.ManifestEtw.EventSetInformation(m_regHandle, eventInfoClass, (void *)data, (int)dataSize);
                }
                catch (TypeLoadException)
                {
                    m_setInformationMissing = true;
                }
            }

            return status;
        }
    }

    internal static class Resources
    {
        internal static string GetResourceString(string key, params object[] args)
        {
            return Environment.GetResourceString(key, args);
        }
    }
}