
using System.Globalization;

namespace System
{
    public struct TimeSpan : IComparable, IComparable<TimeSpan>, IEquatable<TimeSpan>, IFormattable
    {
        public const long TicksPerMillisecond = 10000;
        private const double MillisecondsPerTick = 1.0 / TicksPerMillisecond;
        public const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const double SecondsPerTick = 1.0 / TicksPerSecond;
        public const long TicksPerMinute = TicksPerSecond * 60;
        private const double MinutesPerTick = 1.0 / TicksPerMinute;
        public const long TicksPerHour = TicksPerMinute * 60;
        private const double HoursPerTick = 1.0 / TicksPerHour;
        public const long TicksPerDay = TicksPerHour * 24;
        private const double DaysPerTick = 1.0 / TicksPerDay;
        private const int MillisPerSecond = 1000;
        private const int MillisPerMinute = MillisPerSecond * 60;
        private const int MillisPerHour = MillisPerMinute * 60;
        private const int MillisPerDay = MillisPerHour * 24;
        internal const long MaxSeconds = Int64.MaxValue / TicksPerSecond;
        internal const long MinSeconds = Int64.MinValue / TicksPerSecond;
        internal const long MaxMilliSeconds = Int64.MaxValue / TicksPerMillisecond;
        internal const long MinMilliSeconds = Int64.MinValue / TicksPerMillisecond;
        internal const long TicksPerTenthSecond = TicksPerMillisecond * 100;
        public static readonly TimeSpan Zero = new TimeSpan(0);
        public static readonly TimeSpan MaxValue = new TimeSpan(Int64.MaxValue);
        public static readonly TimeSpan MinValue = new TimeSpan(Int64.MinValue);
        internal long _ticks;
        public TimeSpan(long ticks)
        {
            this._ticks = ticks;
        }

        public TimeSpan(int hours, int minutes, int seconds)
        {
            _ticks = TimeToTicks(hours, minutes, seconds);
        }

        public TimeSpan(int days, int hours, int minutes, int seconds): this (days, hours, minutes, seconds, 0)
        {
        }

        public TimeSpan(int days, int hours, int minutes, int seconds, int milliseconds)
        {
            Int64 totalMilliSeconds = ((Int64)days * 3600 * 24 + (Int64)hours * 3600 + (Int64)minutes * 60 + seconds) * 1000 + milliseconds;
            if (totalMilliSeconds > MaxMilliSeconds || totalMilliSeconds < MinMilliSeconds)
                throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("Overflow_TimeSpanTooLong"));
            _ticks = (long)totalMilliSeconds * TicksPerMillisecond;
        }

        public long Ticks
        {
            get
            {
                return _ticks;
            }
        }

        public int Days
        {
            get
            {
                return (int)(_ticks / TicksPerDay);
            }
        }

        public int Hours
        {
            get
            {
                return (int)((_ticks / TicksPerHour) % 24);
            }
        }

        public int Milliseconds
        {
            get
            {
                return (int)((_ticks / TicksPerMillisecond) % 1000);
            }
        }

        public int Minutes
        {
            get
            {
                return (int)((_ticks / TicksPerMinute) % 60);
            }
        }

        public int Seconds
        {
            get
            {
                return (int)((_ticks / TicksPerSecond) % 60);
            }
        }

        public double TotalDays
        {
            get
            {
                return ((double)_ticks) * DaysPerTick;
            }
        }

        public double TotalHours
        {
            get
            {
                return (double)_ticks * HoursPerTick;
            }
        }

        public double TotalMilliseconds
        {
            get
            {
                double temp = (double)_ticks * MillisecondsPerTick;
                if (temp > MaxMilliSeconds)
                    return (double)MaxMilliSeconds;
                if (temp < MinMilliSeconds)
                    return (double)MinMilliSeconds;
                return temp;
            }
        }

        public double TotalMinutes
        {
            get
            {
                return (double)_ticks * MinutesPerTick;
            }
        }

        public double TotalSeconds
        {
            get
            {
                return (double)_ticks * SecondsPerTick;
            }
        }

        public TimeSpan Add(TimeSpan ts)
        {
            long result = _ticks + ts._ticks;
            if ((_ticks >> 63 == ts._ticks >> 63) && (_ticks >> 63 != result >> 63))
                throw new OverflowException(Environment.GetResourceString("Overflow_TimeSpanTooLong"));
            return new TimeSpan(result);
        }

        public static int Compare(TimeSpan t1, TimeSpan t2)
        {
            if (t1._ticks > t2._ticks)
                return 1;
            if (t1._ticks < t2._ticks)
                return -1;
            return 0;
        }

        public int CompareTo(Object value)
        {
            if (value == null)
                return 1;
            if (!(value is TimeSpan))
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeTimeSpan"));
            long t = ((TimeSpan)value)._ticks;
            if (_ticks > t)
                return 1;
            if (_ticks < t)
                return -1;
            return 0;
        }

        public int CompareTo(TimeSpan value)
        {
            long t = value._ticks;
            if (_ticks > t)
                return 1;
            if (_ticks < t)
                return -1;
            return 0;
        }

        public static TimeSpan FromDays(double value)
        {
            return Interval(value, MillisPerDay);
        }

        public TimeSpan Duration()
        {
            if (Ticks == TimeSpan.MinValue.Ticks)
                throw new OverflowException(Environment.GetResourceString("Overflow_Duration"));
                        return new TimeSpan(_ticks >= 0 ? _ticks : -_ticks);
        }

        public override bool Equals(Object value)
        {
            if (value is TimeSpan)
            {
                return _ticks == ((TimeSpan)value)._ticks;
            }

            return false;
        }

        public bool Equals(TimeSpan obj)
        {
            return _ticks == obj._ticks;
        }

        public static bool Equals(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks == t2._ticks;
        }

        public override int GetHashCode()
        {
            return (int)_ticks ^ (int)(_ticks >> 32);
        }

        public static TimeSpan FromHours(double value)
        {
            return Interval(value, MillisPerHour);
        }

        private static TimeSpan Interval(double value, int scale)
        {
            if (Double.IsNaN(value))
                throw new ArgumentException(Environment.GetResourceString("Arg_CannotBeNaN"));
                        double tmp = value * scale;
            double millis = tmp + (value >= 0 ? 0.5 : -0.5);
            if ((millis > Int64.MaxValue / TicksPerMillisecond) || (millis < Int64.MinValue / TicksPerMillisecond))
                throw new OverflowException(Environment.GetResourceString("Overflow_TimeSpanTooLong"));
            return new TimeSpan((long)millis * TicksPerMillisecond);
        }

        public static TimeSpan FromMilliseconds(double value)
        {
            return Interval(value, 1);
        }

        public static TimeSpan FromMinutes(double value)
        {
            return Interval(value, MillisPerMinute);
        }

        public TimeSpan Negate()
        {
            if (Ticks == TimeSpan.MinValue.Ticks)
                throw new OverflowException(Environment.GetResourceString("Overflow_NegateTwosCompNum"));
                        return new TimeSpan(-_ticks);
        }

        public static TimeSpan FromSeconds(double value)
        {
            return Interval(value, MillisPerSecond);
        }

        public TimeSpan Subtract(TimeSpan ts)
        {
            long result = _ticks - ts._ticks;
            if ((_ticks >> 63 != ts._ticks >> 63) && (_ticks >> 63 != result >> 63))
                throw new OverflowException(Environment.GetResourceString("Overflow_TimeSpanTooLong"));
            return new TimeSpan(result);
        }

        public static TimeSpan FromTicks(long value)
        {
            return new TimeSpan(value);
        }

        internal static long TimeToTicks(int hour, int minute, int second)
        {
            long totalSeconds = (long)hour * 3600 + (long)minute * 60 + (long)second;
            if (totalSeconds > MaxSeconds || totalSeconds < MinSeconds)
                throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("Overflow_TimeSpanTooLong"));
            return totalSeconds * TicksPerSecond;
        }

        public static TimeSpan Parse(String s)
        {
            return TimeSpanParse.Parse(s, null);
        }

        public static TimeSpan Parse(String input, IFormatProvider formatProvider)
        {
            return TimeSpanParse.Parse(input, formatProvider);
        }

        public static TimeSpan ParseExact(String input, String format, IFormatProvider formatProvider)
        {
            return TimeSpanParse.ParseExact(input, format, formatProvider, TimeSpanStyles.None);
        }

        public static TimeSpan ParseExact(String input, String[] formats, IFormatProvider formatProvider)
        {
            return TimeSpanParse.ParseExactMultiple(input, formats, formatProvider, TimeSpanStyles.None);
        }

        public static TimeSpan ParseExact(String input, String format, IFormatProvider formatProvider, TimeSpanStyles styles)
        {
            TimeSpanParse.ValidateStyles(styles, "styles");
            return TimeSpanParse.ParseExact(input, format, formatProvider, styles);
        }

        public static TimeSpan ParseExact(String input, String[] formats, IFormatProvider formatProvider, TimeSpanStyles styles)
        {
            TimeSpanParse.ValidateStyles(styles, "styles");
            return TimeSpanParse.ParseExactMultiple(input, formats, formatProvider, styles);
        }

        public static Boolean TryParse(String s, out TimeSpan result)
        {
            return TimeSpanParse.TryParse(s, null, out result);
        }

        public static Boolean TryParse(String input, IFormatProvider formatProvider, out TimeSpan result)
        {
            return TimeSpanParse.TryParse(input, formatProvider, out result);
        }

        public static Boolean TryParseExact(String input, String format, IFormatProvider formatProvider, out TimeSpan result)
        {
            return TimeSpanParse.TryParseExact(input, format, formatProvider, TimeSpanStyles.None, out result);
        }

        public static Boolean TryParseExact(String input, String[] formats, IFormatProvider formatProvider, out TimeSpan result)
        {
            return TimeSpanParse.TryParseExactMultiple(input, formats, formatProvider, TimeSpanStyles.None, out result);
        }

        public static Boolean TryParseExact(String input, String format, IFormatProvider formatProvider, TimeSpanStyles styles, out TimeSpan result)
        {
            TimeSpanParse.ValidateStyles(styles, "styles");
            return TimeSpanParse.TryParseExact(input, format, formatProvider, styles, out result);
        }

        public static Boolean TryParseExact(String input, String[] formats, IFormatProvider formatProvider, TimeSpanStyles styles, out TimeSpan result)
        {
            TimeSpanParse.ValidateStyles(styles, "styles");
            return TimeSpanParse.TryParseExactMultiple(input, formats, formatProvider, styles, out result);
        }

        public override String ToString()
        {
            return TimeSpanFormat.Format(this, null, null);
        }

        public String ToString(String format)
        {
            return TimeSpanFormat.Format(this, format, null);
        }

        public String ToString(String format, IFormatProvider formatProvider)
        {
            if (LegacyMode)
            {
                return TimeSpanFormat.Format(this, null, null);
            }
            else
            {
                return TimeSpanFormat.Format(this, format, formatProvider);
            }
        }

        public static TimeSpan operator -(TimeSpan t)
        {
            if (t._ticks == TimeSpan.MinValue._ticks)
                throw new OverflowException(Environment.GetResourceString("Overflow_NegateTwosCompNum"));
            return new TimeSpan(-t._ticks);
        }

        public static TimeSpan operator -(TimeSpan t1, TimeSpan t2)
        {
            return t1.Subtract(t2);
        }

        public static TimeSpan operator +(TimeSpan t)
        {
            return t;
        }

        public static TimeSpan operator +(TimeSpan t1, TimeSpan t2)
        {
            return t1.Add(t2);
        }

        public static bool operator ==(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks == t2._ticks;
        }

        public static bool operator !=(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks != t2._ticks;
        }

        public static bool operator <(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks < t2._ticks;
        }

        public static bool operator <=(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks <= t2._ticks;
        }

        public static bool operator>(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks > t2._ticks;
        }

        public static bool operator >=(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks >= t2._ticks;
        }

        private static bool GetLegacyFormatMode()
        {
            return CompatibilitySwitches.IsAppEarlierThanSilverlight4;
        }

        private static volatile bool _legacyConfigChecked;
        private static volatile bool _legacyMode;
        private static bool LegacyMode
        {
            get
            {
                if (!_legacyConfigChecked)
                {
                    _legacyMode = GetLegacyFormatMode();
                    _legacyConfigChecked = true;
                }

                return _legacyMode;
            }
        }
    }
}