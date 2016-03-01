namespace System.Globalization
{
    using System;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security;
    using System.Threading;
    using System.Diagnostics.Contracts;

    internal static class EncodingTable
    {
        private static int lastEncodingItem = GetNumEncodingItems() - 1;
        private static volatile int lastCodePageItem;
        unsafe internal static InternalEncodingDataItem*encodingDataPtr = GetEncodingData();
        unsafe internal static InternalCodePageDataItem*codePageDataPtr = GetCodePageData();
        private static Hashtable hashByName = Hashtable.Synchronized(new Hashtable(StringComparer.OrdinalIgnoreCase));
        private static Hashtable hashByCodePage = Hashtable.Synchronized(new Hashtable());
        static EncodingTable()
        {
        }

        unsafe private static int internalGetCodePageFromName(String name)
        {
            int left = 0;
            int right = lastEncodingItem;
            int index;
            int result;
            while ((right - left) > 3)
            {
                index = ((right - left) / 2) + left;
                result = String.nativeCompareOrdinalIgnoreCaseWC(name, encodingDataPtr[index].webName);
                if (result == 0)
                {
                    return (encodingDataPtr[index].codePage);
                }
                else if (result < 0)
                {
                    right = index;
                }
                else
                {
                    left = index;
                }
            }

            for (; left <= right; left++)
            {
                if (String.nativeCompareOrdinalIgnoreCaseWC(name, encodingDataPtr[left].webName) == 0)
                {
                    return (encodingDataPtr[left].codePage);
                }
            }

            throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_EncodingNotSupported"), name), "name");
        }

        internal static unsafe EncodingInfo[] GetEncodings()
        {
            if (lastCodePageItem == 0)
            {
                int count;
                for (count = 0; codePageDataPtr[count].codePage != 0; count++)
                {
                }

                lastCodePageItem = count;
            }

            EncodingInfo[] arrayEncodingInfo = new EncodingInfo[lastCodePageItem];
            int i;
            for (i = 0; i < lastCodePageItem; i++)
            {
                arrayEncodingInfo[i] = new EncodingInfo(codePageDataPtr[i].codePage, CodePageDataItem.CreateString(codePageDataPtr[i].Names, 0), Environment.GetResourceString("Globalization.cp_" + codePageDataPtr[i].codePage));
            }

            return arrayEncodingInfo;
        }

        internal static int GetCodePageFromName(String name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            Contract.EndContractBlock();
            Object codePageObj;
            codePageObj = hashByName[name];
            if (codePageObj != null)
            {
                return ((int)codePageObj);
            }

            int codePage = internalGetCodePageFromName(name);
            hashByName[name] = codePage;
            return codePage;
        }

        unsafe internal static CodePageDataItem GetCodePageDataItem(int codepage)
        {
            CodePageDataItem dataItem;
            dataItem = (CodePageDataItem)hashByCodePage[codepage];
            if (dataItem != null)
            {
                return dataItem;
            }

            int i = 0;
            int data;
            while ((data = codePageDataPtr[i].codePage) != 0)
            {
                if (data == codepage)
                {
                    dataItem = new CodePageDataItem(i);
                    hashByCodePage[codepage] = dataItem;
                    return (dataItem);
                }

                i++;
            }

            return null;
        }

        private unsafe static extern InternalEncodingDataItem*GetEncodingData();
        private static extern int GetNumEncodingItems();
        private unsafe static extern InternalCodePageDataItem*GetCodePageData();
        internal unsafe static extern byte *nativeCreateOpenFileMapping(String inSectionName, int inBytesToAllocate, out IntPtr mappedFileHandle);
    }

    internal unsafe struct InternalEncodingDataItem
    {
        internal sbyte *webName;
        internal UInt16 codePage;
    }

    internal unsafe struct InternalCodePageDataItem
    {
        internal UInt16 codePage;
        internal UInt16 uiFamilyCodePage;
        internal uint flags;
        internal sbyte *Names;
    }
}