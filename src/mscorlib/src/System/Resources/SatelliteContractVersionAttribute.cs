namespace System.Resources
{
    using System;
    using System.Diagnostics.Contracts;

    public sealed class SatelliteContractVersionAttribute : Attribute
    {
        private String _version;
        public SatelliteContractVersionAttribute(String version)
        {
            if (version == null)
                throw new ArgumentNullException("version");
            Contract.EndContractBlock();
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