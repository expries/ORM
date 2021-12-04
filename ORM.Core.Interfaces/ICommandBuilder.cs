using System.Collections.Generic;
using System.Data;
using ORM.Core.Models;

namespace ORM.Core.Interfaces
{
    public interface ICommandBuilder
    {
        public IDbCommand BuildEnsureCreated(List<Table> tables);

        public IDbCommand BuildGetAll<T>();

        public IDbCommand BuildGetById<T>(object pk);

        public IDbCommand BuildSave<T>(T entity);

        public IDbCommand BuildLoadManyToOne<TMany, TOne>(TMany entity);

        public IDbCommand BuildLoadOneToMany<TOne, TMany>(TOne entity);

        public IDbCommand BuildLoadManyToMany<TManyA, TManyB>(TManyA entity);
    }
}