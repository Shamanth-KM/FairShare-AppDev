using FairShare.Web.Data;
using FairShare.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        public GroupsApiController(ApplicationDbContext ctx) { _ctx = ctx; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Group>>> GetGroups() =>
            await _ctx.Groups.Include(g => g.Members).AsNoTracking().ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Group>> GetGroup(int id)
        {
            var g = await _ctx.Groups
                .Include(x => x.Members).ThenInclude(m => m.User)
                .Include(x => x.Expenses)
                .FirstOrDefaultAsync(x => x.Id == id);
            return g is null ? NotFound() : g;
        }

        [HttpPost]
        public async Task<ActionResult<Group>> Create(Group group)
        {
            _ctx.Groups.Add(group);
            await _ctx.SaveChangesAsync();
            return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Group group)
        {
            if (id != group.Id) return BadRequest();
            _ctx.Entry(group).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var g = await _ctx.Groups.FindAsync(id);
            if (g is null) return NotFound();
            _ctx.Groups.Remove(g);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{groupId:int}/members/{userId:int}")]
        public async Task<IActionResult> AddMember(int groupId, int userId)
        {
            if (!await _ctx.Groups.AnyAsync(g => g.Id == groupId) ||
                !await _ctx.Users.AnyAsync(u => u.Id == userId))
                return NotFound();

            var exists = await _ctx.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
            if (!exists)
            {
                _ctx.GroupMembers.Add(new GroupMember { GroupId = groupId, UserId = userId });
                await _ctx.SaveChangesAsync();
            }
            return NoContent();
        }

        [HttpDelete("{groupId:int}/members/{userId:int}")]
        public async Task<IActionResult> RemoveMember(int groupId, int userId)
        {
            var gm = await _ctx.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
            if (gm is null) return NotFound();
            _ctx.GroupMembers.Remove(gm);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }
    }
}
