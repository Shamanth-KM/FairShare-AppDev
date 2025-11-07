using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

using FairShare.Web.Data;
using FairShare.Web.Models;
using FairShare.Web.Services;
using FairShare.Web.ViewModels;

namespace FairShare.Web.Controllers
{
    [Authorize]
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICurrencyRateService _rateService;

        public GroupsController(ApplicationDbContext context, ICurrencyRateService rateService)
        {
            _context = context;
            _rateService = rateService;
        }

        // GET: /Groups
        public async Task<IActionResult> Index()
        {
            var groups = await _context.Groups
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .ToListAsync();

            // GroupId -> member count (no nav property needed)
            var memberCounts = await _context.GroupMembers
                .GroupBy(gm => gm.GroupId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            ViewBag.MemberCounts = memberCounts;
            return View(groups);
        }

        // GET: /Groups/Details/5[?to=EUR]
        public async Task<IActionResult> Details(int id, string? to)
        {
            var group = await _context.Groups
                .Include(g => g.Expenses)
                    .ThenInclude(e => e.PaidByUser)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group is null) return NotFound();

            // Members (we don’t rely on a nav prop on Group)
            var members = await _context.GroupMembers
                .Where(gm => gm.GroupId == id)
                .Include(gm => gm.User)
                .Select(gm => gm.User)
                .OrderBy(u => u.Name)
                .AsNoTracking()
                .ToListAsync();
            ViewBag.Members = members;

            // Totals (stored in USD)
            var total = group.Expenses?.Sum(e => e.Amount) ?? 0m;
            ViewBag.TotalUSD = $"USD {total:0.00}";

            // Optional currency conversion (fetch-only API)
            if (!string.IsNullOrWhiteSpace(to) &&
                !string.Equals(to, "USD", StringComparison.OrdinalIgnoreCase))
            {
                var rate = await _rateService.GetRateAsync("USD", to);
                ViewBag.ConvertedTo = to.ToUpperInvariant();
                ViewBag.ConvertedTotal = rate.HasValue
                    ? $"{to.ToUpperInvariant()} {(total * rate.Value):0.00}"
                    : "Conversion unavailable right now.";
            }

            return View(group);
        }

        // ============
        // CREATE (with member selection)
        // ============

        // GET: /Groups/Create
        public async Task<IActionResult> Create()
        {
            var vm = new GroupCreateViewModel
            {
                AllUsers = await _context.Users
                    .OrderBy(u => u.Name)
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = $"{u.Name} ({u.Email})"
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        // POST: /Groups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GroupCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.AllUsers = await _context.Users
                    .OrderBy(u => u.Name)
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = $"{u.Name} ({u.Email})"
                    })
                    .ToListAsync();
                return View(vm);
            }

            var group = new Group
            {
                Name = vm.Name,
                Description = vm.Description,
                CreatedUtc = DateTime.UtcNow
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync(); // get Id

            if (vm.SelectedUserIds != null && vm.SelectedUserIds.Count > 0)
            {
                foreach (var userId in vm.SelectedUserIds.Distinct())
                {
                    _context.GroupMembers.Add(new GroupMember
                    {
                        GroupId = group.Id,
                        UserId = userId
                    });
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = group.Id });
        }

        // ============
        // EDIT (with member manage)
        // ============

        // GET: /Groups/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group is null) return NotFound();

            var selectedIds = await _context.GroupMembers
                .Where(gm => gm.GroupId == id)
                .Select(gm => gm.UserId)
                .ToListAsync();

            var vm = new GroupEditViewModel
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CreatedUtc = group.CreatedUtc,
                SelectedUserIds = selectedIds,
                AllUsers = await _context.Users
                    .OrderBy(u => u.Name)
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = $"{u.Name} ({u.Email})"
                    })
                    .ToListAsync()
            };

            return View(vm); // IMPORTANT: return ViewModel
        }

        // POST: /Groups/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GroupEditViewModel vm)
        {
            if (id != vm.Id) return BadRequest();

            var group = await _context.Groups.FindAsync(id);
            if (group is null) return NotFound();

            if (!ModelState.IsValid)
            {
                vm.AllUsers = await _context.Users
                    .OrderBy(u => u.Name)
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = $"{u.Name} ({u.Email})"
                    })
                    .ToListAsync();
                return View(vm);
            }

            // Update basic fields
            group.Name = vm.Name;
            group.Description = vm.Description;

            // Membership diffs
            var existingMemberIds = await _context.GroupMembers
                .Where(gm => gm.GroupId == id)
                .Select(gm => gm.UserId)
                .ToListAsync();

            var newSelected = vm.SelectedUserIds?.Distinct().ToList() ?? new();

            var toAdd = newSelected.Except(existingMemberIds).ToList();
            var toRemove = existingMemberIds.Except(newSelected).ToList();

            if (toAdd.Count > 0)
            {
                foreach (var userId in toAdd)
                {
                    _context.GroupMembers.Add(new GroupMember
                    {
                        GroupId = id,
                        UserId = userId
                    });
                }
            }

            if (toRemove.Count > 0)
            {
                var removeEntities = await _context.GroupMembers
                    .Where(gm => gm.GroupId == id && toRemove.Contains(gm.UserId))
                    .ToListAsync();
                _context.GroupMembers.RemoveRange(removeEntities);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        // ============
        // DELETE
        // ============

        // GET: /Groups/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var group = await _context.Groups.AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);
            if (group is null) return NotFound();

            return View(group);
        }

        // POST: /Groups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group is null) return NotFound();

            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
