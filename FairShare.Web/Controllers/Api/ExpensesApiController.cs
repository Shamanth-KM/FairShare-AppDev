using FairShare.Web.Data;
using FairShare.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        public ExpensesApiController(ApplicationDbContext ctx) { _ctx = ctx; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses() =>
            await _ctx.Expenses
                .Include(e => e.Group)
                .Include(e => e.PaidByUser)
                .Include(e => e.Shares)
                .AsNoTracking()
                .ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Expense>> GetExpense(int id)
        {
            var e = await _ctx.Expenses
                .Include(x => x.Group)
                .Include(x => x.PaidByUser)
                .Include(x => x.Shares).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(x => x.Id == id);
            return e is null ? NotFound() : e;
        }

        public record CreateExpenseDto(string Description, decimal Amount, int GroupId, int PaidByUserId, DateTime? SpentOnUtc);

        [HttpPost]
        public async Task<ActionResult<Expense>> Create(CreateExpenseDto dto)
        {
            var expense = new Expense
            {
                Description = dto.Description,
                Amount = dto.Amount,
                GroupId = dto.GroupId,
                PaidByUserId = dto.PaidByUserId,
                SpentOnUtc = dto.SpentOnUtc ?? DateTime.UtcNow,
                CreatedUtc = DateTime.UtcNow
            };

            _ctx.Expenses.Add(expense);
            await _ctx.SaveChangesAsync();

            var memberIds = await _ctx.GroupMembers
                .Where(m => m.GroupId == expense.GroupId)
                .Select(m => m.UserId)
                .ToListAsync();

            if (memberIds.Count > 0)
            {
                var each = Math.Round(expense.Amount / memberIds.Count, 2);
                foreach (var uid in memberIds)
                {
                    _ctx.ExpenseShares.Add(new ExpenseShare
                    {
                        ExpenseId = expense.Id,
                        UserId = uid,
                        ShareAmount = each,
                        IsSettled = uid == expense.PaidByUserId
                    });
                }
                await _ctx.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetExpense), new { id = expense.Id }, expense);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Expense expense)
        {
            if (id != expense.Id) return BadRequest();
            _ctx.Entry(expense).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _ctx.Expenses.FindAsync(id);
            if (e is null) return NotFound();
            _ctx.Expenses.Remove(e);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{expenseId:int}/shares/{userId:int}/settle")]
        public async Task<IActionResult> SettleShare(int expenseId, int userId)
        {
            var share = await _ctx.ExpenseShares
                .FirstOrDefaultAsync(s => s.ExpenseId == expenseId && s.UserId == userId);
            if (share is null) return NotFound();
            share.IsSettled = true;
            await _ctx.SaveChangesAsync();
            return NoContent();
        }
    }
}
