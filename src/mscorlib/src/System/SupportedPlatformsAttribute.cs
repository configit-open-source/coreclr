using System.Diagnostics.Contracts;

namespace System
{
    public sealed class SupportedPlatformsAttribute : Attribute
    {
        internal Platforms m_platforms = Platforms.All;
        internal static SupportedPlatformsAttribute Default = new SupportedPlatformsAttribute(Platforms.All);
        public SupportedPlatformsAttribute(Platforms platforms)
        {
            if ((platforms & ~Platforms.All) != 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidFlag"), "platforms");
            Contract.EndContractBlock();
            m_platforms = platforms;
        }

        public Platforms Platforms
        {
            get
            {
                return m_platforms;
            }
        }
    }
}