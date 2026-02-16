using Microsoft.Extensions.Configuration;
using Moq;
using NetToDo.Models;
using NetToDo.Services;
using Xunit;

namespace NetToDo.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Mock JwtSettings section
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.Setup(s => s["Secret"]).Returns("SuperSecretKeyForDevelopmentOnly123!");
            mockSection.Setup(s => s["Issuer"]).Returns("NetToDo");
            mockSection.Setup(s => s["Audience"]).Returns("NetToDoUsers");
            
            _mockConfiguration.Setup(c => c.GetSection("JwtSettings")).Returns(mockSection.Object);
            
            _authService = new AuthService(_mockConfiguration.Object);
        }

        [Fact]
        public void GenerateToken_ShouldReturnNonEmptyString()
        {
            // Arrange
            var user = new User { Id = 1, Email = "test@example.com", Name = "Test User" };

            // Act
            var token = _authService.GenerateToken(user);

            // Assert
            Assert.False(string.IsNullOrEmpty(token));
        }

        [Fact]
        public void HashPassword_ShouldReturnHashedString()
        {
            // Arrange
            var password = "Password123!";

            // Act
            var hash = _authService.HashPassword(password);

            // Assert
            Assert.NotEqual(password, hash);
            Assert.True(BCrypt.Net.BCrypt.Verify(password, hash));
        }

        [Fact]
        public void VerifyPassword_ShouldReturnTrueForCorrectPassword()
        {
            // Arrange
            var password = "Password123!";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            // Act
            var result = _authService.VerifyPassword(password, hash);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetGravatarUrl_ShouldReturnCorrectUrl()
        {
            // Arrange
            var email = "test@example.com";
            // MD5 of "test@example.com" is "55502f40dc8b7c769880b10874abc9d0"
            var expectedBase = "https://www.gravatar.com/avatar/55502f40dc8b7c769880b10874abc9d0";

            // Act
            var url = _authService.GetGravatarUrl(email);

            // Assert
            Assert.StartsWith(expectedBase, url);
        }
    }
}
