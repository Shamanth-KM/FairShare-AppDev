using FairShare.Web.Data;
using FairShare.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersApiController : ControllerBase
    {
        private readonly ApplicationDbContext _ctx;
        public UsersApiController(ApplicationDbContext ctx) { _ctx = ctx; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers() =>
            await _ctx.Users.AsNoTracking().ToListAsync();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var u = await _ctx.Users.FindAsync(id);
            return u is null ? NotFound() : u;
        }

        [HttpPost]
        public async Task<ActionResult<User>> Create(User user)
        {
            _ctx.Users.Add(user);
            await _ctx.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, User user)
        {
            if (id != user.Id) return BadRequest();
            _ctx.Entry(user).State = EntityState.Modified;
            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var u = await _ctx.Users.FindAsync(id);
            if (u is null) return NotFound();
            _ctx.Users.Remove(u);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }
    }
}
