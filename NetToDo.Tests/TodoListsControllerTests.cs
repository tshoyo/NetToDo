using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetToDo.Controllers;
using NetToDo.Data;
using NetToDo.Models;
using Xunit;

namespace NetToDo.Tests
{
    public class TodoListsControllerTests
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

        private TodoListsController GetController(AppDbContext context, int userId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            }, "mock"));

            var controller = new TodoListsController(context);
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            return controller;
        }

        [Fact]
        public async Task GetLists_ReturnsUserLists()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            context.TodoLists.Add(new TodoList { Name = "List 1", UserId = userId });
            context.TodoLists.Add(new TodoList { Name = "List 2", UserId = 2 });
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.GetLists();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var lists = Assert.IsType<List<TodoList>>(okResult.Value);
            Assert.Single(lists);
            Assert.Equal("List 1", lists[0].Name);
        }

        [Fact]
        public async Task CreateList_AddsListAndReturnsCreated()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var controller = GetController(context, userId);
            var dto = new CreateListDto("New List");

            // Act
            var result = await controller.CreateList(dto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var list = Assert.IsType<TodoList>(createdResult.Value);
            Assert.Equal("New List", list.Name);
            Assert.Equal(userId, list.UserId);
            Assert.Equal(1, await context.TodoLists.CountAsync());
        }

        [Fact]
        public async Task SoftDelete_SetsIsDeletedTrue()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "To Delete", UserId = userId };
            context.TodoLists.Add(list);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.SoftDelete(list.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var updatedList = await context.TodoLists.IgnoreQueryFilters().FirstAsync(l => l.Id == list.Id);
            Assert.True(updatedList.IsDeleted);
        }

        [Fact]
        public async Task UpdateList_UpdatesName()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "Old Name", UserId = userId };
            context.TodoLists.Add(list);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);
            var dto = new UpdateListDto("Updated Name");

            // Act
            var result = await controller.UpdateList(list.Id, dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedList = Assert.IsType<TodoList>(okResult.Value);
            Assert.Equal("Updated Name", updatedList.Name);
        }
        [Fact]
        public async Task GetDeletedLists_ReturnsOnlyDeleted()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            context.TodoLists.Add(new TodoList { Name = "Deleted", UserId = userId, IsDeleted = true });
            context.TodoLists.Add(new TodoList { Name = "Active", UserId = userId, IsDeleted = false });
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.GetDeletedLists();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var lists = Assert.IsType<List<TodoList>>(okResult.Value);
            Assert.Single(lists);
            Assert.True(lists[0].IsDeleted);
        }

        [Fact]
        public async Task RestoreList_SetsIsDeletedFalse()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "Deleted", UserId = userId, IsDeleted = true };
            context.TodoLists.Add(list);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.RestoreList(list.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var restoredList = Assert.IsType<TodoList>(okResult.Value);
            Assert.False(restoredList.IsDeleted);
        }

        [Fact]
        public async Task PermanentDelete_RemovesFromDb()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var userId = 1;
            var list = new TodoList { Name = "To Vanish", UserId = userId };
            context.TodoLists.Add(list);
            await context.SaveChangesAsync();

            var controller = GetController(context, userId);

            // Act
            var result = await controller.PermanentDelete(list.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.TodoLists.FindAsync(list.Id));
        }
        [Fact]
        public async Task UpdateList_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = GetController(context, 1);
            var dto = new UpdateListDto("New Name");

            // Act
            var result = await controller.UpdateList(999, dto);

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
    }
}
