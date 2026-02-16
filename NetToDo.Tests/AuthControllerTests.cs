using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NetToDo.Controllers;
using NetToDo.Data;
using NetToDo.Models;
using NetToDo.Services;
using Xunit;

namespace NetToDo.Tests
{
    public class AuthControllerTests
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

        [Fact]
        public async Task Register_CreatesUserAndReturnsToken()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var mockAuthService = new Mock<IAuthService>();
            mockAuthService.Setup(s => s.HashPassword(It.IsAny<string>())).Returns("hashed");
            mockAuthService.Setup(s => s.GenerateToken(It.IsAny<User>())).Returns("token");
            mockAuthService.Setup(s => s.GetGravatarUrl(It.IsAny<string>())).Returns("gravatar");

            var controller = new AuthController(context, mockAuthService.Object);
            var dto = new RegisterDto("Test", "test@example.com", "password");

            // Act
            var result = await controller.Register(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Token", okResult.Value.ToString());
            Assert.Equal(1, await context.Users.CountAsync());
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var user = new User { Email = "test@example.com", PasswordHash = "hashed" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var mockAuthService = new Mock<IAuthService>();
            mockAuthService.Setup(s => s.VerifyPassword("password", "hashed")).Returns(true);
            mockAuthService.Setup(s => s.GenerateToken(user)).Returns("token");

            var controller = new AuthController(context, mockAuthService.Object);
            var dto = new LoginDto("test@example.com", "password");

            // Act
            var result = await controller.Login(dto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Token", okResult.Value.ToString());
        }

        [Fact]
        public async Task Register_ExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            using var context = GetDatabaseContext();
            context.Users.Add(new User { Email = "test@example.com" });
            await context.SaveChangesAsync();

            var controller = new AuthController(context, new Mock<IAuthService>().Object);
            var dto = new RegisterDto("Test", "test@example.com", "password");

            // Act
            var result = await controller.Register(dto);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new AuthController(context, new Mock<IAuthService>().Object);
            var dto = new LoginDto("ghost@example.com", "wrong");

            // Act
            var result = await controller.Login(dto);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }
    }

    public class InfoControllerTests
    {
        [Fact]
        public void GetInfo_ReturnsOk()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            var controller = new InfoController(mockConfig.Object);

            // Act
            var result = controller.GetInfo();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
        }
    }
}
