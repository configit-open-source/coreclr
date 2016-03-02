using System.Reflection;

namespace System.Runtime.CompilerServices
{
    public sealed class TypeForwardedToAttribute : Attribute
    {
        private Type _destination;
        public TypeForwardedToAttribute(Type destination)
        {
            _destination = destination;
        }

        public Type Destination
        {
            get
            {
                return _destination;
            }
        }

        internal static TypeForwardedToAttribute[] GetCustomAttribute(RuntimeAssembly assembly)
        {
            Type[] types = null;
            RuntimeAssembly.GetForwardedTypes(assembly.GetNativeHandle(), JitHelpers.GetObjectHandleOnStack(ref types));
            TypeForwardedToAttribute[] attributes = new TypeForwardedToAttribute[types.Length];
            for (int i = 0; i < types.Length; ++i)
                attributes[i] = new TypeForwardedToAttribute(types[i]);
            return attributes;
        }
    }
}