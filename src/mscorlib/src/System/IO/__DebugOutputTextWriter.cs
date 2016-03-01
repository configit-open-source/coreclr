using System;
using System.IO;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using System.Globalization;

namespace System.IO
{
    internal class __DebugOutputTextWriter : TextWriter
    {
        private readonly String _consoleType;
        internal __DebugOutputTextWriter(String consoleType): base (CultureInfo.InvariantCulture)
        {
            _consoleType = consoleType;
        }

        public override Encoding Encoding
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                if (Marshal.SystemDefaultCharSize == 1)
                    return Encoding.Default;
                else
                    return new UnicodeEncoding(false, false);
            }
        }

        public override void Write(char c)
        {
            OutputDebugString(c.ToString());
        }

        public override void Write(String str)
        {
            OutputDebugString(str);
        }

        public override void Write(char[] array)
        {
            if (array != null)
                OutputDebugString(new String(array));
        }

        public override void WriteLine(String str)
        {
            if (str != null)
                OutputDebugString(_consoleType + str);
            else
                OutputDebugString("<null>");
            OutputDebugString(new String(CoreNewLine));
        }

        private static extern void OutputDebugString(String output);
    }
}