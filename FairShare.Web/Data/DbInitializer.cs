using FairShare.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Web.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext ctx)
        {
            await ctx.Database.MigrateAsync();

            if (await ctx.Users.AnyAsync()) return;

            var alice = new User { Name = "Alice", Email = "alice@example.com", IsActive = true, CreatedUtc = DateTime.UtcNow };
            var bob   = new User { Name = "Bob",   Email = "bob@example.com",   IsActive = true, CreatedUtc = DateTime.UtcNow };
            var cara  = new User { Name = "Cara",  Email = "cara@example.com",  IsActive = true, CreatedUtc = DateTime.UtcNow };
            ctx.Users.AddRange(alice, bob, cara);
            await ctx.SaveChangesAsync();

            var roomies = new Group { Name = "Roomies", Description = "Apartment expenses", CreatedUtc = DateTime.UtcNow };
            ctx.Groups.Add(roomies);
            await ctx.SaveChangesAsync();

            ctx.GroupMembers.AddRange(
                new GroupMember { GroupId = roomies.Id, UserId = alice.Id },
                new GroupMember { GroupId = roomies.Id, UserId = bob.Id },
                new GroupMember { GroupId = roomies.Id, UserId = cara.Id }
            );
            await ctx.SaveChangesAsync();

            var groceries = new Expense
            {
                Description = "Groceries",
                Amount = 150.00m,
                GroupId = roomies.Id,
                PaidByUserId = alice.Id,
                SpentOnUtc = DateTime.UtcNow.Date,
                CreatedUtc = DateTime.UtcNow
            };
            ctx.Expenses.Add(groceries);
            await ctx.SaveChangesAsync();

            var each = Math.Round(groceries.Amount / 3m, 2);
            ctx.ExpenseShares.AddRange(
                new ExpenseShare { ExpenseId = groceries.Id, UserId = alice.Id, ShareAmount = each, IsSettled = true  },
                new ExpenseShare { ExpenseId = groceries.Id, UserId = bob.Id,   ShareAmount = each, IsSettled = false },
                new ExpenseShare { ExpenseId = groceries.Id, UserId = cara.Id,  ShareAmount = each, IsSettled = false }
            );
            await ctx.SaveChangesAsync();
        }
    }
}
