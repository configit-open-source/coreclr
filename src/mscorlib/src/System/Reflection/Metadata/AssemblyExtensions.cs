namespace System.Reflection.Metadata
{
    public static class AssemblyExtensions
    {
        private unsafe static extern bool InternalTryGetRawMetadata(RuntimeAssembly assembly, ref byte *blob, ref int length);
        public unsafe static bool TryGetRawMetadata(this Assembly assembly, out byte *blob, out int length)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            blob = null;
            length = 0;
            var runtimeAssembly = assembly as RuntimeAssembly;
            if (runtimeAssembly == null)
            {
                return false;
            }

            return InternalTryGetRawMetadata(runtimeAssembly, ref blob, ref length);
        }
    }
}