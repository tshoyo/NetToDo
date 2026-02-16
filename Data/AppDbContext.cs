using Microsoft.EntityFrameworkCore;
using NetToDo.Models;

namespace NetToDo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<TodoList> TodoLists { get; set; } = null!;
    public DbSet<TodoItem> TodoItems { get; set; } = null!;
    public DbSet<Attachment> Attachments { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Soft delete global filter
        modelBuilder.Entity<TodoItem>().HasQueryFilter(t => !t.IsDeleted);
        modelBuilder.Entity<TodoList>().HasQueryFilter(l => !l.IsDeleted);

        // Relationships
        modelBuilder.Entity<TodoItem>()
            .HasOne(t => t.Parent)
            .WithMany(t => t.Children)
            .HasForeignKey(t => t.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TodoItem>()
            .HasMany(t => t.Attachments)
            .WithOne(a => a.TodoItem)
            .HasForeignKey(a => a.TodoItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TodoList>()
            .HasMany(t => t.Items)
            .WithOne(i => i.List)
            .HasForeignKey(i => i.ListId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasMany(u => u.Lists)
            .WithOne(l => l.User)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
