using System.ComponentModel.DataAnnotations;

namespace NetToDo.Models;

public class TodoList
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<TodoItem> Items { get; set; } = new List<TodoItem>();
}
