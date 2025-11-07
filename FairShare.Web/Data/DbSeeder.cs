using FairShare.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Web.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            await db.Database.MigrateAsync();

            if (!db.Users.Any())
            {
                var u1 = new User { Name="Alex", Email="alex@example.com" };
                var u2 = new User { Name="Blake", Email="blake@example.com" };
                var g1 = new Group { Name="Roommates", Description="Fall 2025" };
                db.AddRange(u1, u2, g1);
                await db.SaveChangesAsync();

                db.GroupMembers.AddRange(
                    new GroupMember { GroupId = g1.Id, UserId = u1.Id },
                    new GroupMember { GroupId = g1.Id, UserId = u2.Id }
                );
                await db.SaveChangesAsync();

                var e = new Expense {
                    Description = "Groceries",
                    Amount = 80.00m,
                    GroupId = g1.Id,
                    PaidByUserId = u1.Id,
                    SpentOnUtc = DateTime.UtcNow
                };
                db.Expenses.Add(e);
                await db.SaveChangesAsync();

                db.ExpenseShares.AddRange(
                    new ExpenseShare { ExpenseId = e.Id, UserId = u1.Id, ShareAmount = 40m, IsSettled=false },
                    new ExpenseShare { ExpenseId = e.Id, UserId = u2.Id, ShareAmount = 40m, IsSettled=false }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
