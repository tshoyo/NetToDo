using System.ComponentModel.DataAnnotations;

namespace NetToDo.Models;

public class User
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public ICollection<TodoList> Lists { get; set; } = new List<TodoList>();
}
