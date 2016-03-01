namespace System.Reflection
{
    public class AssemblyNameProxy : MarshalByRefObject
    {
        public AssemblyName GetAssemblyName(String assemblyFile)
        {
            return AssemblyName.GetAssemblyName(assemblyFile);
        }
    }
}