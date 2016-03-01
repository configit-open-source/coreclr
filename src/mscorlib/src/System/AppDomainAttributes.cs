namespace System
{
    public enum LoaderOptimization
    {
        NotSpecified = 0,
        SingleDomain = 1,
        MultiDomain = 2,
        MultiDomainHost = 3
    }

    public sealed class LoaderOptimizationAttribute : Attribute
    {
        internal byte _val;
        public LoaderOptimizationAttribute(byte value)
        {
            _val = value;
        }

        public LoaderOptimizationAttribute(LoaderOptimization value)
        {
            _val = (byte)value;
        }

        public LoaderOptimization Value
        {
            get
            {
                return (LoaderOptimization)_val;
            }
        }
    }
}