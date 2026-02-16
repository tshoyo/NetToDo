using Microsoft.AspNetCore.Mvc;
using NetToDo.Models;
using NetToDo.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace NetToDo.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TodoListsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TodoListsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetLists()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var lists = await _context.TodoLists
            .Where(l => l.UserId == userId)
            .ToListAsync();
        return Ok(lists);
    }

    [HttpPost]
    public async Task<IActionResult> CreateList(CreateListDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var list = new TodoList
        {
            Name = dto.Name,
            UserId = userId
        };

        _context.TodoLists.Add(list);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLists), new { id = list.Id }, list);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var list = await _context.TodoLists
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        if (list == null) return NotFound();

        list.IsDeleted = true;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeletedLists()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var lists = await _context.TodoLists
            .IgnoreQueryFilters()
            .Where(l => l.IsDeleted && l.UserId == userId)
            .ToListAsync();
        return Ok(lists);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateList(int id, [FromBody] UpdateListDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var list = await _context.TodoLists.FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);
        if (list == null) return NotFound();

        list.Name = dto.Name;
        await _context.SaveChangesAsync();
        return Ok(list);
    }

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> RestoreList(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var list = await _context.TodoLists
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

        if (list == null) return NotFound();

        list.IsDeleted = false;
        await _context.SaveChangesAsync();
        return Ok(list);
    }

    [HttpDelete("{id:int}/permanent")]
    public async Task<IActionResult> PermanentDelete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var list = await _context.TodoLists
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

        if (list == null) return NotFound();

        _context.TodoLists.Remove(list);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateListDto(string Name);
public record UpdateListDto(string Name);
