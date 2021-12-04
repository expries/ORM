using System.Collections.Generic;
using System.Linq;
using ORM.Core.Interfaces;
using ORM.Core.Models.Extensions;

namespace ORM.Core.Loading
{
    public class LazyLoader : ILazyLoader
    {
        private readonly ICommandBuilder _commandBuilder;
        
        private readonly ICache _cache;

        public LazyLoader(ICommandBuilder commandBuilder, ICache cache)
        {
            _commandBuilder = commandBuilder;
            _cache = cache;
        }

        public TOne LoadManyToOne<TMany, TOne>(TMany entity)
        {
            var navigatedProperty = typeof(TMany).GetNavigatedProperty(typeof(TOne));
            var cachedEntry = _cache.Query<TOne>(navigatedProperty);

            var cmd = _commandBuilder.BuildLoadManyToOne<TMany, TOne>(entity);
            var reader = cmd.ExecuteReader();
            var result = (TOne) new ObjectReader<TOne>(reader, this, _cache);
            
            _cache.Save(result, navigatedProperty);
            return result;
        }

        public List<TMany> LoadOneToMany<TOne, TMany>(TOne entity)
        {
            var navigatedProperty = typeof(TOne).GetNavigatedProperty(typeof(TMany));
            var cachedEntry = _cache.Query<List<TMany>>(navigatedProperty);

            var cmd = _commandBuilder.BuildLoadOneToMany<TOne, TMany>(entity);
            var reader = cmd.ExecuteReader();
            var result =  new ObjectReader<TMany>(reader, this, _cache).ToList();
            
            _cache.Save(result, navigatedProperty);
            return result;
        }

        public List<TManyB> LoadManyToMany<TManyA, TManyB>(TManyA entity)
        {
            var navigatedProperty = typeof(TManyA).GetNavigatedProperty(typeof(TManyB));
            var cachedEntry = _cache.Query<List<TManyB>>(navigatedProperty);

            var cmd = _commandBuilder.BuildLoadManyToMany<TManyA, TManyB>(entity);
            var reader = cmd.ExecuteReader();
            var result =  new ObjectReader<TManyB>(reader, this, _cache).ToList();

            _cache.Save(result, navigatedProperty);
            return result;
        }
    }
}