using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleApp1
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }

        public string? Description { get; set; }

        public string? TemporaryData { get; set; }
    }

    public class StoreDbContext : DbContext
    {
        public DbSet<Product> StoreProducts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                @"Server=(localdb)\mssqllocaldb;Database=Onlinestrdb;Trusted_Connection=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //1
            modelBuilder.Entity<Product>()
                .HasKey(p => p.Id);

            //2
            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .HasMaxLength(100);

            //3
            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .IsRequired();

            //4
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(10,2)");

            //5
            modelBuilder.Entity<Product>()
                .Property(p => p.StockQuantity)
                .HasDefaultValue(0);

            //6
            modelBuilder.Entity<Product>()
                .Property(p => p.Description)
                .IsRequired(false);

            //7
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name)
                .IsUnique();

            // 8
            modelBuilder.Entity<Product>()
                .ToTable("StoreProducts");

            //9
            modelBuilder.Entity<Product>()
                .HasCheckConstraint("CK_StoreProducts_Price", "Price >= 0");
        }
    }

    class Program
    {
        static void Main()
        {
            using (var context = new StoreDbContext())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                Console.WriteLine("Database created");
            }
        }
    }
}
