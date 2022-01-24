using System.Linq;
using ORM.Core.Caching;
using ORM.Core.Interfaces;

namespace ORM.Core.Configuration
{
    public class OptionsBuilder
    {
        internal ICache Cache;

        internal ICommandBuilder? CommandBuilder;

        internal IQueryProvider? QueryProvider;

        public OptionsBuilder()
        {
            Cache = new EntityCache();
        }

        public void UseEntityCache()
        {
            Cache = new EntityCache();
        }

        public void UseStateTrackingCache()
        {
            Cache = new StateTrackingCache();
        }

        public void UseCommandBuilder(ICommandBuilder func)
        {
            CommandBuilder = func;
        }

        public void UseQueryProvider(IQueryProvider func)
        {
            QueryProvider = func;
        }
    }
}