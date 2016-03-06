using System.Runtime.Serialization;

namespace System
{
    public class EntryPointNotFoundException : TypeLoadException
    {
        public EntryPointNotFoundException(): base (Environment.GetResourceString("Arg_EntryPointNotFoundException"))
        {
            SetErrorCode(__HResults.COR_E_ENTRYPOINTNOTFOUND);
        }

        public EntryPointNotFoundException(String message): base (message)
        {
            SetErrorCode(__HResults.COR_E_ENTRYPOINTNOTFOUND);
        }

        public EntryPointNotFoundException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(__HResults.COR_E_ENTRYPOINTNOTFOUND);
        }
    }
}