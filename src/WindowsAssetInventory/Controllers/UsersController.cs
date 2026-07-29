using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WindowsAssetInventory.Contracts;
using WindowsAssetInventory.Data;
using WindowsAssetInventory.Models;
using WindowsAssetInventory.Services;

namespace WindowsAssetInventory.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(InventoryDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Users
            .AsNoTracking()
            .Include(user => user.AssignedAssets)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user =>
                user.FullName.Contains(term) ||
                user.Email.Contains(term) ||
                user.Department.Contains(term));
        }

        var users = await query.OrderBy(user => user.FullName).ToListAsync(cancellationToken);
        return Ok(users.Select(user => user.ToDto()).ToArray());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(item => item.AssignedAssets)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return user is null ? UserNotFound(id) : Ok(user.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(
        UserWriteRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureUniqueEmail(request.Email, null, cancellationToken);
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Department = request.Department.Trim()
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(
        int id,
        UserWriteRequest request,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(item => item.AssignedAssets)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return UserNotFound(id);
        }

        await EnsureUniqueEmail(request.Email, id, cancellationToken);
        user.FullName = request.FullName.Trim();
        user.Email = request.Email.Trim().ToLowerInvariant();
        user.Department = request.Department.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(user.ToDto());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(item => item.AssignedAssets)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
        {
            return UserNotFound(id);
        }

        if (user.AssignedAssets.Count > 0)
        {
            throw new DomainConflictException("Return all assigned assets before deleting the user.");
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task EnsureUniqueEmail(
        string email,
        int? excludedId,
        CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var exists = await dbContext.Users.AnyAsync(
            user => user.Email == normalized && (!excludedId.HasValue || user.Id != excludedId.Value),
            cancellationToken);
        if (exists)
        {
            throw new DomainConflictException($"Email address {normalized} already exists.");
        }
    }

    private ObjectResult UserNotFound(int id)
    {
        return Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "User not found",
            detail: $"User {id} does not exist.");
    }
}
