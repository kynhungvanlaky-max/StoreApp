using System;
using System.Collections.Generic;
using System.Linq;
using GameStore.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameStore.API.Data
{
    public static class DbInitializer
    {
        public static void EnsureSeedRolesAndUsers(GameStoreDbContext db)
        {
            var random = new Random(42);

            // 1. Roles
            var requiredRoleNames = new[] { "Administrator", "Customer", "Developer", "Staff" };
            var existingRoleNames = db.Roles
                .Select(r => r.RoleName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var roleName in requiredRoleNames)
            {
                if (!existingRoleNames.Contains(roleName))
                {
                    db.Roles.Add(new Role { RoleName = roleName });
                }
            }
            db.SaveChanges();

            var roleIds = db.Roles
                .Where(r => requiredRoleNames.Contains(r.RoleName))
                .ToDictionary(r => r.RoleName, r => r.Id, StringComparer.OrdinalIgnoreCase);

            // 2. Users
            var seedUsers = new List<(string Username, string Email, string Role, decimal Balance, string Avatar)>
            {
                ("admin", "admin@nexuskeys.com", "Administrator", 50000000m, "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150"),
                ("staff", "staff@nexuskeys.com", "Staff", 20000000m, "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150"),
                ("CDProjektRed", "dev@cdprojekt.com", "Developer", 12000000m, "https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?w=150"),
                ("CapcomDev", "dev@capcom.com", "Developer", 18000000m, "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150"),
                ("FromSoftware", "dev@fromsoft.com", "Developer", 25000000m, "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150"),
                ("GamerPro99", "gamer99@gmail.com", "Customer", 2500000m, "https://images.unsplash.com/photo-1580489944761-15a19d654956?w=150"),
                ("AlexKing", "alex@gmail.com", "Customer", 1200000m, "https://images.unsplash.com/photo-1527980965255-d3b416303d12?w=150"),
                ("LinhNguyen", "linh.nguyen@gmail.com", "Customer", 850000m, "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150"),
                ("MinhTri98", "minhtri98@gmail.com", "Customer", 3400000m, "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150"),
                ("ThuHuong", "thuhuong.gamer@gmail.com", "Customer", 650000m, "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=150"),
                ("HoangLong", "long.hoang@gmail.com", "Customer", 150000m, "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=150")
            };

            foreach (var u in seedUsers)
            {
                var existingUser = db.Users.FirstOrDefault(x => x.Email == u.Email || x.Username == u.Username);
                if (existingUser == null)
                {
                    db.Users.Add(new User
                    {
                        Username = u.Username,
                        Email = u.Email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(u.Role == "Administrator" ? "Admin123!" : (u.Role == "Staff" ? "Staff123!" : (u.Role == "Developer" ? "Dev123!" : "User123!"))),
                        RoleId = roleIds[u.Role],
                        WalletBalance = u.Balance,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(30, 180)),
                        AvatarUrl = u.Avatar,
                        IsLocked = false
                    });
                }
            }
            db.SaveChanges();
            EnsureSeedAiAssistantConfig(db);
            EnsureCleanCatalogAndFixMedia(db);
        }

        private static void EnsureCleanCatalogAndFixMedia(GameStoreDbContext db)
        {
            try
            {
                // Fix broken Unsplash URLs from historic seeds
                var brokenMedia = db.ProductMedia.Where(m => m.MediaUrl.Contains("photo-1508098682722-e99c43a406b2")).ToList();
                if (brokenMedia.Count > 0)
                {
                    foreach (var bm in brokenMedia)
                    {
                        bm.MediaUrl = "https://images.unsplash.com/photo-1579952363873-27f3bade9f55?w=800";
                    }
                    db.SaveChanges();
                }

                // Clean up test / garbage products
                var junkProducts = db.Products.Where(p => p.Name.Contains("asdasd") || p.Name == "ádasdasdasd").ToList();
                if (junkProducts.Count > 0)
                {
                    foreach (var jp in junkProducts)
                    {
                        db.ProductMedia.RemoveRange(db.ProductMedia.Where(m => m.ProductId == jp.Id));
                        db.ProductCategories.RemoveRange(db.ProductCategories.Where(c => c.ProductId == jp.Id));
                        db.ProductKeys.RemoveRange(db.ProductKeys.Where(k => k.ProductId == jp.Id));
                        db.Wishlists.RemoveRange(db.Wishlists.Where(w => w.ProductId == jp.Id));
                        db.CartItems.RemoveRange(db.CartItems.Where(c => c.ProductId == jp.Id));
                        db.Products.Remove(jp);
                    }
                    db.SaveChanges();
                }

                // Clean up obsolete gift cards and category
                var obsoleteGiftCards = db.Products.Where(p => 
                    p.Type == "GiftCard" || 
                    p.Name.Contains("Gift Card") || 
                    p.Name.Contains("Wallet Card") ||
                    p.Name.Contains("Thẻ quà tặng") ||
                    p.Name.Contains("Thẻ nạp")).ToList();
                if (obsoleteGiftCards.Count > 0)
                {
                    foreach (var gp in obsoleteGiftCards)
                    {
                        db.ProductMedia.RemoveRange(db.ProductMedia.Where(m => m.ProductId == gp.Id));
                        db.ProductCategories.RemoveRange(db.ProductCategories.Where(c => c.ProductId == gp.Id));
                        db.ProductKeys.RemoveRange(db.ProductKeys.Where(k => k.ProductId == gp.Id));
                        db.Wishlists.RemoveRange(db.Wishlists.Where(w => w.ProductId == gp.Id));
                        db.CartItems.RemoveRange(db.CartItems.Where(c => c.ProductId == gp.Id));
                        db.Products.Remove(gp);
                    }
                    db.SaveChanges();
                }

                var obsoleteGiftCat = db.Categories.FirstOrDefault(c => c.Name == "Thẻ quà tặng");
                if (obsoleteGiftCat != null)
                {
                    db.ProductCategories.RemoveRange(db.ProductCategories.Where(pc => pc.CategoryId == obsoleteGiftCat.Id));
                    db.Categories.Remove(obsoleteGiftCat);
                    db.SaveChanges();
                }
            }
            catch { }
        }

        public static void Seed(GameStoreDbContext db)
        {
            db.Database.EnsureCreated();
            EnsureSeedRolesAndUsers(db);
            var random = new Random(42);

            var requiredRoleNames = new[] { "Administrator", "Customer", "Developer", "Staff" };
            var roleIds = db.Roles
                .Where(r => requiredRoleNames.Contains(r.RoleName))
                .ToDictionary(r => r.RoleName, r => r.Id, StringComparer.OrdinalIgnoreCase);

            var adminUser = db.Users.First(u => u.RoleId == roleIds["Administrator"]);
            var devUsers = db.Users.Where(u => u.RoleId == roleIds["Developer"]).ToList();
            var devPrimary = devUsers.FirstOrDefault() ?? adminUser;
            var customerUsers = db.Users.Where(u => u.RoleId == roleIds["Customer"]).ToList();
            if (!customerUsers.Any()) customerUsers.Add(adminUser);

            // ==========================================
            // 3. Categories
            // ==========================================
            var categories = new Dictionary<string, Category>();
            string[] catNames = new[]
            {
                "Hành động", "Nhập vai (RPG)", "Chiến thuật", "Bắn súng", "Thể thao",
                "Mô phỏng", "Phần mềm", "Giải đố", "Phiêu lưu",
                "Đua xe", "Sinh tồn", "Kinh dị", "Thế giới mở", "Anime",
                "Đồ họa & Thiết kế 3D", "Biên tập Video & Âm thanh", "Lập trình & Tiện ích", "Công cụ bản quyền"
            };

            foreach (var name in catNames)
            {
                var cat = db.Categories.FirstOrDefault(c => c.Name == name);
                if (cat == null)
                {
                    cat = new Category
                    {
                        Name = name,
                        Description = $"Các tựa game và sản phẩm thuộc thể loại {name} cực kỳ hấp dẫn, bản quyền chính hãng."
                    };
                    db.Categories.Add(cat);
                    db.SaveChanges();
                }
                categories[name] = cat;
            }

            // ==========================================
            // 4. Coupons
            // ==========================================
            var couponsList = new List<(string Code, decimal Discount, int Max, int Usage, int Days)>
            {
                ("WELCOME10", 10m, 500, 24, 90),
                ("NEXUS2026", 15m, 200, 68, 60),
                ("MEGASALE", 25m, 100, 42, 30),
                ("VIPGAMER", 20m, 50, 15, 45),
                ("SUMMER50K", 5m, 1000, 150, 120)
            };

            foreach (var c in couponsList)
            {
                if (!db.Coupons.Any(x => x.Code == c.Code))
                {
                    db.Coupons.Add(new Coupon
                    {
                        Code = c.Code,
                        DiscountPercent = c.Discount,
                        MaxUsage = c.Max,
                        CurrentUsage = c.Usage,
                        ExpiryDate = DateTime.UtcNow.AddDays(c.Days),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    });
                }
            }
            db.SaveChanges();

            // ==========================================
            // 5. Products & ProductMedia & SystemRequirements & ProductKeys
            // ==========================================
            var productSeedList = new List<(string Name, string Desc, decimal Price, string Type, string Pricing, string ImgUrl, string[] Cats, int DevIndex)>
            {
                ("The Witcher 3: Wild Hunt", "Phiêu lưu cùng Geralt of Rivia trong thế giới mở rộng lớn đầy nguy hiểm và quái vật huyền thoại.", 390000m, "Game", "Paid", "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800", new[] { "Nhập vai (RPG)", "Hành động", "Phiêu lưu", "Thế giới mở" }, 0),
                ("Helldivers 2", "Bắn súng góc nhìn thứ ba phối hợp đồng đội chiến đấu vì dân chủ và sinh tồn trong vũ trụ.", 690000m, "Game", "Paid", "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=800", new[] { "Bắn súng", "Hành động", "Sinh tồn" }, 1),
                ("Call of Duty: Modern Warfare III", "Bắn súng FPS đỉnh cao với chiến dịch cốt truyện gay cấn và chế độ Multiplayer trực tuyến rực lửa.", 1750000m, "Game", "Paid", "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=800", new[] { "Bắn súng", "Hành động" }, 0),
                ("EA SPORTS FC 24", "Trải nghiệm bóng đá đỉnh cao thế giới với công nghệ HyperMotionV và hàng ngàn cầu thủ bản quyền.", 1090000m, "Game", "Paid", "https://images.unsplash.com/photo-1579952363873-27f3bade9f55?w=800", new[] { "Thể thao", "Mô phỏng" }, 1),
                ("Adobe Photoshop CC", "Bộ công cụ thiết kế đồ họa và chỉnh sửa ảnh kỹ thuật số hàng đầu thế giới với các tính năng AI chuyên nghiệp.", 590000m, "App", "Paid", "https://images.unsplash.com/photo-1572044162444-ad60f128bdea?w=800", new[] { "Phần mềm", "Đồ họa & Thiết kế 3D", "Công cụ bản quyền" }, 0),
                ("Blender 3D Studio", "Bộ công cụ sáng tạo đồ họa và dựng hình 3D mã nguồn mở chuyên nghiệp dành cho thiết kế, hoạt hình và game.", 0m, "App", "Free", "https://cdn.akamai.steamstatic.com/steam/apps/365670/library_600x900.jpg", new[] { "Phần mềm", "Đồ họa & Thiết kế 3D" }, 1),
                ("Adobe Premiere Pro", "Phần mềm biên tập dựng phim và video 4K/8K chuẩn công nghiệp điện ảnh với hiệu ứng chuyển cảnh mượt mà.", 650000m, "App", "Paid", "https://images.unsplash.com/photo-1574717024653-61fd2cf4d44d?w=800", new[] { "Phần mềm", "Biên tập Video & Âm thanh", "Công cụ bản quyền" }, 0),
                ("FL Studio Producer Edition", "Môi trường sản xuất âm nhạc và hòa âm phối khí chuyên nghiệp với nhạc cụ ảo và thu âm đa kênh.", 4950000m, "App", "Paid", "https://images.unsplash.com/photo-1598488035139-bdbb2231ce04?w=800", new[] { "Phần mềm", "Biên tập Video & Âm thanh", "Công cụ bản quyền" }, 1),
                ("Visual Studio Professional 2022", "Môi trường phát triển tích hợp (IDE) toàn diện dành cho lập trình viên phần mềm hiện đại.", 1050000m, "App", "Paid", "https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=800", new[] { "Phần mềm", "Lập trình & Tiện ích", "Công cụ bản quyền" }, 0),
                ("Starfield", "Khám phá vũ trụ bao la với hàng ngàn hành tinh trong tựa game RPG không gian đình đám từ Bethesda.", 1190000m, "Game", "Paid", "https://images.unsplash.com/photo-1614728894747-a83421e2b9c9?w=800", new[] { "Nhập vai (RPG)", "Phiêu lưu", "Thế giới mở" }, 0),
                ("Sekiro: Shadows Die Twice", "Làm ninja Nhật Bản thời Chiến Quốc, chinh phục những trận đánh trùm nghẹt thở và kịch tính.", 990000m, "Game", "Paid", "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=800", new[] { "Hành động", "Nhập vai (RPG)" }, 2),
                ("Black Myth: Wukong", "Hành động nhập vai thần thoại lấy cảm hứng từ Tây Du Ký với đồ họa Unreal Engine 5 rực rỡ.", 1290000m, "Game", "Paid", "https://images.unsplash.com/photo-1538481199705-c710c4e965fc?w=800", new[] { "Hành động", "Nhập vai (RPG)", "Phiêu lưu" }, 0),
                ("Resident Evil 4 Remake", "Sống sót trước thảm họa sinh học kinh hoàng và chiến đấu cứu con gái Tổng thống.", 1020000m, "Game", "Paid", "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=800", new[] { "Kinh dị", "Sinh tồn", "Hành động" }, 1),
                ("Baldur's Gate 3", "Game RPG đoạt giải Game of the Year với cốt truyện phân nhánh sâu sắc và lối chơi chiến thuật theo lượt.", 990000m, "Game", "Paid", "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=800", new[] { "Nhập vai (RPG)", "Chiến thuật" }, 0),
                ("Mortal Kombat 1", "Tựa game đối kháng huyền thoại tái sinh với đồ họa siêu thực và đòn đánh Fatality hoành tráng.", 1090000m, "Game", "Paid", "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800", new[] { "Hành động" }, 1),
                ("Elden Ring", "Chinh phục vùng đất Lands Between huyền bí trong siêu phẩm RPG hành động của FromSoftware.", 1150000m, "Game", "Paid", "https://images.unsplash.com/photo-1563089145-599997674d42?w=800", new[] { "Nhập vai (RPG)", "Hành động", "Thế giới mở" }, 2),
                ("Red Dead Redemption 2", "Hành trình nghẹt thở của băng đảng outlaw Arthur Morgan miền Tây hoang dã Mỹ.", 1000000m, "Game", "Paid", "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=800", new[] { "Hành động", "Phiêu lưu", "Sinh tồn", "Thế giới mở" }, 0),
                ("Cyberpunk 2077", "Khám phá thành phố tương lai Night City ngập tràn ánh đèn neon và công nghệ cấy ghép sinh học.", 990000m, "Game", "Paid", "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800", new[] { "Nhập vai (RPG)", "Hành động", "Thế giới mở" }, 0),
                ("Ghost of Tsushima", "Trở thành bóng ma Jin Sakai chiến đấu bảo vệ hòn đảo Tsushima khỏi quân xâm lược.", 1290000m, "Game", "Paid", "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=800", new[] { "Hành động", "Phiêu lưu" }, 1),
                ("Horizon Zero Dawn", "Hành trình của Aloy chiến đấu với sinh vật máy móc trong thế giới hậu tận thế.", 690000m, "Game", "Paid", "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=800", new[] { "Hành động", "Nhập vai (RPG)", "Thế giới mở" }, 0),
                ("God of War Ragnarök", "Hành trình cha con Kratos và Atreus đối mặt với trận chiến tận thế Bắc Âu.", 1390000m, "Game", "Paid", "https://images.unsplash.com/photo-1538481199705-c710c4e965fc?w=800", new[] { "Hành động", "Phiêu lưu" }, 1),
                ("Grand Theft Auto V", "Trải nghiệm cuộc sống phiêu lưu kịch tính tại thành phố Los Santos rực rỡ.", 450000m, "Game", "Paid", "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=800", new[] { "Hành động", "Đua xe", "Thế giới mở" }, 0),
                ("Counter-Strike 2", "Tựa game bắn súng góc nhìn thứ nhất thi đấu huyền thoại nhất mọi thời đại.", 0m, "Game", "Free", "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800", new[] { "Bắn súng", "Hành động" }, 1),
                ("Apex Legends", "Bắn súng sinh tồn Battle Royale với hàng loạt nhân vật sở hữu kỹ năng độc đáo.", 0m, "Game", "Free", "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=800", new[] { "Bắn súng", "Sinh tồn" }, 0),
                ("Overwatch 2", "Bắn súng anh hùng 5v5 trực tuyến sôi động với kho anh hùng phong phú.", 0m, "Game", "Free", "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=800", new[] { "Bắn súng" }, 1),
                ("Windows 11 Pro License", "Bản quyền hệ điều hành Windows 11 Pro chính hãng với bảo mật doanh nghiệp và ảo hóa nâng cao.", 2490000m, "App", "Paid", "https://images.unsplash.com/photo-1629654297299-c8506221ca97?w=800", new[] { "Phần mềm", "Lập trình & Tiện ích", "Công cụ bản quyền" }, 1),
                ("AutoCAD Architecture & Design", "Phần mềm thiết kế đồ họa kỹ thuật CAD 2D/3D chuyên nghiệp dành cho kiến trúc và cơ khí.", 3850000m, "App", "Paid", "https://images.unsplash.com/photo-1503387762-592deb58ef4e?w=800", new[] { "Phần mềm", "Đồ họa & Thiết kế 3D", "Công cụ bản quyền" }, 0),
                ("Wallpaper Engine", "Ứng dụng hình nền động đỉnh cao cho PC, hỗ trợ đồ họa thời gian thực và cá nhân hóa không gian làm việc.", 99000m, "App", "Paid", "https://cdn.akamai.steamstatic.com/steam/apps/431960/library_600x900.jpg", new[] { "Phần mềm", "Lập trình & Tiện ích" }, 1),
                ("Substance 3D Painter", "Vẽ chất liệu và kết cấu 3D chuẩn ngành game AAA và kỹ xảo điện ảnh.", 3490000m, "App", "Paid", "https://cdn.akamai.steamstatic.com/steam/apps/1775390/library_600x900.jpg", new[] { "Phần mềm", "Đồ họa & Thiết kế 3D", "Công cụ bản quyền" }, 0)
            };

            foreach (var item in productSeedList)
            {
                var targetDev = devUsers.Count > item.DevIndex ? devUsers[item.DevIndex] : devPrimary;
                var prod = db.Products
                    .Include(p => p.ProductCategories)
                    .Include(p => p.ProductKeys)
                    .Include(p => p.ProductMedia)
                    .FirstOrDefault(p => p.Name == item.Name);

                if (prod == null)
                {
                    prod = new Product
                    {
                        Name = item.Name,
                        Description = item.Desc,
                        DeveloperId = targetDev.Id,
                        Type = item.Type,
                        Pricing = item.Pricing,
                        BasePrice = item.Price,
                        Status = "Active",
                        ReleaseDate = DateTime.UtcNow.AddMonths(-random.Next(1, 36)),
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(30, 200))
                    };
                    db.Products.Add(prod);
                    db.SaveChanges();
                }
                else
                {
                    bool changed = false;
                    if (prod.Type != item.Type)
                    {
                        prod.Type = item.Type;
                        changed = true;
                    }
                    if (prod.Pricing != item.Pricing)
                    {
                        prod.Pricing = item.Pricing;
                        changed = true;
                    }
                    if (prod.BasePrice != item.Price)
                    {
                        prod.BasePrice = item.Price;
                        changed = true;
                    }
                    if (changed)
                    {
                        db.SaveChanges();
                    }
                }

                // Categories
                foreach (var catName in item.Cats)
                {
                    if (categories.TryGetValue(catName, out var catObj))
                    {
                        if (!prod.ProductCategories.Any(pc => pc.CategoryId == catObj.Id))
                        {
                            db.ProductCategories.Add(new ProductCategory
                            {
                                ProductId = prod.Id,
                                CategoryId = catObj.Id
                            });
                        }
                    }
                }

                // Media (Cover + Screenshots)
                if (!db.ProductMedia.Any(m => m.ProductId == prod.Id))
                {
                    db.ProductMedia.AddRange(
                        new ProductMedia
                        {
                            ProductId = prod.Id,
                            MediaType = "Cover",
                            MediaUrl = item.ImgUrl,
                            DisplayOrder = 1
                        },
                        new ProductMedia
                        {
                            ProductId = prod.Id,
                            MediaType = "Screenshot",
                            MediaUrl = "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=800",
                            DisplayOrder = 2
                        },
                        new ProductMedia
                        {
                            ProductId = prod.Id,
                            MediaType = "Screenshot",
                            MediaUrl = "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=800",
                            DisplayOrder = 3
                        }
                    );
                }

                // System Requirements
                if (item.Type == "Game" && !db.SystemRequirements.Any(sr => sr.ProductId == prod.Id))
                {
                    db.SystemRequirements.Add(new SystemRequirement
                    {
                        ProductId = prod.Id,
                        RequirementType = "Minimum",
                        TargetOS = "Windows 10 64-bit",
                        OSVersion = "Windows 10",
                        Processor = "Intel Core i5-8400 / AMD Ryzen 5 2600",
                        Memory = "12 GB RAM",
                        Graphics = "NVIDIA GeForce GTX 1060 6GB / AMD Radeon RX 580",
                        Storage = "70 GB SSD"
                    });

                    db.SystemRequirements.Add(new SystemRequirement
                    {
                        ProductId = prod.Id,
                        RequirementType = "Recommended",
                        TargetOS = "Windows 11 64-bit",
                        OSVersion = "Windows 11",
                        Processor = "Intel Core i7-10700K / AMD Ryzen 7 5800X",
                        Memory = "16 GB RAM",
                        Graphics = "NVIDIA GeForce RTX 3070 / AMD Radeon RX 6800",
                        Storage = "70 GB NVMe SSD"
                    });
                }

                // Product Keys (Ensure at least 15 unsold stock keys exist)
                var unsoldKeysCount = db.ProductKeys.Count(k => k.ProductId == prod.Id && !k.IsSold);
                if (unsoldKeysCount < 10)
                {
                    var prefix = new string(item.Name.Where(char.IsLetterOrDigit).Take(4).ToArray()).ToUpper();
                    if (string.IsNullOrEmpty(prefix)) prefix = "GAME";
                    for (int k = 1; k <= 15; k++)
                    {
                        var keyString = $"{prefix}-{random.Next(1000, 9999)}-{random.Next(1000, 9999)}-{random.Next(1000, 9999)}";
                        db.ProductKeys.Add(new ProductKey
                        {
                            ProductId = prod.Id,
                            KeyCode = keyString,
                            IsSold = false,
                            OrderId = null
                        });
                    }
                }
            }
            db.SaveChanges();

            // Fix broken Unsplash URLs from historic seeds
            var brokenMedia = db.ProductMedia.Where(m => m.MediaUrl.Contains("photo-1508098682722-e99c43a406b2")).ToList();
            if (brokenMedia.Count > 0)
            {
                foreach (var bm in brokenMedia)
                {
                    bm.MediaUrl = "https://images.unsplash.com/photo-1579952363873-27f3bade9f55?w=800";
                }
                db.SaveChanges();
            }

            // Clean up test / garbage products
            var junkProducts = db.Products.Where(p => p.Name.Contains("asdasd") || p.Name == "ádasdasdasd").ToList();
            if (junkProducts.Count > 0)
            {
                foreach (var jp in junkProducts)
                {
                    db.ProductMedia.RemoveRange(db.ProductMedia.Where(m => m.ProductId == jp.Id));
                    db.ProductCategories.RemoveRange(db.ProductCategories.Where(c => c.ProductId == jp.Id));
                    db.ProductKeys.RemoveRange(db.ProductKeys.Where(k => k.ProductId == jp.Id));
                    db.Wishlists.RemoveRange(db.Wishlists.Where(w => w.ProductId == jp.Id));
                    db.CartItems.RemoveRange(db.CartItems.Where(c => c.ProductId == jp.Id));
                    db.Products.Remove(jp);
                }
                db.SaveChanges();
            }

            var allProducts = db.Products.ToList();

            // ==========================================
            // 6. Promotions & ProductPromotions
            // ==========================================
            if (!db.Promotions.Any())
            {
                var summerSale = new Promotion
                {
                    Name = "Summer Mega Sale 2026",
                    Description = "Siêu sale mùa hè lớn nhất năm giảm giá sốc hàng loạt bom tấn AAA.",
                    DiscountPercent = 30m,
                    StartDate = DateTime.UtcNow.AddDays(-10),
                    EndDate = DateTime.UtcNow.AddDays(20)
                };

                var weekendFlash = new Promotion
                {
                    Name = "Flash Deal Cuối Tuần",
                    Description = "Giảm giá đặc biệt lên đến 50% chỉ áp dụng trong 3 ngày cuối tuần.",
                    DiscountPercent = 50m,
                    StartDate = DateTime.UtcNow.AddDays(-1),
                    EndDate = DateTime.UtcNow.AddDays(3)
                };

                var rpgFest = new Promotion
                {
                    Name = "Lễ Hội Nhập Vai & Hành Động RPG",
                    Description = "Ưu đãi các tựa game RPG xuất sắc nhất thế giới mở.",
                    DiscountPercent = 20m,
                    StartDate = DateTime.UtcNow.AddDays(-5),
                    EndDate = DateTime.UtcNow.AddDays(15)
                };

                var upcomingAutumn = new Promotion
                {
                    Name = "Autumn Gamer Fest",
                    Description = "Đón chào mùa thu cùng đại tiệc giảm giá.",
                    DiscountPercent = 35m,
                    StartDate = DateTime.UtcNow.AddDays(30),
                    EndDate = DateTime.UtcNow.AddDays(60)
                };

                db.Promotions.AddRange(summerSale, weekendFlash, rpgFest, upcomingAutumn);
                db.SaveChanges();

                // Link products to promotions
                var promoTargets = allProducts.Where(p => p.Pricing == "Paid").ToList();
                for (int i = 0; i < promoTargets.Count; i++)
                {
                    var prod = promoTargets[i];
                    if (i % 3 == 0)
                    {
                        db.ProductPromotions.Add(new ProductPromotion { ProductId = prod.Id, PromotionId = summerSale.Id });
                    }
                    else if (i % 3 == 1)
                    {
                        db.ProductPromotions.Add(new ProductPromotion { ProductId = prod.Id, PromotionId = weekendFlash.Id });
                    }
                    else
                    {
                        db.ProductPromotions.Add(new ProductPromotion { ProductId = prod.Id, PromotionId = rpgFest.Id });
                    }
                }
                db.SaveChanges();
            }

            // ==========================================
            // 7. Releases
            // ==========================================
            if (!db.Releases.Any())
            {
                foreach (var prod in allProducts)
                {
                    db.Releases.AddRange(
                        new Release
                        {
                            ProductId = prod.Id,
                            VersionNumber = "v1.0.0",
                            TargetOS = "Windows 10/11 64-bit",
                            DownloadUrl = $"https://cdn.nexuskeys.com/builds/{prod.Id}/setup-v1.0.0.exe",
                            Checksum = $"SHA256-{Guid.NewGuid():N}"
                        },
                        new Release
                        {
                            ProductId = prod.Id,
                            VersionNumber = "v1.2.4",
                            TargetOS = "Windows 10/11 64-bit",
                            DownloadUrl = $"https://cdn.nexuskeys.com/builds/{prod.Id}/patch-v1.2.4.exe",
                            Checksum = $"SHA256-{Guid.NewGuid():N}"
                        }
                    );
                }
                db.SaveChanges();
            }

            // ==========================================
            // 8. Reviews & ReviewComments
            // ==========================================
            if (!db.Reviews.Any())
            {
                var reviewTextsPositive = new[]
                {
                    "Game đỉnh cao thực sự! Cốt truyện siêu cuốn, đồ họa đỉnh, nhạc nền nghe mê mẩn.",
                    "Trải nghiệm tuyệt vời, chơi cùng bạn bè siêu vui và mượt mà.",
                    "Xứng đáng từng xu bỏ ra! Hiệu năng tối ưu rất tốt, bắn súng và combo cực sướng tay.",
                    "10/10 không có điểm nào để chê! Đội ngũ phát triển chăm chút rất chi tiết.",
                    "Phần mềm/Game hoạt động hoàn hảo, giao diện mượt mà và dễ sử dụng."
                };

                var reviewTextsConstructive = new[]
                {
                    "Game hay nhưng cần tối ưu thêm chút xíu cho các dòng card đồ họa cũ.",
                    "Cốt truyện ổn, mong nhà phát hành ra thêm các bản cập nhật mở rộng DLC mới."
                };

                foreach (var prod in allProducts)
                {
                    for (int uIdx = 0; uIdx < Math.Min(3, customerUsers.Count); uIdx++)
                    {
                        var cust = customerUsers[uIdx];
                        bool isPositive = (uIdx % 2 == 0);
                        var content = isPositive
                            ? reviewTextsPositive[(prod.Id + uIdx) % reviewTextsPositive.Length]
                            : reviewTextsConstructive[(prod.Id + uIdx) % reviewTextsConstructive.Length];

                        var review = new Review
                        {
                            ProductId = prod.Id,
                            UserId = cust.Id,
                            IsRecommended = isPositive,
                            Content = content,
                            CreatedAt = DateTime.UtcNow.AddDays(-random.Next(2, 40)),
                            IsHidden = false
                        };
                        db.Reviews.Add(review);
                    }
                }
                db.SaveChanges();

                // Review Comments
                var allReviews = db.Reviews.Take(15).ToList();
                foreach (var rev in allReviews)
                {
                    db.ReviewComments.Add(new ReviewComment
                    {
                        ReviewId = rev.Id,
                        UserId = customerUsers[1].Id,
                        Content = "Đồng quan điểm với bạn! Mình vừa phá đảo xong, cảm xúc thật tuyệt vời.",
                        CreatedAt = rev.CreatedAt.AddHours(4),
                        IsHidden = false
                    });
                    db.ReviewComments.Add(new ReviewComment
                    {
                        ReviewId = rev.Id,
                        UserId = adminUser.Id,
                        Content = "Cảm ơn bạn đã tin tưởng ủng hộ NexusKeys! Chúc bạn có những giây phút giải trí tuyệt vời.",
                        CreatedAt = rev.CreatedAt.AddHours(8),
                        IsHidden = false
                    });
                }
                db.SaveChanges();
            }

            // ==========================================
            // 9. Orders, OrderItems, Licenses, Transactions, Sold ProductKeys
            // ==========================================
            if (!db.Orders.Any())
            {
                var paidProducts = allProducts.Where(p => p.Pricing == "Paid").ToList();
                int orderIndex = 1;

                foreach (var cust in customerUsers)
                {
                    // Create 2-3 completed orders per customer
                    for (int o = 0; o < 2; o++)
                    {
                        var orderProducts = paidProducts.Skip((orderIndex * 2) % (paidProducts.Count - 3)).Take(2).ToList();
                        if (!orderProducts.Any()) orderProducts = paidProducts.Take(2).ToList();

                        decimal total = orderProducts.Sum(p => p.BasePrice);

                        var order = new Order
                        {
                            UserId = cust.Id,
                            TotalAmount = total,
                            Status = "Completed",
                            OrderDate = DateTime.UtcNow.AddDays(-random.Next(3, 45)),
                            CouponCode = (orderIndex % 2 == 0) ? "WELCOME10" : null
                        };
                        db.Orders.Add(order);
                        db.SaveChanges();

                        foreach (var prod in orderProducts)
                        {
                            db.OrderItems.Add(new OrderItem
                            {
                                OrderId = order.Id,
                                ProductId = prod.Id,
                                PriceAtPurchase = prod.BasePrice
                            });

                            // Add or assign sold key
                            var prefix = new string(prod.Name.Where(char.IsLetterOrDigit).Take(4).ToArray()).ToUpper();
                            var key = new ProductKey
                            {
                                ProductId = prod.Id,
                                KeyCode = $"{prefix}-{random.Next(1000, 9999)}-{random.Next(1000, 9999)}-{random.Next(1000, 9999)}",
                                IsSold = true,
                                OrderId = order.Id
                            };
                            db.ProductKeys.Add(key);

                            // Add License for customer
                            db.Licenses.Add(new License
                            {
                                UserId = cust.Id,
                                ProductId = prod.Id,
                                Type = "Perpetual",
                                ValidFrom = order.OrderDate,
                                ValidUntil = null,
                                HardwareId = $"HWID-{Guid.NewGuid():N}".Substring(0, 16).ToUpper()
                            });
                        }

                        // Add Transaction for this order
                        db.Transactions.Add(new Transaction
                        {
                            UserId = cust.Id,
                            OrderId = order.Id,
                            Amount = total,
                            TransactionType = "Purchase",
                            PaymentGateway = "Wallet",
                            GatewayTransactionId = $"TXN-{order.Id}-{Guid.NewGuid():N}".Substring(0, 18),
                            Status = "Completed",
                            CreatedAt = order.OrderDate
                        });

                        orderIndex++;
                    }

                    // 1 Deposit transaction per customer
                    db.Transactions.Add(new Transaction
                    {
                        UserId = cust.Id,
                        OrderId = null,
                        Amount = 2000000m,
                        TransactionType = "Deposit",
                        PaymentGateway = (cust.Id % 2 == 0) ? "VNPay" : "MoMo",
                        GatewayTransactionId = $"DEP-{cust.Id}-{Guid.NewGuid():N}".Substring(0, 18),
                        Status = "Completed",
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(5, 60))
                    });

                    // 1 Pending Order
                    var pendingProd = paidProducts[(cust.Id + 3) % paidProducts.Count];
                    var pendingOrder = new Order
                    {
                        UserId = cust.Id,
                        TotalAmount = pendingProd.BasePrice,
                        Status = "Pending",
                        OrderDate = DateTime.UtcNow.AddHours(-random.Next(1, 24)),
                        CouponCode = null
                    };
                    db.Orders.Add(pendingOrder);
                    db.SaveChanges();

                    db.OrderItems.Add(new OrderItem
                    {
                        OrderId = pendingOrder.Id,
                        ProductId = pendingProd.Id,
                        PriceAtPurchase = pendingProd.BasePrice
                    });
                }
                db.SaveChanges();
            }

            // ==========================================
            // 10. Wishlists & CartItems
            // ==========================================
            if (!db.Wishlists.Any())
            {
                foreach (var cust in customerUsers)
                {
                    var wishProducts = allProducts.Skip((cust.Id * 3) % (allProducts.Count - 4)).Take(3).ToList();
                    foreach (var p in wishProducts)
                    {
                        if (!db.Wishlists.Any(w => w.UserId == cust.Id && w.ProductId == p.Id))
                        {
                            db.Wishlists.Add(new Wishlist
                            {
                                UserId = cust.Id,
                                ProductId = p.Id,
                                AddedDate = DateTime.UtcNow.AddDays(-random.Next(1, 15))
                            });
                        }
                    }
                }
                db.SaveChanges();
            }

            if (!db.CartItems.Any())
            {
                foreach (var cust in customerUsers.Take(3))
                {
                    var cartProducts = allProducts.Skip((cust.Id * 2 + 1) % (allProducts.Count - 2)).Take(2).ToList();
                    foreach (var p in cartProducts)
                    {
                        if (!db.CartItems.Any(c => c.UserId == cust.Id && c.ProductId == p.Id))
                        {
                            db.CartItems.Add(new CartItem
                            {
                                UserId = cust.Id,
                                ProductId = p.Id,
                                Quantity = 1,
                                AddedDate = DateTime.UtcNow.AddHours(-random.Next(2, 48))
                            });
                        }
                    }
                }
                db.SaveChanges();
            }

            // ==========================================
            // 11. Notifications
            // ==========================================
            if (!db.Notifications.Any())
            {
                foreach (var cust in customerUsers)
                {
                    db.Notifications.AddRange(
                        new Notification
                        {
                            UserId = cust.Id,
                            Title = "Chào mừng bạn đến với NexusKeys!",
                            Content = "Chúc mừng bạn đã gia nhập cộng đồng game thủ bản quyền lớn nhất. Tặng bạn voucher WELCOME10 giảm ngay 10% đơn hàng đầu tiên.",
                            IsRead = true,
                            Type = "System"
                        },
                        new Notification
                        {
                            UserId = cust.Id,
                            Title = "Đơn hàng đã hoàn tất thành công",
                            Content = "Key kích hoạt bản quyền của bạn đã sẵn sàng trong mục Thư viện / Lịch sử đơn hàng.",
                            IsRead = false,
                            Type = "Order"
                        },
                        new Notification
                        {
                            UserId = cust.Id,
                            Title = "Sự kiện Summer Mega Sale 2026 đang diễn ra!",
                            Content = "Hàng trăm tựa game bom tấn đang giảm giá lên đến 50%. Khám phá ngay tại NexusKeys!",
                            IsRead = false,
                            Type = "Promotion"
                        },
                        new Notification
                        {
                            UserId = cust.Id,
                            Title = "Biến động số dư ví",
                            Content = "Bạn vừa nạp tiền vào ví thành công. Hãy kiểm tra số dư và trải nghiệm dịch vụ nhé.",
                            IsRead = true,
                            Type = "Security"
                        }
                    );
                }
                db.SaveChanges();
            }

            // ==========================================
            // 12. SupportTickets
            // ==========================================
            if (!db.SupportTickets.Any())
            {
                var tickets = new List<SupportTicket>
                {
                    new SupportTicket
                    {
                        UserId = customerUsers[0].Id,
                        Topic = "Hỗ trợ kích hoạt key bản quyền game Steam",
                        Email = customerUsers[0].Email,
                        OrderId = "#1001",
                        Content = "Chào admin, mình vừa mua key tựa game Cyberpunk 2077 nhưng khi nhập vào Steam thì báo đang chờ đồng bộ vùng. Nhờ hỗ trợ ạ!",
                        Status = "Resolved",
                        AdminReply = "Chào bạn, hệ thống đã cấp lại mã key phiên bản Global mới cho bạn. Vui lòng kiểm tra lại trong hòm thư hoặc trang chi tiết đơn hàng nhé!",
                        RepliedAt = DateTime.UtcNow.AddDays(-2),
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        UpdatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new SupportTicket
                    {
                        UserId = customerUsers[1].Id,
                        Topic = "Thắc mắc nạp ví qua cổng thanh toán VNPay",
                        Email = customerUsers[1].Email,
                        OrderId = null,
                        Content = "Mình vừa chuyển khoản qua VNPay QR nhưng chưa thấy số dư tài khoản cập nhật ngay. Nhờ admin kiểm tra giao dịch giúp mình.",
                        Status = "Resolved",
                        AdminReply = "Giao dịch của bạn đã được kiểm tra và cộng tiền vào ví thành công kèm theo 5% bonus nạp đầu nhé!",
                        RepliedAt = DateTime.UtcNow.AddDays(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1).AddHours(-2),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new SupportTicket
                    {
                        UserId = customerUsers[2].Id,
                        Topic = "Yêu cầu tư vấn cấu hình chơi Black Myth: Wukong",
                        Email = customerUsers[2].Email,
                        OrderId = null,
                        Content = "Mình đang dùng GTX 1660 Super và i5 10400F thì có thể chơi mượt tựa game Black Myth Wukong ở mức đồ họa Medium không ạ?",
                        Status = "Pending",
                        AdminReply = null,
                        RepliedAt = null,
                        CreatedAt = DateTime.UtcNow.AddHours(-5),
                        UpdatedAt = null
                    },
                    new SupportTicket
                    {
                        UserId = customerUsers[3].Id,
                        Topic = "Đổi thông tin tài khoản và xác minh bảo mật 2FA",
                        Email = customerUsers[3].Email,
                        OrderId = null,
                        Content = "Em bị mất điện thoại chứa mã xác thực Google Authenticator, mong admin hỗ trợ reset lại 2FA cho tài khoản giúp em với ạ.",
                        Status = "Closed",
                        AdminReply = "Bộ phận hỗ trợ kỹ thuật đã xác minh danh tính và hỗ trợ bạn đặt lại xác thực bảo mật.",
                        RepliedAt = DateTime.UtcNow.AddDays(-5),
                        CreatedAt = DateTime.UtcNow.AddDays(-6),
                        UpdatedAt = DateTime.UtcNow.AddDays(-5)
                    }
                };

                db.SupportTickets.AddRange(tickets);
                db.SaveChanges();
            }

            // ==========================================
            // 13. PaymentGatewayConfigs
            // ==========================================
            if (!db.PaymentGatewayConfigs.Any())
            {
                db.PaymentGatewayConfigs.AddRange(
                    new PaymentGatewayConfig
                    {
                        GatewayName = "VNPay",
                        ConfigKey = "TmnCode",
                        ConfigValue = "NEXUSVNP",
                        UpdatedAt = DateTime.UtcNow
                    },
                    new PaymentGatewayConfig
                    {
                        GatewayName = "VNPay",
                        ConfigKey = "HashSecret",
                        ConfigValue = "9J2QWERTYUIOPASDFGHJKLZXCVB12345",
                        UpdatedAt = DateTime.UtcNow
                    },
                    new PaymentGatewayConfig
                    {
                        GatewayName = "VNPay",
                        ConfigKey = "Url",
                        ConfigValue = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
                        UpdatedAt = DateTime.UtcNow
                    },
                    new PaymentGatewayConfig
                    {
                        GatewayName = "MoMo",
                        ConfigKey = "PartnerCode",
                        ConfigValue = "MOMO_NEXUS_STORE",
                        UpdatedAt = DateTime.UtcNow
                    },
                    new PaymentGatewayConfig
                    {
                        GatewayName = "MoMo",
                        ConfigKey = "AccessKey",
                        ConfigValue = "MOMOKEY_2026_PROD",
                        UpdatedAt = DateTime.UtcNow
                    },
                    new PaymentGatewayConfig
                    {
                        GatewayName = "MoMo",
                        ConfigKey = "SecretKey",
                        ConfigValue = "MOMOSECRET_99887766",
                        UpdatedAt = DateTime.UtcNow
                    }
                );
                db.SaveChanges();
            }

            // ==========================================
            // 14. NewsArticles
            // ==========================================
            if (!db.NewsArticles.Any())
            {
                db.NewsArticles.AddRange(
                    new NewsArticle
                    {
                        Title = "Mega Sale 85% — Cơ hội sở hữu game AAA giá cực sốc",
                        Summary = "Khám phá đợt giảm giá lớn nhất năm tại NexusKeys với hàng ngàn tựa game đình đám giảm sâu tới 85%.",
                        Content = "<p>Chương trình <strong>Mega Sale</strong> chính thức bắt đầu từ hôm nay! Hàng loạt bom tấn như <em>The Witcher 3</em>, <em>Cyberpunk 2077</em>, <em>Elden Ring</em> và <em>Helldivers 2</em> đều có mức giá siêu ưu đãi.</p><p>Hãy nhanh tay nạp ví Nexus để nhận thêm bonus 10% giá trị nạp tiền nhé!</p>",
                        CoverImageUrl = "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=800",
                        AuthorId = devPrimary.Id,
                        PublishedAt = DateTime.UtcNow.AddDays(-2),
                        IsPublished = true
                    },
                    new NewsArticle
                    {
                        Title = "Bản Cập Nhật Mùa Hè: Đại chiến thế giới ngầm",
                        Summary = "Hàng loạt tính năng mới và giao diện tối ưu đã được nâng cấp trong hệ thống NexusKeys.",
                        Content = "<p>Đội ngũ phát triển NexusKeys vừa phát hành bản cập nhật Mùa Hè rực rỡ với giao diện tối ưu mới, tính năng nạp ví tự động và hỗ trợ tư vấn bằng AI Assistant thông minh 24/7.</p>",
                        CoverImageUrl = "https://images.unsplash.com/photo-1511512578047-dfb367046420?w=800",
                        AuthorId = devPrimary.Id,
                        PublishedAt = DateTime.UtcNow.AddDays(-5),
                        IsPublished = true
                    },
                    new NewsArticle
                    {
                        Title = "Giải Đấu Thể Thao Điện Tử Nexus Mùa 1",
                        Summary = "Đăng ký tham gia ngay giải đấu với tổng giá trị giải thưởng lên đến 500.000.000 VNĐ.",
                        Content = "<p>Nhằm tri ân cộng đồng game thủ Việt Nam, NexusKeys trân trọng thông báo giải đấu Esports mùa 1 tranh tài các tựa game đình đám. Tổng giải thưởng bao gồm tiền mặt và voucher mua game độc quyền.</p>",
                        CoverImageUrl = "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=800",
                        AuthorId = devPrimary.Id,
                        PublishedAt = DateTime.UtcNow.AddDays(-10),
                        IsPublished = true
                    },
                    new NewsArticle
                    {
                        Title = "Top 10 Game Nhập Vai Thế Giới Mở Đáng Chơi Nhất Năm",
                        Summary = "Cùng điểm qua những siêu phẩm nhập vai thế giới mở đỉnh cao không thể bỏ lỡ.",
                        Content = "<p>Năm 2026 đánh dấu sự bùng nổ của các tựa game RPG đồ họa cực đỉnh. Từ thế giới huyền bí Lands Between trong <em>Elden Ring</em> cho đến thành phố tương lai rực rỡ của <em>Cyberpunk 2077</em>, mỗi tựa game đều mang lại hàng trăm giờ chơi hấp dẫn.</p>",
                        CoverImageUrl = "https://images.unsplash.com/photo-1538481199705-c710c4e965fc?w=800",
                        AuthorId = devPrimary.Id,
                        PublishedAt = DateTime.UtcNow.AddDays(-15),
                        IsPublished = true
                    }
                );
                db.SaveChanges();
            }

            // ==========================================
            // 15. AuditLogs
            // ==========================================
            if (!db.AuditLogs.Any())
            {
                var auditEntries = new List<AuditLog>
                {
                    new AuditLog
                    {
                        AdminId = adminUser.Id,
                        AdminUsername = adminUser.Username,
                        Action = "Create",
                        EntityType = "Promotion",
                        EntityId = "1",
                        Details = "Khởi tạo chương trình Summer Mega Sale 2026 giảm 30%",
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new AuditLog
                    {
                        AdminId = adminUser.Id,
                        AdminUsername = adminUser.Username,
                        Action = "Update",
                        EntityType = "PaymentGatewayConfig",
                        EntityId = "VNPay",
                        Details = "Cập nhật cấu hình TmnCode và HashSecret cho cổng VNPay",
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    },
                    new AuditLog
                    {
                        AdminId = adminUser.Id,
                        AdminUsername = adminUser.Username,
                        Action = "Create",
                        EntityType = "Coupon",
                        EntityId = "NEXUS2026",
                        Details = "Tạo mã coupon giảm giá 15% tối đa 200 lượt",
                        CreatedAt = DateTime.UtcNow.AddDays(-7)
                    },
                    new AuditLog
                    {
                        AdminId = adminUser.Id,
                        AdminUsername = adminUser.Username,
                        Action = "Reply",
                        EntityType = "SupportTicket",
                        EntityId = "1",
                        Details = "Phản hồi và xử lý hoàn tất yêu cầu hỗ trợ key game Steam #1001",
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new AuditLog
                    {
                        AdminId = adminUser.Id,
                        AdminUsername = adminUser.Username,
                        Action = "Update",
                        EntityType = "User",
                        EntityId = customerUsers[0].Id.ToString(),
                        Details = "Cộng tiền khuyến mãi ví người dùng thân thiết",
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    }
                };

                db.AuditLogs.AddRange(auditEntries);
                db.SaveChanges();
            }

            // ==========================================
            // 16. RefreshTokens & PasswordResetTokens
            // ==========================================
            if (!db.RefreshTokens.Any())
            {
                foreach (var u in customerUsers.Take(3))
                {
                    db.RefreshTokens.Add(new RefreshToken
                    {
                        UserId = u.Id,
                        Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        Expires = DateTime.UtcNow.AddDays(14)
                    });
                }
                db.SaveChanges();
            }

            if (!db.PasswordResetTokens.Any())
            {
                db.PasswordResetTokens.Add(new PasswordResetToken
                {
                    UserId = customerUsers[0].Id,
                    Token = Guid.NewGuid().ToString("N"),
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    ExpiresAt = DateTime.UtcNow.AddDays(-9),
                    IsUsed = true
                });
                db.SaveChanges();
            }
        }

        public static void EnsureSeedAiAssistantConfig(GameStoreDbContext db)
        {
            try
            {
                if (db.Database.IsRelational())
                {
                    db.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS ""AiAssistantConfigs"" (
                            ""Id"" SERIAL PRIMARY KEY,
                            ""ConfigKey"" VARCHAR(100) NOT NULL,
                            ""ConfigValue"" TEXT NOT NULL,
                            ""UpdatedAt"" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        );
                    ");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB INIT] AiAssistantConfigs table creation note: {ex.Message}");
            }
        }
    }
}
