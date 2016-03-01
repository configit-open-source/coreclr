

namespace System.Runtime.CompilerServices
{
    internal sealed class TypeDependencyAttribute : Attribute
    {
        private string typeName;
        public TypeDependencyAttribute(string typeName)
        {
            if (typeName == null)
                throw new ArgumentNullException("typeName");
                        this.typeName = typeName;
        }
    }
}