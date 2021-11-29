using ORM.Application.Entities;

namespace ORM.Application.Show
{
    public static class InsertObject
    {
        public static void ShowProduct()
        {
            var dbContext = Program.CreateDbContext();
            var product = new Product {Name = "My Book", Price = 10, Purchases = 5};
            dbContext.Save(product);
        }

        public static void ShowSeller()
        {
            var dbContext = Program.CreateDbContext();
            var seller = new ORM.Application.Entities.Seller {Name = "Seller 123" };
            dbContext.Save(seller);
        }
    }
}