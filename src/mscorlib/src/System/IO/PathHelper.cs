using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32;

namespace System.IO
{
    unsafe internal struct PathHelper
    {
        private int m_capacity;
        private int m_length;
        private int m_maxPath;
        private char *m_arrayPtr;
        private StringBuilder m_sb;
        private bool useStackAlloc;
        private bool doNotTryExpandShortFileName;
        internal PathHelper(char *charArrayPtr, int length)
        {
            Contract.Requires(charArrayPtr != null);
            Contract.Requires(length == Path.MaxPath);
            this.m_length = 0;
            this.m_sb = null;
            this.m_arrayPtr = charArrayPtr;
            this.m_capacity = length;
            this.m_maxPath = Path.MaxPath;
            useStackAlloc = true;
            doNotTryExpandShortFileName = false;
        }

        internal PathHelper(int capacity, int maxPath)
        {
            this.m_length = 0;
            this.m_arrayPtr = null;
            this.useStackAlloc = false;
            this.m_sb = new StringBuilder(capacity);
            this.m_capacity = capacity;
            this.m_maxPath = maxPath;
            doNotTryExpandShortFileName = false;
        }

        internal int Length
        {
            get
            {
                if (useStackAlloc)
                {
                    return m_length;
                }
                else
                {
                    return m_sb.Length;
                }
            }

            set
            {
                if (useStackAlloc)
                {
                    m_length = value;
                }
                else
                {
                    m_sb.Length = value;
                }
            }
        }

        internal int Capacity
        {
            get
            {
                return m_capacity;
            }
        }

        internal char this[int index]
        {
            [System.Security.SecurityCritical]
            get
            {
                Contract.Requires(index >= 0 && index < Length);
                if (useStackAlloc)
                {
                    return m_arrayPtr[index];
                }
                else
                {
                    return m_sb[index];
                }
            }

            [System.Security.SecurityCritical]
            set
            {
                Contract.Requires(index >= 0 && index < Length);
                if (useStackAlloc)
                {
                    m_arrayPtr[index] = value;
                }
                else
                {
                    m_sb[index] = value;
                }
            }
        }

        internal unsafe void Append(char value)
        {
            if (Length + 1 >= m_capacity)
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
            if (useStackAlloc)
            {
                m_arrayPtr[Length] = value;
                m_length++;
            }
            else
            {
                m_sb.Append(value);
            }
        }

        internal unsafe int GetFullPathName()
        {
            if (useStackAlloc)
            {
                char *finalBuffer = stackalloc char[Path.MaxPath + 1];
                int result = Win32Native.GetFullPathName(m_arrayPtr, Path.MaxPath + 1, finalBuffer, IntPtr.Zero);
                if (result > Path.MaxPath)
                {
                    char *tempBuffer = stackalloc char[result];
                    finalBuffer = tempBuffer;
                    result = Win32Native.GetFullPathName(m_arrayPtr, result, finalBuffer, IntPtr.Zero);
                }

                if (result >= Path.MaxPath)
                    throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                Contract.Assert(result < Path.MaxPath, "did we accidently remove a PathTooLongException check?");
                if (result == 0 && m_arrayPtr[0] != '\0')
                {
                    __Error.WinIOError();
                }
                else if (result < Path.MaxPath)
                {
                    finalBuffer[result] = '\0';
                }

                doNotTryExpandShortFileName = false;
                String.wstrcpy(m_arrayPtr, finalBuffer, result);
                Length = result;
                return result;
            }
            else
            {
                StringBuilder finalBuffer = new StringBuilder(m_capacity + 1);
                int result = Win32Native.GetFullPathName(m_sb.ToString(), m_capacity + 1, finalBuffer, IntPtr.Zero);
                if (result > m_maxPath)
                {
                    finalBuffer.Length = result;
                    result = Win32Native.GetFullPathName(m_sb.ToString(), result, finalBuffer, IntPtr.Zero);
                }

                if (result >= m_maxPath)
                    throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                Contract.Assert(result < m_maxPath, "did we accidentally remove a PathTooLongException check?");
                if (result == 0 && m_sb[0] != '\0')
                {
                    if (Length >= m_maxPath)
                    {
                        throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                    }

                    __Error.WinIOError();
                }

                doNotTryExpandShortFileName = false;
                m_sb = finalBuffer;
                return result;
            }
        }

        internal unsafe bool TryExpandShortFileName()
        {
            if (doNotTryExpandShortFileName)
                return false;
            if (useStackAlloc)
            {
                NullTerminate();
                char *buffer = UnsafeGetArrayPtr();
                char *shortFileNameBuffer = stackalloc char[Path.MaxPath + 1];
                int r = Win32Native.GetLongPathName(buffer, shortFileNameBuffer, Path.MaxPath);
                if (r >= Path.MaxPath)
                    throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                if (r == 0)
                {
                    int lastErr = Marshal.GetLastWin32Error();
                    if (lastErr == Win32Native.ERROR_FILE_NOT_FOUND || lastErr == Win32Native.ERROR_PATH_NOT_FOUND)
                        doNotTryExpandShortFileName = true;
                    return false;
                }

                String.wstrcpy(buffer, shortFileNameBuffer, r);
                Length = r;
                NullTerminate();
                return true;
            }
            else
            {
                StringBuilder sb = GetStringBuilder();
                String origName = sb.ToString();
                String tempName = origName;
                bool addedPrefix = false;
                if (tempName.Length > Path.MaxPath)
                {
                    tempName = Path.AddLongPathPrefix(tempName);
                    addedPrefix = true;
                }

                sb.Capacity = m_capacity;
                sb.Length = 0;
                int r = Win32Native.GetLongPathName(tempName, sb, m_capacity);
                if (r == 0)
                {
                    int lastErr = Marshal.GetLastWin32Error();
                    if (Win32Native.ERROR_FILE_NOT_FOUND == lastErr || Win32Native.ERROR_PATH_NOT_FOUND == lastErr)
                        doNotTryExpandShortFileName = true;
                    sb.Length = 0;
                    sb.Append(origName);
                    return false;
                }

                if (addedPrefix)
                    r -= 4;
                if (r >= m_maxPath)
                    throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                sb = Path.RemoveLongPathPrefix(sb);
                Length = sb.Length;
                return true;
            }
        }

        internal unsafe void Fixup(int lenSavedName, int lastSlash)
        {
            if (useStackAlloc)
            {
                char *savedName = stackalloc char[lenSavedName];
                String.wstrcpy(savedName, m_arrayPtr + lastSlash + 1, lenSavedName);
                Length = lastSlash;
                NullTerminate();
                doNotTryExpandShortFileName = false;
                bool r = TryExpandShortFileName();
                Append(Path.DirectorySeparatorChar);
                if (Length + lenSavedName >= Path.MaxPath)
                    throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                String.wstrcpy(m_arrayPtr + Length, savedName, lenSavedName);
                Length = Length + lenSavedName;
            }
            else
            {
                String savedName = m_sb.ToString(lastSlash + 1, lenSavedName);
                Length = lastSlash;
                doNotTryExpandShortFileName = false;
                bool r = TryExpandShortFileName();
                Append(Path.DirectorySeparatorChar);
                if (Length + lenSavedName >= m_maxPath)
                    throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                m_sb.Append(savedName);
            }
        }

        internal unsafe bool OrdinalStartsWith(String compareTo, bool ignoreCase)
        {
            if (Length < compareTo.Length)
                return false;
            if (useStackAlloc)
            {
                NullTerminate();
                if (ignoreCase)
                {
                    String s = new String(m_arrayPtr, 0, compareTo.Length);
                    return compareTo.Equals(s, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    for (int i = 0; i < compareTo.Length; i++)
                    {
                        if (m_arrayPtr[i] != compareTo[i])
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
            else
            {
                if (ignoreCase)
                {
                    return m_sb.ToString().StartsWith(compareTo, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    return m_sb.ToString().StartsWith(compareTo, StringComparison.Ordinal);
                }
            }
        }

        private unsafe bool OrdinalEqualsStackAlloc(String compareTo)
        {
            Contract.Requires(useStackAlloc, "Currently no efficient implementation for StringBuilder.OrdinalEquals(String)");
            if (Length != compareTo.Length)
            {
                return false;
            }

            for (int i = 0; i < compareTo.Length; i++)
            {
                if (m_arrayPtr[i] != compareTo[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override String ToString()
        {
            if (useStackAlloc)
            {
                return new String(m_arrayPtr, 0, Length);
            }
            else
            {
                return m_sb.ToString();
            }
        }

        internal String ToStringOrExisting(String existingString)
        {
            if (useStackAlloc)
            {
                return OrdinalEqualsStackAlloc(existingString) ? existingString : new String(m_arrayPtr, 0, Length);
            }
            else
            {
                string newString = m_sb.ToString();
                return String.Equals(newString, existingString, StringComparison.Ordinal) ? existingString : newString;
            }
        }

        private unsafe char *UnsafeGetArrayPtr()
        {
            Contract.Requires(useStackAlloc, "This should never be called for PathHelpers wrapping a StringBuilder");
            return m_arrayPtr;
        }

        private StringBuilder GetStringBuilder()
        {
            Contract.Requires(!useStackAlloc, "This should never be called for PathHelpers that wrap a stackalloc'd buffer");
            return m_sb;
        }

        private unsafe void NullTerminate()
        {
            Contract.Requires(useStackAlloc, "This should never be called for PathHelpers wrapping a StringBuilder");
            m_arrayPtr[m_length] = '\0';
        }
    }
}