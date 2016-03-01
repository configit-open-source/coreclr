namespace System.Globalization
{
    using System;
    using System.Globalization;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    internal enum FORMATFLAGS
    {
        None = 0x00000000,
        UseGenitiveMonth = 0x00000001,
        UseLeapYearMonth = 0x00000002,
        UseSpacesInMonthNames = 0x00000004,
        UseHebrewParsing = 0x00000008,
        UseSpacesInDayNames = 0x00000010,
        UseDigitPrefixInTokens = 0x00000020
    }

    internal enum CalendarId : ushort
    {
        GREGORIAN = 1,
        GREGORIAN_US = 2,
        JAPAN = 3,
        TAIWAN = 4,
        KOREA = 5,
        HIJRI = 6,
        THAI = 7,
        HEBREW = 8,
        GREGORIAN_ME_FRENCH = 9,
        GREGORIAN_ARABIC = 10,
        GREGORIAN_XLIT_ENGLISH = 11,
        GREGORIAN_XLIT_FRENCH = 12,
        JULIAN = 13,
        JAPANESELUNISOLAR = 14,
        CHINESELUNISOLAR = 15,
        SAKA = 16,
        LUNAR_ETO_CHN = 17,
        LUNAR_ETO_KOR = 18,
        LUNAR_ETO_ROKUYOU = 19,
        KOREANLUNISOLAR = 20,
        TAIWANLUNISOLAR = 21,
        PERSIAN = 22,
        UMALQURA = 23,
        LAST_CALENDAR = 23
    }

    internal class DateTimeFormatInfoScanner
    {
        internal const char MonthPostfixChar = '\xe000';
        internal const char IgnorableSymbolChar = '\xe001';
        internal const String CJKYearSuff = "\u5e74";
        internal const String CJKMonthSuff = "\u6708";
        internal const String CJKDaySuff = "\u65e5";
        internal const String KoreanYearSuff = "\ub144";
        internal const String KoreanMonthSuff = "\uc6d4";
        internal const String KoreanDaySuff = "\uc77c";
        internal const String KoreanHourSuff = "\uc2dc";
        internal const String KoreanMinuteSuff = "\ubd84";
        internal const String KoreanSecondSuff = "\ucd08";
        internal const String CJKHourSuff = "\u6642";
        internal const String ChineseHourSuff = "\u65f6";
        internal const String CJKMinuteSuff = "\u5206";
        internal const String CJKSecondSuff = "\u79d2";
        internal List<String> m_dateWords = new List<String>();
        private static volatile Dictionary<String, String> s_knownWords;
        static Dictionary<String, String> KnownWords
        {
            get
            {
                if (s_knownWords == null)
                {
                    Dictionary<String, String> temp = new Dictionary<String, String>();
                    temp.Add("/", String.Empty);
                    temp.Add("-", String.Empty);
                    temp.Add(".", String.Empty);
                    temp.Add(CJKYearSuff, String.Empty);
                    temp.Add(CJKMonthSuff, String.Empty);
                    temp.Add(CJKDaySuff, String.Empty);
                    temp.Add(KoreanYearSuff, String.Empty);
                    temp.Add(KoreanMonthSuff, String.Empty);
                    temp.Add(KoreanDaySuff, String.Empty);
                    temp.Add(KoreanHourSuff, String.Empty);
                    temp.Add(KoreanMinuteSuff, String.Empty);
                    temp.Add(KoreanSecondSuff, String.Empty);
                    temp.Add(CJKHourSuff, String.Empty);
                    temp.Add(ChineseHourSuff, String.Empty);
                    temp.Add(CJKMinuteSuff, String.Empty);
                    temp.Add(CJKSecondSuff, String.Empty);
                    s_knownWords = temp;
                }

                return (s_knownWords);
            }
        }

        internal static int SkipWhiteSpacesAndNonLetter(String pattern, int currentIndex)
        {
            while (currentIndex < pattern.Length)
            {
                char ch = pattern[currentIndex];
                if (ch == '\\')
                {
                    currentIndex++;
                    if (currentIndex < pattern.Length)
                    {
                        ch = pattern[currentIndex];
                        if (ch == '\'')
                        {
                            continue;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                if (Char.IsLetter(ch) || ch == '\'' || ch == '.')
                {
                    break;
                }

                currentIndex++;
            }

            return (currentIndex);
        }

        internal void AddDateWordOrPostfix(String formatPostfix, String str)
        {
            if (str.Length > 0)
            {
                if (str.Equals("."))
                {
                    AddIgnorableSymbols(".");
                    return;
                }

                String words;
                if (KnownWords.TryGetValue(str, out words) == false)
                {
                    if (m_dateWords == null)
                    {
                        m_dateWords = new List<String>();
                    }

                    if (formatPostfix == "MMMM")
                    {
                        String temp = MonthPostfixChar + str;
                        if (!m_dateWords.Contains(temp))
                        {
                            m_dateWords.Add(temp);
                        }
                    }
                    else
                    {
                        if (!m_dateWords.Contains(str))
                        {
                            m_dateWords.Add(str);
                        }

                        if (str[str.Length - 1] == '.')
                        {
                            String strWithoutDot = str.Substring(0, str.Length - 1);
                            if (!m_dateWords.Contains(strWithoutDot))
                            {
                                m_dateWords.Add(strWithoutDot);
                            }
                        }
                    }
                }
            }
        }

        internal int AddDateWords(String pattern, int index, String formatPostfix)
        {
            int newIndex = SkipWhiteSpacesAndNonLetter(pattern, index);
            if (newIndex != index && formatPostfix != null)
            {
                formatPostfix = null;
            }

            index = newIndex;
            StringBuilder dateWord = new StringBuilder();
            while (index < pattern.Length)
            {
                char ch = pattern[index];
                if (ch == '\'')
                {
                    AddDateWordOrPostfix(formatPostfix, dateWord.ToString());
                    index++;
                    break;
                }
                else if (ch == '\\')
                {
                    index++;
                    if (index < pattern.Length)
                    {
                        dateWord.Append(pattern[index]);
                        index++;
                    }
                }
                else if (Char.IsWhiteSpace(ch))
                {
                    AddDateWordOrPostfix(formatPostfix, dateWord.ToString());
                    if (formatPostfix != null)
                    {
                        formatPostfix = null;
                    }

                    dateWord.Length = 0;
                    index++;
                }
                else
                {
                    dateWord.Append(ch);
                    index++;
                }
            }

            return (index);
        }

        internal static int ScanRepeatChar(String pattern, char ch, int index, out int count)
        {
            count = 1;
            while (++index < pattern.Length && pattern[index] == ch)
            {
                count++;
            }

            return (index);
        }

        internal void AddIgnorableSymbols(String text)
        {
            if (m_dateWords == null)
            {
                m_dateWords = new List<String>();
            }

            String temp = IgnorableSymbolChar + text;
            if (!m_dateWords.Contains(temp))
            {
                m_dateWords.Add(temp);
            }
        }

        enum FoundDatePattern
        {
            None = 0x0000,
            FoundYearPatternFlag = 0x0001,
            FoundMonthPatternFlag = 0x0002,
            FoundDayPatternFlag = 0x0004,
            FoundYMDPatternFlag = 0x0007
        }

        FoundDatePattern m_ymdFlags = FoundDatePattern.None;
        internal void ScanDateWord(String pattern)
        {
            m_ymdFlags = FoundDatePattern.None;
            int i = 0;
            while (i < pattern.Length)
            {
                char ch = pattern[i];
                int chCount;
                switch (ch)
                {
                    case '\'':
                        i = AddDateWords(pattern, i + 1, null);
                        break;
                    case 'M':
                        i = ScanRepeatChar(pattern, 'M', i, out chCount);
                        if (chCount >= 4)
                        {
                            if (i < pattern.Length && pattern[i] == '\'')
                            {
                                i = AddDateWords(pattern, i + 1, "MMMM");
                            }
                        }

                        m_ymdFlags |= FoundDatePattern.FoundMonthPatternFlag;
                        break;
                    case 'y':
                        i = ScanRepeatChar(pattern, 'y', i, out chCount);
                        m_ymdFlags |= FoundDatePattern.FoundYearPatternFlag;
                        break;
                    case 'd':
                        i = ScanRepeatChar(pattern, 'd', i, out chCount);
                        if (chCount <= 2)
                        {
                            m_ymdFlags |= FoundDatePattern.FoundDayPatternFlag;
                        }

                        break;
                    case '\\':
                        i += 2;
                        break;
                    case '.':
                        if (m_ymdFlags == FoundDatePattern.FoundYMDPatternFlag)
                        {
                            AddIgnorableSymbols(".");
                            m_ymdFlags = FoundDatePattern.None;
                        }

                        i++;
                        break;
                    default:
                        if (m_ymdFlags == FoundDatePattern.FoundYMDPatternFlag && !Char.IsWhiteSpace(ch))
                        {
                            m_ymdFlags = FoundDatePattern.None;
                        }

                        i++;
                        break;
                }
            }
        }

        internal String[] GetDateWordsOfDTFI(DateTimeFormatInfo dtfi)
        {
            String[] datePatterns = dtfi.GetAllDateTimePatterns('D');
            int i;
            for (i = 0; i < datePatterns.Length; i++)
            {
                ScanDateWord(datePatterns[i]);
            }

            datePatterns = dtfi.GetAllDateTimePatterns('d');
            for (i = 0; i < datePatterns.Length; i++)
            {
                ScanDateWord(datePatterns[i]);
            }

            datePatterns = dtfi.GetAllDateTimePatterns('y');
            for (i = 0; i < datePatterns.Length; i++)
            {
                ScanDateWord(datePatterns[i]);
            }

            ScanDateWord(dtfi.MonthDayPattern);
            datePatterns = dtfi.GetAllDateTimePatterns('T');
            for (i = 0; i < datePatterns.Length; i++)
            {
                ScanDateWord(datePatterns[i]);
            }

            datePatterns = dtfi.GetAllDateTimePatterns('t');
            for (i = 0; i < datePatterns.Length; i++)
            {
                ScanDateWord(datePatterns[i]);
            }

            String[] result = null;
            if (m_dateWords != null && m_dateWords.Count > 0)
            {
                result = new String[m_dateWords.Count];
                for (i = 0; i < m_dateWords.Count; i++)
                {
                    result[i] = m_dateWords[i];
                }
            }

            return (result);
        }

        internal static FORMATFLAGS GetFormatFlagGenitiveMonth(String[] monthNames, String[] genitveMonthNames, String[] abbrevMonthNames, String[] genetiveAbbrevMonthNames)
        {
            return ((!EqualStringArrays(monthNames, genitveMonthNames) || !EqualStringArrays(abbrevMonthNames, genetiveAbbrevMonthNames)) ? FORMATFLAGS.UseGenitiveMonth : 0);
        }

        internal static FORMATFLAGS GetFormatFlagUseSpaceInMonthNames(String[] monthNames, String[] genitveMonthNames, String[] abbrevMonthNames, String[] genetiveAbbrevMonthNames)
        {
            FORMATFLAGS formatFlags = 0;
            formatFlags |= (ArrayElementsBeginWithDigit(monthNames) || ArrayElementsBeginWithDigit(genitveMonthNames) || ArrayElementsBeginWithDigit(abbrevMonthNames) || ArrayElementsBeginWithDigit(genetiveAbbrevMonthNames) ? FORMATFLAGS.UseDigitPrefixInTokens : 0);
            formatFlags |= (ArrayElementsHaveSpace(monthNames) || ArrayElementsHaveSpace(genitveMonthNames) || ArrayElementsHaveSpace(abbrevMonthNames) || ArrayElementsHaveSpace(genetiveAbbrevMonthNames) ? FORMATFLAGS.UseSpacesInMonthNames : 0);
            return (formatFlags);
        }

        internal static FORMATFLAGS GetFormatFlagUseSpaceInDayNames(String[] dayNames, String[] abbrevDayNames)
        {
            return ((ArrayElementsHaveSpace(dayNames) || ArrayElementsHaveSpace(abbrevDayNames)) ? FORMATFLAGS.UseSpacesInDayNames : 0);
        }

        internal static FORMATFLAGS GetFormatFlagUseHebrewCalendar(int calID)
        {
            return (calID == (int)CalendarId.HEBREW ? FORMATFLAGS.UseHebrewParsing | FORMATFLAGS.UseLeapYearMonth : 0);
        }

        private static bool EqualStringArrays(string[] array1, string[] array2)
        {
            if (array1 == array2)
            {
                return true;
            }

            if (array1.Length != array2.Length)
            {
                return false;
            }

            for (int i = 0; i < array1.Length; i++)
            {
                if (!array1[i].Equals(array2[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ArrayElementsHaveSpace(string[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array[i].Length; j++)
                {
                    if (Char.IsWhiteSpace(array[i][j]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ArrayElementsBeginWithDigit(string[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Length > 0 && array[i][0] >= '0' && array[i][0] <= '9')
                {
                    int index = 1;
                    while (index < array[i].Length && array[i][index] >= '0' && array[i][index] <= '9')
                    {
                        index++;
                    }

                    if (index == array[i].Length)
                    {
                        return (false);
                    }

                    if (index == array[i].Length - 1)
                    {
                        switch (array[i][index])
                        {
                            case '\x6708':
                            case '\xc6d4':
                                return (false);
                        }
                    }

                    if (index == array[i].Length - 4)
                    {
                        if (array[i][index] == '\'' && array[i][index + 1] == ' ' && array[i][index + 2] == '\x6708' && array[i][index + 3] == '\'')
                        {
                            return (false);
                        }
                    }

                    return (true);
                }
            }

            return false;
        }
    }
}