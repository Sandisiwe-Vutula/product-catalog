using Microsoft.EntityFrameworkCore;
using ProductCatalog.API.Models;

namespace ProductCatalog.API.Data
{

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Name).IsRequired().HasMaxLength(200);
                e.Property(p => p.SKU).IsRequired().HasMaxLength(50);
                e.Property(p => p.Price).HasPrecision(18, 2);
                e.HasIndex(p => p.SKU).IsUnique();

                e.HasOne(p => p.Category)
                 .WithMany(c => c.Products)
                 .HasForeignKey(p => p.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Category>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).IsRequired().HasMaxLength(100);

                e.HasOne(c => c.ParentCategory)
                 .WithMany(c => c.SubCategories)
                 .HasForeignKey(c => c.ParentCategoryId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            var created = new DateTime(2025, 1, 1);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and accessories", ParentCategoryId = null },
                new Category { Id = 2, Name = "Computers", Description = "Desktops, laptops, and peripherals", ParentCategoryId = 1 },
                new Category { Id = 3, Name = "Phones", Description = "Smartphones and accessories", ParentCategoryId = 1 },
                new Category { Id = 4, Name = "Clothing", Description = "Apparel and fashion", ParentCategoryId = null },
                new Category { Id = 5, Name = "Mens", Description = "Men's clothing", ParentCategoryId = 4 },
                new Category { Id = 6, Name = "Womens", Description = "Women's clothing", ParentCategoryId = 4 }
            );

            modelBuilder.Entity<Product>().HasData(

                // Computers
                new Product { Id = 1, Name = "Laptop Pro 15", Description = "High-performance laptop", SKU = "LAP-001", Price = 1299.99m, Quantity = 15, CategoryId = 2, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 2, Name = "Laptop Air 13", Description = "Lightweight ultrabook", SKU = "LAP-002", Price = 999.99m, Quantity = 20, CategoryId = 2, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 3, Name = "Gaming Desktop X", Description = "High-end gaming PC", SKU = "DESK-001", Price = 1899.99m, Quantity = 8, CategoryId = 2, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 4, Name = "Wireless Mouse", Description = "Ergonomic wireless mouse", SKU = "MOU-001", Price = 49.99m, Quantity = 50, CategoryId = 2, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 5, Name = "Mechanical Keyboard", Description = "RGB mechanical keyboard", SKU = "KEY-001", Price = 89.99m, Quantity = 40, CategoryId = 2, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 6, Name = "27\" 4K Monitor", Description = "Ultra HD monitor", SKU = "MON-001", Price = 399.99m, Quantity = 25, CategoryId = 2, CreatedAt = created, UpdatedAt = created },

                // Phones
                new Product { Id = 7, Name = "iPhone 15", Description = "Latest Apple smartphone", SKU = "PHN-001", Price = 999.00m, Quantity = 30, CategoryId = 3, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 8, Name = "Samsung Galaxy S24", Description = "Android flagship", SKU = "PHN-002", Price = 899.00m, Quantity = 18, CategoryId = 3, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 9, Name = "Google Pixel 8", Description = "Google smartphone", SKU = "PHN-003", Price = 799.00m, Quantity = 22, CategoryId = 3, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 10, Name = "Phone Case Pro", Description = "Shockproof case", SKU = "ACC-001", Price = 29.99m, Quantity = 100, CategoryId = 3, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 11, Name = "Wireless Charger", Description = "Fast charging pad", SKU = "ACC-002", Price = 39.99m, Quantity = 60, CategoryId = 3, CreatedAt = created, UpdatedAt = created },

                // Men's Clothing
                new Product { Id = 12, Name = "Classic T-Shirt", Description = "100% cotton t-shirt", SKU = "MEN-001", Price = 19.99m, Quantity = 100, CategoryId = 5, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 13, Name = "Slim Fit Jeans", Description = "Stretch denim jeans", SKU = "MEN-002", Price = 59.99m, Quantity = 75, CategoryId = 5, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 14, Name = "Leather Jacket", Description = "Premium leather jacket", SKU = "MEN-003", Price = 199.99m, Quantity = 12, CategoryId = 5, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 15, Name = "Running Shoes", Description = "Comfort sports shoes", SKU = "MEN-004", Price = 89.99m, Quantity = 40, CategoryId = 5, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 16, Name = "Formal Shirt", Description = "Office wear shirt", SKU = "MEN-005", Price = 39.99m, Quantity = 65, CategoryId = 5, CreatedAt = created, UpdatedAt = created },

                // Women's Clothing
                new Product { Id = 17, Name = "Summer Dress", Description = "Floral summer dress", SKU = "WOM-001", Price = 49.99m, Quantity = 55, CategoryId = 6, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 18, Name = "High Heels", Description = "Elegant high heels", SKU = "WOM-002", Price = 79.99m, Quantity = 30, CategoryId = 6, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 19, Name = "Handbag Classic", Description = "Leather handbag", SKU = "WOM-003", Price = 129.99m, Quantity = 20, CategoryId = 6, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 20, Name = "Yoga Pants", Description = "Stretch yoga pants", SKU = "WOM-004", Price = 34.99m, Quantity = 70, CategoryId = 6, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 21, Name = "Winter Coat", Description = "Warm winter coat", SKU = "WOM-005", Price = 149.99m, Quantity = 15, CategoryId = 6, CreatedAt = created, UpdatedAt = created },

                // Extra Electronics for Pagination
                new Product { Id = 22, Name = "Bluetooth Speaker", Description = "Portable speaker", SKU = "ELE-001", Price = 59.99m, Quantity = 45, CategoryId = 1, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 23, Name = "Smart Watch", Description = "Fitness smartwatch", SKU = "ELE-002", Price = 199.99m, Quantity = 28, CategoryId = 1, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 24, Name = "Noise Cancelling Headphones", Description = "Over-ear headphones", SKU = "ELE-003", Price = 249.99m, Quantity = 33, CategoryId = 1, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 25, Name = "USB-C Hub", Description = "Multiport adapter", SKU = "ELE-004", Price = 69.99m, Quantity = 80, CategoryId = 1, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 26, Name = "External SSD 1TB", Description = "Portable storage drive", SKU = "ELE-005", Price = 159.99m, Quantity = 35, CategoryId = 1, CreatedAt = created, UpdatedAt = created },
                new Product { Id = 27, Name = "Tablet Pro 11", Description = "High resolution tablet", SKU = "ELE-006", Price = 599.99m, Quantity = 18, CategoryId = 1, CreatedAt = created, UpdatedAt = created }
            );
        }
    }
}
