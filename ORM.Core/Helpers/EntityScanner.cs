using System;
using System.Collections.Generic;
using System.Linq;
using ORM.Infrastructure.Database;

namespace ORM.Core.Helpers
{
    public static class AssemblyScanner
    {
        public static IEnumerable<Type> GetEntityTypes(System.Reflection.Assembly assembly)
        {
            var dbContexts = GetDbContextTypes(assembly);
            IEnumerable<Type> entities = new List<Type>();
            
            foreach (var context in dbContexts)
            {
                var properties = context.GetProperties();
                var propertyTypes = properties.Select(p => p.PropertyType).Distinct();
                var genericProperties = propertyTypes.Where(t => t.IsGenericType);
                var dbSetProperties = genericProperties.Where(t => t.GetGenericTypeDefinition() == typeof(DbSet<>));
                var entityTypes = dbSetProperties.Select(t => t.GetGenericArguments().FirstOrDefault());
                entities = entities.Union(entityTypes);
            }

            return entities;
        }
        
        public static IEnumerable<Type> GetDbContextTypes(System.Reflection.Assembly assembly)
        {
            var types = assembly.GetTypes();
            var contexts = types.Where(t => t.IsSubclassOf(typeof(DbContext)));
            return contexts;
        }
    }
}