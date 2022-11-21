using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Task1_Ben
{
    public class CachingProviderBase
    {


        protected MemoryCache cache;

        static readonly object padlock = new object();


        public CachingProviderBase()
        {
            cache = new MemoryCache("CachingProvider");
        }

        // adds the List<Dictionary> to the cache with value key
        // key is of the form "[TOPIC][STARTIME][ENDTIME][LANGS]"
        // [LANGS] is of the form "en,es" or "any"
        public virtual void AddItem(string key, ResponseClass value)
        {
            lock (padlock)
            {
                cache.Add(key, value, DateTimeOffset.MaxValue);
            }
        }

        // removes the item with "key" index
        public virtual void RemoveItem(string key)
        {
            lock (padlock)
            {
                cache.Remove(key);
            }
        }

        // gets the item assocociated with "key"
        public virtual ResponseClass GetItem(string key)
        {
            lock (padlock)
            {
                var res = cache[key];
                if (res != null)
                {
                    return (ResponseClass)res;
                }
            }

            return null;
        }

    }
}
