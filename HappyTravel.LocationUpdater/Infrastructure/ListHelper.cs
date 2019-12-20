using System;
using System.Collections.Generic;

namespace HappyTravel.LocationUpdater.Infrastructure
{
    internal static class ListHelper
    {
        public static IEnumerable<List<T>> SplitList<T>(List<T> items, int batchSize)
        {
            for (var i = 0; i < items.Count; i += batchSize)
                yield return items.GetRange(i, Math.Min(batchSize, items.Count - i));
        }
    }
}