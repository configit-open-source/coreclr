using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace System.Resources
{
    internal class ResourceFallbackManager : IEnumerable<CultureInfo>
    {
        private CultureInfo m_startingCulture;
        private CultureInfo m_neutralResourcesCulture;
        private bool m_useParents;
        private static CultureInfo[] cachedOsFallbackArray;
        internal ResourceFallbackManager(CultureInfo startingCulture, CultureInfo neutralResourcesCulture, bool useParents)
        {
            if (startingCulture != null)
            {
                m_startingCulture = startingCulture;
            }
            else
            {
                m_startingCulture = CultureInfo.CurrentUICulture;
            }

            m_neutralResourcesCulture = neutralResourcesCulture;
            m_useParents = useParents;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<CultureInfo> GetEnumerator()
        {
            bool reachedNeutralResourcesCulture = false;
            CultureInfo currentCulture = m_startingCulture;
            do
            {
                if (m_neutralResourcesCulture != null && currentCulture.Name == m_neutralResourcesCulture.Name)
                {
                    yield return CultureInfo.InvariantCulture;
                    reachedNeutralResourcesCulture = true;
                    break;
                }

                yield return currentCulture;
                currentCulture = currentCulture.Parent;
            }
            while (m_useParents && !currentCulture.HasInvariantCultureName);
            if (!m_useParents || m_startingCulture.HasInvariantCultureName)
            {
                yield break;
            }

            if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                CultureInfo[] osFallbackArray = LoadPreferredCultures();
                if (osFallbackArray != null)
                {
                    foreach (CultureInfo ci in osFallbackArray)
                    {
                        if (m_startingCulture.Name != ci.Name && m_startingCulture.Parent.Name != ci.Name)
                        {
                            yield return ci;
                        }
                    }
                }
            }

            if (reachedNeutralResourcesCulture)
                yield break;
            yield return CultureInfo.InvariantCulture;
        }

        private static CultureInfo[] LoadPreferredCultures()
        {
            String[] cultureNames = GetResourceFallbackArray();
            if (cultureNames == null)
                return null;
            bool useCachedNames = (cachedOsFallbackArray != null && cultureNames.Length == cachedOsFallbackArray.Length);
            if (useCachedNames)
            {
                for (int i = 0; i < cultureNames.Length; i++)
                {
                    if (!String.Equals(cultureNames[i], cachedOsFallbackArray[i].Name))
                    {
                        useCachedNames = false;
                        break;
                    }
                }
            }

            if (useCachedNames)
                return cachedOsFallbackArray;
            cachedOsFallbackArray = LoadCulturesFromNames(cultureNames);
            return cachedOsFallbackArray;
        }

        private static CultureInfo[] LoadCulturesFromNames(String[] cultureNames)
        {
            if (cultureNames == null)
                return null;
            CultureInfo[] cultures = new CultureInfo[cultureNames.Length];
            int culturesIndex = 0;
            for (int i = 0; i < cultureNames.Length; i++)
            {
                cultures[culturesIndex] = CultureInfo.GetCultureInfo(cultureNames[i]);
                if (!Object.ReferenceEquals(cultures[culturesIndex], null))
                    culturesIndex++;
            }

            if (culturesIndex != cultureNames.Length)
            {
                CultureInfo[] ret = new CultureInfo[culturesIndex];
                Array.Copy(cultures, ret, culturesIndex);
                cultures = ret;
            }

            return cultures;
        }

        private static String[] GetResourceFallbackArray()
        {
            return CultureInfo.nativeGetResourceFallbackArray();
        }
    }
}