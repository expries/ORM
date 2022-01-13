namespace ORM.Application.Show
{
    public static class EnsureCreated
    {
        public static void Show()
        {
            var dbContext = DbFactory.CreateDbContext();
            dbContext.EnsureCreated();
        }
    }
}