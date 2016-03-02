namespace System.Runtime.CompilerServices
{
    public sealed class ReferenceAssemblyAttribute : Attribute
    {
        private String _description;
        public ReferenceAssemblyAttribute()
        {
        }

        public ReferenceAssemblyAttribute(String description)
        {
            _description = description;
        }

        public String Description
        {
            get
            {
                return _description;
            }
        }
    }
}