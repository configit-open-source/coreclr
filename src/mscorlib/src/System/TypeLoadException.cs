using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    public class TypeLoadException : SystemException, ISerializable
    {
        public TypeLoadException(): base (Environment.GetResourceString("Arg_TypeLoadException"))
        {
            SetErrorCode(__HResults.COR_E_TYPELOAD);
        }

        public TypeLoadException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_TYPELOAD);
        }

        public TypeLoadException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_TYPELOAD);
        }

        public override String Message
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                SetMessageField();
                return _message;
            }
        }

        private void SetMessageField()
        {
            if (_message == null)
            {
                if ((ClassName == null) && (ResourceId == 0))
                    _message = Environment.GetResourceString("Arg_TypeLoadException");
                else
                {
                    if (AssemblyName == null)
                        AssemblyName = Environment.GetResourceString("IO_UnknownFileName");
                    if (ClassName == null)
                        ClassName = Environment.GetResourceString("IO_UnknownFileName");
                    String format = null;
                    GetTypeLoadExceptionMessage(ResourceId, JitHelpers.GetStringHandleOnStack(ref format));
                    _message = String.Format(CultureInfo.CurrentCulture, format, ClassName, AssemblyName, MessageArg);
                }
            }
        }

        public String TypeName
        {
            get
            {
                if (ClassName == null)
                    return String.Empty;
                return ClassName;
            }
        }

        private TypeLoadException(String className, String assemblyName, String messageArg, int resourceId): base (null)
        {
            SetErrorCode(__HResults.COR_E_TYPELOAD);
            ClassName = className;
            AssemblyName = assemblyName;
            MessageArg = messageArg;
            ResourceId = resourceId;
            SetMessageField();
        }

        protected TypeLoadException(SerializationInfo info, StreamingContext context): base (info, context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            ClassName = info.GetString("TypeLoadClassName");
            AssemblyName = info.GetString("TypeLoadAssemblyName");
            MessageArg = info.GetString("TypeLoadMessageArg");
            ResourceId = info.GetInt32("TypeLoadResourceID");
        }

        private static extern void GetTypeLoadExceptionMessage(int resourceId, StringHandleOnStack retString);
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            base.GetObjectData(info, context);
            info.AddValue("TypeLoadClassName", ClassName, typeof (String));
            info.AddValue("TypeLoadAssemblyName", AssemblyName, typeof (String));
            info.AddValue("TypeLoadMessageArg", MessageArg, typeof (String));
            info.AddValue("TypeLoadResourceID", ResourceId);
        }

        private String ClassName;
        private String AssemblyName;
        private String MessageArg;
        internal int ResourceId;
    }
}