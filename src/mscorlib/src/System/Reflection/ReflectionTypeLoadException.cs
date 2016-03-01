
using System.Runtime.Serialization;

namespace System.Reflection
{
    public sealed class ReflectionTypeLoadException : SystemException, ISerializable
    {
        private Type[] _classes;
        private Exception[] _exceptions;
        private ReflectionTypeLoadException(): base (Environment.GetResourceString("ReflectionTypeLoad_LoadFailed"))
        {
            SetErrorCode(__HResults.COR_E_REFLECTIONTYPELOAD);
        }

        private ReflectionTypeLoadException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_REFLECTIONTYPELOAD);
        }

        public ReflectionTypeLoadException(Type[] classes, Exception[] exceptions): base (null)
        {
            _classes = classes;
            _exceptions = exceptions;
            SetErrorCode(__HResults.COR_E_REFLECTIONTYPELOAD);
        }

        public ReflectionTypeLoadException(Type[] classes, Exception[] exceptions, String message): base (message)
        {
            _classes = classes;
            _exceptions = exceptions;
            SetErrorCode(__HResults.COR_E_REFLECTIONTYPELOAD);
        }

        internal ReflectionTypeLoadException(SerializationInfo info, StreamingContext context): base (info, context)
        {
            _classes = (Type[])(info.GetValue("Types", typeof (Type[])));
            _exceptions = (Exception[])(info.GetValue("Exceptions", typeof (Exception[])));
        }

        public Type[] Types
        {
            get
            {
                return _classes;
            }
        }

        public Exception[] LoaderExceptions
        {
            get
            {
                return _exceptions;
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

                        base.GetObjectData(info, context);
            info.AddValue("Types", _classes, typeof (Type[]));
            info.AddValue("Exceptions", _exceptions, typeof (Exception[]));
        }
    }
}