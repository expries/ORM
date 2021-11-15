using System.Collections.Generic;

namespace ORM.Core.Interfaces
{
    public interface ILazyLoader
    {
        public TOne LoadManyToOne<TMany, TOne>(object pk);

        public List<TMany> LoadOneToMany<TOne, TMany>(object pk);
        
        public List<TManyB> LoadManyToMany<TManyA, TManyB>(object pk);
    }
}