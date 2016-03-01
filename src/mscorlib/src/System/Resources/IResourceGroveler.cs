namespace System.Resources
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.Versioning;

    internal interface IResourceGroveler
    {
        ResourceSet GrovelForResourceSet(CultureInfo culture, Dictionary<String, ResourceSet> localResourceSets, bool tryParents, bool createIfNotExists, ref StackCrawlMark stackMark);
    }
}