namespace System.Resources
{
    public class MissingSatelliteAssemblyException : SystemException
    {
        private String _cultureName;
        public MissingSatelliteAssemblyException(): base (Environment.GetResourceString("MissingSatelliteAssembly_Default"))
        {
            SetErrorCode(System.__HResults.COR_E_MISSINGSATELLITEASSEMBLY);
        }

        public MissingSatelliteAssemblyException(String message): base (message)
        {
            SetErrorCode(System.__HResults.COR_E_MISSINGSATELLITEASSEMBLY);
        }

        public MissingSatelliteAssemblyException(String message, String cultureName): base (message)
        {
            SetErrorCode(System.__HResults.COR_E_MISSINGSATELLITEASSEMBLY);
            _cultureName = cultureName;
        }

        public MissingSatelliteAssemblyException(String message, Exception inner): base (message, inner)
        {
            SetErrorCode(System.__HResults.COR_E_MISSINGSATELLITEASSEMBLY);
        }

        public String CultureName
        {
            get
            {
                return _cultureName;
            }
        }
    }
}