using Microsoft.EntityFrameworkCore;
using FairShare.Web.Models;

namespace FairShare.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) {}

        public DbSet<User> Users => Set<User>();
        public DbSet<Group> Groups => Set<Group>();
        public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
        public DbSet<Expense> Expenses => Set<Expense>();
        public DbSet<ExpenseShare> ExpenseShares => Set<ExpenseShare>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<GroupMember>()
                .HasIndex(gm => new { gm.UserId, gm.GroupId })
                .IsUnique();

            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(gm => gm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Expense>()
                .Property(e => e.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.Group)
                .WithMany(g => g.Expenses)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Expense>()
                .HasOne(e => e.PaidByUser)
                .WithMany(u => u.ExpensesPaid)
                .HasForeignKey(e => e.PaidByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExpenseShare>()
                .Property(es => es.ShareAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ExpenseShare>()
                .HasOne(es => es.Expense)
                .WithMany(e => e.Shares)
                .HasForeignKey(es => es.ExpenseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExpenseShare>()
                .HasOne(es => es.User)
                .WithMany(u => u.ExpenseShares)
                .HasForeignKey(es => es.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
