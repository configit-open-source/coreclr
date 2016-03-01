using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.Serialization;

namespace System
{
    public struct DateTimeOffset : IComparable, IFormattable, ISerializable, IDeserializationCallback, IComparable<DateTimeOffset>, IEquatable<DateTimeOffset>
    {
        internal const Int64 MaxOffset = TimeSpan.TicksPerHour * 14;
        internal const Int64 MinOffset = -MaxOffset;
        private const long UnixEpochTicks = TimeSpan.TicksPerDay * DateTime.DaysTo1970;
        private const long UnixEpochSeconds = UnixEpochTicks / TimeSpan.TicksPerSecond;
        private const long UnixEpochMilliseconds = UnixEpochTicks / TimeSpan.TicksPerMillisecond;
        internal const long UnixMinSeconds = DateTime.MinTicks / TimeSpan.TicksPerSecond - UnixEpochSeconds;
        internal const long UnixMaxSeconds = DateTime.MaxTicks / TimeSpan.TicksPerSecond - UnixEpochSeconds;
        public static readonly DateTimeOffset MinValue = new DateTimeOffset(DateTime.MinTicks, TimeSpan.Zero);
        public static readonly DateTimeOffset MaxValue = new DateTimeOffset(DateTime.MaxTicks, TimeSpan.Zero);
        private DateTime m_dateTime;
        private Int16 m_offsetMinutes;
        public DateTimeOffset(long ticks, TimeSpan offset)
        {
            m_offsetMinutes = ValidateOffset(offset);
            DateTime dateTime = new DateTime(ticks);
            m_dateTime = ValidateDate(dateTime, offset);
        }

        public DateTimeOffset(DateTime dateTime)
        {
            TimeSpan offset;
            if (dateTime.Kind != DateTimeKind.Utc)
            {
                offset = TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
            }
            else
            {
                offset = new TimeSpan(0);
            }

            m_offsetMinutes = ValidateOffset(offset);
            m_dateTime = ValidateDate(dateTime, offset);
        }

        public DateTimeOffset(DateTime dateTime, TimeSpan offset)
        {
            if (dateTime.Kind == DateTimeKind.Local)
            {
                if (offset != TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime))
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_OffsetLocalMismatch"), "offset");
                }
            }
            else if (dateTime.Kind == DateTimeKind.Utc)
            {
                if (offset != TimeSpan.Zero)
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_OffsetUtcMismatch"), "offset");
                }
            }

            m_offsetMinutes = ValidateOffset(offset);
            m_dateTime = ValidateDate(dateTime, offset);
        }

        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, TimeSpan offset)
        {
            m_offsetMinutes = ValidateOffset(offset);
            m_dateTime = ValidateDate(new DateTime(year, month, day, hour, minute, second), offset);
        }

        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, TimeSpan offset)
        {
            m_offsetMinutes = ValidateOffset(offset);
            m_dateTime = ValidateDate(new DateTime(year, month, day, hour, minute, second, millisecond), offset);
        }

        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar calendar, TimeSpan offset)
        {
            m_offsetMinutes = ValidateOffset(offset);
            m_dateTime = ValidateDate(new DateTime(year, month, day, hour, minute, second, millisecond, calendar), offset);
        }

        public static DateTimeOffset Now
        {
            get
            {
                return new DateTimeOffset(DateTime.Now);
            }
        }

        public static DateTimeOffset UtcNow
        {
            get
            {
                return new DateTimeOffset(DateTime.UtcNow);
            }
        }

        public DateTime DateTime
        {
            get
            {
                return ClockDateTime;
            }
        }

        public DateTime UtcDateTime
        {
            [Pure]
            get
            {
                Contract.Ensures(Contract.Result<DateTime>().Kind == DateTimeKind.Utc);
                return DateTime.SpecifyKind(m_dateTime, DateTimeKind.Utc);
            }
        }

        public DateTime LocalDateTime
        {
            [Pure]
            get
            {
                Contract.Ensures(Contract.Result<DateTime>().Kind == DateTimeKind.Local);
                return UtcDateTime.ToLocalTime();
            }
        }

        public DateTimeOffset ToOffset(TimeSpan offset)
        {
            return new DateTimeOffset((m_dateTime + offset).Ticks, offset);
        }

        private DateTime ClockDateTime
        {
            get
            {
                return new DateTime((m_dateTime + Offset).Ticks, DateTimeKind.Unspecified);
            }
        }

        public DateTime Date
        {
            get
            {
                return ClockDateTime.Date;
            }
        }

        public int Day
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 1);
                Contract.Ensures(Contract.Result<int>() <= 31);
                return ClockDateTime.Day;
            }
        }

        public DayOfWeek DayOfWeek
        {
            get
            {
                Contract.Ensures(Contract.Result<DayOfWeek>() >= DayOfWeek.Sunday);
                Contract.Ensures(Contract.Result<DayOfWeek>() <= DayOfWeek.Saturday);
                return ClockDateTime.DayOfWeek;
            }
        }

        public int DayOfYear
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 1);
                Contract.Ensures(Contract.Result<int>() <= 366);
                return ClockDateTime.DayOfYear;
            }
        }

        public int Hour
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                Contract.Ensures(Contract.Result<int>() < 24);
                return ClockDateTime.Hour;
            }
        }

        public int Millisecond
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                Contract.Ensures(Contract.Result<int>() < 1000);
                return ClockDateTime.Millisecond;
            }
        }

        public int Minute
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                Contract.Ensures(Contract.Result<int>() < 60);
                return ClockDateTime.Minute;
            }
        }

        public int Month
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 1);
                return ClockDateTime.Month;
            }
        }

        public TimeSpan Offset
        {
            get
            {
                return new TimeSpan(0, m_offsetMinutes, 0);
            }
        }

        public int Second
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                Contract.Ensures(Contract.Result<int>() < 60);
                return ClockDateTime.Second;
            }
        }

        public long Ticks
        {
            get
            {
                return ClockDateTime.Ticks;
            }
        }

        public long UtcTicks
        {
            get
            {
                return UtcDateTime.Ticks;
            }
        }

        public TimeSpan TimeOfDay
        {
            get
            {
                return ClockDateTime.TimeOfDay;
            }
        }

        public int Year
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 1 && Contract.Result<int>() <= 9999);
                return ClockDateTime.Year;
            }
        }

        public DateTimeOffset Add(TimeSpan timeSpan)
        {
            return new DateTimeOffset(ClockDateTime.Add(timeSpan), Offset);
        }

        public DateTimeOffset AddDays(double days)
        {
            return new DateTimeOffset(ClockDateTime.AddDays(days), Offset);
        }

        public DateTimeOffset AddHours(double hours)
        {
            return new DateTimeOffset(ClockDateTime.AddHours(hours), Offset);
        }

        public DateTimeOffset AddMilliseconds(double milliseconds)
        {
            return new DateTimeOffset(ClockDateTime.AddMilliseconds(milliseconds), Offset);
        }

        public DateTimeOffset AddMinutes(double minutes)
        {
            return new DateTimeOffset(ClockDateTime.AddMinutes(minutes), Offset);
        }

        public DateTimeOffset AddMonths(int months)
        {
            return new DateTimeOffset(ClockDateTime.AddMonths(months), Offset);
        }

        public DateTimeOffset AddSeconds(double seconds)
        {
            return new DateTimeOffset(ClockDateTime.AddSeconds(seconds), Offset);
        }

        public DateTimeOffset AddTicks(long ticks)
        {
            return new DateTimeOffset(ClockDateTime.AddTicks(ticks), Offset);
        }

        public DateTimeOffset AddYears(int years)
        {
            return new DateTimeOffset(ClockDateTime.AddYears(years), Offset);
        }

        public static int Compare(DateTimeOffset first, DateTimeOffset second)
        {
            return DateTime.Compare(first.UtcDateTime, second.UtcDateTime);
        }

        int IComparable.CompareTo(Object obj)
        {
            if (obj == null)
                return 1;
            if (!(obj is DateTimeOffset))
            {
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDateTimeOffset"));
            }

            DateTime objUtc = ((DateTimeOffset)obj).UtcDateTime;
            DateTime utc = UtcDateTime;
            if (utc > objUtc)
                return 1;
            if (utc < objUtc)
                return -1;
            return 0;
        }

        public int CompareTo(DateTimeOffset other)
        {
            DateTime otherUtc = other.UtcDateTime;
            DateTime utc = UtcDateTime;
            if (utc > otherUtc)
                return 1;
            if (utc < otherUtc)
                return -1;
            return 0;
        }

        public override bool Equals(Object obj)
        {
            if (obj is DateTimeOffset)
            {
                return UtcDateTime.Equals(((DateTimeOffset)obj).UtcDateTime);
            }

            return false;
        }

        public bool Equals(DateTimeOffset other)
        {
            return UtcDateTime.Equals(other.UtcDateTime);
        }

        public bool EqualsExact(DateTimeOffset other)
        {
            return (ClockDateTime == other.ClockDateTime && Offset == other.Offset && ClockDateTime.Kind == other.ClockDateTime.Kind);
        }

        public static bool Equals(DateTimeOffset first, DateTimeOffset second)
        {
            return DateTime.Equals(first.UtcDateTime, second.UtcDateTime);
        }

        public static DateTimeOffset FromFileTime(long fileTime)
        {
            return new DateTimeOffset(DateTime.FromFileTime(fileTime));
        }

        public static DateTimeOffset FromUnixTimeSeconds(long seconds)
        {
            if (seconds < UnixMinSeconds || seconds > UnixMaxSeconds)
            {
                throw new ArgumentOutOfRangeException("seconds", string.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), UnixMinSeconds, UnixMaxSeconds));
            }

            long ticks = seconds * TimeSpan.TicksPerSecond + UnixEpochTicks;
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        public static DateTimeOffset FromUnixTimeMilliseconds(long milliseconds)
        {
            const long MinMilliseconds = DateTime.MinTicks / TimeSpan.TicksPerMillisecond - UnixEpochMilliseconds;
            const long MaxMilliseconds = DateTime.MaxTicks / TimeSpan.TicksPerMillisecond - UnixEpochMilliseconds;
            if (milliseconds < MinMilliseconds || milliseconds > MaxMilliseconds)
            {
                throw new ArgumentOutOfRangeException("milliseconds", string.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), MinMilliseconds, MaxMilliseconds));
            }

            long ticks = milliseconds * TimeSpan.TicksPerMillisecond + UnixEpochTicks;
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        public override int GetHashCode()
        {
            return UtcDateTime.GetHashCode();
        }

        public static DateTimeOffset Parse(String input)
        {
            TimeSpan offset;
            DateTime dateResult = DateTimeParse.Parse(input, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out offset);
            return new DateTimeOffset(dateResult.Ticks, offset);
        }

        public static DateTimeOffset Parse(String input, IFormatProvider formatProvider)
        {
            return Parse(input, formatProvider, DateTimeStyles.None);
        }

        public static DateTimeOffset Parse(String input, IFormatProvider formatProvider, DateTimeStyles styles)
        {
            styles = ValidateStyles(styles, "styles");
            TimeSpan offset;
            DateTime dateResult = DateTimeParse.Parse(input, DateTimeFormatInfo.GetInstance(formatProvider), styles, out offset);
            return new DateTimeOffset(dateResult.Ticks, offset);
        }

        public static DateTimeOffset ParseExact(String input, String format, IFormatProvider formatProvider)
        {
            return ParseExact(input, format, formatProvider, DateTimeStyles.None);
        }

        public static DateTimeOffset ParseExact(String input, String format, IFormatProvider formatProvider, DateTimeStyles styles)
        {
            styles = ValidateStyles(styles, "styles");
            TimeSpan offset;
            DateTime dateResult = DateTimeParse.ParseExact(input, format, DateTimeFormatInfo.GetInstance(formatProvider), styles, out offset);
            return new DateTimeOffset(dateResult.Ticks, offset);
        }

        public static DateTimeOffset ParseExact(String input, String[] formats, IFormatProvider formatProvider, DateTimeStyles styles)
        {
            styles = ValidateStyles(styles, "styles");
            TimeSpan offset;
            DateTime dateResult = DateTimeParse.ParseExactMultiple(input, formats, DateTimeFormatInfo.GetInstance(formatProvider), styles, out offset);
            return new DateTimeOffset(dateResult.Ticks, offset);
        }

        public TimeSpan Subtract(DateTimeOffset value)
        {
            return UtcDateTime.Subtract(value.UtcDateTime);
        }

        public DateTimeOffset Subtract(TimeSpan value)
        {
            return new DateTimeOffset(ClockDateTime.Subtract(value), Offset);
        }

        public long ToFileTime()
        {
            return UtcDateTime.ToFileTime();
        }

        public long ToUnixTimeSeconds()
        {
            long seconds = UtcDateTime.Ticks / TimeSpan.TicksPerSecond;
            return seconds - UnixEpochSeconds;
        }

        public long ToUnixTimeMilliseconds()
        {
            long milliseconds = UtcDateTime.Ticks / TimeSpan.TicksPerMillisecond;
            return milliseconds - UnixEpochMilliseconds;
        }

        public DateTimeOffset ToLocalTime()
        {
            return ToLocalTime(false);
        }

        internal DateTimeOffset ToLocalTime(bool throwOnOverflow)
        {
            return new DateTimeOffset(UtcDateTime.ToLocalTime(throwOnOverflow));
        }

        public override String ToString()
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return DateTimeFormat.Format(ClockDateTime, null, DateTimeFormatInfo.CurrentInfo, Offset);
        }

        public String ToString(String format)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return DateTimeFormat.Format(ClockDateTime, format, DateTimeFormatInfo.CurrentInfo, Offset);
        }

        public String ToString(IFormatProvider formatProvider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return DateTimeFormat.Format(ClockDateTime, null, DateTimeFormatInfo.GetInstance(formatProvider), Offset);
        }

        public String ToString(String format, IFormatProvider formatProvider)
        {
            Contract.Ensures(Contract.Result<String>() != null);
            return DateTimeFormat.Format(ClockDateTime, format, DateTimeFormatInfo.GetInstance(formatProvider), Offset);
        }

        public DateTimeOffset ToUniversalTime()
        {
            return new DateTimeOffset(UtcDateTime);
        }

        public static Boolean TryParse(String input, out DateTimeOffset result)
        {
            TimeSpan offset;
            DateTime dateResult;
            Boolean parsed = DateTimeParse.TryParse(input, DateTimeFormatInfo.CurrentInfo, DateTimeStyles.None, out dateResult, out offset);
            result = new DateTimeOffset(dateResult.Ticks, offset);
            return parsed;
        }

        public static Boolean TryParse(String input, IFormatProvider formatProvider, DateTimeStyles styles, out DateTimeOffset result)
        {
            styles = ValidateStyles(styles, "styles");
            TimeSpan offset;
            DateTime dateResult;
            Boolean parsed = DateTimeParse.TryParse(input, DateTimeFormatInfo.GetInstance(formatProvider), styles, out dateResult, out offset);
            result = new DateTimeOffset(dateResult.Ticks, offset);
            return parsed;
        }

        public static Boolean TryParseExact(String input, String format, IFormatProvider formatProvider, DateTimeStyles styles, out DateTimeOffset result)
        {
            styles = ValidateStyles(styles, "styles");
            TimeSpan offset;
            DateTime dateResult;
            Boolean parsed = DateTimeParse.TryParseExact(input, format, DateTimeFormatInfo.GetInstance(formatProvider), styles, out dateResult, out offset);
            result = new DateTimeOffset(dateResult.Ticks, offset);
            return parsed;
        }

        public static Boolean TryParseExact(String input, String[] formats, IFormatProvider formatProvider, DateTimeStyles styles, out DateTimeOffset result)
        {
            styles = ValidateStyles(styles, "styles");
            TimeSpan offset;
            DateTime dateResult;
            Boolean parsed = DateTimeParse.TryParseExactMultiple(input, formats, DateTimeFormatInfo.GetInstance(formatProvider), styles, out dateResult, out offset);
            result = new DateTimeOffset(dateResult.Ticks, offset);
            return parsed;
        }

        private static Int16 ValidateOffset(TimeSpan offset)
        {
            Int64 ticks = offset.Ticks;
            if (ticks % TimeSpan.TicksPerMinute != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_OffsetPrecision"), "offset");
            }

            if (ticks < MinOffset || ticks > MaxOffset)
            {
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("Argument_OffsetOutOfRange"));
            }

            return (Int16)(offset.Ticks / TimeSpan.TicksPerMinute);
        }

        private static DateTime ValidateDate(DateTime dateTime, TimeSpan offset)
        {
            Contract.Assert(offset.Ticks >= MinOffset && offset.Ticks <= MaxOffset, "Offset not validated.");
            Int64 utcTicks = dateTime.Ticks - offset.Ticks;
            if (utcTicks < DateTime.MinTicks || utcTicks > DateTime.MaxTicks)
            {
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("Argument_UTCOutOfRange"));
            }

            return new DateTime(utcTicks, DateTimeKind.Unspecified);
        }

        private static DateTimeStyles ValidateStyles(DateTimeStyles style, String parameterName)
        {
            if ((style & DateTimeFormatInfo.InvalidDateTimeStyles) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDateTimeStyles"), parameterName);
            }

            if (((style & (DateTimeStyles.AssumeLocal)) != 0) && ((style & (DateTimeStyles.AssumeUniversal)) != 0))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_ConflictingDateTimeStyles"), parameterName);
            }

            if ((style & DateTimeStyles.NoCurrentDateDefault) != 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeOffsetInvalidDateTimeStyles"), parameterName);
            }

            Contract.EndContractBlock();
            style &= ~DateTimeStyles.RoundtripKind;
            style &= ~DateTimeStyles.AssumeLocal;
            return style;
        }

        public static implicit operator DateTimeOffset(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime);
        }

        public static DateTimeOffset operator +(DateTimeOffset dateTimeOffset, TimeSpan timeSpan)
        {
            return new DateTimeOffset(dateTimeOffset.ClockDateTime + timeSpan, dateTimeOffset.Offset);
        }

        public static DateTimeOffset operator -(DateTimeOffset dateTimeOffset, TimeSpan timeSpan)
        {
            return new DateTimeOffset(dateTimeOffset.ClockDateTime - timeSpan, dateTimeOffset.Offset);
        }

        public static TimeSpan operator -(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime - right.UtcDateTime;
        }

        public static bool operator ==(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime == right.UtcDateTime;
        }

        public static bool operator !=(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime != right.UtcDateTime;
        }

        public static bool operator <(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime < right.UtcDateTime;
        }

        public static bool operator <=(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime <= right.UtcDateTime;
        }

        public static bool operator>(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime > right.UtcDateTime;
        }

        public static bool operator >=(DateTimeOffset left, DateTimeOffset right)
        {
            return left.UtcDateTime >= right.UtcDateTime;
        }
    }
}