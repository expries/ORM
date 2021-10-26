using System.Reflection;

namespace ORM.Core.Interfaces
{
    public interface IDbContext
    {
        public void EnsureCreated(Assembly? assembly);
        
        public void Save(object entity);
    }
}