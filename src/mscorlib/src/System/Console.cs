namespace System
{
    using System;
    using System.IO;
    using System.Text;
    using System.Globalization;
    using System.Security;
    using System.Security.Permissions;
    using Microsoft.Win32;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Runtime.InteropServices;
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.CodeAnalysis;
    using System.Collections.Generic;

    public static class Console
    {
        private const int DefaultConsoleBufferSize = 256;
        private const short AltVKCode = 0x12;
        private const int NumberLockVKCode = 0x90;
        private const int CapsLockVKCode = 0x14;
        private const int MinBeepFrequency = 37;
        private const int MaxBeepFrequency = 32767;
        private const int MaxConsoleTitleLength = 24500;
        private static volatile TextReader _in;
        private static volatile TextWriter _out;
        private static volatile TextWriter _error;
        private static volatile ConsoleCancelEventHandler _cancelCallbacks;
        private static volatile ControlCHooker _hooker;
        private static Win32Native.InputRecord _cachedInputRecord;
        private static volatile bool _haveReadDefaultColors;
        private static volatile byte _defaultColors;
        private static volatile Encoding _inputEncoding = null;
        private static volatile Encoding _outputEncoding = null;
        private static volatile Object s_InternalSyncObject;
        private static Object InternalSyncObject
        {
            get
            {
                Contract.Ensures(Contract.Result<Object>() != null);
                if (s_InternalSyncObject == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange<Object>(ref s_InternalSyncObject, o, null);
                }

                return s_InternalSyncObject;
            }
        }

        private static volatile Object s_ReadKeySyncObject;
        private static Object ReadKeySyncObject
        {
            get
            {
                Contract.Ensures(Contract.Result<Object>() != null);
                if (s_ReadKeySyncObject == null)
                {
                    Object o = new Object();
                    Interlocked.CompareExchange<Object>(ref s_ReadKeySyncObject, o, null);
                }

                return s_ReadKeySyncObject;
            }
        }

        private static volatile IntPtr _consoleInputHandle;
        private static volatile IntPtr _consoleOutputHandle;
        private static IntPtr ConsoleInputHandle
        {
            [System.Security.SecurityCritical]
            get
            {
                if (_consoleInputHandle == IntPtr.Zero)
                {
                    _consoleInputHandle = Win32Native.GetStdHandle(Win32Native.STD_INPUT_HANDLE);
                }

                return _consoleInputHandle;
            }
        }

        private static IntPtr ConsoleOutputHandle
        {
            [System.Security.SecurityCritical]
            get
            {
                if (_consoleOutputHandle == IntPtr.Zero)
                {
                    _consoleOutputHandle = Win32Native.GetStdHandle(Win32Native.STD_OUTPUT_HANDLE);
                }

                return _consoleOutputHandle;
            }
        }

        public static TextReader In
        {
            [System.Security.SecuritySafeCritical]
            [HostProtection(UI = true)]
            get
            {
                Contract.Ensures(Contract.Result<TextReader>() != null);
                if (_in == null)
                {
                    lock (InternalSyncObject)
                    {
                        if (_in == null)
                        {
                            Stream s = OpenStandardInput(DefaultConsoleBufferSize);
                            TextReader tr;
                            if (s == Stream.Null)
                                tr = StreamReader.Null;
                            else
                            {
                                Encoding enc = Encoding.UTF8;
                                tr = TextReader.Synchronized(new StreamReader(s, enc, false, DefaultConsoleBufferSize, true));
                            }

                            System.Threading.Thread.MemoryBarrier();
                            _in = tr;
                        }
                    }
                }

                return _in;
            }
        }

        public static TextWriter Out
        {
            [HostProtection(UI = true)]
            get
            {
                Contract.Ensures(Contract.Result<TextWriter>() != null);
                if (_out == null)
                    InitializeStdOutError(true);
                return _out;
            }
        }

        public static TextWriter Error
        {
            [HostProtection(UI = true)]
            get
            {
                Contract.Ensures(Contract.Result<TextWriter>() != null);
                if (_error == null)
                    InitializeStdOutError(false);
                return _error;
            }
        }

        private static void InitializeStdOutError(bool stdout)
        {
            lock (InternalSyncObject)
            {
                if (stdout && _out != null)
                    return;
                else if (!stdout && _error != null)
                    return;
                TextWriter writer = null;
                Stream s;
                if (stdout)
                    s = OpenStandardOutput(DefaultConsoleBufferSize);
                else
                    s = OpenStandardError(DefaultConsoleBufferSize);
                if (s == Stream.Null)
                {
                    if (CheckOutputDebug())
                        writer = MakeDebugOutputTextWriter((stdout) ? "Console.Out: " : "Console.Error: ");
                    else
                        writer = TextWriter.Synchronized(StreamWriter.Null);
                }
                else
                {
                    Encoding encoding = Encoding.UTF8;
                    StreamWriter stdxxx = new StreamWriter(s, encoding, DefaultConsoleBufferSize, true);
                    stdxxx.HaveWrittenPreamble = true;
                    stdxxx.AutoFlush = true;
                    writer = TextWriter.Synchronized(stdxxx);
                }

                if (stdout)
                    _out = writer;
                else
                    _error = writer;
                Contract.Assert((stdout && _out != null) || (!stdout && _error != null), "Didn't set Console::_out or _error appropriately!");
            }
        }

        private static bool CheckOutputDebug()
        {
            new System.Security.Permissions.RegistryPermission(RegistryPermissionAccess.Read | RegistryPermissionAccess.Write, "HKEY_LOCAL_MACHINE").Assert();
            RegistryKey rk = Registry.LocalMachine;
            using (rk = rk.OpenSubKey("Software\\Microsoft\\.NETFramework", false))
            {
                if (rk != null)
                {
                    Object obj = rk.GetValue("ConsoleSpewToDebugger", 0);
                    if (obj != null && ((int)obj) != 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static TextWriter MakeDebugOutputTextWriter(String streamLabel)
        {
            TextWriter output = new __DebugOutputTextWriter(streamLabel);
            output.WriteLine("Output redirected to debugger from a bit bucket.");
            return TextWriter.Synchronized(output);
        }

        private static Stream GetStandardFile(int stdHandleName, FileAccess access, int bufferSize)
        {
            IntPtr handle = Win32Native.GetStdHandle(stdHandleName);
            SafeFileHandle sh = new SafeFileHandle(handle, false);
            if (sh.IsInvalid)
            {
                sh.SetHandleAsInvalid();
                return Stream.Null;
            }

            if (stdHandleName != Win32Native.STD_INPUT_HANDLE && !ConsoleHandleIsWritable(sh))
            {
                return Stream.Null;
            }

            const bool useFileAPIs = true;
            Stream console = new __ConsoleStream(sh, access, useFileAPIs);
            return console;
        }

        private static unsafe bool ConsoleHandleIsWritable(SafeFileHandle outErrHandle)
        {
            int bytesWritten;
            byte junkByte = 0x41;
            int r = Win32Native.WriteFile(outErrHandle, &junkByte, 0, out bytesWritten, IntPtr.Zero);
            return r != 0;
        }

        public static Encoding InputEncoding
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Contract.Ensures(Contract.Result<Encoding>() != null);
                if (null != _inputEncoding)
                    return _inputEncoding;
                lock (InternalSyncObject)
                {
                    if (null != _inputEncoding)
                        return _inputEncoding;
                    uint cp = Win32Native.GetConsoleCP();
                    _inputEncoding = Encoding.GetEncoding((int)cp);
                    return _inputEncoding;
                }
            }
        }

        public static Encoding OutputEncoding
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Contract.Ensures(Contract.Result<Encoding>() != null);
                if (null != _outputEncoding)
                    return _outputEncoding;
                lock (InternalSyncObject)
                {
                    if (null != _outputEncoding)
                        return _outputEncoding;
                    uint cp = Win32Native.GetConsoleOutputCP();
                    _outputEncoding = Encoding.GetEncoding((int)cp);
                    return _outputEncoding;
                }
            }
        }

        public static void Beep()
        {
            Beep(800, 200);
        }

        public static void Beep(int frequency, int duration)
        {
            if (frequency < MinBeepFrequency || frequency > MaxBeepFrequency)
                throw new ArgumentOutOfRangeException("frequency", frequency, Environment.GetResourceString("ArgumentOutOfRange_BeepFrequency", MinBeepFrequency, MaxBeepFrequency));
            if (duration <= 0)
                throw new ArgumentOutOfRangeException("duration", duration, Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            Contract.EndContractBlock();
            Win32Native.Beep(frequency, duration);
        }

        public static void Clear()
        {
            Win32Native.COORD coordScreen = new Win32Native.COORD();
            Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi;
            bool success;
            int conSize;
            IntPtr hConsole = ConsoleOutputHandle;
            if (hConsole == Win32Native.INVALID_HANDLE_VALUE)
                throw new IOException(Environment.GetResourceString("IO.IO_NoConsole"));
            csbi = GetBufferInfo();
            conSize = csbi.dwSize.X * csbi.dwSize.Y;
            int numCellsWritten = 0;
            success = Win32Native.FillConsoleOutputCharacter(hConsole, ' ', conSize, coordScreen, out numCellsWritten);
            if (!success)
                __Error.WinIOError();
            numCellsWritten = 0;
            success = Win32Native.FillConsoleOutputAttribute(hConsole, csbi.wAttributes, conSize, coordScreen, out numCellsWritten);
            if (!success)
                __Error.WinIOError();
            success = Win32Native.SetConsoleCursorPosition(hConsole, coordScreen);
            if (!success)
                __Error.WinIOError();
        }

        private static Win32Native.Color ConsoleColorToColorAttribute(ConsoleColor color, bool isBackground)
        {
            if ((((int)color) & ~0xf) != 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidConsoleColor"));
            Contract.EndContractBlock();
            Win32Native.Color c = (Win32Native.Color)color;
            if (isBackground)
                c = (Win32Native.Color)((int)c << 4);
            return c;
        }

        private static ConsoleColor ColorAttributeToConsoleColor(Win32Native.Color c)
        {
            if ((c & Win32Native.Color.BackgroundMask) != 0)
                c = (Win32Native.Color)(((int)c) >> 4);
            return (ConsoleColor)c;
        }

        public static ConsoleColor BackgroundColor
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                bool succeeded;
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);
                if (!succeeded)
                    return ConsoleColor.Black;
                Win32Native.Color c = (Win32Native.Color)csbi.wAttributes & Win32Native.Color.BackgroundMask;
                return ColorAttributeToConsoleColor(c);
            }

            [System.Security.SecuritySafeCritical]
            set
            {
                new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
                Win32Native.Color c = ConsoleColorToColorAttribute(value, true);
                bool succeeded;
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);
                if (!succeeded)
                    return;
                Contract.Assert(_haveReadDefaultColors, "Setting the foreground color before we've read the default foreground color!");
                short attrs = csbi.wAttributes;
                attrs &= ~((short)Win32Native.Color.BackgroundMask);
                attrs = (short)(((uint)(ushort)attrs) | ((uint)(ushort)c));
                Win32Native.SetConsoleTextAttribute(ConsoleOutputHandle, attrs);
            }
        }

        public static ConsoleColor ForegroundColor
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                bool succeeded;
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);
                if (!succeeded)
                    return ConsoleColor.Gray;
                Win32Native.Color c = (Win32Native.Color)csbi.wAttributes & Win32Native.Color.ForegroundMask;
                return ColorAttributeToConsoleColor(c);
            }

            [System.Security.SecuritySafeCritical]
            set
            {
                new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
                Win32Native.Color c = ConsoleColorToColorAttribute(value, false);
                bool succeeded;
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);
                if (!succeeded)
                    return;
                Contract.Assert(_haveReadDefaultColors, "Setting the foreground color before we've read the default foreground color!");
                short attrs = csbi.wAttributes;
                attrs &= ~((short)Win32Native.Color.ForegroundMask);
                attrs = (short)(((uint)(ushort)attrs) | ((uint)(ushort)c));
                Win32Native.SetConsoleTextAttribute(ConsoleOutputHandle, attrs);
            }
        }

        public static void ResetColor()
        {
            new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
            bool succeeded;
            Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);
            if (!succeeded)
                return;
            Contract.Assert(_haveReadDefaultColors, "Setting the foreground color before we've read the default foreground color!");
            short defaultAttrs = (short)(ushort)_defaultColors;
            Win32Native.SetConsoleTextAttribute(ConsoleOutputHandle, defaultAttrs);
        }

        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop)
        {
            MoveBufferArea(sourceLeft, sourceTop, sourceWidth, sourceHeight, targetLeft, targetTop, ' ', ConsoleColor.Black, BackgroundColor);
        }

        public unsafe static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor)
        {
            if (sourceForeColor < ConsoleColor.Black || sourceForeColor > ConsoleColor.White)
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidConsoleColor"), "sourceForeColor");
            if (sourceBackColor < ConsoleColor.Black || sourceBackColor > ConsoleColor.White)
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidConsoleColor"), "sourceBackColor");
            Contract.EndContractBlock();
            Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
            Win32Native.COORD bufferSize = csbi.dwSize;
            if (sourceLeft < 0 || sourceLeft > bufferSize.X)
                throw new ArgumentOutOfRangeException("sourceLeft", sourceLeft, Environment.GetResourceString("ArgumentOutOfRange_ConsoleBufferBoundaries"));
            if (sourceTop < 0 || sourceTop > bufferSize.Y)
                throw new ArgumentOutOfRangeException("sourceTop", sourceTop, Environment.GetResourceString("ArgumentOutOfRange_ConsoleBufferBoundaries"));
            if (sourceWidth < 0 || sourceWidth > bufferSize.X - sourceLeft)
                throw new ArgumentOutOfRangeException("sourceWidth", sourceWidth, Environment.GetResourceString("ArgumentOutOfRange_ConsoleBufferBoundaries"));
            if (sourceHeight < 0 || sourceTop > bufferSize.Y - sourceHeight)
                throw new ArgumentOutOfRangeException("sourceHeight", sourceHeight, Environment.GetResourceString("ArgumentOutOfRange_ConsoleBufferBoundaries"));
            if (targetLeft < 0 || targetLeft > bufferSize.X)
                throw new ArgumentOutOfRangeException("targetLeft", targetLeft, Environment.GetResourceString("ArgumentOutOfRange_ConsoleBufferBoundaries"));
            if (targetTop < 0 || targetTop > bufferSize.Y)
                throw new ArgumentOutOfRangeException("targetTop", targetTop, Environment.GetResourceString("ArgumentOutOfRange_ConsoleBufferBoundaries"));
            if (sourceWidth == 0 || sourceHeight == 0)
                return;
            new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
            Win32Native.CHAR_INFO[] data = new Win32Native.CHAR_INFO[sourceWidth * sourceHeight];
            bufferSize.X = (short)sourceWidth;
            bufferSize.Y = (short)sourceHeight;
            Win32Native.COORD bufferCoord = new Win32Native.COORD();
            Win32Native.SMALL_RECT readRegion = new Win32Native.SMALL_RECT();
            readRegion.Left = (short)sourceLeft;
            readRegion.Right = (short)(sourceLeft + sourceWidth - 1);
            readRegion.Top = (short)sourceTop;
            readRegion.Bottom = (short)(sourceTop + sourceHeight - 1);
            bool r;
            fixed (Win32Native.CHAR_INFO*pCharInfo = data)
                r = Win32Native.ReadConsoleOutput(ConsoleOutputHandle, pCharInfo, bufferSize, bufferCoord, ref readRegion);
            if (!r)
                __Error.WinIOError();
            Win32Native.COORD writeCoord = new Win32Native.COORD();
            writeCoord.X = (short)sourceLeft;
            Win32Native.Color c = ConsoleColorToColorAttribute(sourceBackColor, true);
            c |= ConsoleColorToColorAttribute(sourceForeColor, false);
            short attr = (short)c;
            int numWritten;
            for (int i = sourceTop; i < sourceTop + sourceHeight; i++)
            {
                writeCoord.Y = (short)i;
                r = Win32Native.FillConsoleOutputCharacter(ConsoleOutputHandle, sourceChar, sourceWidth, writeCoord, out numWritten);
                Contract.Assert(numWritten == sourceWidth, "FillConsoleOutputCharacter wrote the wrong number of chars!");
                if (!r)
                    __Error.WinIOError();
                r = Win32Native.FillConsoleOutputAttribute(ConsoleOutputHandle, attr, sourceWidth, writeCoord, out numWritten);
                if (!r)
                    __Error.WinIOError();
            }

            Win32Native.SMALL_RECT writeRegion = new Win32Native.SMALL_RECT();
            writeRegion.Left = (short)targetLeft;
            writeRegion.Right = (short)(targetLeft + sourceWidth);
            writeRegion.Top = (short)targetTop;
            writeRegion.Bottom = (short)(targetTop + sourceHeight);
            fixed (Win32Native.CHAR_INFO*pCharInfo = data)
                r = Win32Native.WriteConsoleOutput(ConsoleOutputHandle, pCharInfo, bufferSize, bufferCoord, ref writeRegion);
        }

        private static Win32Native.CONSOLE_SCREEN_BUFFER_INFO GetBufferInfo()
        {
            bool junk;
            return GetBufferInfo(true, out junk);
        }

        private static Win32Native.CONSOLE_SCREEN_BUFFER_INFO GetBufferInfo(bool throwOnNoConsole, out bool succeeded)
        {
            succeeded = false;
            Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi;
            bool success;
            IntPtr hConsole = ConsoleOutputHandle;
            if (hConsole == Win32Native.INVALID_HANDLE_VALUE)
            {
                if (!throwOnNoConsole)
                    return new Win32Native.CONSOLE_SCREEN_BUFFER_INFO();
                else
                    throw new IOException(Environment.GetResourceString("IO.IO_NoConsole"));
            }

            success = Win32Native.GetConsoleScreenBufferInfo(hConsole, out csbi);
            if (!success)
            {
                success = Win32Native.GetConsoleScreenBufferInfo(Win32Native.GetStdHandle(Win32Native.STD_ERROR_HANDLE), out csbi);
                if (!success)
                    success = Win32Native.GetConsoleScreenBufferInfo(Win32Native.GetStdHandle(Win32Native.STD_INPUT_HANDLE), out csbi);
                if (!success)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    if (errorCode == Win32Native.ERROR_INVALID_HANDLE && !throwOnNoConsole)
                        return new Win32Native.CONSOLE_SCREEN_BUFFER_INFO();
                    __Error.WinIOError(errorCode, null);
                }
            }

            if (!_haveReadDefaultColors)
            {
                Contract.Assert((int)Win32Native.Color.ColorMask == 0xff, "Make sure one byte is large enough to store a Console color value!");
                _defaultColors = (byte)(csbi.wAttributes & (short)Win32Native.Color.ColorMask);
                _haveReadDefaultColors = true;
            }

            succeeded = true;
            return csbi;
        }

        public static int BufferHeight
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.dwSize.Y;
            }

            set
            {
                SetBufferSize(BufferWidth, value);
            }
        }

        public static int BufferWidth
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.dwSize.X;
            }

            set
            {
                SetBufferSize(value, BufferHeight);
            }
        }

        public static void SetBufferSize(int width, int height)
        {
            new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
            Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
            Win32Native.SMALL_RECT srWindow = csbi.srWindow;
            if (width < srWindow.Right + 1 || width >= Int16.MaxValue)
                throw new ArgumentOutOfRangeException("width", width, Environment.GetResourceString("ArgumentOutOfRange_ConsoleBufferLessThanWindowSize"));
            if (height < srWindow.Bottom + 1 || height >= Int16.MaxValue)
                throw new ArgumentOutOfRangeException("height", height, Environment.GetResourceString("ArgumentOutOfRange_ConsoleBufferLessThanWindowSize"));
            Win32Native.COORD size = new Win32Native.COORD();
            size.X = (short)width;
            size.Y = (short)height;
            bool r = Win32Native.SetConsoleScreenBufferSize(ConsoleOutputHandle, size);
            if (!r)
                __Error.WinIOError();
        }

        public static int WindowHeight
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.srWindow.Bottom - csbi.srWindow.Top + 1;
            }

            set
            {
                SetWindowSize(WindowWidth, value);
            }
        }

        public static int WindowWidth
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.srWindow.Right - csbi.srWindow.Left + 1;
            }

            set
            {
                SetWindowSize(value, WindowHeight);
            }
        }

        public static unsafe void SetWindowSize(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width", width, Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height", height, Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
            Contract.EndContractBlock();
            new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
            Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
            bool r;
            bool resizeBuffer = false;
            Win32Native.COORD size = new Win32Native.COORD();
            size.X = csbi.dwSize.X;
            size.Y = csbi.dwSize.Y;
            if (csbi.dwSize.X < csbi.srWindow.Left + width)
            {
                if (csbi.srWindow.Left >= Int16.MaxValue - width)
                    throw new ArgumentOutOfRangeException("width", Environment.GetResourceString("ArgumentOutOfRange_ConsoleWindowBufferSize"));
                size.X = (short)(csbi.srWindow.Left + width);
                resizeBuffer = true;
            }

            if (csbi.dwSize.Y < csbi.srWindow.Top + height)
            {
                if (csbi.srWindow.Top >= Int16.MaxValue - height)
                    throw new ArgumentOutOfRangeException("height", Environment.GetResourceString("ArgumentOutOfRange_ConsoleWindowBufferSize"));
                size.Y = (short)(csbi.srWindow.Top + height);
                resizeBuffer = true;
            }

            if (resizeBuffer)
            {
                r = Win32Native.SetConsoleScreenBufferSize(ConsoleOutputHandle, size);
                if (!r)
                    __Error.WinIOError();
            }

            Win32Native.SMALL_RECT srWindow = csbi.srWindow;
            srWindow.Bottom = (short)(srWindow.Top + height - 1);
            srWindow.Right = (short)(srWindow.Left + width - 1);
            r = Win32Native.SetConsoleWindowInfo(ConsoleOutputHandle, true, &srWindow);
            if (!r)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (resizeBuffer)
                {
                    Win32Native.SetConsoleScreenBufferSize(ConsoleOutputHandle, csbi.dwSize);
                }

                Win32Native.COORD bounds = Win32Native.GetLargestConsoleWindowSize(ConsoleOutputHandle);
                if (width > bounds.X)
                    throw new ArgumentOutOfRangeException("width", width, Environment.GetResourceString("ArgumentOutOfRange_ConsoleWindowSize_Size", bounds.X));
                if (height > bounds.Y)
                    throw new ArgumentOutOfRangeException("height", height, Environment.GetResourceString("ArgumentOutOfRange_ConsoleWindowSize_Size", bounds.Y));
                __Error.WinIOError(errorCode, String.Empty);
            }
        }

        public static int LargestWindowWidth
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Win32Native.COORD bounds = Win32Native.GetLargestConsoleWindowSize(ConsoleOutputHandle);
                return bounds.X;
            }
        }

        public static int LargestWindowHeight
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Win32Native.COORD bounds = Win32Native.GetLargestConsoleWindowSize(ConsoleOutputHandle);
                return bounds.Y;
            }
        }

        public static int WindowLeft
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.srWindow.Left;
            }

            set
            {
                SetWindowPosition(value, WindowTop);
            }
        }

        public static int WindowTop
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.srWindow.Top;
            }

            set
            {
                SetWindowPosition(WindowLeft, value);
            }
        }

        public static unsafe void SetWindowPosition(int left, int top)
        {
            new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
            Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
            Win32Native.SMALL_RECT srWindow = csbi.srWindow;
            int newRight = left + srWindow.Right - srWindow.Left + 1;
            if (left < 0 || newRight > csbi.dwSize.X || newRight < 0)
                throw new ArgumentOutOfRangeException("left", left, Environment.GetResourceString("ArgumentOutOfRange_ConsoleWindowPos"));
            int newBottom = top + srWindow.Bottom - srWindow.Top + 1;
            if (top < 0 || newBottom > csbi.dwSize.Y || newBottom < 0)
                throw new ArgumentOutOfRangeException("top", top, Environment.GetResourceString("ArgumentOutOfRange_ConsoleWindowPos"));
            srWindow.Bottom -= (short)(srWindow.Top - top);
            srWindow.Right -= (short)(srWindow.Left - left);
            srWindow.Left = (short)left;
            srWindow.Top = (short)top;
            bool r = Win32Native.SetConsoleWindowInfo(ConsoleOutputHandle, true, &srWindow);
            if (!r)
                __Error.WinIOError();
        }

        public static int CursorLeft
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.dwCursorPosition.X;
            }

            set
            {
                SetCursorPosition(value, CursorTop);
            }
        }

        public static int CursorTop
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.dwCursorPosition.Y;
            }

            set
            {
                SetCursorPosition(CursorLeft, value);
            }
        }

        public static void SetCursorPosition(int left, int top)
        {
            if (left < 0 || left >= Int16.MaxValue)
                throw new ArgumentOutOfRangeException("left", left, Environment.GetResourceString("ArgumentOutOfRange_ConsoleBufferBoundaries"));
            if (top < 0 || top >= Int16.MaxValue)
                throw new ArgumentOutOfRangeException("top", top, Environment.GetResourceString("ArgumentOutOfRange_ConsoleBufferBoundaries"));
            Contract.EndContractBlock();
            new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
            IntPtr hConsole = ConsoleOutputHandle;
            Win32Native.COORD coords = new Win32Native.COORD();
            coords.X = (short)left;
            coords.Y = (short)top;
            bool r = Win32Native.SetConsoleCursorPosition(hConsole, coords);
            if (!r)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Win32Native.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                if (left < 0 || left >= csbi.dwSize.X)
                    throw new ArgumentOutOfRangeException("left", left, Environment.GetResourceString("ArgumentOutOfRange_ConsoleBufferBoundaries"));
                if (top < 0 || top >= csbi.dwSize.Y)
                    throw new ArgumentOutOfRangeException("top", top, Environment.GetResourceString("ArgumentOutOfRange_ConsoleBufferBoundaries"));
                __Error.WinIOError(errorCode, String.Empty);
            }
        }

        public static int CursorSize
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Win32Native.CONSOLE_CURSOR_INFO cci;
                IntPtr hConsole = ConsoleOutputHandle;
                bool r = Win32Native.GetConsoleCursorInfo(hConsole, out cci);
                if (!r)
                    __Error.WinIOError();
                return cci.dwSize;
            }

            [System.Security.SecuritySafeCritical]
            set
            {
                if (value < 1 || value > 100)
                    throw new ArgumentOutOfRangeException("value", value, Environment.GetResourceString("ArgumentOutOfRange_CursorSize"));
                Contract.EndContractBlock();
                new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
                Win32Native.CONSOLE_CURSOR_INFO cci;
                IntPtr hConsole = ConsoleOutputHandle;
                bool r = Win32Native.GetConsoleCursorInfo(hConsole, out cci);
                if (!r)
                    __Error.WinIOError();
                cci.dwSize = value;
                r = Win32Native.SetConsoleCursorInfo(hConsole, ref cci);
                if (!r)
                    __Error.WinIOError();
            }
        }

        public static bool CursorVisible
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                Win32Native.CONSOLE_CURSOR_INFO cci;
                IntPtr hConsole = ConsoleOutputHandle;
                bool r = Win32Native.GetConsoleCursorInfo(hConsole, out cci);
                if (!r)
                    __Error.WinIOError();
                return cci.bVisible;
            }

            [System.Security.SecuritySafeCritical]
            set
            {
                new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
                Win32Native.CONSOLE_CURSOR_INFO cci;
                IntPtr hConsole = ConsoleOutputHandle;
                bool r = Win32Native.GetConsoleCursorInfo(hConsole, out cci);
                if (!r)
                    __Error.WinIOError();
                cci.bVisible = value;
                r = Win32Native.SetConsoleCursorInfo(hConsole, ref cci);
                if (!r)
                    __Error.WinIOError();
            }
        }

        private static extern Int32 GetTitleNative(StringHandleOnStack outTitle, out Int32 outTitleLength);
        public static String Title
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                string title = null;
                int titleLength = -1;
                Int32 r = GetTitleNative(JitHelpers.GetStringHandleOnStack(ref title), out titleLength);
                if (0 != r)
                {
                    __Error.WinIOError(r, String.Empty);
                }

                if (titleLength > MaxConsoleTitleLength)
                    throw new InvalidOperationException(Environment.GetResourceString("ArgumentOutOfRange_ConsoleTitleTooLong"));
                Contract.Assert(title.Length == titleLength);
                return title;
            }

            [System.Security.SecuritySafeCritical]
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Length > MaxConsoleTitleLength)
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_ConsoleTitleTooLong"));
                Contract.EndContractBlock();
                new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
                if (!Win32Native.SetConsoleTitle(value))
                    __Error.WinIOError();
            }
        }

        [Flags]
        internal enum ControlKeyState
        {
            RightAltPressed = 0x0001,
            LeftAltPressed = 0x0002,
            RightCtrlPressed = 0x0004,
            LeftCtrlPressed = 0x0008,
            ShiftPressed = 0x0010,
            NumLockOn = 0x0020,
            ScrollLockOn = 0x0040,
            CapsLockOn = 0x0080,
            EnhancedKey = 0x0100
        }

        public static ConsoleKeyInfo ReadKey()
        {
            return ReadKey(false);
        }

        private static bool IsAltKeyDown(Win32Native.InputRecord ir)
        {
            return (((ControlKeyState)ir.keyEvent.controlKeyState) & (ControlKeyState.LeftAltPressed | ControlKeyState.RightAltPressed)) != 0;
        }

        private static bool IsKeyDownEvent(Win32Native.InputRecord ir)
        {
            return (ir.eventType == Win32Native.KEY_EVENT && ir.keyEvent.keyDown);
        }

        private static bool IsModKey(Win32Native.InputRecord ir)
        {
            short keyCode = ir.keyEvent.virtualKeyCode;
            return ((keyCode >= 0x10 && keyCode <= 0x12) || keyCode == 0x14 || keyCode == 0x90 || keyCode == 0x91);
        }

        public static ConsoleKeyInfo ReadKey(bool intercept)
        {
            Win32Native.InputRecord ir;
            int numEventsRead = -1;
            bool r;
            lock (ReadKeySyncObject)
            {
                if (_cachedInputRecord.eventType == Win32Native.KEY_EVENT)
                {
                    ir = _cachedInputRecord;
                    if (_cachedInputRecord.keyEvent.repeatCount == 0)
                        _cachedInputRecord.eventType = -1;
                    else
                    {
                        _cachedInputRecord.keyEvent.repeatCount--;
                    }
                }
                else
                {
                    while (true)
                    {
                        r = Win32Native.ReadConsoleInput(ConsoleInputHandle, out ir, 1, out numEventsRead);
                        if (!r || numEventsRead == 0)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ConsoleReadKeyOnFile"));
                        }

                        short keyCode = ir.keyEvent.virtualKeyCode;
                        if (!IsKeyDownEvent(ir))
                        {
                            if (keyCode != AltVKCode)
                                continue;
                        }

                        char ch = (char)ir.keyEvent.uChar;
                        if (ch == 0)
                        {
                            if (IsModKey(ir))
                                continue;
                        }

                        ConsoleKey key = (ConsoleKey)keyCode;
                        if (IsAltKeyDown(ir) && ((key >= ConsoleKey.NumPad0 && key <= ConsoleKey.NumPad9) || (key == ConsoleKey.Clear) || (key == ConsoleKey.Insert) || (key >= ConsoleKey.PageUp && key <= ConsoleKey.DownArrow)))
                        {
                            continue;
                        }

                        if (ir.keyEvent.repeatCount > 1)
                        {
                            ir.keyEvent.repeatCount--;
                            _cachedInputRecord = ir;
                        }

                        break;
                    }
                }
            }

            ControlKeyState state = (ControlKeyState)ir.keyEvent.controlKeyState;
            bool shift = (state & ControlKeyState.ShiftPressed) != 0;
            bool alt = (state & (ControlKeyState.LeftAltPressed | ControlKeyState.RightAltPressed)) != 0;
            bool control = (state & (ControlKeyState.LeftCtrlPressed | ControlKeyState.RightCtrlPressed)) != 0;
            ConsoleKeyInfo info = new ConsoleKeyInfo((char)ir.keyEvent.uChar, (ConsoleKey)ir.keyEvent.virtualKeyCode, shift, alt, control);
            if (!intercept)
                Console.Write(ir.keyEvent.uChar);
            return info;
        }

        public static bool KeyAvailable
        {
            [System.Security.SecuritySafeCritical]
            [HostProtection(UI = true)]
            get
            {
                if (_cachedInputRecord.eventType == Win32Native.KEY_EVENT)
                    return true;
                Win32Native.InputRecord ir = new Win32Native.InputRecord();
                int numEventsRead = 0;
                while (true)
                {
                    bool r = Win32Native.PeekConsoleInput(ConsoleInputHandle, out ir, 1, out numEventsRead);
                    if (!r)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        if (errorCode == Win32Native.ERROR_INVALID_HANDLE)
                            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ConsoleKeyAvailableOnFile"));
                        __Error.WinIOError(errorCode, "stdin");
                    }

                    if (numEventsRead == 0)
                        return false;
                    if (!IsKeyDownEvent(ir) || IsModKey(ir))
                    {
                        r = Win32Native.ReadConsoleInput(ConsoleInputHandle, out ir, 1, out numEventsRead);
                        if (!r)
                            __Error.WinIOError();
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }

        public static bool NumberLock
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                short s = Win32Native.GetKeyState(NumberLockVKCode);
                return (s & 1) == 1;
            }
        }

        public static bool CapsLock
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                short s = Win32Native.GetKeyState(CapsLockVKCode);
                return (s & 1) == 1;
            }
        }

        public static bool TreatControlCAsInput
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                IntPtr handle = ConsoleInputHandle;
                if (handle == Win32Native.INVALID_HANDLE_VALUE)
                    throw new IOException(Environment.GetResourceString("IO.IO_NoConsole"));
                int mode = 0;
                bool r = Win32Native.GetConsoleMode(handle, out mode);
                if (!r)
                    __Error.WinIOError();
                return (mode & Win32Native.ENABLE_PROCESSED_INPUT) == 0;
            }

            [System.Security.SecuritySafeCritical]
            set
            {
                new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
                IntPtr handle = ConsoleInputHandle;
                if (handle == Win32Native.INVALID_HANDLE_VALUE)
                    throw new IOException(Environment.GetResourceString("IO.IO_NoConsole"));
                int mode = 0;
                bool r = Win32Native.GetConsoleMode(handle, out mode);
                if (value)
                    mode &= ~Win32Native.ENABLE_PROCESSED_INPUT;
                else
                    mode |= Win32Native.ENABLE_PROCESSED_INPUT;
                r = Win32Native.SetConsoleMode(handle, mode);
                if (!r)
                    __Error.WinIOError();
            }
        }

        internal sealed class ControlCHooker : CriticalFinalizerObject
        {
            private bool _hooked;
            private Win32Native.ConsoleCtrlHandlerRoutine _handler;
            internal ControlCHooker()
            {
                _handler = new Win32Native.ConsoleCtrlHandlerRoutine(BreakEvent);
            }

            ~ControlCHooker()
            {
                Unhook();
            }

            internal void Hook()
            {
                if (!_hooked)
                {
                    bool r = Win32Native.SetConsoleCtrlHandler(_handler, true);
                    if (!r)
                        __Error.WinIOError();
                    _hooked = true;
                }
            }

            internal void Unhook()
            {
                if (_hooked)
                {
                    bool r = Win32Native.SetConsoleCtrlHandler(_handler, false);
                    if (!r)
                        __Error.WinIOError();
                    _hooked = false;
                }
            }
        }

        private sealed class ControlCDelegateData
        {
            internal ConsoleSpecialKey ControlKey;
            internal bool Cancel;
            internal bool DelegateStarted;
            internal ManualResetEvent CompletionEvent;
            internal ConsoleCancelEventHandler CancelCallbacks;
            internal ControlCDelegateData(ConsoleSpecialKey controlKey, ConsoleCancelEventHandler cancelCallbacks)
            {
                this.ControlKey = controlKey;
                this.CancelCallbacks = cancelCallbacks;
                this.CompletionEvent = new ManualResetEvent(false);
            }
        }

        private static bool BreakEvent(int controlType)
        {
            if (controlType == Win32Native.CTRL_C_EVENT || controlType == Win32Native.CTRL_BREAK_EVENT)
            {
                ConsoleCancelEventHandler cancelCallbacks = Console._cancelCallbacks;
                if (cancelCallbacks == null)
                {
                    return false;
                }

                ConsoleSpecialKey controlKey = (controlType == 0) ? ConsoleSpecialKey.ControlC : ConsoleSpecialKey.ControlBreak;
                ControlCDelegateData delegateData = new ControlCDelegateData(controlKey, cancelCallbacks);
                WaitCallback controlCCallback = new WaitCallback(ControlCDelegate);
                if (!ThreadPool.QueueUserWorkItem(controlCCallback, delegateData))
                {
                    Contract.Assert(false, "ThreadPool.QueueUserWorkItem returned false without throwing. Unable to execute ControlC handler");
                    return false;
                }

                TimeSpan controlCWaitTime = new TimeSpan(0, 0, 30);
                delegateData.CompletionEvent.WaitOne(controlCWaitTime, false);
                if (!delegateData.DelegateStarted)
                {
                    Contract.Assert(false, "ThreadPool.QueueUserWorkItem did not execute the handler within 30 seconds.");
                    return false;
                }

                delegateData.CompletionEvent.WaitOne();
                delegateData.CompletionEvent.Close();
                return delegateData.Cancel;
            }

            return false;
        }

        private static void ControlCDelegate(object data)
        {
            ControlCDelegateData controlCData = (ControlCDelegateData)data;
            try
            {
                controlCData.DelegateStarted = true;
                ConsoleCancelEventArgs args = new ConsoleCancelEventArgs(controlCData.ControlKey);
                controlCData.CancelCallbacks(null, args);
                controlCData.Cancel = args.Cancel;
            }
            finally
            {
                controlCData.CompletionEvent.Set();
            }
        }

        public static event ConsoleCancelEventHandler CancelKeyPress
        {
            [System.Security.SecuritySafeCritical]
            add
            {
                new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
                lock (InternalSyncObject)
                {
                    _cancelCallbacks += value;
                    if (_hooker == null)
                    {
                        _hooker = new ControlCHooker();
                        _hooker.Hook();
                    }
                }
            }

            [System.Security.SecuritySafeCritical]
            remove
            {
                new UIPermission(UIPermissionWindow.SafeTopLevelWindows).Demand();
                lock (InternalSyncObject)
                {
                    _cancelCallbacks -= value;
                    Contract.Assert(_cancelCallbacks == null || _cancelCallbacks.GetInvocationList().Length > 0, "Teach Console::CancelKeyPress to handle a non-null but empty list of callbacks");
                    if (_hooker != null && _cancelCallbacks == null)
                        _hooker.Unhook();
                }
            }
        }

        public static Stream OpenStandardError()
        {
            return OpenStandardError(DefaultConsoleBufferSize);
        }

        public static Stream OpenStandardError(int bufferSize)
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            return GetStandardFile(Win32Native.STD_ERROR_HANDLE, FileAccess.Write, bufferSize);
        }

        public static Stream OpenStandardInput()
        {
            return OpenStandardInput(DefaultConsoleBufferSize);
        }

        public static Stream OpenStandardInput(int bufferSize)
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            return GetStandardFile(Win32Native.STD_INPUT_HANDLE, FileAccess.Read, bufferSize);
        }

        public static Stream OpenStandardOutput()
        {
            return OpenStandardOutput(DefaultConsoleBufferSize);
        }

        public static Stream OpenStandardOutput(int bufferSize)
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.EndContractBlock();
            return GetStandardFile(Win32Native.STD_OUTPUT_HANDLE, FileAccess.Write, bufferSize);
        }

        public static void SetIn(TextReader newIn)
        {
            if (newIn == null)
                throw new ArgumentNullException("newIn");
            Contract.EndContractBlock();
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            newIn = TextReader.Synchronized(newIn);
            lock (InternalSyncObject)
            {
                _in = newIn;
            }
        }

        public static void SetOut(TextWriter newOut)
        {
            if (newOut == null)
                throw new ArgumentNullException("newOut");
            Contract.EndContractBlock();
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            newOut = TextWriter.Synchronized(newOut);
            lock (InternalSyncObject)
            {
                _out = newOut;
            }
        }

        public static void SetError(TextWriter newError)
        {
            if (newError == null)
                throw new ArgumentNullException("newError");
            Contract.EndContractBlock();
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            newError = TextWriter.Synchronized(newError);
            lock (InternalSyncObject)
            {
                _error = newError;
            }
        }

        public static int Read()
        {
            return In.Read();
        }

        public static String ReadLine()
        {
            return In.ReadLine();
        }

        public static void WriteLine()
        {
            Out.WriteLine();
        }

        public static void WriteLine(bool value)
        {
            Out.WriteLine(value);
        }

        public static void WriteLine(char value)
        {
            Out.WriteLine(value);
        }

        public static void WriteLine(char[] buffer)
        {
            Out.WriteLine(buffer);
        }

        public static void WriteLine(char[] buffer, int index, int count)
        {
            Out.WriteLine(buffer, index, count);
        }

        public static void WriteLine(decimal value)
        {
            Out.WriteLine(value);
        }

        public static void WriteLine(double value)
        {
            Out.WriteLine(value);
        }

        public static void WriteLine(float value)
        {
            Out.WriteLine(value);
        }

        public static void WriteLine(int value)
        {
            Out.WriteLine(value);
        }

        public static void WriteLine(uint value)
        {
            Out.WriteLine(value);
        }

        public static void WriteLine(long value)
        {
            Out.WriteLine(value);
        }

        public static void WriteLine(ulong value)
        {
            Out.WriteLine(value);
        }

        public static void WriteLine(Object value)
        {
            Out.WriteLine(value);
        }

        public static void WriteLine(String value)
        {
            Out.WriteLine(value);
        }

        public static void WriteLine(String format, Object arg0)
        {
            Out.WriteLine(format, arg0);
        }

        public static void WriteLine(String format, Object arg0, Object arg1)
        {
            Out.WriteLine(format, arg0, arg1);
        }

        public static void WriteLine(String format, Object arg0, Object arg1, Object arg2)
        {
            Out.WriteLine(format, arg0, arg1, arg2);
        }

        public static void WriteLine(String format, Object arg0, Object arg1, Object arg2, Object arg3, __arglist)
        {
            Object[] objArgs;
            int argCount;
            ArgIterator args = new ArgIterator(__arglist);
            argCount = args.GetRemainingCount() + 4;
            objArgs = new Object[argCount];
            objArgs[0] = arg0;
            objArgs[1] = arg1;
            objArgs[2] = arg2;
            objArgs[3] = arg3;
            for (int i = 4; i < argCount; i++)
            {
                objArgs[i] = TypedReference.ToObject(args.GetNextArg());
            }

            Out.WriteLine(format, objArgs);
        }

        public static void WriteLine(String format, params Object[] arg)
        {
            if (arg == null)
                Out.WriteLine(format, null, null);
            else
                Out.WriteLine(format, arg);
        }

        public static void Write(String format, Object arg0)
        {
            Out.Write(format, arg0);
        }

        public static void Write(String format, Object arg0, Object arg1)
        {
            Out.Write(format, arg0, arg1);
        }

        public static void Write(String format, Object arg0, Object arg1, Object arg2)
        {
            Out.Write(format, arg0, arg1, arg2);
        }

        public static void Write(String format, Object arg0, Object arg1, Object arg2, Object arg3, __arglist)
        {
            Object[] objArgs;
            int argCount;
            ArgIterator args = new ArgIterator(__arglist);
            argCount = args.GetRemainingCount() + 4;
            objArgs = new Object[argCount];
            objArgs[0] = arg0;
            objArgs[1] = arg1;
            objArgs[2] = arg2;
            objArgs[3] = arg3;
            for (int i = 4; i < argCount; i++)
            {
                objArgs[i] = TypedReference.ToObject(args.GetNextArg());
            }

            Out.Write(format, objArgs);
        }

        public static void Write(String format, params Object[] arg)
        {
            if (arg == null)
                Out.Write(format, null, null);
            else
                Out.Write(format, arg);
        }

        public static void Write(bool value)
        {
            Out.Write(value);
        }

        public static void Write(char value)
        {
            Out.Write(value);
        }

        public static void Write(char[] buffer)
        {
            Out.Write(buffer);
        }

        public static void Write(char[] buffer, int index, int count)
        {
            Out.Write(buffer, index, count);
        }

        public static void Write(double value)
        {
            Out.Write(value);
        }

        public static void Write(decimal value)
        {
            Out.Write(value);
        }

        public static void Write(float value)
        {
            Out.Write(value);
        }

        public static void Write(int value)
        {
            Out.Write(value);
        }

        public static void Write(uint value)
        {
            Out.Write(value);
        }

        public static void Write(long value)
        {
            Out.Write(value);
        }

        public static void Write(ulong value)
        {
            Out.Write(value);
        }

        public static void Write(Object value)
        {
            Out.Write(value);
        }

        public static void Write(String value)
        {
            Out.Write(value);
        }
    }
}