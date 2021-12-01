using System.Collections.Generic;
using System.Data;
using ORM.Core.Models;

namespace ORM.Core.Interfaces
{
    public interface ICommandBuilder
    {
        public IDbCommand BuildEnsureCreated(List<Table> tables);

        public IDbCommand BuildSelect<T>();

        public IDbCommand BuildSelectById<T>(object pk);

        public IDbCommand BuildSave<T>(T entity);

        public IDbCommand BuildSelectManyToOne<TMany, TOne>(TMany entity);

        public IDbCommand BuildSelectOneToMany<TOne, TMany>(TOne entity);

        public IDbCommand BuildSelectManyToMany<TManyA, TManyB>(TManyA entity);
    }
}