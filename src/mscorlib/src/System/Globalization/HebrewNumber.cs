namespace System.Globalization
{
    using System;
    using System.Text;
    using System.Diagnostics.Contracts;

    internal struct HebrewNumberParsingContext
    {
        internal HebrewNumber.HS state;
        internal int result;
        public HebrewNumberParsingContext(int result)
        {
            state = HebrewNumber.HS.Start;
            this.result = result;
        }
    }

    internal enum HebrewNumberParsingState
    {
        InvalidHebrewNumber,
        NotHebrewDigit,
        FoundEndOfHebrewNumber,
        ContinueParsing
    }

    internal class HebrewNumber
    {
        private HebrewNumber()
        {
        }

        internal static String ToString(int Number)
        {
            char cTens = '\x0';
            char cUnits;
            int Hundreds, Tens;
            StringBuilder szHebrew = new StringBuilder();
            if (Number > 5000)
            {
                Number -= 5000;
            }

            Contract.Assert(Number > 0 && Number <= 999, "Number is out of range.");
            ;
            Hundreds = Number / 100;
            if (Hundreds > 0)
            {
                Number -= Hundreds * 100;
                for (int i = 0; i < (Hundreds / 4); i++)
                {
                    szHebrew.Append('\x05ea');
                }

                int remains = Hundreds % 4;
                if (remains > 0)
                {
                    szHebrew.Append((char)((int)'\x05e6' + remains));
                }
            }

            Tens = Number / 10;
            Number %= 10;
            switch (Tens)
            {
                case (0):
                    cTens = '\x0';
                    break;
                case (1):
                    cTens = '\x05d9';
                    break;
                case (2):
                    cTens = '\x05db';
                    break;
                case (3):
                    cTens = '\x05dc';
                    break;
                case (4):
                    cTens = '\x05de';
                    break;
                case (5):
                    cTens = '\x05e0';
                    break;
                case (6):
                    cTens = '\x05e1';
                    break;
                case (7):
                    cTens = '\x05e2';
                    break;
                case (8):
                    cTens = '\x05e4';
                    break;
                case (9):
                    cTens = '\x05e6';
                    break;
            }

            cUnits = (char)(Number > 0 ? ((int)'\x05d0' + Number - 1) : 0);
            if ((cUnits == '\x05d4') && (cTens == '\x05d9'))
            {
                cUnits = '\x05d5';
                cTens = '\x05d8';
            }

            if ((cUnits == '\x05d5') && (cTens == '\x05d9'))
            {
                cUnits = '\x05d6';
                cTens = '\x05d8';
            }

            if (cTens != '\x0')
            {
                szHebrew.Append(cTens);
            }

            if (cUnits != '\x0')
            {
                szHebrew.Append(cUnits);
            }

            if (szHebrew.Length > 1)
            {
                szHebrew.Insert(szHebrew.Length - 1, '"');
            }
            else
            {
                szHebrew.Append('\'');
            }

            return (szHebrew.ToString());
        }

        enum HebrewToken
        {
            Invalid = -1,
            Digit400 = 0,
            Digit200_300 = 1,
            Digit100 = 2,
            Digit10 = 3,
            Digit1 = 4,
            Digit6_7 = 5,
            Digit7 = 6,
            Digit9 = 7,
            SingleQuote = 8,
            DoubleQuote = 9
        }

        ;
        class HebrewValue
        {
            internal HebrewToken token;
            internal int value;
            internal HebrewValue(HebrewToken token, int value)
            {
                this.token = token;
                this.value = value;
            }
        }

        static HebrewValue[] HebrewValues = {new HebrewValue(HebrewToken.Digit1, 1), new HebrewValue(HebrewToken.Digit1, 2), new HebrewValue(HebrewToken.Digit1, 3), new HebrewValue(HebrewToken.Digit1, 4), new HebrewValue(HebrewToken.Digit1, 5), new HebrewValue(HebrewToken.Digit6_7, 6), new HebrewValue(HebrewToken.Digit6_7, 7), new HebrewValue(HebrewToken.Digit1, 8), new HebrewValue(HebrewToken.Digit9, 9), new HebrewValue(HebrewToken.Digit10, 10), new HebrewValue(HebrewToken.Invalid, -1), new HebrewValue(HebrewToken.Digit10, 20), new HebrewValue(HebrewToken.Digit10, 30), new HebrewValue(HebrewToken.Invalid, -1), new HebrewValue(HebrewToken.Digit10, 40), new HebrewValue(HebrewToken.Invalid, -1), new HebrewValue(HebrewToken.Digit10, 50), new HebrewValue(HebrewToken.Digit10, 60), new HebrewValue(HebrewToken.Digit10, 70), new HebrewValue(HebrewToken.Invalid, -1), new HebrewValue(HebrewToken.Digit10, 80), new HebrewValue(HebrewToken.Invalid, -1), new HebrewValue(HebrewToken.Digit10, 90), new HebrewValue(HebrewToken.Digit100, 100), new HebrewValue(HebrewToken.Digit200_300, 200), new HebrewValue(HebrewToken.Digit200_300, 300), new HebrewValue(HebrewToken.Digit400, 400), };
        const int minHebrewNumberCh = 0x05d0;
        static char maxHebrewNumberCh = (char)(minHebrewNumberCh + HebrewValues.Length - 1);
        internal enum HS
        {
            _err = -1,
            Start = 0,
            S400 = 1,
            S400_400 = 2,
            S400_X00 = 3,
            S400_X0 = 4,
            X00_DQ = 5,
            S400_X00_X0 = 6,
            X0_DQ = 7,
            X = 8,
            X0 = 9,
            X00 = 10,
            S400_DQ = 11,
            S400_400_DQ = 12,
            S400_400_100 = 13,
            S9 = 14,
            X00_S9 = 15,
            S9_DQ = 16,
            END = 100
        }

        readonly static HS[][] NumberPasingState = {new HS[]{HS.S400, HS.X00, HS.X00, HS.X0, HS.X, HS.X, HS.X, HS.S9, HS._err, HS._err}, new HS[]{HS.S400_400, HS.S400_X00, HS.S400_X00, HS.S400_X0, HS._err, HS._err, HS._err, HS.X00_S9, HS.END, HS.S400_DQ}, new HS[]{HS._err, HS._err, HS.S400_400_100, HS.S400_X0, HS._err, HS._err, HS._err, HS.X00_S9, HS._err, HS.S400_400_DQ}, new HS[]{HS._err, HS._err, HS._err, HS.S400_X00_X0, HS._err, HS._err, HS._err, HS.X00_S9, HS._err, HS.X00_DQ}, new HS[]{HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS.X0_DQ}, new HS[]{HS._err, HS._err, HS._err, HS.END, HS.END, HS.END, HS.END, HS.END, HS._err, HS._err}, new HS[]{HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS.X0_DQ}, new HS[]{HS._err, HS._err, HS._err, HS._err, HS.END, HS.END, HS.END, HS.END, HS._err, HS._err}, new HS[]{HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS.END, HS._err}, new HS[]{HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS.END, HS.X0_DQ}, new HS[]{HS._err, HS._err, HS._err, HS.S400_X0, HS._err, HS._err, HS._err, HS.X00_S9, HS.END, HS.X00_DQ}, new HS[]{HS.END, HS.END, HS.END, HS.END, HS.END, HS.END, HS.END, HS.END, HS._err, HS._err}, new HS[]{HS._err, HS._err, HS.END, HS.END, HS.END, HS.END, HS.END, HS.END, HS._err, HS._err}, new HS[]{HS._err, HS._err, HS._err, HS.S400_X00_X0, HS._err, HS._err, HS._err, HS.X00_S9, HS._err, HS.X00_DQ}, new HS[]{HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS.END, HS.S9_DQ}, new HS[]{HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS._err, HS.S9_DQ}, new HS[]{HS._err, HS._err, HS._err, HS._err, HS._err, HS.END, HS.END, HS._err, HS._err, HS._err}, };
        internal static HebrewNumberParsingState ParseByChar(char ch, ref HebrewNumberParsingContext context)
        {
            HebrewToken token;
            if (ch == '\'')
            {
                token = HebrewToken.SingleQuote;
            }
            else if (ch == '\"')
            {
                token = HebrewToken.DoubleQuote;
            }
            else
            {
                int index = (int)ch - minHebrewNumberCh;
                if (index >= 0 && index < HebrewValues.Length)
                {
                    token = HebrewValues[index].token;
                    if (token == HebrewToken.Invalid)
                    {
                        return (HebrewNumberParsingState.NotHebrewDigit);
                    }

                    context.result += HebrewValues[index].value;
                }
                else
                {
                    return (HebrewNumberParsingState.NotHebrewDigit);
                }
            }

            context.state = NumberPasingState[(int)context.state][(int)token];
            if (context.state == HS._err)
            {
                return (HebrewNumberParsingState.InvalidHebrewNumber);
            }

            if (context.state == HS.END)
            {
                return (HebrewNumberParsingState.FoundEndOfHebrewNumber);
            }

            return (HebrewNumberParsingState.ContinueParsing);
        }

        internal static bool IsDigit(char ch)
        {
            if (ch >= minHebrewNumberCh && ch <= maxHebrewNumberCh)
            {
                return (HebrewValues[ch - minHebrewNumberCh].value >= 0);
            }

            return (ch == '\'' || ch == '\"');
        }
    }
}