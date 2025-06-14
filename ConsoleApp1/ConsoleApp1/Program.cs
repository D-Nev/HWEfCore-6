using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;


namespace ConsoleApp1;

    class Program
    {
        static void Main()
        {
            using (ApplicationContext db = new ApplicationContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();

                var company1 = new Company { Name = "Tech Enterprises" };
                var company2 = new Company { Name = "Global Solutions Inc." };

                var user1 = new User { Name = "John Doe", Age = 41, Company = company1 };
                var user2 = new User { Name = "Jane Smith", Age = 18, Company = company1 };
                var user3 = new User { Name = "Mark Johnson", Age = 35, Company = company2 };

                db.Users.AddRange(user1, user2, user3);
                db.SaveChanges();

                //1
                 db.Database.ExecuteSqlRaw(@"""
                 CREATE PROCEDURE GetUsersAndCompanies
                 AS
                 BEGIN
                        SELECT u.Id as UserId, u.Name as UserName, u.Age as UserAge, u.CompanyId as CompanyId, c.Name as CompanyName
                        FROM Users u
                        INNER JOIN Companies c ON u.CompanyId = c.Id;
                 END
                 """);

                //2
                 db.Database.ExecuteSqlRaw(@"""
                 CREATE PROCEDURE GetUsersByNameLike
                 @NamePattern NVARCHAR(100)
                 AS
                    BEGIN
                        SELECT *
                        FROM Users
                        WHERE Name LIKE '%' + @NamePattern + '%';
                 END
                 """);

                //3
                db.Database.ExecuteSqlRaw(@"""
                CREATE PROCEDURE GetAverageUserAge
                @AverageAge DECIMAL(10, 2) OUTPUT
                AS
                BEGIN
                    SELECT @AverageAge = AVG(Age)
                    FROM Users;
                END;
                """);
            }       

                using (ApplicationContext db = new ApplicationContext())
                {
                //1
                var allUsers = db.UserWithCompanyViewModels.FromSqlRaw("EXECUTE GetUsersAndCompanies").ToList();
                //2
                SqlParameter nameParam = new("@NamePattern", "John");
                var allUsersByName = db.Users.FromSqlRaw("EXECUTE GetUsersByNameLike @NamePattern", nameParam).ToList();
                //3
                var averageAgeParam = new SqlParameter("@AverageAge", SqlDbType.Decimal)
                {
                    Direction = ParameterDirection.Output
                };
                db.Database.ExecuteSqlRaw("EXECUTE GetAverageUserAge @AverageAge OUTPUT", averageAgeParam);
                Console.WriteLine(averageAgeParam.Value);
           
                }
        }
    }

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public int CompanyId { get; set; }
    public Company Company { get; set; }
}

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<User> Users { get; set; }
}

public class UserWithCompanyViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public int UserAge { get; set; }
    public string CompanyName { get; set; }
    public int CompanyId { get; set; }
}

public class ApplicationContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<UserWithCompanyViewModel> UserWithCompanyViewModels { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(@"Server=(localdb)\\mssqllocaldb;Database=uscomdb;Trusted_Connection=True;TrustServerCertificate=True;");
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserWithCompanyViewModel>().HasNoKey();
        base.OnModelCreating(modelBuilder);
    }
}



