using System.Reflection;

namespace ORM.Core.Cache
{
    public class CacheEntry
    {
        public PropertyInfo? Property { get; }
        
        public object Value { get; }

        public CacheEntry(object value, PropertyInfo? property = null)
        {
            Value = value;
            Property = property;
        }
    }
}