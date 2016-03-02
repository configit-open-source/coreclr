

namespace System.Runtime.Versioning
{
    public sealed class TargetFrameworkAttribute : Attribute
    {
        private String _frameworkName;
        private String _frameworkDisplayName;
        public TargetFrameworkAttribute(String frameworkName)
        {
            if (frameworkName == null)
                throw new ArgumentNullException("frameworkName");
                        _frameworkName = frameworkName;
        }

        public String FrameworkName
        {
            get
            {
                return _frameworkName;
            }
        }

        public String FrameworkDisplayName
        {
            get
            {
                return _frameworkDisplayName;
            }

            set
            {
                _frameworkDisplayName = value;
            }
        }
    }
}