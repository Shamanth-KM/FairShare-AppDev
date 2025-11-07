using System.ComponentModel.DataAnnotations;

namespace FairShare.Web.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 9999999)]
        public decimal Amount { get; set; }

        public DateTime SpentOnUtc { get; set; } = DateTime.UtcNow;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public int? GroupId { get; set; }
        public Group? Group { get; set; } = default!;

        public int? PaidByUserId { get; set; }
        public User? PaidByUser { get; set; } = default!;

        public ICollection<ExpenseShare> Shares { get; set; } = new List<ExpenseShare>();
    }
}
