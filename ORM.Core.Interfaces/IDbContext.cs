using System.Collections.Generic;
using System.Reflection;
using ORM.Core.Models;

namespace ORM.Core.Interfaces
{
    public interface IDbContext
    {
        public void EnsureCreated(Assembly? assembly = null);
        
        public void Save<T>(T entity);

        public IEnumerable<T> GetAll<T>();

        public T GetById<T>(object pk);
    }
}