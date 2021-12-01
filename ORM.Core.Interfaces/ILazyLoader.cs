using System.Collections.Generic;

namespace ORM.Core.Interfaces
{
    public interface ILazyLoader
    {
        public TOne LoadManyToOne<TMany, TOne>(TMany entity);

        public List<TMany> LoadOneToMany<TOne, TMany>(TOne entity);
        
        public List<TManyB> LoadManyToMany<TManyA, TManyB>(TManyA entity);
    }
}