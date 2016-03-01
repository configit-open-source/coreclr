using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace System.Resources
{
    internal interface IResourceGroveler
    {
        ResourceSet GrovelForResourceSet(CultureInfo culture, Dictionary<String, ResourceSet> localResourceSets, bool tryParents, bool createIfNotExists, ref StackCrawlMark stackMark);
    }
}