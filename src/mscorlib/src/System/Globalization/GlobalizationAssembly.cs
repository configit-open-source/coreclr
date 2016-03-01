namespace System.Globalization
{
    using System;
    using System.Reflection;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Security;
    using System.Security.Principal;
    using System.Security.Permissions;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.IO;
    using System.Diagnostics.Contracts;

    internal sealed class GlobalizationAssembly
    {
        internal unsafe static byte *GetGlobalizationResourceBytePtr(Assembly assembly, String tableName)
        {
            Contract.Assert(assembly != null, "assembly can not be null.  This should be generally the mscorlib.dll assembly.");
            Contract.Assert(tableName != null, "table name can not be null");
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

            Contract.Assert(false, String.Format(CultureInfo.CurrentCulture, "Didn't get the resource table {0} for System.Globalization from {1}", tableName, assembly));
            throw new InvalidOperationException();
        }
    }
}