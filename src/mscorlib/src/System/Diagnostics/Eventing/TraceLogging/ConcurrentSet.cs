using System.Threading;

namespace System.Diagnostics.Tracing
{
    internal struct ConcurrentSet<KeyType, ItemType>
        where ItemType : ConcurrentSetItem<KeyType, ItemType>
    {
        private ItemType[] items;
        public ItemType TryGet(KeyType key)
        {
            ItemType item;
            var oldItems = this.items;
            if (oldItems != null)
            {
                var lo = 0;
                var hi = oldItems.Length;
                do
                {
                    int i = (lo + hi) / 2;
                    item = oldItems[i];
                    int cmp = item.Compare(key);
                    if (cmp == 0)
                    {
                        goto Done;
                    }
                    else if (cmp < 0)
                    {
                        lo = i + 1;
                    }
                    else
                    {
                        hi = i;
                    }
                }
                while (lo != hi);
            }

            item = null;
            Done:
                return item;
        }

        public ItemType GetOrAdd(ItemType newItem)
        {
            ItemType item;
            var oldItems = this.items;
            ItemType[] newItems;
            Retry:
                if (oldItems == null)
                {
                    newItems = new ItemType[]{newItem};
                }
                else
                {
                    var lo = 0;
                    var hi = oldItems.Length;
                    do
                    {
                        int i = (lo + hi) / 2;
                        item = oldItems[i];
                        int cmp = item.Compare(newItem);
                        if (cmp == 0)
                        {
                            goto Done;
                        }
                        else if (cmp < 0)
                        {
                            lo = i + 1;
                        }
                        else
                        {
                            hi = i;
                        }
                    }
                    while (lo != hi);
                    int oldLength = oldItems.Length;
                    newItems = new ItemType[oldLength + 1];
                    Array.Copy(oldItems, 0, newItems, 0, lo);
                    newItems[lo] = newItem;
                    Array.Copy(oldItems, lo, newItems, lo + 1, oldLength - lo);
                }

            newItems = Interlocked.CompareExchange(ref this.items, newItems, oldItems);
            if (oldItems != newItems)
            {
                oldItems = newItems;
                goto Retry;
            }

            item = newItem;
            Done:
                return item;
        }
    }
}