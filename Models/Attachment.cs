using System.ComponentModel.DataAnnotations;

namespace NetToDo.Models;

public class Attachment
{
    public int Id { get; set; }
    
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required, System.Text.Json.Serialization.JsonIgnore]
    public string FilePath { get; set; } = string.Empty;

    public int TodoItemId { get; set; }
    public TodoItem TodoItem { get; set; } = null!;
}
