using System.Collections;
using System.Collections.Generic;

namespace ORM.Infrastructure.Database
{
    public class DbSet<TEntity> : IEnumerable<TEntity> where TEntity : class
    {
        private readonly List<TEntity> _entities = new List<TEntity>();

        public IEnumerator<TEntity> GetEnumerator()
        {
            return _entities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}