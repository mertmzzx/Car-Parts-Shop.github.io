using CarPartsShop.API.Auth;
using CarPartsShop.API.Models;
using CarPartsShop.API.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace CarPartsShop.API.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(this IServiceProvider services, IConfiguration config)
        {
            using var scope = services.CreateScope();

            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Roles
            foreach (var role in new[] { Roles.Customer, Roles.SalesAssistant, Roles.Administrator })
                if (!await roleMgr.RoleExistsAsync(role))
                    await roleMgr.CreateAsync(new AppRole { Name = role });

            var adminCfg = config.GetSection("AdminSeed");
            var adminUserName = adminCfg["UserName"];
            var adminEmail = adminCfg["Email"];
            var adminPassword = adminCfg["Password"];

            if (!string.IsNullOrWhiteSpace(adminUserName) &&
                !string.IsNullOrWhiteSpace(adminEmail) &&
                !string.IsNullOrWhiteSpace(adminPassword))
            {
                var admin = await userMgr.FindByNameAsync(adminUserName);
                if (admin == null)
                {
                    admin = new AppUser
                    {
                        UserName = adminUserName,
                        Email = adminEmail,
                        FirstName = "Admin",
                        LastName = "User"
                    };
                    var result = await userMgr.CreateAsync(admin, adminPassword);
                    if (result.Succeeded)
                        await userMgr.AddToRoleAsync(admin, Roles.Administrator);
                }
            }

            // Data Seed
            await db.Database.MigrateAsync();

            if (!await db.Categories.AnyAsync())
            {
                db.Categories.AddRange(
                    new Category { Name = "Brakes" },
                    new Category { Name = "Engine" },
                    new Category { Name = "Suspension" },
                    new Category { Name = "Filters" },
                    new Category { Name = "Electrical" },
                    new Category { Name = "Lighting" }
                );
                await db.SaveChangesAsync();
            }

            async Task<int> Cat(string name) =>
                (await db.Categories.FirstAsync(c => c.Name == name)).Id;

            if (!await db.Parts.AnyAsync())
            {
                db.Parts.AddRange(
                    new Part
                    {
                        Name = "Brake Pad Set",
                        Sku = "BRK-001",
                        Description = "High-performance ceramic pads.",
                        Price = 45.99m,
                        QuantityInStock = 82,
                        CategoryId = await Cat("Brakes"),
                        ImageUrl = null 
                    },
                    new Part
                    {
                        Name = "Brake Disc",
                        Sku = "BRK-002",
                        Description = "Vented disc for better cooling.",
                        Price = 69.50m,
                        QuantityInStock = 40,
                        CategoryId = await Cat("Brakes"),
                        ImageUrl = null
                    },
                    new Part
                    {
                        Name = "Oil Filter",
                        Sku = "ENG-OF-100",
                        Description = "OEM-grade engine oil filter.",
                        Price = 12.50m,
                        QuantityInStock = 201,
                        CategoryId = await Cat("Engine"),
                        ImageUrl = null
                    },
                    new Part
                    {
                        Name = "Timing Belt",
                        Sku = "ENG-TB-200",
                        Description = "Reinforced timing belt.",
                        Price = 39.90m,
                        QuantityInStock = 61,
                        CategoryId = await Cat("Engine"),
                        ImageUrl = null
                    },
                    new Part
                    {
                        Name = "Shock Absorber",
                        Sku = "SUS-003",
                        Description = "Gas-charged shock absorber.",
                        Price = 89.00m,
                        QuantityInStock = 30,
                        CategoryId = await Cat("Suspension"),
                        ImageUrl = null
                    },
                    new Part
                    {
                        Name = "Air Filter",
                        Sku = "FLT-AF-001",
                        Description = "High-flow air filter.",
                        Price = 17.99m,
                        QuantityInStock = 150,
                        CategoryId = await Cat("Filters"),
                        ImageUrl = null
                    },
                    new Part
                    {
                        Name = "Alternator",
                        Sku = "ELE-ALT-001",
                        Description = "12V high-output alternator.",
                        Price = 199.00m,
                        QuantityInStock = 15,
                        CategoryId = await Cat("Electrical"),
                        ImageUrl = null
                    },
                    new Part
                    {
                        Name = "H7 Bulb",
                        Sku = "LGT-H7-055",
                        Description = "Halogen bulb H7 55W.",
                        Price = 6.50m,
                        QuantityInStock = 302,
                        CategoryId = await Cat("Lighting"),
                        ImageUrl = null
                    }
                );

                await db.SaveChangesAsync();
            }

            // extra parts
            {
                var catMap = await db.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);

                // placeholder image
                string PH2(string text) => $"https://placehold.co/400x300?text={WebUtility.UrlEncode(text)}";

                var moreParts = new[]
                {
                    // Brakes
                    new { Name = "Front Brake Pad Set Premium", Sku = "BRK-101", Desc = "Low-dust ceramic pads for quiet, confident stops.", Price = 54.99m, Qty = 60, Cat = "Brakes", Img = "Front Brake Pad Set" },
                    new { Name = "Rear Brake Pad Set",            Sku = "BRK-102", Desc = "OE-style semi-metallic pads.",                     Price = 39.99m, Qty = 85, Cat = "Brakes", Img = "Rear Brake Pads" },
                    new { Name = "Brake Caliper (Left Front)",    Sku = "BRK-201", Desc = "Remanufactured caliper with bracket.",             Price = 89.00m, Qty = 20, Cat = "Brakes", Img = "Brake Caliper LF" },
            
                    // Engine
                    new { Name = "Oil Filter Pro",                Sku = "ENG-OF-200", Desc = "High capacity filter for extended intervals.",  Price = 14.50m, Qty = 120, Cat = "Engine", Img = "Oil Filter Pro" },
                    new { Name = "Spark Plug Set (4pcs)",         Sku = "ENG-SP-010", Desc = "Iridium plugs for reliable ignition.",          Price = 24.99m, Qty = 150, Cat = "Engine", Img = "Spark Plug Set" },
                    new { Name = "Timing Chain Kit",              Sku = "ENG-TC-300", Desc = "Complete kit with guides and tensioner.",       Price = 169.00m, Qty = 12,  Cat = "Engine", Img = "Timing Chain Kit" },
            
                    // Suspension
                    new { Name = "Front Strut Assembly",          Sku = "SUS-STR-110", Desc = "Complete quick-strut assembly.",               Price = 129.00m, Qty = 18,  Cat = "Suspension", Img = "Front Strut" },
                    new { Name = "Lower Control Arm (Right)",     Sku = "SUS-CA-210",  Desc = "Forged arm with pre-installed bushing.",       Price = 79.00m,  Qty = 25,  Cat = "Suspension", Img = "Control Arm R" },
            
                    // Filters
                    new { Name = "Cabin Air Filter",              Sku = "FLT-CF-050",  Desc = "HEPA-grade cabin filter.",                     Price = 15.99m, Qty = 200, Cat = "Filters", Img = "Cabin Filter" },
                    new { Name = "Fuel Filter Inline",            Sku = "FLT-FF-070",  Desc = "High flow fuel filter.",                       Price = 19.49m, Qty = 140, Cat = "Filters", Img = "Fuel Filter" },
            
                    // Electrical
                    new { Name = "Starter Motor 12V",             Sku = "ELE-STA-040", Desc = "High-torque starter motor.",                   Price = 159.00m, Qty = 10,  Cat = "Electrical", Img = "Starter Motor" },
                    new { Name = "Battery 60Ah",                  Sku = "ELE-BAT-060", Desc = "Maintenance-free battery, 540A CCA.",          Price = 115.00m, Qty = 22,  Cat = "Electrical", Img = "Battery 60Ah" },
            
                    // Lighting
                    new { Name = "LED H7 Bulb (Pair)",            Sku = "LGT-H7-LED",  Desc = "Cool white LED upgrade, canbus ready.",        Price = 29.99m, Qty = 90,  Cat = "Lighting", Img = "LED H7 Pair" },
                    new { Name = "Headlight Assembly (Left)",     Sku = "LGT-HL-200",  Desc = "DOT-approved replacement housing.",            Price = 139.00m, Qty = 8,   Cat = "Lighting", Img = "Headlight Left" },
                };

                foreach (var s in moreParts)
                {
                    if (!catMap.TryGetValue(s.Cat, out var catId))
                        continue; // skip if category missing

                    // skip if SKU already exists
                    var exists = await db.Parts.AsNoTracking().AnyAsync(p => p.Sku == s.Sku);
                    if (exists) continue;

                    db.Parts.Add(new Part
                    {
                        Name = s.Name,
                        Sku = s.Sku,
                        Description = s.Desc,
                        Price = s.Price,
                        QuantityInStock = s.Qty,
                        CategoryId = catId,
                        ImageUrl = PH2(s.Img ?? s.Name) // use placeholder
                    });
                }

                if (db.ChangeTracker.HasChanges())
                    await db.SaveChangesAsync();
            }


            // Force inline SVG for ALL parts (no external DNS)
            static string SvgDataUri(string text)
            {
                // Build a minimal SVG and URL-encode it for the data URI
                var svg =
$@"<svg xmlns='http://www.w3.org/2000/svg' width='400' height='300'>
  <rect width='100%' height='100%' fill='#e9ecef'/>
  <text x='50%' y='50%' dominant-baseline='middle' text-anchor='middle'
        font-family='Arial, Helvetica, sans-serif' font-size='20' fill='#6c757d'>{WebUtility.HtmlEncode(text)}</text>
</svg>";
                return "data:image/svg+xml;utf8," + Uri.EscapeDataString(svg);
            }

            var parts = await db.Parts.ToListAsync();
            foreach (var p in parts)
                p.ImageUrl = SvgDataUri(p.Name ?? p.Sku);

            await db.SaveChangesAsync();
        }
    }
}
