

namespace System.Resources
{
    public sealed class SatelliteContractVersionAttribute : Attribute
    {
        private String _version;
        public SatelliteContractVersionAttribute(String version)
        {
            if (version == null)
                throw new ArgumentNullException("version");
                        _version = version;
        }

        public String Version
        {
            get
            {
                return _version;
            }
        }
    }
}