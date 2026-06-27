using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using GameStore.API.Data;
using Microsoft.Extensions.DependencyInjection;

var optionsBuilder = new DbContextOptionsBuilder<GameStoreDbContext>();
optionsBuilder.UseSqlServer("Server=DESKTOP-2F6HTKJ\\SQLEXPRESS;Database=GameStoreDB;User Id=hoangky12345s;Password=Hoangky123@;TrustServerCertificate=True;MultipleActiveResultSets=true;");

using (var db = new GameStoreDbContext(optionsBuilder.Options))
{
    var keys = db.ProductKeys.Include(k => k.Order).ToList();
    Console.WriteLine("--- PRODUCT KEYS ---");
    foreach (var k in keys)
    {
        Console.WriteLine($"KeyId: {k.Id}, ProductId: {k.ProductId}, Key: {k.KeyCode}, IsSold: {k.IsSold}, OrderId: {k.OrderId}, OrderUser: {k.Order?.UserId}");
    }
}
