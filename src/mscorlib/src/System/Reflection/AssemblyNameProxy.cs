namespace System.Reflection
{
    using System;
    using System.Runtime.Versioning;

    public class AssemblyNameProxy : MarshalByRefObject
    {
        public AssemblyName GetAssemblyName(String assemblyFile)
        {
            return AssemblyName.GetAssemblyName(assemblyFile);
        }
    }
}