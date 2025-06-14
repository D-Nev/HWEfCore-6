using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ConsoleApp2;

class Program
{
    static void Main()
    {
        using (var db = new ApplicationContext())
        {
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            CreateStoredProcedures(db);
        }

        using (var db = new ApplicationContext())
        {
            //1
            var userById = db.Users.FromSqlRaw("EXEC GetUserById @UserId",
                new SqlParameter("@UserId", 1)).ToList();

            //2
            var activeUsers = db.Users.FromSqlRaw("EXEC GetActiveUsers").ToList();

            //3
            db.Database.ExecuteSqlRaw("EXEC AddUser @Name, @Email, @IsActive, @RegistrationDate, @BirthDate",
                new SqlParameter("@Name", "New User"),
                new SqlParameter("@Email", "new@example.com"),
                new SqlParameter("@IsActive", true),
                new SqlParameter("@RegistrationDate", DateTime.Now),
                new SqlParameter("@BirthDate", new DateTime(1990, 1, 1)));

            //4
            db.Database.ExecuteSqlRaw("EXEC UpdateUserEmail @UserId, @NewEmail",
                new SqlParameter("@UserId", 1),
                new SqlParameter("@NewEmail", "updated@example.com"));

            //5
            db.Database.ExecuteSqlRaw("EXEC DeleteUser @UserId",
                new SqlParameter("@UserId", 3));

            //6
            var countParam = new SqlParameter("@Count", SqlDbType.Int) { Direction = ParameterDirection.Output };
            db.Database.ExecuteSqlRaw("EXEC GetActiveUsersCount @Count OUTPUT", countParam);
            int activeUsersCount = (int)countParam.Value;

            //7
            var usersByDate = db.Users.FromSqlRaw("EXEC GetUsersByRegistrationDateRange @StartDate, @EndDate",
                new SqlParameter("@StartDate", new DateTime(2022, 1, 1)),
                new SqlParameter("@EndDate", new DateTime(2023, 12, 31))).ToList();

            //8
            var avgAgeParam = new SqlParameter("@AverageAge", SqlDbType.Float) { Direction = ParameterDirection.Output };
            db.Database.ExecuteSqlRaw("EXEC GetAverageAge @AverageAge OUTPUT", avgAgeParam);
            double averageAge = (double)avgAgeParam.Value;

            //9
            var userOrders = db.Orders.FromSqlRaw("EXEC GetOrdersByUserId @UserId",
                new SqlParameter("@UserId", 1)).ToList();

            //10
            var products = db.Products.FromSqlRaw("EXEC GetProductsByPriceRange @MinPrice, @MaxPrice",
                new SqlParameter("@MinPrice", 100),
                new SqlParameter("@MaxPrice", 500)).ToList();

            //11
            var orderSummary = db.Set<OrderSummary>().FromSqlRaw("EXEC GetUserOrdersSummary @UserId",
                new SqlParameter("@UserId", 1)).ToList();

            //12
            var expensiveOrder = db.Orders.FromSqlRaw("EXEC GetMostExpensiveOrder").ToList();
        }
    }

    static void CreateStoredProcedures(ApplicationContext db)
    {
        db.Database.ExecuteSqlRaw(@"
                CREATE OR ALTER PROCEDURE GetUserById
                    @UserId INT
                AS BEGIN SELECT * FROM Users WHERE Id = @UserId END");

        db.Database.ExecuteSqlRaw(@"
                CREATE OR ALTER PROCEDURE GetActiveUsers
                AS BEGIN SELECT * FROM Users WHERE IsActive = 1 END");

        db.Database.ExecuteSqlRaw(@"
                CREATE OR ALTER PROCEDURE AddUser
                    @Name NVARCHAR(100),
                    @Email NVARCHAR(100),
                    @IsActive BIT,
                    @RegistrationDate DATETIME,
                    @BirthDate DATETIME
                AS BEGIN
                    INSERT INTO Users (Name, Email, IsActive, RegistrationDate, BirthDate)
                    VALUES (@Name, @Email, @IsActive, @RegistrationDate, @BirthDate)
                END");

        db.Database.ExecuteSqlRaw(@"
                CREATE OR ALTER PROCEDURE UpdateUserEmail
                    @UserId INT,
                    @NewEmail NVARCHAR(100)
                AS BEGIN UPDATE Users SET Email = @NewEmail WHERE Id = @UserId END");

        db.Database.ExecuteSqlRaw(@"
                CREATE OR ALTER PROCEDURE DeleteUser
                    @UserId INT
                AS BEGIN DELETE FROM Users WHERE Id = @UserId END");

        db.Database.ExecuteSqlRaw(@"
                CREATE OR ALTER PROCEDURE GetActiveUsersCount
                    @Count INT OUTPUT
                AS BEGIN SELECT @Count = COUNT(*) FROM Users WHERE IsActive = 1 END");

        db.Database.ExecuteSqlRaw(@"
                CREATE OR ALTER PROCEDURE GetUsersByRegistrationDateRange
                    @StartDate DATETIME,
                    @EndDate DATETIME
                AS BEGIN
                    SELECT * FROM Users 
                    WHERE RegistrationDate BETWEEN @StartDate AND @EndDate
                END");

        db.Database.ExecuteSqlRaw(@"
                CREATE OR ALTER PROCEDURE GetAverageAge
                    @AverageAge FLOAT OUTPUT
                AS BEGIN
                    SELECT @AverageAge = AVG(DATEDIFF(YEAR, BirthDate, GETDATE())) 
                    FROM Users
                END");

        db.Database.ExecuteSqlRaw(@"
                CREATE OR ALTER PROCEDURE GetOrdersByUserId
                    @UserId INT
                AS BEGIN SELECT * FROM Orders WHERE UserId = @UserId END");

        db.Database.ExecuteSqlRaw(@"
                CREATE OR ALTER PROCEDURE GetProductsByPriceRange
                    @MinPrice DECIMAL(18,2) = NULL,
                    @MaxPrice DECIMAL(18,2) = NULL
                AS BEGIN
                    SELECT * FROM Products
                    WHERE (@MinPrice IS NULL OR Price >= @MinPrice)
                        AND (@MaxPrice IS NULL OR Price <= @MaxPrice)
                END");

        db.Database.ExecuteSqlRaw(@"
                CREATE OR ALTER PROCEDURE GetUserOrdersSummary
                    @UserId INT
                AS BEGIN
                    SELECT Id, OrderDate, TotalAmount 
                    FROM Orders 
                    WHERE UserId = @UserId
                END");

        db.Database.ExecuteSqlRaw(@"
                CREATE OR ALTER PROCEDURE GetMostExpensiveOrder
                AS BEGIN
                    SELECT TOP 1 * 
                    FROM Orders 
                    ORDER BY TotalAmount DESC
                END");
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime RegistrationDate { get; set; }
    public DateTime BirthDate { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
}

public class OrderSummary
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ApplicationContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=testdb;Trusted_Connection=True;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderSummary>().HasNoKey();

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@example.com",
                IsActive = true,
                RegistrationDate = new DateTime(2023, 5, 10),
                BirthDate = new DateTime(1990, 3, 15)
            },
            new User
            {
                Id = 2,
                Name = "Alice Smith",
                Email = "alice@example.com",
                IsActive = true,
                RegistrationDate = new DateTime(2022, 8, 21),
                BirthDate = new DateTime(1995, 7, 20)
            },
            new User
            {
                Id = 3,
                Name = "Michael Johnson",
                Email = "michael@example.com",
                IsActive = false,
                RegistrationDate = new DateTime(2021, 2, 5),
                BirthDate = new DateTime(1985, 11, 30)
            }
        );

        modelBuilder.Entity<Order>().HasData(
            new Order
            {
                Id = 1,
                UserId = 1,
                OrderDate = new DateTime(2024, 1, 10),
                TotalAmount = 120.50m
            },
            new Order
            {
                Id = 2,
                UserId = 2,
                OrderDate = new DateTime(2024, 2, 15),
                TotalAmount = 250.00m
            },
            new Order
            {
                Id = 3,
                UserId = 1,
                OrderDate = new DateTime(2024, 3, 5),
                TotalAmount = 75.30m
            }
        );

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = 1,
                Name = "Dell XPS 13 Laptop",
                Price = 1200.99m
            },
            new Product
            {
                Id = 2,
                Name = "Logitech MX Master 3 Mouse",
                Price = 99.99m
            },
            new Product
            {
                Id = 3,
                Name = "LG UltraGear 27\" Monitor",
                Price = 399.99m
            },
            new Product
            {
                Id = 4,
                Name = "Razer BlackWidow Keyboard",
                Price = 149.50m
            }
        );
    }
}

