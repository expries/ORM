using System.Linq;
using ORM.Core.Caching;
using ORM.Core.Interfaces;

namespace ORM.Core.Configuration
{
    /// <summary>
    /// Applies a configuration on a database context context
    /// </summary>
    public class OptionsBuilder
    {
        /// <summary>
        /// Used for caching entity
        /// </summary>
        internal ICache Cache;

        /// <summary>
        /// Used for building commands to be executed against the current database connection
        /// </summary>
        internal ICommandBuilder? CommandBuilder;

        /// <summary>
        /// Query provider for executing LINQ expressions against the current database connection 
        /// </summary>
        internal IQueryProvider? QueryProvider;

        public OptionsBuilder()
        {
            Cache = new EntityCache();
        }

        /// <summary>
        /// Use an entity cache
        /// </summary>
        public void UseEntityCache()
        {
            Cache = new EntityCache();
        }

        /// <summary>
        /// Use a state tracking cache
        /// </summary>
        public void UseStateTrackingCache()
        {
            Cache = new StateTrackingCache();
        }

        /// <summary>
        /// Use a specific command builder.
        /// This is used for implementing your own translator for database context operations.
        /// </summary>
        /// <param name="commandBuilder"></param>
        public void UseCommandBuilder(ICommandBuilder commandBuilder)
        {
            CommandBuilder = commandBuilder;
        }

        /// <summary>
        /// Use a specific query provider.
        /// This is used for implementing your own expression tree translator. 
        /// </summary>
        /// <param name="queryProvider"></param>
        public void UseQueryProvider(IQueryProvider queryProvider)
        {
            QueryProvider = queryProvider;
        }
    }
}