using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using GameStore.API.Endpoints;
using GameStore.API.Entities;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GameStore.Tests.UnitTests
{
    public class JwtLogicTests
    {
        private readonly IConfiguration _config;

        public JwtLogicTests()
        {
            var inMemorySettings = new Dictionary<string, string?> {
                {"Jwt:Key", "SuperSecretKey12345!@#$%^&*()_+-=SuperSecretKey12345!@#$%^&*()_+-="},
                {"Jwt:Issuer", "http://localhost:5021"},
                {"Jwt:Audience", "http://localhost:5205"}
            };

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public void UT_05_UT_06_GenerateJwtToken_ValidTokenAndClaims()
        {
            // Arrange
            var user = new User
            {
                Id = 99,
                Username = "testjwtuser",
                Email = "jwt@example.com",
                Role = new Role { RoleName = "Administrator" }
            };

            // Act - Using Reflection to invoke private static method
            var methodInfo = typeof(AuthEndpoints).GetMethod("GenerateJwtToken", BindingFlags.NonPublic | BindingFlags.Static);
            var result = methodInfo.Invoke(null, new object[] { user, _config });
            var tokenString = result as string;

            // Assert
            tokenString.Should().NotBeNullOrEmpty();
            tokenString.Should().StartWith("ey");

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(tokenString);

            // Verify Claims
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "99");
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "jwt@example.com");
            jwtToken.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Name && c.Value == "testjwtuser");
            jwtToken.Claims.Should().Contain(c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Administrator");
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        }
    }
}
