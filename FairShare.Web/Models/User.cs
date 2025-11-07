using System.ComponentModel.DataAnnotations;

namespace FairShare.Web.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
        public ICollection<Expense> ExpensesPaid { get; set; } = new List<Expense>();
        public ICollection<ExpenseShare> ExpenseShares { get; set; } = new List<ExpenseShare>();
    }
}
