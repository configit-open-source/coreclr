namespace System.Globalization
{
    using System;

    public class DaylightTime
    {
        internal DateTime m_start;
        internal DateTime m_end;
        internal TimeSpan m_delta;
        private DaylightTime()
        {
        }

        public DaylightTime(DateTime start, DateTime end, TimeSpan delta)
        {
            m_start = start;
            m_end = end;
            m_delta = delta;
        }

        public DateTime Start
        {
            get
            {
                return m_start;
            }
        }

        public DateTime End
        {
            get
            {
                return m_end;
            }
        }

        public TimeSpan Delta
        {
            get
            {
                return m_delta;
            }
        }
    }
}