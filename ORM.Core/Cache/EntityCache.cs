using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ORM.Core.Interfaces;

namespace ORM.Core.Cache
{
    public class EntityCache : ICache
    {
        private readonly List<CacheEntry> _cache = new List<CacheEntry>();
        
        public void Save<T>(T entity, PropertyInfo? property = null)
        {
            var cacheEntry = new CacheEntry(entity, property);
            _cache.Add(cacheEntry);
        }

        public T? Query<T>(Func<T, bool> predicate)
        {
            var typedValues = _cache.Select(c => c.Value).OfType<T>();
            return typedValues.FirstOrDefault(predicate);
        }

        public T? Query<T>(PropertyInfo property)
        {
            var entries = _cache.Where(c => c.Property == property);
            foreach (var entry in entries)
            {
                if (entry.Value is T typedValue)
                {
                    return typedValue;
                }
            }
            return default;
        }
    }
}