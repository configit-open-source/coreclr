
using System.IO;
using System.Reflection;

namespace System.Globalization
{
    internal sealed class GlobalizationAssembly
    {
        internal unsafe static byte *GetGlobalizationResourceBytePtr(Assembly assembly, String tableName)
        {
                                    Stream stream = assembly.GetManifestResourceStream(tableName);
            UnmanagedMemoryStream bytesStream = stream as UnmanagedMemoryStream;
            if (bytesStream != null)
            {
                byte *bytes = bytesStream.PositionPointer;
                if (bytes != null)
                {
                    return (bytes);
                }
            }

                        throw new InvalidOperationException();
        }
    }
}