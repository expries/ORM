using System;
using System.Reflection;

namespace ORM.Core.Interfaces
{
    public interface ICache
    {
        public void Save<T>(T entity, PropertyInfo? property = null);

        public T? Query<T>(Func<T, bool> predicate);
        
        public T? Query<T>(PropertyInfo property);
    }
}