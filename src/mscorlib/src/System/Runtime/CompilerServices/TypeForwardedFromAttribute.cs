namespace System.Runtime.CompilerServices
{
    public sealed class TypeForwardedFromAttribute : Attribute
    {
        string assemblyFullName;
        private TypeForwardedFromAttribute()
        {
        }

        public TypeForwardedFromAttribute(string assemblyFullName)
        {
            if (String.IsNullOrEmpty(assemblyFullName))
            {
                throw new ArgumentNullException("assemblyFullName");
            }

            this.assemblyFullName = assemblyFullName;
        }

        public string AssemblyFullName
        {
            get
            {
                return assemblyFullName;
            }
        }
    }
}