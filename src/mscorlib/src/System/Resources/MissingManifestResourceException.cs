using System;
using System.Runtime.Serialization;

namespace System.Resources
{
    public class MissingManifestResourceException : SystemException
    {
        public MissingManifestResourceException(): base (Environment.GetResourceString("Arg_MissingManifestResourceException"))
        {
            SetErrorCode(System.__HResults.COR_E_MISSINGMANIFESTRESOURCE);
        }

        public MissingManifestResourceException(String message): base (message)
        {
            SetErrorCode(System.__HResults.COR_E_MISSINGMANIFESTRESOURCE);
        }

        public MissingManifestResourceException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(System.__HResults.COR_E_MISSINGMANIFESTRESOURCE);
        }
    }
}