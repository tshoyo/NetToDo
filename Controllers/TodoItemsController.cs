using Microsoft.AspNetCore.Mvc;
using NetToDo.Models;
using NetToDo.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace NetToDo.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TodoItemsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TodoItemsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetItems(int listId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var items = await _context.TodoItems
            .Include(i => i.Attachments)
            .Where(i => i.ListId == listId && i.List.UserId == userId)
            .OrderBy(i => i.Position)
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetItem(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var item = await _context.TodoItems
            .IgnoreQueryFilters()
            .Include(i => i.Attachments)
            .Include(i => i.Children)
            .FirstOrDefaultAsync(i => i.Id == id && i.List.UserId == userId);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem(CreateItemDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var list = await _context.TodoLists.FirstOrDefaultAsync(l => l.Id == dto.ListId && l.UserId == userId);
        if (list == null) return BadRequest("Invalid list.");

        var item = new TodoItem
        {
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            ListId = dto.ListId,
            ParentId = dto.ParentId
        };

        _context.TodoItems.Add(item);
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPatch("{id:int}/complete")]
    public async Task<IActionResult> CompleteItem(int id, [FromBody] bool isCompleted)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var item = await _context.TodoItems
            .Include(i => i.Children)
            .FirstOrDefaultAsync(i => i.Id == id && i.List.UserId == userId);
        
        if (item == null) return NotFound();

        item.IsCompleted = isCompleted;

        // Trigger children completion if marking as completed
        if (isCompleted)
        {
            await CompleteChildren(item);
        }

        await _context.SaveChangesAsync();
        return Ok(item);
    }

    private async Task CompleteChildren(TodoItem parent)
    {
        var children = await _context.TodoItems
            .Where(i => i.ParentId == parent.Id)
            .ToListAsync();

        foreach (var child in children)
        {
            child.IsCompleted = true;
            await CompleteChildren(child); // Recursive
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var item = await _context.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id && i.List.UserId == userId);
        if (item == null) return NotFound();

        item.IsDeleted = true;
        await SoftDeleteChildren(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task SoftDeleteChildren(TodoItem parent)
    {
        var children = await _context.TodoItems
            .Where(i => i.ParentId == parent.Id)
            .ToListAsync();

        foreach (var child in children)
        {
            child.IsDeleted = true;
            await SoftDeleteChildren(child);
        }
    }

    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeletedItems()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        // IgnoreQueryFilters to see soft-deleted items
        var items = await _context.TodoItems
            .IgnoreQueryFilters()
            .Include(i => i.Attachments)
            .Where(i => i.IsDeleted && i.List.UserId == userId)
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentItems()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var items = await _context.TodoItems
            .Include(i => i.Attachments)
            .Where(i => i.List.UserId == userId)
            .OrderByDescending(i => i.Id)
            .Take(10)
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("grouped-by-day")]
    public async Task<IActionResult> GetGroupedByDay()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var items = await _context.TodoItems
            .Include(i => i.Attachments)
            .Where(i => i.List.UserId == userId && i.DueDate.HasValue)
            .ToListAsync();

        var grouped = items
            .GroupBy(i => i.DueDate!.Value.Date)
            .Select(g => new { Date = g.Key, Items = g.OrderBy(i => i.Position).ToList() });

        return Ok(grouped);
    }

    [HttpPost("{id:int}/attachments")]
    public async Task<IActionResult> AddAttachment(int id, IFormFile file)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var item = await _context.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id && i.List.UserId == userId);
        if (item == null) return NotFound();

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, file.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new Attachment
        {
            FileName = file.FileName,
            FilePath = filePath,
            TodoItemId = id
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();

        return Ok(attachment);
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateItemDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var item = await _context.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id && i.List.UserId == userId);
        if (item == null) return NotFound();

        if (dto.Title != null) item.Title = dto.Title;
        if (dto.Description != null) item.Description = dto.Description;
        
        // Only update DueDate if it's explicitly provided in the request body
        // For simplicity in this DTO, we'll assume if it's null it means 'keep as is' 
        // unless we want to allow clearing. Let's allow clearing by checking a flag or similar.
        // Actually, let's just make the frontend send the existing value if it doesn't want to change it.
        if (dto.DueDate != null || dto.ClearDueDate == true) 
        {
            item.DueDate = dto.ClearDueDate == true ? null : dto.DueDate;
        }

        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPost("{id:int}/restore")]
    public async Task<IActionResult> RestoreItem(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var item = await _context.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id && i.List.UserId == userId);
        
        if (item == null) return NotFound();
        item.IsDeleted = false;
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}/permanent")]
    public async Task<IActionResult> PermanentDelete(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var item = await _context.TodoItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id && i.List.UserId == userId);
        
        if (item == null) return NotFound();
        _context.TodoItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("attachments/{attachmentId:int}")]
    public async Task<IActionResult> DeleteAttachment(int attachmentId)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        // Ownership check: Attachment -> Item -> List -> User
        var attachment = await _context.Attachments
            .IgnoreQueryFilters()
            .Include(a => a.TodoItem)
                .ThenInclude(i => i.List)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TodoItem.List.UserId == userId);
        
        if (attachment == null) return NotFound();

        // Delete physical file
        try 
        {
            if (System.IO.File.Exists(attachment.FilePath))
            {
                System.IO.File.Delete(attachment.FilePath);
            }
        }
        catch (IOException)
        {
            // Log error or handle if file is locked
        }

        _context.Attachments.Remove(attachment);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderItems([FromBody] List<ReorderDto> items)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        foreach (var itemDto in items)
        {
            var item = await _context.TodoItems
                .FirstOrDefaultAsync(i => i.Id == itemDto.Id && i.List.UserId == userId);
            
            if (item != null)
            {
                item.Position = itemDto.Position;
                item.ParentId = itemDto.ParentId;
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record ReorderDto(int Id, int Position, int? ParentId);

public record CreateItemDto(string Title, string? Description, DateTime? DueDate, int ListId, int? ParentId);
public record UpdateItemDto(string? Title, string? Description, DateTime? DueDate, bool? ClearDueDate);
