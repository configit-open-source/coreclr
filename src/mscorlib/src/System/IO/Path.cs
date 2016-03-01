using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Win32;

namespace System.IO
{
    public static class Path
    {
        public static readonly char DirectorySeparatorChar = '\\';
        internal const string DirectorySeparatorCharAsString = "\\";
        public static readonly char AltDirectorySeparatorChar = '/';
        public static readonly char VolumeSeparatorChar = ':';
        public static readonly char[] InvalidPathChars = {'\"', '<', '>', '|', '\0', (Char)1, (Char)2, (Char)3, (Char)4, (Char)5, (Char)6, (Char)7, (Char)8, (Char)9, (Char)10, (Char)11, (Char)12, (Char)13, (Char)14, (Char)15, (Char)16, (Char)17, (Char)18, (Char)19, (Char)20, (Char)21, (Char)22, (Char)23, (Char)24, (Char)25, (Char)26, (Char)27, (Char)28, (Char)29, (Char)30, (Char)31};
        internal static readonly char[] TrimEndChars = {(char)0x9, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20, (char)0x85, (char)0xA0};
        private static readonly char[] RealInvalidPathChars = {'\"', '<', '>', '|', '\0', (Char)1, (Char)2, (Char)3, (Char)4, (Char)5, (Char)6, (Char)7, (Char)8, (Char)9, (Char)10, (Char)11, (Char)12, (Char)13, (Char)14, (Char)15, (Char)16, (Char)17, (Char)18, (Char)19, (Char)20, (Char)21, (Char)22, (Char)23, (Char)24, (Char)25, (Char)26, (Char)27, (Char)28, (Char)29, (Char)30, (Char)31};
        private static readonly char[] InvalidPathCharsWithAdditionalChecks = {'\"', '<', '>', '|', '\0', (Char)1, (Char)2, (Char)3, (Char)4, (Char)5, (Char)6, (Char)7, (Char)8, (Char)9, (Char)10, (Char)11, (Char)12, (Char)13, (Char)14, (Char)15, (Char)16, (Char)17, (Char)18, (Char)19, (Char)20, (Char)21, (Char)22, (Char)23, (Char)24, (Char)25, (Char)26, (Char)27, (Char)28, (Char)29, (Char)30, (Char)31, '*', '?'};
        private static readonly char[] InvalidFileNameChars = {'\"', '<', '>', '|', '\0', (Char)1, (Char)2, (Char)3, (Char)4, (Char)5, (Char)6, (Char)7, (Char)8, (Char)9, (Char)10, (Char)11, (Char)12, (Char)13, (Char)14, (Char)15, (Char)16, (Char)17, (Char)18, (Char)19, (Char)20, (Char)21, (Char)22, (Char)23, (Char)24, (Char)25, (Char)26, (Char)27, (Char)28, (Char)29, (Char)30, (Char)31, ':', '*', '?', '\\', '/'};
        public static readonly char PathSeparator = ';';
        internal static readonly int MaxPath = 260;
        private static readonly int MaxDirectoryLength = 255;
        internal const int MAX_PATH = 260;
        internal const int MAX_DIRECTORY_PATH = 248;
        public static String ChangeExtension(String path, String extension)
        {
            if (path != null)
            {
                CheckInvalidPathChars(path);
                String s = path;
                for (int i = path.Length; --i >= 0;)
                {
                    char ch = path[i];
                    if (ch == '.')
                    {
                        s = path.Substring(0, i);
                        break;
                    }

                    if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                        break;
                }

                if (extension != null && path.Length != 0)
                {
                    if (extension.Length == 0 || extension[0] != '.')
                    {
                        s = s + ".";
                    }

                    s = s + extension;
                }

                return s;
            }

            return null;
        }

        public static String GetDirectoryName(String path)
        {
            if (path != null)
            {
                CheckInvalidPathChars(path);
                if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                {
                    string normalizedPath = NormalizePath(path, false);
                    if (path.Length > 0)
                    {
                        try
                        {
                            string tempPath = Path.RemoveLongPathPrefix(path);
                            int pos = 0;
                            while (pos < tempPath.Length && (tempPath[pos] != '?' && tempPath[pos] != '*'))
                                pos++;
                            if (pos > 0)
                                Path.GetFullPath(tempPath.Substring(0, pos));
                        }
                        catch (SecurityException)
                        {
                            if (path.IndexOf("~", StringComparison.Ordinal) != -1)
                            {
                                normalizedPath = NormalizePath(path, false, false);
                            }
                        }
                        catch (PathTooLongException)
                        {
                        }
                        catch (NotSupportedException)
                        {
                        }
                        catch (IOException)
                        {
                        }
                        catch (ArgumentException)
                        {
                        }
                    }

                    path = normalizedPath;
                }

                int root = GetRootLength(path);
                int i = path.Length;
                if (i > root)
                {
                    i = path.Length;
                    if (i == root)
                        return null;
                    while (i > root && path[--i] != DirectorySeparatorChar && path[i] != AltDirectorySeparatorChar)
                        ;
                    String dir = path.Substring(0, i);
                    if (CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
                    {
                        if (dir.Length >= MaxPath - 1)
                            throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                    }

                    return dir;
                }
            }

            return null;
        }

        internal static int GetRootLength(String path)
        {
            CheckInvalidPathChars(path);
            int i = 0;
            int length = path.Length;
            if (length >= 1 && (IsDirectorySeparator(path[0])))
            {
                i = 1;
                if (length >= 2 && (IsDirectorySeparator(path[1])))
                {
                    i = 2;
                    int n = 2;
                    while (i < length && ((path[i] != DirectorySeparatorChar && path[i] != AltDirectorySeparatorChar) || --n > 0))
                        i++;
                }
            }
            else if (length >= 2 && path[1] == VolumeSeparatorChar)
            {
                i = 2;
                if (length >= 3 && (IsDirectorySeparator(path[2])))
                    i++;
            }

            return i;
        }

        internal static bool IsDirectorySeparator(char c)
        {
            return (c == DirectorySeparatorChar || c == AltDirectorySeparatorChar);
        }

        public static char[] GetInvalidPathChars()
        {
            return (char[])RealInvalidPathChars.Clone();
        }

        public static char[] GetInvalidFileNameChars()
        {
            return (char[])InvalidFileNameChars.Clone();
        }

        public static String GetExtension(String path)
        {
            if (path == null)
                return null;
            CheckInvalidPathChars(path);
            int length = path.Length;
            for (int i = length; --i >= 0;)
            {
                char ch = path[i];
                if (ch == '.')
                {
                    if (i != length - 1)
                        return path.Substring(i, length - i);
                    else
                        return String.Empty;
                }

                if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                    break;
            }

            return String.Empty;
        }

        public static String GetFullPath(String path)
        {
            String fullPath = GetFullPathInternal(path);
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, path, fullPath);
            state.EnsureState();
            return fullPath;
        }

        internal static String UnsafeGetFullPath(String path)
        {
            String fullPath = GetFullPathInternal(path);
            return fullPath;
        }

        internal static String GetFullPathInternal(String path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Contract.EndContractBlock();
            String newPath = NormalizePath(path, true);
            return newPath;
        }

        internal unsafe static String NormalizePath(String path, bool fullCheck)
        {
            return NormalizePath(path, fullCheck, MaxPath);
        }

        internal unsafe static String NormalizePath(String path, bool fullCheck, bool expandShortPaths)
        {
            return NormalizePath(path, fullCheck, MaxPath, expandShortPaths);
        }

        internal unsafe static String NormalizePath(String path, bool fullCheck, int maxPathLength)
        {
            return NormalizePath(path, fullCheck, maxPathLength, true);
        }

        internal unsafe static String NormalizePath(String path, bool fullCheck, int maxPathLength, bool expandShortPaths)
        {
            Contract.Requires(path != null, "path can't be null");
            if (fullCheck)
            {
                path = path.TrimEnd(TrimEndChars);
                CheckInvalidPathChars(path);
            }

            int index = 0;
            PathHelper newBuffer;
            if (path.Length + 1 <= MaxPath)
            {
                char *m_arrayPtr = stackalloc char[MaxPath];
                newBuffer = new PathHelper(m_arrayPtr, MaxPath);
            }
            else
            {
                newBuffer = new PathHelper(path.Length + Path.MaxPath, maxPathLength);
            }

            uint numSpaces = 0;
            uint numDots = 0;
            bool fixupDirectorySeparator = false;
            uint numSigChars = 0;
            int lastSigChar = -1;
            bool startedWithVolumeSeparator = false;
            bool firstSegment = true;
            int lastDirectorySeparatorPos = 0;
            bool mightBeShortFileName = false;
            if (path.Length > 0 && (path[0] == DirectorySeparatorChar || path[0] == AltDirectorySeparatorChar))
            {
                newBuffer.Append('\\');
                index++;
                lastSigChar = 0;
            }

            while (index < path.Length)
            {
                char currentChar = path[index];
                if (currentChar == DirectorySeparatorChar || currentChar == AltDirectorySeparatorChar)
                {
                    if (numSigChars == 0)
                    {
                        if (numDots > 0)
                        {
                            int start = lastSigChar + 1;
                            if (path[start] != '.')
                                throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                            if (numDots >= 2)
                            {
                                if (startedWithVolumeSeparator && numDots > 2)
                                    throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                                if (path[start + 1] == '.')
                                {
                                    for (int i = start + 2; i < start + numDots; i++)
                                    {
                                        if (path[i] != '.')
                                            throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                                    }

                                    numDots = 2;
                                }
                                else
                                {
                                    if (numDots > 1)
                                        throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                                    numDots = 1;
                                }
                            }

                            if (numDots == 2)
                            {
                                newBuffer.Append('.');
                            }

                            newBuffer.Append('.');
                            fixupDirectorySeparator = false;
                        }

                        if (numSpaces > 0 && firstSegment)
                        {
                            if (index + 1 < path.Length && (path[index + 1] == DirectorySeparatorChar || path[index + 1] == AltDirectorySeparatorChar))
                            {
                                newBuffer.Append(DirectorySeparatorChar);
                            }
                        }
                    }

                    numDots = 0;
                    numSpaces = 0;
                    if (!fixupDirectorySeparator)
                    {
                        fixupDirectorySeparator = true;
                        newBuffer.Append(DirectorySeparatorChar);
                    }

                    numSigChars = 0;
                    lastSigChar = index;
                    startedWithVolumeSeparator = false;
                    firstSegment = false;
                    if (mightBeShortFileName)
                    {
                        newBuffer.TryExpandShortFileName();
                        mightBeShortFileName = false;
                    }

                    int thisPos = newBuffer.Length - 1;
                    if (thisPos - lastDirectorySeparatorPos > MaxDirectoryLength)
                    {
                        throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                    }

                    lastDirectorySeparatorPos = thisPos;
                }
                else if (currentChar == '.')
                {
                    numDots++;
                }
                else if (currentChar == ' ')
                {
                    numSpaces++;
                }
                else
                {
                    if (currentChar == '~' && expandShortPaths)
                        mightBeShortFileName = true;
                    fixupDirectorySeparator = false;
                    if (firstSegment && currentChar == VolumeSeparatorChar)
                    {
                        char driveLetter = (index > 0) ? path[index - 1] : ' ';
                        bool validPath = ((numDots == 0) && (numSigChars >= 1) && (driveLetter != ' '));
                        if (!validPath)
                            throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                        startedWithVolumeSeparator = true;
                        if (numSigChars > 1)
                        {
                            int spaceCount = 0;
                            while ((spaceCount < newBuffer.Length) && newBuffer[spaceCount] == ' ')
                                spaceCount++;
                            if (numSigChars - spaceCount == 1)
                            {
                                newBuffer.Length = 0;
                                newBuffer.Append(driveLetter);
                            }
                        }

                        numSigChars = 0;
                    }
                    else
                    {
                        numSigChars += 1 + numDots + numSpaces;
                    }

                    if (numDots > 0 || numSpaces > 0)
                    {
                        int numCharsToCopy = (lastSigChar >= 0) ? index - lastSigChar - 1 : index;
                        if (numCharsToCopy > 0)
                        {
                            for (int i = 0; i < numCharsToCopy; i++)
                            {
                                newBuffer.Append(path[lastSigChar + 1 + i]);
                            }
                        }

                        numDots = 0;
                        numSpaces = 0;
                    }

                    newBuffer.Append(currentChar);
                    lastSigChar = index;
                }

                index++;
            }

            if (newBuffer.Length - 1 - lastDirectorySeparatorPos > MaxDirectoryLength)
            {
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
            }

            if (numSigChars == 0)
            {
                if (numDots > 0)
                {
                    int start = lastSigChar + 1;
                    if (path[start] != '.')
                        throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                    if (numDots >= 2)
                    {
                        if (startedWithVolumeSeparator && numDots > 2)
                            throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                        if (path[start + 1] == '.')
                        {
                            for (int i = start + 2; i < start + numDots; i++)
                            {
                                if (path[i] != '.')
                                    throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                            }

                            numDots = 2;
                        }
                        else
                        {
                            if (numDots > 1)
                                throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
                            numDots = 1;
                        }
                    }

                    if (numDots == 2)
                    {
                        newBuffer.Append('.');
                    }

                    newBuffer.Append('.');
                }
            }

            if (newBuffer.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegal"));
            if (fullCheck)
            {
                if (newBuffer.OrdinalStartsWith("http:", false) || newBuffer.OrdinalStartsWith("file:", false))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_PathUriFormatNotSupported"));
                }
            }

            if (mightBeShortFileName)
            {
                newBuffer.TryExpandShortFileName();
            }

            int result = 1;
            if (fullCheck)
            {
                result = newBuffer.GetFullPathName();
                mightBeShortFileName = false;
                for (int i = 0; i < newBuffer.Length && !mightBeShortFileName; i++)
                {
                    if (newBuffer[i] == '~' && expandShortPaths)
                        mightBeShortFileName = true;
                }

                if (mightBeShortFileName)
                {
                    bool r = newBuffer.TryExpandShortFileName();
                    if (!r)
                    {
                        int lastSlash = -1;
                        for (int i = newBuffer.Length - 1; i >= 0; i--)
                        {
                            if (newBuffer[i] == DirectorySeparatorChar)
                            {
                                lastSlash = i;
                                break;
                            }
                        }

                        if (lastSlash >= 0)
                        {
                            if (newBuffer.Length >= maxPathLength)
                                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
                            int lenSavedName = newBuffer.Length - lastSlash - 1;
                            Contract.Assert(lastSlash < newBuffer.Length, "path unexpectedly ended in a '\'");
                            newBuffer.Fixup(lenSavedName, lastSlash);
                        }
                    }
                }
            }

            if (result != 0)
            {
                if (newBuffer.Length > 1 && newBuffer[0] == '\\' && newBuffer[1] == '\\')
                {
                    int startIndex = 2;
                    while (startIndex < result)
                    {
                        if (newBuffer[startIndex] == '\\')
                        {
                            startIndex++;
                            break;
                        }
                        else
                        {
                            startIndex++;
                        }
                    }

                    if (startIndex == result)
                        throw new ArgumentException(Environment.GetResourceString("Arg_PathIllegalUNC"));
                    if (newBuffer.OrdinalStartsWith("\\\\?\\globalroot", true))
                        throw new ArgumentException(Environment.GetResourceString("Arg_PathGlobalRoot"));
                }
            }

            if (newBuffer.Length >= maxPathLength)
                throw new PathTooLongException(Environment.GetResourceString("IO.PathTooLong"));
            if (result == 0)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == 0)
                    errorCode = Win32Native.ERROR_BAD_PATHNAME;
                __Error.WinIOError(errorCode, path);
                return null;
            }

            return newBuffer.ToStringOrExisting(path);
        }

        internal const int MaxLongPath = 32000;
        private const string LongPathPrefix = @"\\?\";
        private const string UNCPathPrefix = @"\\";
        private const string UNCLongPathPrefixToInsert = @"?\UNC\";
        private const string UNCLongPathPrefix = @"\\?\UNC\";
        internal unsafe static bool HasLongPathPrefix(String path)
        {
            return path.StartsWith(LongPathPrefix, StringComparison.Ordinal);
        }

        internal unsafe static String AddLongPathPrefix(String path)
        {
            if (path.StartsWith(LongPathPrefix, StringComparison.Ordinal))
                return path;
            if (path.StartsWith(UNCPathPrefix, StringComparison.Ordinal))
                return path.Insert(2, UNCLongPathPrefixToInsert);
            return LongPathPrefix + path;
        }

        internal unsafe static String RemoveLongPathPrefix(String path)
        {
            if (!path.StartsWith(LongPathPrefix, StringComparison.Ordinal))
                return path;
            if (path.StartsWith(UNCLongPathPrefix, StringComparison.OrdinalIgnoreCase))
                return path.Remove(2, 6);
            return path.Substring(4);
        }

        internal unsafe static StringBuilder RemoveLongPathPrefix(StringBuilder pathSB)
        {
            string path = pathSB.ToString();
            if (!path.StartsWith(LongPathPrefix, StringComparison.Ordinal))
                return pathSB;
            if (path.StartsWith(UNCLongPathPrefix, StringComparison.OrdinalIgnoreCase))
                return pathSB.Remove(2, 6);
            return pathSB.Remove(0, 4);
        }

        public static String GetFileName(String path)
        {
            if (path != null)
            {
                CheckInvalidPathChars(path);
                int length = path.Length;
                for (int i = length; --i >= 0;)
                {
                    char ch = path[i];
                    if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                        return path.Substring(i + 1, length - i - 1);
                }
            }

            return path;
        }

        public static String GetFileNameWithoutExtension(String path)
        {
            path = GetFileName(path);
            if (path != null)
            {
                int i;
                if ((i = path.LastIndexOf('.')) == -1)
                    return path;
                else
                    return path.Substring(0, i);
            }

            return null;
        }

        public static String GetPathRoot(String path)
        {
            if (path == null)
                return null;
            path = NormalizePath(path, false);
            return path.Substring(0, GetRootLength(path));
        }

        public static String GetTempPath()
        {
            StringBuilder sb = new StringBuilder(MaxPath);
            uint r = Win32Native.GetTempPath(MaxPath, sb);
            String path = sb.ToString();
            if (r == 0)
                __Error.WinIOError();
            path = GetFullPathInternal(path);
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, String.Empty, path);
            state.EnsureState();
            return path;
        }

        internal static bool IsRelative(string path)
        {
            Contract.Assert(path != null, "path can't be null");
            if ((path.Length >= 3 && path[1] == VolumeSeparatorChar && path[2] == DirectorySeparatorChar && ((path[0] >= 'a' && path[0] <= 'z') || (path[0] >= 'A' && path[0] <= 'Z'))) || (path.Length >= 2 && path[0] == '\\' && path[1] == '\\'))
                return false;
            else
                return true;
        }

        public static String GetRandomFileName()
        {
            byte[] key = new byte[10];
            RNGCryptoServiceProvider rng = null;
            try
            {
                rng = new RNGCryptoServiceProvider();
                rng.GetBytes(key);
            }
            finally
            {
                if (rng != null)
                {
                    rng.Dispose();
                }
            }

            char[] rndCharArray = Path.ToBase32StringSuitableForDirName(key).ToCharArray();
            rndCharArray[8] = '.';
            return new String(rndCharArray, 0, 12);
        }

        public static String GetTempFileName()
        {
            return InternalGetTempFileName(true);
        }

        internal static String UnsafeGetTempFileName()
        {
            return InternalGetTempFileName(false);
        }

        private static String InternalGetTempFileName(bool checkHost)
        {
            String path = GetTempPath();
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, String.Empty, path);
                state.EnsureState();
            }

            StringBuilder sb = new StringBuilder(MaxPath);
            uint r = Win32Native.GetTempFileName(path, "tmp", 0, sb);
            if (r == 0)
                __Error.WinIOError();
            return sb.ToString();
        }

        public static bool HasExtension(String path)
        {
            if (path != null)
            {
                CheckInvalidPathChars(path);
                for (int i = path.Length; --i >= 0;)
                {
                    char ch = path[i];
                    if (ch == '.')
                    {
                        if (i != path.Length - 1)
                            return true;
                        else
                            return false;
                    }

                    if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                        break;
                }
            }

            return false;
        }

        public static bool IsPathRooted(String path)
        {
            if (path != null)
            {
                CheckInvalidPathChars(path);
                int length = path.Length;
                if ((length >= 1 && (path[0] == DirectorySeparatorChar || path[0] == AltDirectorySeparatorChar)) || (length >= 2 && path[1] == VolumeSeparatorChar))
                    return true;
            }

            return false;
        }

        public static String Combine(String path1, String path2)
        {
            if (path1 == null || path2 == null)
                throw new ArgumentNullException((path1 == null) ? "path1" : "path2");
            Contract.EndContractBlock();
            CheckInvalidPathChars(path1);
            CheckInvalidPathChars(path2);
            return CombineNoChecks(path1, path2);
        }

        public static String Combine(String path1, String path2, String path3)
        {
            if (path1 == null || path2 == null || path3 == null)
                throw new ArgumentNullException((path1 == null) ? "path1" : (path2 == null) ? "path2" : "path3");
            Contract.EndContractBlock();
            CheckInvalidPathChars(path1);
            CheckInvalidPathChars(path2);
            CheckInvalidPathChars(path3);
            return CombineNoChecks(CombineNoChecks(path1, path2), path3);
        }

        public static String Combine(String path1, String path2, String path3, String path4)
        {
            if (path1 == null || path2 == null || path3 == null || path4 == null)
                throw new ArgumentNullException((path1 == null) ? "path1" : (path2 == null) ? "path2" : (path3 == null) ? "path3" : "path4");
            Contract.EndContractBlock();
            CheckInvalidPathChars(path1);
            CheckInvalidPathChars(path2);
            CheckInvalidPathChars(path3);
            CheckInvalidPathChars(path4);
            return CombineNoChecks(CombineNoChecks(CombineNoChecks(path1, path2), path3), path4);
        }

        public static String Combine(params String[] paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException("paths");
            }

            Contract.EndContractBlock();
            int finalSize = 0;
            int firstComponent = 0;
            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i] == null)
                {
                    throw new ArgumentNullException("paths");
                }

                if (paths[i].Length == 0)
                {
                    continue;
                }

                CheckInvalidPathChars(paths[i]);
                if (Path.IsPathRooted(paths[i]))
                {
                    firstComponent = i;
                    finalSize = paths[i].Length;
                }
                else
                {
                    finalSize += paths[i].Length;
                }

                char ch = paths[i][paths[i].Length - 1];
                if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar)
                    finalSize++;
            }

            StringBuilder finalPath = StringBuilderCache.Acquire(finalSize);
            for (int i = firstComponent; i < paths.Length; i++)
            {
                if (paths[i].Length == 0)
                {
                    continue;
                }

                if (finalPath.Length == 0)
                {
                    finalPath.Append(paths[i]);
                }
                else
                {
                    char ch = finalPath[finalPath.Length - 1];
                    if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar)
                    {
                        finalPath.Append(DirectorySeparatorChar);
                    }

                    finalPath.Append(paths[i]);
                }
            }

            return StringBuilderCache.GetStringAndRelease(finalPath);
        }

        private static String CombineNoChecks(String path1, String path2)
        {
            if (path2.Length == 0)
                return path1;
            if (path1.Length == 0)
                return path2;
            if (IsPathRooted(path2))
                return path2;
            char ch = path1[path1.Length - 1];
            if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar)
                return path1 + DirectorySeparatorCharAsString + path2;
            return path1 + path2;
        }

        private static readonly Char[] s_Base32Char = {'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5'};
        internal static String ToBase32StringSuitableForDirName(byte[] buff)
        {
            Contract.Assert(((buff.Length % 5) == 0), "Unexpected hash length");
            StringBuilder sb = StringBuilderCache.Acquire();
            byte b0, b1, b2, b3, b4;
            int l, i;
            l = buff.Length;
            i = 0;
            do
            {
                b0 = (i < l) ? buff[i++] : (byte)0;
                b1 = (i < l) ? buff[i++] : (byte)0;
                b2 = (i < l) ? buff[i++] : (byte)0;
                b3 = (i < l) ? buff[i++] : (byte)0;
                b4 = (i < l) ? buff[i++] : (byte)0;
                sb.Append(s_Base32Char[b0 & 0x1F]);
                sb.Append(s_Base32Char[b1 & 0x1F]);
                sb.Append(s_Base32Char[b2 & 0x1F]);
                sb.Append(s_Base32Char[b3 & 0x1F]);
                sb.Append(s_Base32Char[b4 & 0x1F]);
                sb.Append(s_Base32Char[(((b0 & 0xE0) >> 5) | ((b3 & 0x60) >> 2))]);
                sb.Append(s_Base32Char[(((b1 & 0xE0) >> 5) | ((b4 & 0x60) >> 2))]);
                b2 >>= 5;
                Contract.Assert(((b2 & 0xF8) == 0), "Unexpected set bits");
                if ((b3 & 0x80) != 0)
                    b2 |= 0x08;
                if ((b4 & 0x80) != 0)
                    b2 |= 0x10;
                sb.Append(s_Base32Char[b2]);
            }
            while (i < l);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        internal static void CheckSearchPattern(String searchPattern)
        {
            int index;
            while ((index = searchPattern.IndexOf("..", StringComparison.Ordinal)) != -1)
            {
                if (index + 2 == searchPattern.Length)
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidSearchPattern"));
                if ((searchPattern[index + 2] == DirectorySeparatorChar) || (searchPattern[index + 2] == AltDirectorySeparatorChar))
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidSearchPattern"));
                searchPattern = searchPattern.Substring(index + 2);
            }
        }

        internal static bool HasIllegalCharacters(String path, bool checkAdditional)
        {
            Contract.Requires(path != null);
            if (checkAdditional)
            {
                return path.IndexOfAny(InvalidPathCharsWithAdditionalChecks) >= 0;
            }

            return path.IndexOfAny(RealInvalidPathChars) >= 0;
        }

        internal static void CheckInvalidPathChars(String path, bool checkAdditional = false)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (Path.HasIllegalCharacters(path, checkAdditional))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPathChars"));
        }

        internal static String InternalCombine(String path1, String path2)
        {
            if (path1 == null || path2 == null)
                throw new ArgumentNullException((path1 == null) ? "path1" : "path2");
            Contract.EndContractBlock();
            CheckInvalidPathChars(path1);
            CheckInvalidPathChars(path2);
            if (path2.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_PathEmpty"), "path2");
            if (IsPathRooted(path2))
                throw new ArgumentException(Environment.GetResourceString("Arg_Path2IsRooted"), "path2");
            int i = path1.Length;
            if (i == 0)
                return path2;
            char ch = path1[i - 1];
            if (ch != DirectorySeparatorChar && ch != AltDirectorySeparatorChar && ch != VolumeSeparatorChar)
                return path1 + DirectorySeparatorCharAsString + path2;
            return path1 + path2;
        }
    }
}