namespace System.Text
{
    using System;
    using System.Security;
    using System.Globalization;
    using System.Text;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    public enum NormalizationForm
    {
    }

    internal enum ExtendedNormalizationForms
    {
        FormIdna = 0xd,
        FormIdnaDisallowUnassigned = 0x10d
    }

    internal class Normalization
    {
        private static volatile bool IDNA;
        private static volatile bool IDNADisallowUnassigned;
        private static volatile bool Other;
        private const int ERROR_SUCCESS = 0;
        private const int ERROR_NOT_ENOUGH_MEMORY = 8;
        private const int ERROR_INVALID_PARAMETER = 87;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        private const int ERROR_NO_UNICODE_TRANSLATION = 1113;
        static private unsafe void InitializeForm(NormalizationForm form, String strDataFile)
        {
            byte *pTables = null;
            if (!Environment.IsWindows8OrAbove)
            {
                if (strDataFile == null)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNormalizationForm"));
                }

                pTables = GlobalizationAssembly.GetGlobalizationResourceBytePtr(typeof (Normalization).Assembly, strDataFile);
                if (pTables == null)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNormalizationForm"));
                }
            }

            nativeNormalizationInitNormalization(form, pTables);
        }

        static private void EnsureInitialized(NormalizationForm form)
        {
            switch ((ExtendedNormalizationForms)form)
            {
                case ExtendedNormalizationForms.FormIdna:
                    if (IDNA)
                        return;
                    InitializeForm(form, "normidna.nlp");
                    IDNA = true;
                    break;
                case ExtendedNormalizationForms.FormIdnaDisallowUnassigned:
                    if (IDNADisallowUnassigned)
                        return;
                    InitializeForm(form, "normidna.nlp");
                    IDNADisallowUnassigned = true;
                    break;
                default:
                    if (Other)
                        return;
                    InitializeForm(form, null);
                    Other = true;
                    break;
            }
        }

        internal static bool IsNormalized(String strInput, NormalizationForm normForm)
        {
            Contract.Requires(strInput != null);
            EnsureInitialized(normForm);
            int iError = ERROR_SUCCESS;
            bool result = nativeNormalizationIsNormalizedString(normForm, ref iError, strInput, strInput.Length);
            switch (iError)
            {
                case ERROR_SUCCESS:
                    break;
                case ERROR_INVALID_PARAMETER:
                case ERROR_NO_UNICODE_TRANSLATION:
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"), "strInput");
                case ERROR_NOT_ENOUGH_MEMORY:
                    throw new OutOfMemoryException(Environment.GetResourceString("Arg_OutOfMemoryException"));
                default:
                    throw new InvalidOperationException(Environment.GetResourceString("UnknownError_Num", iError));
            }

            return result;
        }

        internal static String Normalize(String strInput, NormalizationForm normForm)
        {
            Contract.Requires(strInput != null);
            EnsureInitialized(normForm);
            int iError = ERROR_SUCCESS;
            int iLength = nativeNormalizationNormalizeString(normForm, ref iError, strInput, strInput.Length, null, 0);
            if (iError != ERROR_SUCCESS)
            {
                if (iError == ERROR_INVALID_PARAMETER)
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequenceNoIndex"), "strInput");
                if (iError == ERROR_NOT_ENOUGH_MEMORY)
                    throw new OutOfMemoryException(Environment.GetResourceString("Arg_OutOfMemoryException"));
                throw new InvalidOperationException(Environment.GetResourceString("UnknownError_Num", iError));
            }

            if (iLength == 0)
                return String.Empty;
            char[] cBuffer = null;
            for (;;)
            {
                cBuffer = new char[iLength];
                iLength = nativeNormalizationNormalizeString(normForm, ref iError, strInput, strInput.Length, cBuffer, cBuffer.Length);
                if (iError == ERROR_SUCCESS)
                    break;
                switch (iError)
                {
                    case ERROR_INSUFFICIENT_BUFFER:
                        Contract.Assert(iLength > cBuffer.Length, "Buffer overflow should have iLength > cBuffer.Length");
                        continue;
                    case ERROR_INVALID_PARAMETER:
                    case ERROR_NO_UNICODE_TRANSLATION:
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidCharSequence", iLength), "strInput");
                    case ERROR_NOT_ENOUGH_MEMORY:
                        throw new OutOfMemoryException(Environment.GetResourceString("Arg_OutOfMemoryException"));
                    default:
                        throw new InvalidOperationException(Environment.GetResourceString("UnknownError_Num", iError));
                }
            }

            return new String(cBuffer, 0, iLength);
        }

        unsafe private static extern int nativeNormalizationNormalizeString(NormalizationForm normForm, ref int iError, String lpSrcString, int cwSrcLength, char[] lpDstString, int cwDstLength);
        unsafe private static extern bool nativeNormalizationIsNormalizedString(NormalizationForm normForm, ref int iError, String lpString, int cwLength);
        unsafe private static extern void nativeNormalizationInitNormalization(NormalizationForm normForm, byte *pTableData);
    }
}