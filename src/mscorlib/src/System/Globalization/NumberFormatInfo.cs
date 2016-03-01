namespace System.Globalization
{
    using System.Security.Permissions;
    using System.Runtime.Serialization;
    using System.Text;
    using System;
    using System.Diagnostics.Contracts;

    sealed public class NumberFormatInfo : ICloneable, IFormatProvider
    {
        private static volatile NumberFormatInfo invariantInfo;
        internal int[] numberGroupSizes = new int[]{3};
        internal int[] currencyGroupSizes = new int[]{3};
        internal int[] percentGroupSizes = new int[]{3};
        internal String positiveSign = "+";
        internal String negativeSign = "-";
        internal String numberDecimalSeparator = ".";
        internal String numberGroupSeparator = ",";
        internal String currencyGroupSeparator = ",";
        internal String currencyDecimalSeparator = ".";
        internal String currencySymbol = "\x00a4";
        internal String ansiCurrencySymbol = null;
        internal String nanSymbol = "NaN";
        internal String positiveInfinitySymbol = "Infinity";
        internal String negativeInfinitySymbol = "-Infinity";
        internal String percentDecimalSeparator = ".";
        internal String percentGroupSeparator = ",";
        internal String percentSymbol = "%";
        internal String perMilleSymbol = "\u2030";
        internal String[] nativeDigits = {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9"};
        internal int m_dataItem = 0;
        internal int numberDecimalDigits = 2;
        internal int currencyDecimalDigits = 2;
        internal int currencyPositivePattern = 0;
        internal int currencyNegativePattern = 0;
        internal int numberNegativePattern = 1;
        internal int percentPositivePattern = 0;
        internal int percentNegativePattern = 0;
        internal int percentDecimalDigits = 2;
        internal bool isReadOnly = false;
        internal bool m_useUserOverride = false;
        internal bool m_isInvariant = false;
        public NumberFormatInfo(): this (null)
        {
        }

        private void OnSerializing(StreamingContext ctx)
        {
        }

        private void OnDeserializing(StreamingContext ctx)
        {
        }

        private void OnDeserialized(StreamingContext ctx)
        {
        }

        static private void VerifyDecimalSeparator(String decSep, String propertyName)
        {
            if (decSep == null)
            {
                throw new ArgumentNullException(propertyName, Environment.GetResourceString("ArgumentNull_String"));
            }

            if (decSep.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyDecString"));
            }

            Contract.EndContractBlock();
        }

        static private void VerifyGroupSeparator(String groupSep, String propertyName)
        {
            if (groupSep == null)
            {
                throw new ArgumentNullException(propertyName, Environment.GetResourceString("ArgumentNull_String"));
            }

            Contract.EndContractBlock();
        }

        static private void VerifyNativeDigits(String[] nativeDig, String propertyName)
        {
            if (nativeDig == null)
            {
                throw new ArgumentNullException(propertyName, Environment.GetResourceString("ArgumentNull_Array"));
            }

            if (nativeDig.Length != 10)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNativeDigitCount"), propertyName);
            }

            Contract.EndContractBlock();
            for (int i = 0; i < nativeDig.Length; i++)
            {
                if (nativeDig[i] == null)
                {
                    throw new ArgumentNullException(propertyName, Environment.GetResourceString("ArgumentNull_ArrayValue"));
                }

                if (nativeDig[i].Length != 1)
                {
                    if (nativeDig[i].Length != 2)
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNativeDigitValue"), propertyName);
                    }
                    else if (!char.IsSurrogatePair(nativeDig[i][0], nativeDig[i][1]))
                    {
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNativeDigitValue"), propertyName);
                    }
                }

                if (CharUnicodeInfo.GetDecimalDigitValue(nativeDig[i], 0) != i && CharUnicodeInfo.GetUnicodeCategory(nativeDig[i], 0) != UnicodeCategory.PrivateUse)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNativeDigitValue"), propertyName);
                }
            }
        }

        internal NumberFormatInfo(CultureData cultureData)
        {
            if (cultureData != null)
            {
                cultureData.GetNFIValues(this);
                if (cultureData.IsInvariantCulture)
                {
                    this.m_isInvariant = true;
                }
            }
        }

        private void VerifyWritable()
        {
            if (isReadOnly)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
            }

            Contract.EndContractBlock();
        }

        public static NumberFormatInfo InvariantInfo
        {
            get
            {
                if (invariantInfo == null)
                {
                    NumberFormatInfo nfi = new NumberFormatInfo();
                    nfi.m_isInvariant = true;
                    invariantInfo = ReadOnly(nfi);
                }

                return invariantInfo;
            }
        }

        public static NumberFormatInfo GetInstance(IFormatProvider formatProvider)
        {
            NumberFormatInfo info;
            CultureInfo cultureProvider = formatProvider as CultureInfo;
            if (cultureProvider != null && !cultureProvider.m_isInherited)
            {
                info = cultureProvider.numInfo;
                if (info != null)
                {
                    return info;
                }
                else
                {
                    return cultureProvider.NumberFormat;
                }
            }

            info = formatProvider as NumberFormatInfo;
            if (info != null)
            {
                return info;
            }

            if (formatProvider != null)
            {
                info = formatProvider.GetFormat(typeof (NumberFormatInfo)) as NumberFormatInfo;
                if (info != null)
                {
                    return info;
                }
            }

            return CurrentInfo;
        }

        public Object Clone()
        {
            NumberFormatInfo n = (NumberFormatInfo)MemberwiseClone();
            n.isReadOnly = false;
            return n;
        }

        public int CurrencyDecimalDigits
        {
            get
            {
                return currencyDecimalDigits;
            }

            set
            {
                if (value < 0 || value > 99)
                {
                    throw new ArgumentOutOfRangeException("CurrencyDecimalDigits", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, 99));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                currencyDecimalDigits = value;
            }
        }

        public String CurrencyDecimalSeparator
        {
            get
            {
                return currencyDecimalSeparator;
            }

            set
            {
                VerifyWritable();
                VerifyDecimalSeparator(value, "CurrencyDecimalSeparator");
                currencyDecimalSeparator = value;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return isReadOnly;
            }
        }

        static internal void CheckGroupSize(String propName, int[] groupSize)
        {
            for (int i = 0; i < groupSize.Length; i++)
            {
                if (groupSize[i] < 1)
                {
                    if (i == groupSize.Length - 1 && groupSize[i] == 0)
                        return;
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGroupSize"), propName);
                }
                else if (groupSize[i] > 9)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_InvalidGroupSize"), propName);
                }
            }
        }

        public int[] CurrencyGroupSizes
        {
            get
            {
                return ((int[])currencyGroupSizes.Clone());
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("CurrencyGroupSizes", Environment.GetResourceString("ArgumentNull_Obj"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                Int32[] inputSizes = (Int32[])value.Clone();
                CheckGroupSize("CurrencyGroupSizes", inputSizes);
                currencyGroupSizes = inputSizes;
            }
        }

        public int[] NumberGroupSizes
        {
            get
            {
                return ((int[])numberGroupSizes.Clone());
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("NumberGroupSizes", Environment.GetResourceString("ArgumentNull_Obj"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                Int32[] inputSizes = (Int32[])value.Clone();
                CheckGroupSize("NumberGroupSizes", inputSizes);
                numberGroupSizes = inputSizes;
            }
        }

        public int[] PercentGroupSizes
        {
            get
            {
                return ((int[])percentGroupSizes.Clone());
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("PercentGroupSizes", Environment.GetResourceString("ArgumentNull_Obj"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                Int32[] inputSizes = (Int32[])value.Clone();
                CheckGroupSize("PercentGroupSizes", inputSizes);
                percentGroupSizes = inputSizes;
            }
        }

        public String CurrencyGroupSeparator
        {
            get
            {
                return currencyGroupSeparator;
            }

            set
            {
                VerifyWritable();
                VerifyGroupSeparator(value, "CurrencyGroupSeparator");
                currencyGroupSeparator = value;
            }
        }

        public String CurrencySymbol
        {
            get
            {
                return currencySymbol;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("CurrencySymbol", Environment.GetResourceString("ArgumentNull_String"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                currencySymbol = value;
            }
        }

        public static NumberFormatInfo CurrentInfo
        {
            get
            {
                System.Globalization.CultureInfo culture = System.Threading.Thread.CurrentThread.CurrentCulture;
                if (!culture.m_isInherited)
                {
                    NumberFormatInfo info = culture.numInfo;
                    if (info != null)
                    {
                        return info;
                    }
                }

                return ((NumberFormatInfo)culture.GetFormat(typeof (NumberFormatInfo)));
            }
        }

        public String NaNSymbol
        {
            get
            {
                return nanSymbol;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("NaNSymbol", Environment.GetResourceString("ArgumentNull_String"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                nanSymbol = value;
            }
        }

        public int CurrencyNegativePattern
        {
            get
            {
                return currencyNegativePattern;
            }

            set
            {
                if (value < 0 || value > 15)
                {
                    throw new ArgumentOutOfRangeException("CurrencyNegativePattern", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, 15));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                currencyNegativePattern = value;
            }
        }

        public int NumberNegativePattern
        {
            get
            {
                return numberNegativePattern;
            }

            set
            {
                if (value < 0 || value > 4)
                {
                    throw new ArgumentOutOfRangeException("NumberNegativePattern", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, 4));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                numberNegativePattern = value;
            }
        }

        public int PercentPositivePattern
        {
            get
            {
                return percentPositivePattern;
            }

            set
            {
                if (value < 0 || value > 3)
                {
                    throw new ArgumentOutOfRangeException("PercentPositivePattern", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, 3));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                percentPositivePattern = value;
            }
        }

        public int PercentNegativePattern
        {
            get
            {
                return percentNegativePattern;
            }

            set
            {
                if (value < 0 || value > 11)
                {
                    throw new ArgumentOutOfRangeException("PercentNegativePattern", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, 11));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                percentNegativePattern = value;
            }
        }

        public String NegativeInfinitySymbol
        {
            get
            {
                return negativeInfinitySymbol;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("NegativeInfinitySymbol", Environment.GetResourceString("ArgumentNull_String"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                negativeInfinitySymbol = value;
            }
        }

        public String NegativeSign
        {
            get
            {
                return negativeSign;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("NegativeSign", Environment.GetResourceString("ArgumentNull_String"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                negativeSign = value;
            }
        }

        public int NumberDecimalDigits
        {
            get
            {
                return numberDecimalDigits;
            }

            set
            {
                if (value < 0 || value > 99)
                {
                    throw new ArgumentOutOfRangeException("NumberDecimalDigits", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, 99));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                numberDecimalDigits = value;
            }
        }

        public String NumberDecimalSeparator
        {
            get
            {
                return numberDecimalSeparator;
            }

            set
            {
                VerifyWritable();
                VerifyDecimalSeparator(value, "NumberDecimalSeparator");
                numberDecimalSeparator = value;
            }
        }

        public String NumberGroupSeparator
        {
            get
            {
                return numberGroupSeparator;
            }

            set
            {
                VerifyWritable();
                VerifyGroupSeparator(value, "NumberGroupSeparator");
                numberGroupSeparator = value;
            }
        }

        public int CurrencyPositivePattern
        {
            get
            {
                return currencyPositivePattern;
            }

            set
            {
                if (value < 0 || value > 3)
                {
                    throw new ArgumentOutOfRangeException("CurrencyPositivePattern", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, 3));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                currencyPositivePattern = value;
            }
        }

        public String PositiveInfinitySymbol
        {
            get
            {
                return positiveInfinitySymbol;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("PositiveInfinitySymbol", Environment.GetResourceString("ArgumentNull_String"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                positiveInfinitySymbol = value;
            }
        }

        public String PositiveSign
        {
            get
            {
                return positiveSign;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("PositiveSign", Environment.GetResourceString("ArgumentNull_String"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                positiveSign = value;
            }
        }

        public int PercentDecimalDigits
        {
            get
            {
                return percentDecimalDigits;
            }

            set
            {
                if (value < 0 || value > 99)
                {
                    throw new ArgumentOutOfRangeException("PercentDecimalDigits", String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, 99));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                percentDecimalDigits = value;
            }
        }

        public String PercentDecimalSeparator
        {
            get
            {
                return percentDecimalSeparator;
            }

            set
            {
                VerifyWritable();
                VerifyDecimalSeparator(value, "PercentDecimalSeparator");
                percentDecimalSeparator = value;
            }
        }

        public String PercentGroupSeparator
        {
            get
            {
                return percentGroupSeparator;
            }

            set
            {
                VerifyWritable();
                VerifyGroupSeparator(value, "PercentGroupSeparator");
                percentGroupSeparator = value;
            }
        }

        public String PercentSymbol
        {
            get
            {
                return percentSymbol;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("PercentSymbol", Environment.GetResourceString("ArgumentNull_String"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                percentSymbol = value;
            }
        }

        public String PerMilleSymbol
        {
            get
            {
                return perMilleSymbol;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("PerMilleSymbol", Environment.GetResourceString("ArgumentNull_String"));
                }

                Contract.EndContractBlock();
                VerifyWritable();
                perMilleSymbol = value;
            }
        }

        public String[] NativeDigits
        {
            get
            {
                return (String[])nativeDigits.Clone();
            }

            set
            {
                VerifyWritable();
                VerifyNativeDigits(value, "NativeDigits");
                nativeDigits = value;
            }
        }

        public Object GetFormat(Type formatType)
        {
            return formatType == typeof (NumberFormatInfo) ? this : null;
        }

        public static NumberFormatInfo ReadOnly(NumberFormatInfo nfi)
        {
            if (nfi == null)
            {
                throw new ArgumentNullException("nfi");
            }

            Contract.EndContractBlock();
            if (nfi.IsReadOnly)
            {
                return (nfi);
            }

            NumberFormatInfo info = (NumberFormatInfo)(nfi.MemberwiseClone());
            info.isReadOnly = true;
            return info;
        }

        private const NumberStyles InvalidNumberStyles = ~(NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign | NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowExponent | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowHexSpecifier);
        internal static void ValidateParseStyleInteger(NumberStyles style)
        {
            if ((style & InvalidNumberStyles) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNumberStyles"), "style");
            }

            Contract.EndContractBlock();
            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if ((style & ~NumberStyles.HexNumber) != 0)
                {
                    throw new ArgumentException(Environment.GetResourceString("Arg_InvalidHexStyle"));
                }
            }
        }

        internal static void ValidateParseStyleFloatingPoint(NumberStyles style)
        {
            if ((style & InvalidNumberStyles) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNumberStyles"), "style");
            }

            Contract.EndContractBlock();
            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_HexStyleNotSupported"));
            }
        }
    }
}