using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FairShare.Web.Data;      // DbContext namespace
using FairShare.Web.Models;    // Expense, Group, User, ExpenseShare, GroupMember

namespace FairShare.Web.Controllers
{
    public class ExpensesController : Controller
    {
        private readonly ApplicationDbContext _ctx;
        public ExpensesController(ApplicationDbContext ctx) => _ctx = ctx;

        // GET: /Expenses
        public async Task<IActionResult> Index()
        {
            var list = await _ctx.Expenses
                .Include(e => e.Group)
                .Include(e => e.PaidByUser)
                .AsNoTracking()
                .OrderByDescending(e => e.SpentOnUtc)
                .ToListAsync();

            return View(list);
        }

        // GET: /Expenses/Create
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new Expense { SpentOnUtc = System.DateTime.UtcNow });
        }

        // POST: /Expenses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Description,Amount,GroupId,PaidByUserId,SpentOnUtc")] Expense e)
        {
            // 🔎 prove the action is hit (comment out when done)
            ViewData["DebugHit"] = "POST /Expenses/Create was hit";

            // Basic validation
            if (string.IsNullOrWhiteSpace(e.Description))
                ModelState.AddModelError(nameof(e.Description), "Description is required.");
            if (e.Amount <= 0)
                ModelState.AddModelError(nameof(e.Amount), "Enter a positive amount.");

            // normalize date to UTC
            if (e.SpentOnUtc.Kind == System.DateTimeKind.Unspecified)
                e.SpentOnUtc = System.DateTime.SpecifyKind(e.SpentOnUtc, System.DateTimeKind.Utc);

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(e);
            }

            try
            {
                // fields many schemas require
                e.CreatedUtc = System.DateTime.UtcNow;

                _ctx.Expenses.Add(e);
                await _ctx.SaveChangesAsync();

                // mirror your API: create shares for group members
                var memberIds = await _ctx.GroupMembers
                    .Where(m => m.GroupId == e.GroupId)
                    .Select(m => m.UserId)
                    .ToListAsync();

                if (memberIds.Count > 0)
                {
                    var each = System.Math.Round(e.Amount / memberIds.Count, 2);
                    foreach (var uid in memberIds)
                    {
                        _ctx.ExpenseShares.Add(new ExpenseShare
                        {
                            ExpenseId = e.Id,
                            UserId = uid,
                            ShareAmount = each,
                            IsSettled = uid == e.PaidByUserId
                        });
                    }
                    await _ctx.SaveChangesAsync();
                }

                TempData["Toast"] = "Expense added.";
                return RedirectToAction(nameof(Index));
            }
            catch (System.Exception ex)
            {
                // ✅ Surface the real DB error (FK/NOT NULL/etc.) on the form
                ModelState.AddModelError(string.Empty, "Save failed: " + ex.Message);
                await LoadDropdowns();
                return View(e);
            }
        }

        private async Task LoadDropdowns()
        {
            ViewBag.GroupId = new SelectList(
                await _ctx.Groups.AsNoTracking().OrderBy(g => g.Name).ToListAsync(),
                "Id", "Name");

            ViewBag.PaidByUserId = new SelectList(
                await _ctx.Users.AsNoTracking().OrderBy(u => u.Name).ToListAsync(),
                "Id", "Name");
        }

        // GET: /Expenses/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var e = await _ctx.Expenses
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (e == null) return NotFound();

            await LoadDropdowns();
            return View(e);
        }

        // POST: /Expenses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Description,Amount,GroupId,PaidByUserId,SpentOnUtc")] Expense e)
        {
            if (id != e.Id) return NotFound();

            if (string.IsNullOrWhiteSpace(e.Description))
                ModelState.AddModelError(nameof(e.Description), "Description is required.");
            if (e.Amount <= 0)
                ModelState.AddModelError(nameof(e.Amount), "Enter a positive amount.");

            if (e.SpentOnUtc.Kind == DateTimeKind.Unspecified)
                e.SpentOnUtc = DateTime.SpecifyKind(e.SpentOnUtc, DateTimeKind.Utc);

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View(e);
            }

            // Load current row from DB, update allowed fields
            var dbE = await _ctx.Expenses.FirstOrDefaultAsync(x => x.Id == id);
            if (dbE == null) return NotFound();

            dbE.Description = e.Description;
            dbE.Amount = e.Amount;
            dbE.GroupId = e.GroupId;
            dbE.PaidByUserId = e.PaidByUserId;
            dbE.SpentOnUtc = e.SpentOnUtc;

            await _ctx.SaveChangesAsync();

            // OPTIONAL: if amount/group changed, recompute shares like your Create logic
            var shares = await _ctx.ExpenseShares.Where(s => s.ExpenseId == id).ToListAsync();
            _ctx.ExpenseShares.RemoveRange(shares);
            await _ctx.SaveChangesAsync();

            var memberIds = await _ctx.GroupMembers
                .Where(m => m.GroupId == dbE.GroupId)
                .Select(m => m.UserId)
                .ToListAsync();

            if (memberIds.Count > 0)
            {
                var each = Math.Round(dbE.Amount / memberIds.Count, 2);
                foreach (var uid in memberIds)
                {
                    _ctx.ExpenseShares.Add(new ExpenseShare
                    {
                        ExpenseId = dbE.Id,
                        UserId = uid,
                        ShareAmount = each,
                        IsSettled = uid == dbE.PaidByUserId
                    });
                }
                await _ctx.SaveChangesAsync();
            }

            TempData["Toast"] = "Expense updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Expenses/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _ctx.Expenses
                .Include(x => x.Group)
                .Include(x => x.PaidByUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (e == null) return NotFound();
            return View(e);
        }

        // POST: /Expenses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var e = await _ctx.Expenses
                .Include(x => x.Shares) // include shares so we can remove them if needed
                .FirstOrDefaultAsync(x => x.Id == id);

            if (e == null) return RedirectToAction(nameof(Index));

            // If your DB doesn't have cascade delete, remove shares first
            if (e.Shares != null && e.Shares.Count > 0)
                _ctx.ExpenseShares.RemoveRange(e.Shares);

            _ctx.Expenses.Remove(e);
            await _ctx.SaveChangesAsync();

            TempData["Toast"] = "Expense deleted.";
            return RedirectToAction(nameof(Index));
        }

    }
}
