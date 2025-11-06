using System.ComponentModel.DataAnnotations;

namespace FairShare.Web.Models
{
    public class ExpenseShare
    {
        public int Id { get; set; }
        public int ExpenseId { get; set; }
        public Expense Expense { get; set; } = default!;
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        [Range(0, 9999999)]
        public decimal ShareAmount { get; set; }

        public bool IsSettled { get; set; } = false;
    }
}
