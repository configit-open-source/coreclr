

namespace System.Resources
{
    public sealed class NeutralResourcesLanguageAttribute : Attribute
    {
        private String _culture;
        private UltimateResourceFallbackLocation _fallbackLoc;
        public NeutralResourcesLanguageAttribute(String cultureName)
        {
            if (cultureName == null)
                throw new ArgumentNullException("cultureName");
                        _culture = cultureName;
            _fallbackLoc = UltimateResourceFallbackLocation.MainAssembly;
        }

        public NeutralResourcesLanguageAttribute(String cultureName, UltimateResourceFallbackLocation location)
        {
            if (cultureName == null)
                throw new ArgumentNullException("cultureName");
            if (!Enum.IsDefined(typeof (UltimateResourceFallbackLocation), location))
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidNeutralResourcesLanguage_FallbackLoc", location));
                        _culture = cultureName;
            _fallbackLoc = location;
        }

        public String CultureName
        {
            get
            {
                return _culture;
            }
        }

        public UltimateResourceFallbackLocation Location
        {
            get
            {
                return _fallbackLoc;
            }
        }
    }
}