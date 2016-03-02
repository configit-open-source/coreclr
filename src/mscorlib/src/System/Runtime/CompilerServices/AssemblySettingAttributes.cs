namespace System.Runtime.CompilerServices
{
    public enum LoadHint
    {
        Default = 0x0000,
        Always = 0x0001,
        Sometimes = 0x0002
    }

    public sealed class DefaultDependencyAttribute : Attribute
    {
        private LoadHint loadHint;
        public DefaultDependencyAttribute(LoadHint loadHintArgument)
        {
            loadHint = loadHintArgument;
        }

        public LoadHint LoadHint
        {
            get
            {
                return loadHint;
            }
        }
    }

    public sealed class DependencyAttribute : Attribute
    {
        private String dependentAssembly;
        private LoadHint loadHint;
        public DependencyAttribute(String dependentAssemblyArgument, LoadHint loadHintArgument)
        {
            dependentAssembly = dependentAssemblyArgument;
            loadHint = loadHintArgument;
        }

        public String DependentAssembly
        {
            get
            {
                return dependentAssembly;
            }
        }

        public LoadHint LoadHint
        {
            get
            {
                return loadHint;
            }
        }
    }
}