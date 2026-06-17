using System;
using System.Collections.Generic;
using System.Linq;
using GameStore.API.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace GameStore.API.Data
{
    public static class DbSeeder
    {
        public static void Seed(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GameStoreDbContext>();

            context.Database.EnsureCreated();

            // Chỉ seed data nếu database chưa có Role nào
            if (context.Set<Role>().Any())
            {
                return;
            }

            // 1. Roles
            var adminRole = new Role { RoleName = "Administrator" };
            var devRole = new Role { RoleName = "Developer" };
            var customerRole = new Role { RoleName = "Customer" };
            context.Set<Role>().AddRange(adminRole, devRole, customerRole);
            context.SaveChanges();

            // 2. Users (Admin, 1 Dev, 5 Customers)
            var admin = new User { Username = "SuperAdmin", Email = "admin@nexus.com", PasswordHash = "hash1", RoleId = adminRole.Id, CreatedAt = DateTime.UtcNow, WalletBalance = 9999999 };
            var dev = new User { Username = "CDProjektRed", Email = "dev@cdpr.com", PasswordHash = "hash2", RoleId = devRole.Id, CreatedAt = DateTime.UtcNow, WalletBalance = 5000000 };
            
            var customers = new List<User>
            {
                new User { Username = "GamerVN99", Email = "gamer99@gmail.com", PasswordHash = "hash3", RoleId = customerRole.Id, CreatedAt = DateTime.UtcNow, WalletBalance = 2000000 },
                new User { Username = "DarkKnight", Email = "dark@yahoo.com", PasswordHash = "hash4", RoleId = customerRole.Id, CreatedAt = DateTime.UtcNow, WalletBalance = 1500000 },
                new User { Username = "YasuoMains", Email = "yasuoprogmail.com", PasswordHash = "hash5", RoleId = customerRole.Id, CreatedAt = DateTime.UtcNow, WalletBalance = 3500000 },
                new User { Username = "FakerFan", Email = "faker@sktt1.com", PasswordHash = "hash6", RoleId = customerRole.Id, CreatedAt = DateTime.UtcNow, WalletBalance = 500000 },
                new User { Username = "NoobMaster69", Email = "noob@thor.com", PasswordHash = "hash7", RoleId = customerRole.Id, CreatedAt = DateTime.UtcNow, WalletBalance = 800000 }
            };

            context.Users.Add(admin);
            context.Users.Add(dev);
            context.Users.AddRange(customers);
            context.SaveChanges();

            // 3. Categories
            var catNames = new[] { "Hành động", "Nhập vai", "Chiến thuật", "Thể thao", "Giải đố", "Tiện ích", "Phần mềm", "Đồ họa" };
            var cats = new List<Category>();
            foreach (var c in catNames) {
                cats.Add(new Category { Name = c, Description = c });
            }
            context.Categories.AddRange(cats);
            context.SaveChanges();

            // 4. Products (20 Games + 4 Apps)
            var gamesData = new List<(string Name, decimal Price, string Image, string Desc)>
            {
                ("Black Myth: Wukong", 1290000, "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800&q=80", "Hành động nhập vai bối cảnh Tây Du Ký"),
                ("Cyberpunk 2077", 990000, "https://images.unsplash.com/photo-1605901309584-818e25960b8f?w=800&q=80", "Khám phá thành phố Night City tội phạm"),
                ("Red Dead Redemption 2", 1000000, "https://images.unsplash.com/photo-1493711662062-fa541adb3fc8?w=800&q=80", "Siêu phẩm viễn tây thế giới mở"),
                ("The Witcher 3: Wild Hunt", 390000, "https://images.unsplash.com/photo-1552820728-8b83bb6b773f?w=800&q=80", "Phiêu lưu cùng Geralt of Rivia"),
                ("EA SPORTS FC 24", 1090000, "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=800&q=80", "Trò chơi bóng đá hấp dẫn nhất hành tinh"),
                ("God of War Ragnarök", 1390000, "https://images.unsplash.com/photo-1585504198199-20277593b94f?w=800&q=80", "Cuộc chiến của các vị thần Bắc Âu"),
                ("Hogwarts Legacy", 890000, "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800&q=80", "Nhập vai phù thủy tại trường Hogwarts"),
                ("Elden Ring", 1190000, "https://images.unsplash.com/photo-1605901309584-818e25960b8f?w=800&q=80", "Siêu phẩm Soulslike năm 2022"),
                ("Marvel's Spider-Man Remastered", 1150000, "https://images.unsplash.com/photo-1493711662062-fa541adb3fc8?w=800&q=80", "Đu tơ trên các tòa nhà New York"),
                ("Horizon Zero Dawn", 690000, "https://images.unsplash.com/photo-1552820728-8b83bb6b773f?w=800&q=80", "Chiến đấu với quái vật máy móc khổng lồ"),
                ("Sekiro: Shadows Die Twice", 990000, "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=800&q=80", "Làm ninja Nhật Bản thời Chiến Quốc"),
                ("Call of Duty: Modern Warfare III", 1750000, "https://images.unsplash.com/photo-1585504198199-20277593b94f?w=800&q=80", "Bắn súng FPS cực cháy"),
                ("Baldur's Gate 3", 990000, "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800&q=80", "RPG hay nhất thế giới hiện tại"),
                ("Resident Evil 4 Remake", 1020000, "https://images.unsplash.com/photo-1605901309584-818e25960b8f?w=800&q=80", "Sống sót trước giáo phái điên cuồng"),
                ("Starfield", 1190000, "https://images.unsplash.com/photo-1493711662062-fa541adb3fc8?w=800&q=80", "Khám phá hàng ngàn hành tinh"),
                ("Mortal Kombat 1", 1090000, "https://images.unsplash.com/photo-1552820728-8b83bb6b773f?w=800&q=80", "Game đối kháng siêu bạo lực"),
                ("Assassin's Creed Mirage", 890000, "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=800&q=80", "Trở về cội nguồn sát thủ"),
                ("Ghost of Tsushima", 1290000, "https://images.unsplash.com/photo-1585504198199-20277593b94f?w=800&q=80", "Hành trình của Samurai Jin Sakai"),
                ("Helldivers 2", 690000, "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800&q=80", "Bảo vệ Super Earth cùng đồng đội"),
                ("Forza Horizon 5", 990000, "https://images.unsplash.com/photo-1605901309584-818e25960b8f?w=800&q=80", "Đua xe thế giới mở đỉnh cao")
            };

            var appsData = new List<(string Name, decimal Price, string Image, string Desc)>
            {
                ("Photoshop Clone", 500000, "https://images.unsplash.com/photo-1611162617474-5b21e879e113?w=800&q=80", "Phần mềm chỉnh sửa ảnh"),
                ("Video Editor Pro", 800000, "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800&q=80", "Chỉnh sửa video chuyên nghiệp"),
                ("3D Modeler", 1500000, "https://images.unsplash.com/photo-1552820728-8b83bb6b773f?w=800&q=80", "Dựng hình 3D cao cấp"),
                ("Music Studio", 1200000, "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=800&q=80", "Sản xuất âm nhạc")
            };

            var products = new List<Product>();
            var r = new Random();

            foreach (var g in gamesData)
            {
                var p = new Product
                {
                    Name = g.Name,
                    Description = g.Desc,
                    BasePrice = g.Price,
                    DeveloperId = dev.Id,
                    Status = "Active",
                    Type = "Game",
                    Pricing = "Paid",
                    ReleaseDate = DateTime.UtcNow.AddMonths(-r.Next(1, 24)),
                    CreatedAt = DateTime.UtcNow
                };
                products.Add(p);
            }
            foreach (var a in appsData)
            {
                var p = new Product
                {
                    Name = a.Name,
                    Description = a.Desc,
                    BasePrice = a.Price,
                    DeveloperId = dev.Id,
                    Status = "Active",
                    Type = "App",
                    Pricing = "Paid",
                    ReleaseDate = DateTime.UtcNow.AddMonths(-r.Next(1, 24)),
                    CreatedAt = DateTime.UtcNow
                };
                products.Add(p);
            }

            context.Products.AddRange(products);
            context.SaveChanges();

            // 5. Product Media & Categories
            var medias = new List<ProductMedia>();
            var productCategories = new List<ProductCategory>();

            for (int i = 0; i < products.Count; i++)
            {
                var p = products[i];
                var imageUrl = p.Type == "Game" ? gamesData.First(g => g.Name == p.Name).Image : appsData.First(a => a.Name == p.Name).Image;
                
                medias.Add(new ProductMedia
                {
                    ProductId = p.Id,
                    MediaType = "Cover",
                    MediaUrl = imageUrl,
                    DisplayOrder = 1
                });

                // Assign categories
                if (p.Type == "Game")
                {
                    // Game gets random categories from 0 to 4
                    int catIdx = r.Next(0, 5);
                    productCategories.Add(new ProductCategory { ProductId = p.Id, CategoryId = cats[catIdx].Id });
                }
                else
                {
                    // App gets random categories from 5 to 7
                    int catIdx = r.Next(5, 8);
                    productCategories.Add(new ProductCategory { ProductId = p.Id, CategoryId = cats[catIdx].Id });
                }
            }
            context.Set<ProductMedia>().AddRange(medias);
            context.Set<ProductCategory>().AddRange(productCategories);
            context.SaveChanges();

            // 6. Orders & Product Keys
            for (int i = 0; i < 20; i++)
            {
                var customer = customers[r.Next(customers.Count)];
                var product = products[r.Next(products.Count)];

                var order = new Order
                {
                    UserId = customer.Id,
                    TotalAmount = product.BasePrice,
                    Status = "Thành công",
                    OrderDate = DateTime.UtcNow.AddDays(-r.Next(1, 30))
                };
                context.Orders.Add(order);
                context.SaveChanges();

                var key = new ProductKey
                {
                    ProductId = product.Id,
                    OrderId = order.Id,
                    KeyCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper() + "-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    IsSold = true
                };
                context.Set<ProductKey>().Add(key);
            }

            for (int i = 0; i < 50; i++)
            {
                var product = products[r.Next(products.Count)];
                var key = new ProductKey
                {
                    ProductId = product.Id,
                    OrderId = null,
                    KeyCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper() + "-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    IsSold = false
                };
                context.Set<ProductKey>().Add(key);
            }

            context.SaveChanges();

            // 7. News Articles
            var newsData = new List<NewsArticle>
            {
                new NewsArticle { Title = "Bản Cập Nhật Mùa Hè: Đại chiến thế giới ngầm", Summary = "Hàng loạt tính năng mới và bản đồ sa mạc đã được mở khóa. Các game thủ đã sẵn sàng chưa?", Content = "<p>Chào mừng đến với bản cập nhật lớn nhất năm! Bản đồ mới, vũ khí mới và chế độ sinh tồn 100 người chơi sẽ mang đến trải nghiệm cực kỳ khốc liệt.</p><p>Đừng bỏ lỡ sự kiện đăng nhập 7 ngày nhận ngay skin súng huyền thoại!</p>", CoverImageUrl = "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800&q=80", AuthorId = dev.Id, PublishedAt = DateTime.UtcNow.AddDays(-2) },
                new NewsArticle { Title = "Giải Đấu Thể Thao Điện Tử Nexus Mùa 1", Summary = "Đăng ký ngay để tranh giải thưởng lên tới 500.000.000 VND.", Content = "<p>Nexus Store tự hào công bố giải đấu eSports đầu tiên với tựa game Cyber Strike. Các đội tuyển có thể đăng ký từ hôm nay.</p><p>Vòng loại sẽ diễn ra trực tuyến, vòng chung kết sẽ được tổ chức tại sân vận động Nexus Arena.</p>", CoverImageUrl = "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=800&q=80", AuthorId = admin.Id, PublishedAt = DateTime.UtcNow.AddDays(-5) },
                new NewsArticle { Title = "Đánh Giá Black Myth: Wukong - Tuyệt Phẩm Hành Động", Summary = "Tựa game đang làm mưa làm gió có thực sự xứng đáng với điểm 10 tuyệt đối?", Content = "<p>Đồ họa đỉnh cao, cốt truyện sâu sắc và hệ thống chiến đấu cực kỳ mãn nhãn. Wukong không chỉ là một tựa game, đó là một tác phẩm nghệ thuật.</p><p>Hệ thống boss phong phú với hàng loạt cơ chế biến hóa khôn lường chắc chắn sẽ làm hài lòng những game thủ khó tính nhất.</p>", CoverImageUrl = "https://images.unsplash.com/photo-1605901309584-818e25960b8f?w=800&q=80", AuthorId = admin.Id, PublishedAt = DateTime.UtcNow.AddDays(-1) },
                new NewsArticle { Title = "Giảm Giá Lên Tới 80% Tuần Lễ Vàng", Summary = "Cơ hội sở hữu hàng ngàn tựa game AAA với mức giá rẻ như cho.", Content = "<p>Từ ngày mai đến hết tuần, toàn bộ hệ thống Nexus Store sẽ áp dụng chương trình flash sale giảm giá sốc từ 30% đến 80%.</p><p>Hãy nhanh tay thêm các game yêu thích vào giỏ hàng ngay bây giờ!</p>", CoverImageUrl = "https://images.unsplash.com/photo-1552820728-8b83bb6b773f?w=800&q=80", AuthorId = admin.Id, PublishedAt = DateTime.UtcNow.AddHours(-5) }
            };
            context.Set<NewsArticle>().AddRange(newsData);
            context.SaveChanges();
        }
    }
}
