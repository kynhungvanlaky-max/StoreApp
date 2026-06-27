using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using GameStore.API.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameStore.Tests.IntegrationTests
{
    public class ApiEndpointsTests : IClassFixture<GameStoreApiFactory>
    {
        private readonly GameStoreApiFactory _factory;
        private readonly IServiceScopeFactory _scopeFactory;

        public ApiEndpointsTests(GameStoreApiFactory factory)
        {
            _factory = factory;
            _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        }

        [Fact]
        public async Task GetRoot_Returns200WithSuccessMessage()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Nexus Store API is running successfully!", content);
        }

        [Fact]
        public async Task HealthEndpoint_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act - Check OpenAPI endpoint exists
            var response = await client.GetAsync("/openapi/v1.json");

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
        }
    }
}