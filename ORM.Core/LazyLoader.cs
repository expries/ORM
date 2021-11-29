using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
using ORM.Core.Interfaces;

namespace ORM.Core
{
    public class LazyLoader : ILazyLoader
    {
        private readonly IDbConnection _dbConnection;

        private readonly ISqlDialect _sqlDialect;

        private bool loaded;
        
        public LazyLoader(IDbConnection dbConnection, ISqlDialect sqlDialect)
        {
            _sqlDialect = sqlDialect;
            _dbConnection = dbConnection;
        }

        public TOne LoadManyToOne<TMany, TOne>(object pk)
        {
            string sql = _sqlDialect.TranslateSelectManyToOne<TMany, TOne>(pk);
            var cmd = BuildCommand(sql);
            var reader = cmd.ExecuteReader();
            return new ObjectReader<TOne>(reader, this);
        }

        public List<TMany> LoadOneToMany<TOne, TMany>(object pk)
        {
            if (loaded)
            {
                return new List<TMany>();
            }
            
            string sql = _sqlDialect.TranslateSelectOneToMany<TOne, TMany>(pk);
            var cmd = BuildCommand(sql);
            var reader = cmd.ExecuteReader();
            loaded = true;
            return new ObjectReader<TMany>(reader, this).ToList();
        }

        public List<TManyB> LoadManyToMany<TManyA, TManyB>(object pk)
        {
            if (loaded)
            {
                return new List<TManyB>();
            }
            
            string sql = _sqlDialect.TranslateSelectManyToMany<TManyA, TManyB>(pk);
            var cmd = BuildCommand(sql);
            var reader = cmd.ExecuteReader();
            loaded = true;
            return new ObjectReader<TManyB>(reader, this).ToList();
        }

        private static IDbCommand BuildCommand(string sql)
        {
            var connection = new NpgsqlConnection("Server=localhost;Port=5432;User Id=postgres;Password=postgres;");
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            return cmd;
        }
    }
}