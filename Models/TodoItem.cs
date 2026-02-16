using System.ComponentModel.DataAnnotations;

namespace NetToDo.Models;

public class TodoItem
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; } // Supports Markdown

    public DateTime? DueDate { get; set; }
    
    public bool IsCompleted { get; set; }
    
    public bool IsDeleted { get; set; } // Soft delete
    public int Position { get; set; }

    public int ListId { get; set; }
    public TodoList List { get; set; } = null!;

    public int? ParentId { get; set; }
    public TodoItem? Parent { get; set; }
    
    public ICollection<TodoItem> Children { get; set; } = new List<TodoItem>();
    
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
