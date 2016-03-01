namespace System.Globalization
{
    using System;
    using System.Runtime.Serialization;
    using System.Diagnostics.Contracts;

    public class RegionInfo
    {
        internal String m_name;
        internal CultureData m_cultureData;
        internal static volatile RegionInfo s_currentRegionInfo;
        public RegionInfo(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");
            if (name.Length == 0)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_NoRegionInvariantCulture"));
            }

            Contract.EndContractBlock();
            this.m_cultureData = CultureData.GetCultureDataForRegion(name, true);
            if (this.m_cultureData == null)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidCultureName"), name), "name");
            if (this.m_cultureData.IsNeutralCulture)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidNeutralRegionName", name), "name");
            SetName(name);
        }

        internal RegionInfo(CultureData cultureData)
        {
            this.m_cultureData = cultureData;
            this.m_name = this.m_cultureData.SREGIONNAME;
        }

        private void SetName(string name)
        {
            this.m_name = this.m_cultureData.SREGIONNAME;
        }

        int m_cultureId;
        internal int m_dataItem = 0;
        private void OnDeserialized(StreamingContext ctx)
        {
            this.m_cultureData = CultureData.GetCultureData(m_name, true);
            if (this.m_cultureData == null)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_InvalidCultureName"), m_name), "m_name");
            if (m_cultureId == 0)
            {
                SetName(this.m_name);
            }
            else
            {
                this.m_name = this.m_cultureData.SREGIONNAME;
            }
        }

        private void OnSerializing(StreamingContext ctx)
        {
        }

        public static RegionInfo CurrentRegion
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                RegionInfo temp = s_currentRegionInfo;
                if (temp == null)
                {
                    temp = new RegionInfo(CultureInfo.CurrentCulture.m_cultureData);
                    temp.m_name = temp.m_cultureData.SREGIONNAME;
                    s_currentRegionInfo = temp;
                }

                return temp;
            }
        }

        public virtual String Name
        {
            get
            {
                Contract.Assert(m_name != null, "Expected RegionInfo.m_name to be populated already");
                return (m_name);
            }
        }

        public virtual String EnglishName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (this.m_cultureData.SENGCOUNTRY);
            }
        }

        public virtual String DisplayName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (this.m_cultureData.SLOCALIZEDCOUNTRY);
            }
        }

        public virtual String NativeName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (this.m_cultureData.SNATIVECOUNTRY);
            }
        }

        public virtual String TwoLetterISORegionName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (this.m_cultureData.SISO3166CTRYNAME);
            }
        }

        public virtual bool IsMetric
        {
            get
            {
                int value = this.m_cultureData.IMEASURE;
                return (value == 0);
            }
        }

        public virtual int GeoId
        {
            get
            {
                return (this.m_cultureData.IGEOID);
            }
        }

        public virtual String CurrencyEnglishName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (this.m_cultureData.SENGLISHCURRENCY);
            }
        }

        public virtual String CurrencyNativeName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (this.m_cultureData.SNATIVECURRENCY);
            }
        }

        public virtual String CurrencySymbol
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (this.m_cultureData.SCURRENCY);
            }
        }

        public virtual String ISOCurrencySymbol
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return (this.m_cultureData.SINTLSYMBOL);
            }
        }

        public override bool Equals(Object value)
        {
            RegionInfo that = value as RegionInfo;
            if (that != null)
            {
                return this.Name.Equals(that.Name);
            }

            return (false);
        }

        public override int GetHashCode()
        {
            return (this.Name.GetHashCode());
        }

        public override String ToString()
        {
            return (Name);
        }
    }
}