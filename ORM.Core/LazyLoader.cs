using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
using ORM.Core.Interfaces;

namespace ORM.Core
{
    public class LazyLoader : ILazyLoader
    {
        private readonly ICommandBuilder _commandBuilder;

        private int _loadCounter = 0;
        
        public LazyLoader(ICommandBuilder commandBuilder)
        {
            _commandBuilder = commandBuilder;
        }

        public TOne LoadManyToOne<TMany, TOne>(TMany entity)
        {
            var cmd = _commandBuilder.BuildSelectManyToOne<TMany, TOne>(entity);
            var reader = cmd.ExecuteReader();
            return new ObjectReader<TOne>(reader, this);
        }

        public List<TMany> LoadOneToMany<TOne, TMany>(TOne entity)
        {
            if (++_loadCounter > 10)
            {
                return new List<TMany>();
            }

            var cmd = _commandBuilder.BuildSelectOneToMany<TOne, TMany>(entity);
            var reader = cmd.ExecuteReader();
            return new ObjectReader<TMany>(reader, this).ToList();
        }

        public List<TManyB> LoadManyToMany<TManyA, TManyB>(TManyA entity)
        {
            if (++_loadCounter > 10)
            {
                return new List<TManyB>();
            }
            
            var cmd = _commandBuilder.BuildSelectManyToMany<TManyA, TManyB>(entity);
            var reader = cmd.ExecuteReader();
            return new ObjectReader<TManyB>(reader, this).ToList();
        }
    }
}