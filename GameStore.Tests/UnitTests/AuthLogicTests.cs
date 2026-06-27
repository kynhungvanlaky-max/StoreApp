using System;
using System.Security.Cryptography;
using FluentAssertions;
using Xunit;
using BCrypt.Net;

namespace GameStore.Tests.UnitTests
{
    public class AuthLogicTests
    {
        [Fact]
        public void UT_02_BCryptHashPassword()
        {
            // Arrange
            string password = "StrongPassword123!";

            // Act
            string hash = BCrypt.Net.BCrypt.HashPassword(password);

            // Assert
            hash.Should().NotBeNullOrEmpty();
            hash.Should().NotBe(password);
            hash.Should().StartWith("$2"); // BCrypt hashes start with $2a$, $2b$, etc.
        }

        [Fact]
        public void UT_03_BCryptVerifyPassword_Correct()
        {
            // Arrange
            string password = "StrongPassword123!";
            string hash = BCrypt.Net.BCrypt.HashPassword(password);

            // Act
            bool isMatch = BCrypt.Net.BCrypt.Verify(password, hash);

            // Assert
            isMatch.Should().BeTrue();
        }

        [Fact]
        public void UT_04_BCryptVerifyPassword_Incorrect()
        {
            // Arrange
            string password = "StrongPassword123!";
            string wrongPassword = "WrongPassword!";
            string hash = BCrypt.Net.BCrypt.HashPassword(password);

            // Act
            bool isMatch = BCrypt.Net.BCrypt.Verify(wrongPassword, hash);

            // Assert
            isMatch.Should().BeFalse();
        }

        [Fact]
        public void UT_07_GenerateRefreshToken_LengthAndFormat()
        {
            // Arrange
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            
            // Act
            rng.GetBytes(randomNumber);
            var refreshToken = Convert.ToBase64String(randomNumber);

            // Assert
            refreshToken.Should().NotBeNullOrEmpty();
            refreshToken.Length.Should().Be(44); // 32 bytes base64 encoded is 44 chars
        }
    }
}
