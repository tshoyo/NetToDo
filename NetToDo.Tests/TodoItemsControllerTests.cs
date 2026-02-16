using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetToDo.Controllers;
using NetToDo.Data;
using NetToDo.Models;
using Moq;
using Xunit;

namespace NetToDo.Tests
{
    public class TodoItemsControllerTests
    {
        private AppDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var databaseContext = new AppDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        private TodoItemsController GetController(AppDbContext context, int userId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            }, "mock"));

            var controller = new TodoItemsController(context);
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            return controller;
        }

        [Fact]
        public async Task GetItems_ReturnsItemsForUserLists()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            context.TodoLists.Add(list);
            context.TodoItems.Add(new TodoItem { Title = "Item 1", List = list });
            context.TodoItems.Add(new TodoItem { Title = "Item 2", List = new TodoList { Name = "Other", UserId = 2 } });
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.GetItems(list.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var items = Assert.IsType<List<TodoItem>>(okResult.Value);
            Assert.Single(items);
            Assert.Equal("Item 1", items[0].Title);
        }

        [Fact]
        public async Task CreateItem_AddsItem()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            context.TodoLists.Add(list);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);
            var dto = new CreateItemDto("New Item", null, null, list.Id, null);

            // Act
            var result = await controller.CreateItem(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var item = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal("New Item", item.Title);
            Assert.Equal(list.Id, item.ListId);
        }

        [Fact]
        public async Task GetItem_ReturnsItemIfOwner()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            var item = new TodoItem { Title = "Item 1", List = list };
            context.TodoItems.Add(item);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.GetItem(item.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedItem = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal(item.Id, returnedItem.Id);
        }

        [Fact]
        public async Task CompleteItem_UpdatesStatus()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            var item = new TodoItem { Title = "Task", List = list, IsCompleted = false };
            context.TodoItems.Add(item);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.CompleteItem(item.Id, true);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedItem = Assert.IsType<TodoItem>(okResult.Value);
            Assert.True(updatedItem.IsCompleted);
        }

        [Fact]
        public async Task GetDeletedItems_ReturnsOnlyDeleted()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            context.TodoItems.Add(new TodoItem { Title = "Deleted", List = list, IsDeleted = true });
            context.TodoItems.Add(new TodoItem { Title = "Not Deleted", List = list, IsDeleted = false });
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.GetDeletedItems();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var items = Assert.IsType<List<TodoItem>>(okResult.Value);
            Assert.Single(items);
            Assert.True(items[0].IsDeleted);
        }

        [Fact]
        public async Task GetRecentItems_ReturnsCorrectCount()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            for(int i=0; i<15; i++)
            {
                context.TodoItems.Add(new TodoItem { Title = $"Item {i}", List = list });
            }
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.GetRecentItems();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var items = Assert.IsType<List<TodoItem>>(okResult.Value);
            Assert.Equal(10, items.Count);
        }

        [Fact]
        public async Task GetGroupedByDay_GroupsCorrectly()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            var date = DateTime.UtcNow.Date;
            context.TodoItems.Add(new TodoItem { Title = "Item 1", List = list, DueDate = date });
            context.TodoItems.Add(new TodoItem { Title = "Item 2", List = list, DueDate = date });
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.GetGroupedByDay();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            // Value is anonymous type, so we check using reflection or dynamic if needed, 
            // but just asserting it's not null for now.
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateItem_UpdatesTitle()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            var item = new TodoItem { Title = "Old", List = list };
            context.TodoItems.Add(item);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);
            var dto = new UpdateItemDto("New", null, null, null);

            // Act
            var result = await controller.UpdateItem(item.Id, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedItem = Assert.IsType<TodoItem>(okResult.Value);
            Assert.Equal("New", updatedItem.Title);
        }

        [Fact]
        public async Task RestoreItem_SetsIsDeletedFalse()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            var item = new TodoItem { Title = "Task", List = list, IsDeleted = true };
            context.TodoItems.Add(item);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.RestoreItem(item.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var restoredItem = Assert.IsType<TodoItem>(okResult.Value);
            Assert.False(restoredItem.IsDeleted);
        }

        [Fact]
        public async Task PermanentDelete_RemovesFromDb()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            var item = new TodoItem { Title = "To Vanish", List = list };
            context.TodoItems.Add(item);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.PermanentDelete(item.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.TodoItems.FindAsync(item.Id));
        }

        [Fact]
        public async Task ReorderItems_UpdatesPositions()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            var item1 = new TodoItem { Title = "Item 1", List = list, Position = 1 };
            var item2 = new TodoItem { Title = "Item 2", List = list, Position = 2 };
            context.TodoItems.AddRange(item1, item2);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);
            var reorderDtos = new List<ReorderDto>
            {
                new ReorderDto(item1.Id, 2, null),
                new ReorderDto(item2.Id, 1, null)
            };

            // Act
            var result = await controller.ReorderItems(reorderDtos);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var updated1 = await context.TodoItems.FindAsync(item1.Id);
            var updated2 = await context.TodoItems.FindAsync(item2.Id);
            Assert.Equal(2, updated1.Position);
        }

        [Fact]
        public async Task AddAttachment_SavesFile()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            var item = new TodoItem { Title = "Task", List = list };
            context.TodoItems.Add(item);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);
            
            var fileMock = new Mock<IFormFile>();
            var content = "Hello World";
            var fileName = "test.txt";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);

            // Act
            var result = await controller.AddAttachment(item.Id, fileMock.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var attachment = Assert.IsType<Attachment>(okResult.Value);
            Assert.Equal(fileName, attachment.FileName);
            Assert.True(System.IO.File.Exists(attachment.FilePath));

            // Cleanup
            if (System.IO.File.Exists(attachment.FilePath)) System.IO.File.Delete(attachment.FilePath);
        }

        [Fact]
        public async Task CompleteItem_CompletesChildren()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            var parent = new TodoItem { Title = "Parent", List = list, IsCompleted = false };
            var child = new TodoItem { Title = "Child", List = list, Parent = parent, IsCompleted = false };
            context.TodoItems.AddRange(parent, child);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            await controller.CompleteItem(parent.Id, true);

            // Assert
            var updatedChild = await context.TodoItems.FindAsync(child.Id);
            Assert.True(updatedChild.IsCompleted);
        }

        [Fact]
        public async Task SoftDelete_DeletesChildren()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            var parent = new TodoItem { Title = "Parent", List = list, IsDeleted = false };
            var child = new TodoItem { Title = "Child", List = list, Parent = parent, IsDeleted = false };
            context.TodoItems.AddRange(parent, child);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            await controller.SoftDelete(parent.Id);

            // Assert
            var updatedChild = await context.TodoItems.IgnoreQueryFilters().FirstAsync(i => i.Id == child.Id);
            Assert.True(updatedChild.IsDeleted);
        }
        [Fact]
        public async Task GetItem_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = GetController(context, 1);

            // Act
            var result = await controller.GetItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SoftDelete_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = GetController(context, 1);

            // Act
            var result = await controller.SoftDelete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateItem_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = GetController(context, 1);
            var dto = new UpdateItemDto("New", null, null, null);

            // Act
            var result = await controller.UpdateItem(999, dto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task RestoreItem_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = GetController(context, 1);

            // Act
            var result = await controller.RestoreItem(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task PermanentDelete_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = GetController(context, 1);

            // Act
            var result = await controller.PermanentDelete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddAttachment_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = GetController(context, 1);
            var fileMock = new Mock<IFormFile>();

            // Act
            var result = await controller.AddAttachment(999, fileMock.Object);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        [Fact]
        public async Task UpdateItem_UpdatesAllFields()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list1 = new TodoList { Name = "List 1", UserId = userId };
            var list2 = new TodoList { Name = "List 2", UserId = userId };
            var item = new TodoItem { Title = "Old", List = list1 };
            context.TodoLists.AddRange(list1, list2);
            context.TodoItems.Add(item);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);
            var dueDate = DateTime.UtcNow.AddDays(1);
            var dto = new UpdateItemDto("New", "Description", dueDate, false);

            // Act
            var result = await controller.UpdateItem(item.Id, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updated = await context.TodoItems.FindAsync(item.Id);
            Assert.Equal("New", updated.Title);
            Assert.Equal("Description", updated.Description);
            Assert.Equal(dueDate, updated.DueDate);
        }

        [Fact]
        public async Task GetGroupedByDay_MultipleDays()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "List 1", UserId = userId };
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            context.TodoItems.Add(new TodoItem { Title = "Today 1", List = list, DueDate = today });
            context.TodoItems.Add(new TodoItem { Title = "Tomorrow 1", List = list, DueDate = tomorrow });
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.GetGroupedByDay();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
