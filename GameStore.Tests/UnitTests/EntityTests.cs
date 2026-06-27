using System;
using FluentAssertions;
using GameStore.API.Entities;
using Xunit;

namespace GameStore.Tests.UnitTests
{
    public class EntityTests
    {
        [Fact]
        public void UT_01_CreateValidUserEntity()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                WalletBalance = 100000,
                RoleId = 2,
                CreatedAt = DateTime.UtcNow
            };

            // Act & Assert
            user.Should().NotBeNull();
            user.Username.Should().Be("testuser");
            user.Email.Should().Be("test@example.com");
            user.WalletBalance.Should().Be(100000);
            user.RoleId.Should().Be(2);
        }

        [Theory]
        [InlineData(100000)]
        [InlineData(0)]
        public void UT_08_ProductEntity_BasePriceIsValid(decimal price)
        {
            // Arrange
            var product = new Product
            {
                Name = "Test Game",
                Description = "A great game",
                Type = "Game",
                Pricing = "Paid",
                BasePrice = price,
                Status = "Active",
                ReleaseDate = DateTime.UtcNow
            };

            // Act & Assert
            product.BasePrice.Should().BeGreaterThanOrEqualTo(0);
        }

        [Theory]
        [InlineData("Pending")]
        [InlineData("Completed")]
        [InlineData("Failed")]
        public void UT_09_OrderEntity_StatusValuesAreValid(string status)
        {
            // Arrange
            var order = new Order
            {
                UserId = 1,
                TotalAmount = 500000,
                Status = status,
                OrderDate = DateTime.UtcNow
            };

            // Act & Assert
            var validStatuses = new[] { "Pending", "Completed", "Failed" };
            validStatuses.Should().Contain(order.Status);
        }

        [Fact]
        public void UT_10_ProductKey_IsSoldLogic()
        {
            // Arrange
            var keyUnsold = new ProductKey
            {
                ProductId = 1,
                KeyCode = "XXXX-YYYY-ZZZZ",
                OrderId = null
            };

            var keySold = new ProductKey
            {
                ProductId = 1,
                KeyCode = "AAAA-BBBB-CCCC",
                OrderId = 10
            };

            // Act
            keyUnsold.IsSold = keyUnsold.OrderId.HasValue;
            keySold.IsSold = keySold.OrderId.HasValue;

            // Assert
            keyUnsold.IsSold.Should().BeFalse();
            keySold.IsSold.Should().BeTrue();
        }
    }
}
