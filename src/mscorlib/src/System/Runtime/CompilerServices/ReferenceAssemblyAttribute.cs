namespace System.Runtime.CompilerServices
{
    using System;

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