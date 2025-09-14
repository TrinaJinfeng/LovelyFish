using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LovelyFish.API.Server.Models;
using LovelyFish.API.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using LovelyFish.API.Server.Models.Dtos;

[Route("api/[controller]")]
[ApiController]
public class FishOwnersController : ControllerBase
{
    private readonly LovelyFishContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public FishOwnersController(LovelyFishContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: api/FishOwners
    // get all public infomation.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FishOwnerDto>>> GetPublicOwners()
    {
        var owners = await _context.FishOwners
        .Select(o => new FishOwnerDto
        {
            OwnerID = o.OwnerID,
            UserId = o.UserId,  // frontend to check if it is the account person
            UserName = o.UserName,
            Phone = o.IsContactPublic ? o.Phone : null,
            Email = o.IsContactPublic ? o.Email : null,
            Location = o.Location,
            FishName = o.FishName,
            IsContactPublic = o.IsContactPublic
        })
        .ToListAsync();

        return Ok(owners);
    }
        
    //    //return await _context.FishOwners
    //    //                     //.Where(f => f.IsContactPublic)
    //    //                     .ToListAsync();
    //}

    // GET: api/FishOwners/5
    // get single infomation
    [HttpGet("{id}")]
    public async Task<ActionResult<FishOwnerDto>> GetOwner(int id)
    {
        var owner = await _context.FishOwners.FindAsync(id);
        if (owner == null) return NotFound();

        var dto = new FishOwnerDto
        {
            OwnerID = owner.OwnerID,
            UserId = owner.UserId,
            UserName = owner.UserName,
            Phone = owner.IsContactPublic ? owner.Phone : null,
            Email = owner.IsContactPublic ? owner.Email : null,
            Location = owner.Location,
            FishName = owner.FishName,
            IsContactPublic = owner.IsContactPublic
        };

        return dto;
    }

    //    // public contact or not
    //    if (!owner.IsContactPublic)
    //    {
    //        owner.Phone = null;
    //        owner.Email = null;
    //    }

    //    return owner;
    //}

    // POST: api/FishOwners
    // add info
    //[HttpPost]
    //public async Task<ActionResult<FishOwner>> PostOwner(FishOwner owner)
    //{
    //    Console.WriteLine("Received JSON:");
    //    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(owner));

    //    _context.FishOwners.Add(owner);
    //    await _context.SaveChangesAsync();

    //    return CreatedAtAction(nameof(GetOwner), new { id = owner.OwnerID }, owner);
    //}

    // POST: api/FishOwners
    // add info (requires login)
    [HttpPost]
    [Authorize] // must login
    public async Task<ActionResult<FishOwnerDto>> PostOwner([FromBody] FishOwnerCreateRequest newOwner)
    {
        // get current login account
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        var owner = new FishOwner //mapping to FishOwners
        {
            UserName = newOwner.UserName,
            Phone = newOwner.Phone,
            Email = newOwner.Email,
            Location = newOwner.Location,
            FishName = newOwner.FishName,
            IsContactPublic = newOwner.IsContactPublic,
            UserId = currentUser.Id,       // auto-link currentuser in account
            CreatedAt = DateTime.UtcNow
        };
      
        _context.FishOwners.Add(owner);
        await _context.SaveChangesAsync();

        var dto = new FishOwnerDto
        {
            OwnerID = owner.OwnerID,
            UserId = owner.UserId,
            UserName = owner.UserName,
            Phone = owner.IsContactPublic ? owner.Phone : null,
            Email = owner.IsContactPublic ? owner.Email : null,
            Location = owner.Location,
            FishName = owner.FishName,
            IsContactPublic = owner.IsContactPublic
        };

        return CreatedAtAction(nameof(GetOwner), new { id = owner.OwnerID }, dto);
    }


    // PUT: api/FishOwners/5
    // update info
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> PutOwner(int id, [FromBody] FishOwnerUpdateRequest updatedOwner)
    {
        var existingOwner = await _context.FishOwners.FindAsync(id);
        if (existingOwner == null) return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Unauthorized();

        // only admin and record owner can put the record
        var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
        if (existingOwner.UserId != currentUser.Id && !isAdmin)
        {
            return Forbid();
        }

        // update the record except userid
        existingOwner.UserName = updatedOwner.UserName;
        existingOwner.Phone = updatedOwner.Phone;
        existingOwner.Email = updatedOwner.Email;
        existingOwner.Location = updatedOwner.Location;
        existingOwner.FishName = updatedOwner.FishName;
        existingOwner.IsContactPublic = updatedOwner.IsContactPublic;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.FishOwners.Any(e => e.OwnerID == id)) return NotFound();
            else throw;
        }

        return NoContent();
    }

    // DELETE: api/FishOwners/5
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteOwner(int id)
    {
        var owner = await _context.FishOwners.FindAsync(id);
        if (owner == null) return NotFound();

        _context.FishOwners.Remove(owner);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
