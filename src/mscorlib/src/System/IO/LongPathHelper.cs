// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace System.IO
{
    /// <summary>
    /// Wrapper to help with path normalization.
    /// </summary>
    internal class LongPathHelper
    {

    /// <summary>
    /// Normalize the given path.
    /// </summary>
    /// <remarks>
    /// Normalizes via Win32 GetFullPathName(). It will also trim all "typical" whitespace at the end of the path (see s_trimEndChars). Will also trim initial
    /// spaces if the path is determined to be rooted.
    /// 
    /// Note that invalid characters will be checked after the path is normalized, which could remove bad characters. (C:\|\..\a.txt -- C:\a.txt)
    /// </remarks>
    /// <param name="path">Path to normalize</param>
    /// <param name="checkInvalidCharacters">True to check for invalid characters</param>
    /// <param name="expandShortPaths">Attempt to expand short paths if true</param>
    /// <exception cref="ArgumentException">Thrown if the path is an illegal UNC (does not contain a full server/share) or contains illegal characters.</exception>
    /// <exception cref="PathTooLongException">Thrown if the path or a path segment exceeds the filesystem limits.</exception>
    /// <exception cref="FileNotFoundException">Thrown if Windows returns ERROR_FILE_NOT_FOUND. (See Win32Marshal.GetExceptionForWin32Error)</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown if Windows returns ERROR_PATH_NOT_FOUND. (See Win32Marshal.GetExceptionForWin32Error)</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if Windows returns ERROR_ACCESS_DENIED. (See Win32Marshal.GetExceptionForWin32Error)</exception>
    /// <exception cref="IOException">Thrown if Windows returns an error that doesn't map to the above. (See Win32Marshal.GetExceptionForWin32Error)</exception>
    /// <returns>Normalized path</returns>
    [System.Security.SecurityCritical]
    unsafe internal static string Normalize( string path, uint maxPathLength, bool checkInvalidCharacters, bool expandShortPaths ) {
      throw new NotImplementedException();
    }

        [System.Security.SecurityCritical]
        unsafe internal static string GetLongPathName(string path)
        {
          return null;
        }

        [System.Security.SecurityCritical]
        private static void GetErrorAndThrow(string path)
        {
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode == 0)
                errorCode = Win32Native.ERROR_BAD_PATHNAME;
            __Error.WinIOError(errorCode, path);
        }

        // It is significantly more complicated to get the long path with minimal allocations if we're injecting the extended dos path prefix. The implicit version
        // should match up with what is in CoreFx System.Runtime.Extensions.
#if !FEATURE_IMPLICIT_LONGPATH
#else // !FEATURE_IMPLICIT_LONGPATH

        private static uint GetInputBuffer(StringBuffer content, bool isDosUnc, out StringBuffer buffer)
        {
            uint length = content.Length;

            length += isDosUnc ? (uint)PathInternal.UncExtendedPrefixToInsert.Length : (uint)PathInternal.ExtendedPathPrefix.Length;
            buffer = new StringBuffer(length);

            if (isDosUnc)
            {
                buffer.CopyFrom(bufferIndex: 0, source: PathInternal.UncExtendedPathPrefix);
                uint prefixDifference = (uint)(PathInternal.UncExtendedPathPrefix.Length - PathInternal.UncPathPrefix.Length);
                content.CopyTo(bufferIndex: prefixDifference, destination: buffer, destinationIndex: (uint)PathInternal.ExtendedPathPrefix.Length, count: content.Length - prefixDifference);
                return prefixDifference;
            }
            else
            {
                uint prefixSize = (uint)PathInternal.ExtendedPathPrefix.Length;
                buffer.CopyFrom(bufferIndex: 0, source: PathInternal.ExtendedPathPrefix);
                content.CopyTo(bufferIndex: 0, destination: buffer, destinationIndex: prefixSize, count: content.Length);
                return prefixSize;
            }
        }

        [System.Security.SecuritySafeCritical]
        private static string TryExpandShortFileName(StringBuffer outputBuffer, string originalPath)
        {
            // We'll have one of a few cases by now (the normalized path will have already:
            //
            //  1. Dos path (C:\)
            //  2. Dos UNC (\\Server\Share)
            //  3. Dos device path (\\.\C:\, \\?\C:\)
            //
            // We want to put the extended syntax on the front if it doesn't already have it, which may mean switching from \\.\.

            uint rootLength = PathInternal.GetRootLength(outputBuffer);
            bool isDevice = PathInternal.IsDevice(outputBuffer);

            StringBuffer inputBuffer = null;
            bool isDosUnc = false;
            uint rootDifference = 0;
            bool wasDotDevice = false;

            // Add the extended prefix before expanding to allow growth over MAX_PATH
            if (isDevice)
            {
                // We have one of the following (\\?\ or \\.\)
                // We will never get \??\ here as GetFullPathName() does not recognize \??\ and will return it as C:\??\ (or whatever the current drive is).
                inputBuffer = new StringBuffer();
                inputBuffer.Append(outputBuffer);

                if (outputBuffer[2] == '.')
                {
                    wasDotDevice = true;
                    inputBuffer[2] = '?';
                }
            }
            else
            {
                // \\Server\Share, but not \\.\ or \\?\.
                // We need to know this to be able to push \\?\UNC\ on if required
                isDosUnc = outputBuffer.Length > 1 && outputBuffer[0] == '\\' && outputBuffer[1] == '\\' && !PathInternal.IsDevice(outputBuffer);
                rootDifference = GetInputBuffer(outputBuffer, isDosUnc, out inputBuffer);
            }

            rootLength += rootDifference;
            uint inputLength = inputBuffer.Length;

            bool success = false;
            uint foundIndex = inputBuffer.Length - 1;

            while (!success)
            {
                uint result = Win32Native.GetLongPathNameW(inputBuffer.GetHandle(), outputBuffer.GetHandle(), outputBuffer.CharCapacity);

                // Replace any temporary null we added
                if (inputBuffer[foundIndex] == '\0') inputBuffer[foundIndex] = '\\';

                if (result == 0)
                {
                    // Look to see if we couldn't find the file
                    int error = Marshal.GetLastWin32Error();
                    if (error != Win32Native.ERROR_FILE_NOT_FOUND && error != Win32Native.ERROR_PATH_NOT_FOUND)
                    {
                        // Some other failure, give up
                        break;
                    }

                    // We couldn't find the path at the given index, start looking further back in the string.
                    foundIndex--;

                    for (; foundIndex > rootLength && inputBuffer[foundIndex] != '\\'; foundIndex--) ;
                    if (foundIndex == rootLength)
                    {
                        // Can't trim the path back any further
                        break;
                    }
                    else
                    {
                        // Temporarily set a null in the string to get Windows to look further up the path
                        inputBuffer[foundIndex] = '\0';
                    }
                }
                else if (result > outputBuffer.CharCapacity)
                {
                    // Not enough space. The result count for this API does not include the null terminator.
                    outputBuffer.EnsureCharCapacity(result);
                    result = Win32Native.GetLongPathNameW(inputBuffer.GetHandle(), outputBuffer.GetHandle(), outputBuffer.CharCapacity);
                }
                else
                {
                    // Found the path
                    success = true;
                    outputBuffer.Length = result;
                    if (foundIndex < inputLength - 1)
                    {
                        // It was a partial find, put the non-existent part of the path back
                        outputBuffer.Append(inputBuffer, foundIndex, inputBuffer.Length - foundIndex);
                    }
                }
            }

            // Strip out the prefix and return the string
            StringBuffer bufferToUse = success ? outputBuffer : inputBuffer;
            if (wasDotDevice)
                bufferToUse[2] = '.';

            string returnValue = null;

            int newLength = (int)(bufferToUse.Length - rootDifference);
            if (isDosUnc)
            {
                // Need to go from \\?\UNC\ to \\?\UN\\
                bufferToUse[(uint)PathInternal.UncExtendedPathPrefix.Length - 1] = '\\';
            }

            // We now need to strip out any added characters at the front of the string
            if (bufferToUse.SubstringEquals(originalPath, rootDifference, newLength))
            {
                // Use the original path to avoid allocating
                returnValue = originalPath;
            }
            else
            {
                returnValue = bufferToUse.Substring(rootDifference, newLength);
            }

            inputBuffer.Dispose();
            return returnValue;
        }
#endif // FEATURE_IMPLICIT_LONGPATH
    }
}