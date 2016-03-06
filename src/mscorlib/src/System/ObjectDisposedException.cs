using System.Runtime.Serialization;

namespace System
{
    public class ObjectDisposedException : InvalidOperationException
    {
        private String objectName;
        private ObjectDisposedException(): this (null, Environment.GetResourceString("ObjectDisposed_Generic"))
        {
        }

        public ObjectDisposedException(String objectName): this (objectName, Environment.GetResourceString("ObjectDisposed_Generic"))
        {
        }

        public ObjectDisposedException(String objectName, String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_OBJECTDISPOSED);
            this.objectName = objectName;
        }

        public ObjectDisposedException(String message, Exception innerException): base (message, innerException)
        {
            SetErrorCode(__HResults.COR_E_OBJECTDISPOSED);
        }

        public override String Message
        {
            get
            {
                String name = ObjectName;
                if (name == null || name.Length == 0)
                    return base.Message;
                String objectDisposed = Environment.GetResourceString("ObjectDisposed_ObjectName_Name", name);
                return base.Message + Environment.NewLine + objectDisposed;
            }
        }

        public String ObjectName
        {
            get
            {
                if ((objectName == null) && !CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    return String.Empty;
                }

                return objectName;
            }
        }
    }
}