using ORM.Application.DbContexts;

namespace ORM.Application.Show
{
    public static class EnsureCreated
    {
        public static void Show()
        {
            var dbContext = new ShopContext();
            dbContext.EnsureCreated();
        }
    }
}