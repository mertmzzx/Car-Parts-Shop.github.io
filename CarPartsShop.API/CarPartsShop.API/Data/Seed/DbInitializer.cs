using CarPartsShop.API.Models;
using CarPartsShop.API.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarPartsShop.API.Data.Seed
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context, string? testUserId)
        {
            context.Database.Migrate();

            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Brakes", Description = "Brake pads, discs and related parts" },
                    new Category { Name = "Engine", Description = "Engine components and accessories" },
                    new Category { Name = "Suspension", Description = "Shock absorbers, springs and more" }
                };

                context.Categories.AddRange(categories);
                context.SaveChanges();
            }

            if (!context.Parts.Any())
            {
                var brakes = context.Categories.FirstOrDefault(c => c.Name == "Brakes");
                var engine = context.Categories.FirstOrDefault(c => c.Name == "Engine");
                var suspension = context.Categories.FirstOrDefault(c => c.Name == "Suspension");

                var parts = new List<Part>
                {
                    new Part { Name = "Brake Pad", Sku = "BRK-001", Description = "High quality brake pad", Price = 45.99m, QuantityInStock = 50, CategoryId = brakes!.Id },
                    new Part { Name = "Oil Filter", Sku = "ENG-002", Description = "Durable oil filter", Price = 12.50m, QuantityInStock = 100, CategoryId = engine!.Id },
                    new Part { Name = "Shock Absorber", Sku = "SUS-003", Description = "Heavy-duty shock absorber", Price = 89.00m, QuantityInStock = 30, CategoryId = suspension!.Id }
                };

                context.Parts.AddRange(parts);
                context.SaveChanges();
            }

            if (!string.IsNullOrWhiteSpace(testUserId) && !context.Customers.Any())
            {
                var customers = new List<Customer>
                {
                    new Customer
                    {
                        FirstName = "Test",
                        LastName = "User",
                        Email = "test@example.com",
                        UserId = testUserId 
                    }
                };

                context.Customers.AddRange(customers);
                context.SaveChanges();
            }
        }


    }
}
