namespace System.Reflection
{
    using System;

    public struct InterfaceMapping
    {
        public Type TargetType;
        public Type InterfaceType;
        public MethodInfo[] TargetMethods;
        public MethodInfo[] InterfaceMethods;
    }
}