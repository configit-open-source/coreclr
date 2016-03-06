
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
    }
}