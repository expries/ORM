using ORM.Application.DbContexts;
using ORM.Core;

namespace ORM.Application.Show
{
    public static class EnsureCreated
    {
        private static readonly DbContext DbContext = new SchoolContext();
        
        public static void Show()
        {
            DbContext.EnsureCreated();
        }
    }
}