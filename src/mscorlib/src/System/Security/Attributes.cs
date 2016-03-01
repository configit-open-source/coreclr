namespace System.Security
{
    sealed internal class DynamicSecurityMethodAttribute : System.Attribute
    {
    }

    sealed public class SuppressUnmanagedCodeSecurityAttribute : System.Attribute
    {
    }

    sealed public class UnverifiableCodeAttribute : System.Attribute
    {
    }

    sealed public class AllowPartiallyTrustedCallersAttribute : System.Attribute
    {
        private PartialTrustVisibilityLevel _visibilityLevel;
        public AllowPartiallyTrustedCallersAttribute()
        {
        }

        public PartialTrustVisibilityLevel PartialTrustVisibilityLevel
        {
            get
            {
                return _visibilityLevel;
            }

            set
            {
                _visibilityLevel = value;
            }
        }
    }

    public enum PartialTrustVisibilityLevel
    {
        VisibleToAllHosts = 0,
        NotVisibleByDefault = 1
    }

    sealed public class SecurityCriticalAttribute : System.Attribute
    {
        public SecurityCriticalAttribute()
        {
        }
    }

    sealed public class SecurityTreatAsSafeAttribute : System.Attribute
    {
        public SecurityTreatAsSafeAttribute()
        {
        }
    }

    sealed public class SecuritySafeCriticalAttribute : System.Attribute
    {
        public SecuritySafeCriticalAttribute()
        {
        }
    }

    sealed public class SecurityTransparentAttribute : System.Attribute
    {
        public SecurityTransparentAttribute()
        {
        }
    }
}