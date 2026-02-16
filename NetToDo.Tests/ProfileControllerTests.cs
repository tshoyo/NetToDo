using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NetToDo.Controllers;
using NetToDo.Data;
using NetToDo.Models;
using NetToDo.Services;
using Xunit;

namespace NetToDo.Tests
{
    public class ProfileControllerTests
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

        private ProfileController GetController(AppDbContext context, int userId, IAuthService authService)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            }, "mock"));

            var controller = new ProfileController(context, authService);
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
            return controller;
        }

        [Fact]
        public async Task GetProfile_ReturnsUserSelection()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var user = new User { Id = 1, Name = "Test", Email = "test@example.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = GetController(context, 1, new Mock<IAuthService>().Object);

            // Act
            var result = await controller.GetProfile();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }

        [Fact]
        public async Task UpdateProfile_UpdatesFields()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var user = new User { Id = 1, Name = "Old", Email = "old@example.com" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var mockAuthService = new Mock<IAuthService>();
            var controller = GetController(context, 1, mockAuthService.Object);
            var dto = new UpdateProfileDto("New", "new@example.com", null);

            // Act
            var result = await controller.UpdateProfile(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedUser = await context.Users.FindAsync(1);
            Assert.Equal("New", updatedUser.Name);
            Assert.Equal("new@example.com", updatedUser.Email);
        }

        [Fact]
        public async Task DeleteAccount_RemovesUser()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var user = new User { Id = 1, Name = "Test" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = GetController(context, 1, new Mock<IAuthService>().Object);

            // Act
            var result = await controller.DeleteAccount();

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(await context.Users.FindAsync(1));
        }

        [Fact]
        public async Task UpdateProfile_UpdatesPassword()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var user = new User { Name = "Old", Email = "old@example.com", PasswordHash = "old_hash" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var mockAuthService = new Mock<IAuthService>();
            mockAuthService.Setup(a => a.HashPassword("new_password")).Returns("new_hash");
            var controller = GetController(context, user.Id, mockAuthService.Object);
            var dto = new UpdateProfileDto(null, null, "new_password");

            // Act
            var result = await controller.UpdateProfile(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedUser = await context.Users.FindAsync(user.Id);
            Assert.Equal("new_hash", updatedUser.PasswordHash);
        }

        
        [Fact]
        public async Task GetProfile_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = GetController(context, 999, new Mock<IAuthService>().Object);

            // Act
            var result = await controller.GetProfile();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_ReturnsNotFound()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = GetController(context, 999, new Mock<IAuthService>().Object);
            var dto = new UpdateProfileDto("Test", "test@test.com", null);

            // Act
            var result = await controller.UpdateProfile(dto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
