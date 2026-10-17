using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Frontend.Services
{
    public static class GameMediaHelper
    {
        private static readonly Dictionary<string, int> TitleToSteamAppId = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Ghost of Tsushima", 2215430 },
            { "Starfield", 1716740 },
            { "EA SPORTS FC 24", 2195250 },
            { "EA SPORTS FC 25", 2669320 },
            { "Red Dead Redemption 2", 1174180 },
            { "Mortal Kombat 1", 1971870 },
            { "The Witcher 3: Wild Hunt", 292030 },
            { "The Witcher 3", 292030 },
            { "Resident Evil 4 Remake", 2050650 },
            { "Resident Evil 4", 2050650 },
            { "Call of Duty: Modern Warfare III", 1938090 },
            { "Black Myth: Wukong", 2358720 },
            { "Counter-Strike 2", 730 },
            { "Horizon Zero Dawn", 1151640 },
            { "Helldivers 2", 553850 },
            { "Baldur's Gate 3", 1086940 },
            { "Elden Ring", 1245620 },
            { "Apex Legends", 1172470 },
            { "Grand Theft Auto V", 271590 },
            { "Cyberpunk 2077", 1091500 },
            { "God of War Ragnarök", 2322010 },
            { "God of War", 2322010 },
            { "Sekiro: Shadows Die Twice", 814380 },
            { "Overwatch 2", 2357570 },
            { "Hogwarts Legacy", 990080 },
            { "Silent Hill 2", 2124490 },
            { "Silent Hill 2 Remake", 2124490 },
            { "Dragon Age: The Veilguard", 1845910 },
            { "Control", 870780 },
            { "Risk of Rain 2", 632360 },
            { "Dave The Diver", 1868140 },
            { "Hades II", 1145350 },
            { "Destiny 2", 1085660 },
            { "Warframe", 230410 },
            { "PUBG: BATTLEGROUNDS", 578080 },
            { "PUBG", 578080 },
            { "Forza Horizon 5", 1551360 },
            { "Outward", 794260 },
            { "Medieval Dynasty", 1129580 },
            { "Planet Zoo", 703080 },
            { "Agent A", 801480 },
            { "Graveyard Keeper", 599140 }
        };

        private static int? FindAppId(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            foreach (var kvp in TitleToSteamAppId)
            {
                if (name.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Key.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        public static string GetCover(string? name, string? fallbackUrl)
        {
            var appId = FindAppId(name);
            if (appId.HasValue)
            {
                return $"https://cdn.akamai.steamstatic.com/steam/apps/{appId.Value}/library_600x900.jpg";
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                if (name.Contains("Steam Wallet", StringComparison.OrdinalIgnoreCase))
                    return "https://cdn.akamai.steamstatic.com/steam/apps/1091500/capsule_616x353.jpg";
                if (name.Contains("Epic Games Gift", StringComparison.OrdinalIgnoreCase))
                    return "https://cdn.akamai.steamstatic.com/steam/apps/2358720/capsule_616x353.jpg";
                if (name.Contains("Photoshop", StringComparison.OrdinalIgnoreCase) || name.Contains("Software", StringComparison.OrdinalIgnoreCase))
                    return "https://cdn.akamai.steamstatic.com/steam/apps/870780/library_600x900.jpg";
                if (name.Contains("3D Modeler", StringComparison.OrdinalIgnoreCase))
                    return "https://cdn.akamai.steamstatic.com/steam/apps/1245620/library_600x900.jpg";
                if (name.Contains("Video Editor", StringComparison.OrdinalIgnoreCase))
                    return "https://cdn.akamai.steamstatic.com/steam/apps/1174180/library_600x900.jpg";
                if (name.Contains("Music Studio", StringComparison.OrdinalIgnoreCase))
                    return "https://cdn.akamai.steamstatic.com/steam/apps/292030/library_600x900.jpg";
                if (name.Contains("DevStudio", StringComparison.OrdinalIgnoreCase) || name.Contains("Code IDE", StringComparison.OrdinalIgnoreCase))
                    return "https://cdn.akamai.steamstatic.com/steam/apps/1086940/library_600x900.jpg";
            }

            if (!string.IsNullOrWhiteSpace(fallbackUrl) && !fallbackUrl.Contains("unsplash.com"))
            {
                return fallbackUrl;
            }

            return "https://cdn.akamai.steamstatic.com/steam/apps/2358720/library_600x900.jpg";
        }

        public static string GetHeroBanner(string? name, string? fallbackUrl)
        {
            var appId = FindAppId(name);
            if (appId.HasValue)
            {
                return $"https://cdn.akamai.steamstatic.com/steam/apps/{appId.Value}/library_hero.jpg";
            }

            if (!string.IsNullOrWhiteSpace(fallbackUrl) && !fallbackUrl.Contains("unsplash.com"))
            {
                return fallbackUrl;
            }

            return "https://cdn.akamai.steamstatic.com/steam/apps/2358720/library_hero.jpg";
        }

        public static string GetCapsule(string? name, string? fallbackUrl)
        {
            var appId = FindAppId(name);
            if (appId.HasValue)
            {
                return $"https://cdn.akamai.steamstatic.com/steam/apps/{appId.Value}/capsule_616x353.jpg";
            }

            if (!string.IsNullOrWhiteSpace(fallbackUrl) && !fallbackUrl.Contains("unsplash.com"))
            {
                return fallbackUrl;
            }

            return "https://cdn.akamai.steamstatic.com/steam/apps/2358720/capsule_616x353.jpg";
        }

        public static List<MediaGalleryItem> GetMediaGallery(string? name, string? mainUrl)
        {
            var list = new List<MediaGalleryItem>();
            var appId = FindAppId(name);

            if (appId.HasValue)
            {
                int id = appId.Value;
                list.Add(new MediaGalleryItem($"https://cdn.akamai.steamstatic.com/steam/apps/{id}/library_hero.jpg", "Image", "Hero Cinematic"));
                list.Add(new MediaGalleryItem($"https://cdn.akamai.steamstatic.com/steam/apps/{id}/page_bg_raw.jpg", "Image", "Gameplay Panorama"));
                list.Add(new MediaGalleryItem($"https://cdn.akamai.steamstatic.com/steam/apps/{id}/capsule_616x353.jpg", "Image", "Capsule Art"));
                list.Add(new MediaGalleryItem($"https://cdn.akamai.steamstatic.com/steam/apps/{id}/header.jpg", "Image", "Action Banner"));
            }
            else
            {
                string cover = GetCover(name, mainUrl);
                string hero = GetHeroBanner(name, mainUrl);
                list.Add(new MediaGalleryItem(hero, "Image", "Hero Spotlight"));
                list.Add(new MediaGalleryItem(cover, "Image", "Poster Art"));
            }

            return list;
        }

        public static string RemoveDiacritics(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalizedString.Length);
            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace("đ", "d")
                .Replace("Đ", "D");
        }

        public static bool IsApp(string? name, string? type, IEnumerable<string>? categories = null)
        {
            var trimmedType = type?.Trim();
            // Explicit type takes absolute precedence
            if (string.Equals(trimmedType, "Game", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(trimmedType, "GiftCard", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmedType, "Gift Card", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(trimmedType, "Software", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmedType, "App", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Fallback for null/empty type: check categories
            if (categories != null)
            {
                foreach (var c in categories)
                {
                    if (string.IsNullOrWhiteSpace(c)) continue;
                    var cNorm = RemoveDiacritics(c);
                    if (c.Contains("Phần mềm", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("Software", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("Ứng dụng", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("Đồ họa", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("Thiết kế", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("3D", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("Video", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("Âm thanh", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("Lập trình", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("Tiện ích", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("Công cụ", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("Bản quyền", StringComparison.OrdinalIgnoreCase) ||
                        cNorm.Contains("phan mem", StringComparison.OrdinalIgnoreCase) ||
                        cNorm.Contains("ung dung", StringComparison.OrdinalIgnoreCase) ||
                        cNorm.Contains("do hoa", StringComparison.OrdinalIgnoreCase) ||
                        cNorm.Contains("thiet ke", StringComparison.OrdinalIgnoreCase) ||
                        cNorm.Contains("am thanh", StringComparison.OrdinalIgnoreCase) ||
                        cNorm.Contains("lap trinh", StringComparison.OrdinalIgnoreCase) ||
                        cNorm.Contains("tien ich", StringComparison.OrdinalIgnoreCase) ||
                        cNorm.Contains("cong cu", StringComparison.OrdinalIgnoreCase) ||
                        cNorm.Contains("ban quyen", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // Fallback for null/empty type: check name heuristics
            if (!string.IsNullOrWhiteSpace(name))
            {
                var nNorm = RemoveDiacritics(name);
                if (name.Contains("Photoshop", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("3D Modeler", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Video Editor", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Music Studio", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("DevStudio", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Visual Studio", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Software", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Office", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Windows 1", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Antivirus", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Phần mềm", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Ứng dụng", StringComparison.OrdinalIgnoreCase) ||
                    nNorm.Contains("phan mem", StringComparison.OrdinalIgnoreCase) ||
                    nNorm.Contains("ung dung", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsGiftCard(string? name, string? type, IEnumerable<string>? categories = null)
        {
            var trimmedType = type?.Trim();
            // Explicit type takes absolute precedence
            if (string.Equals(trimmedType, "Game", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmedType, "Software", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmedType, "App", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(trimmedType, "GiftCard", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmedType, "Gift Card", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Fallback for null/empty type: check categories
            if (categories != null)
            {
                foreach (var c in categories)
                {
                    if (string.IsNullOrWhiteSpace(c)) continue;
                    var cNorm = RemoveDiacritics(c);
                    if (c.Contains("Thẻ quà tặng", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("Gift Card", StringComparison.OrdinalIgnoreCase) ||
                        c.Contains("Wallet", StringComparison.OrdinalIgnoreCase) ||
                        cNorm.Contains("the qua tang", StringComparison.OrdinalIgnoreCase) ||
                        cNorm.Contains("the nap", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            // Fallback for null/empty type: check name heuristics
            if (!string.IsNullOrWhiteSpace(name))
            {
                var nNorm = RemoveDiacritics(name);
                if (name.Contains("Gift Card", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Wallet", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Thẻ quà tặng", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Thẻ nạp", StringComparison.OrdinalIgnoreCase) ||
                    nNorm.Contains("gift card", StringComparison.OrdinalIgnoreCase) ||
                    nNorm.Contains("the qua tang", StringComparison.OrdinalIgnoreCase) ||
                    nNorm.Contains("the nap", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetProductType(string? name, string? type, IEnumerable<string>? categories = null)
        {
            if (IsGiftCard(name, type, categories)) return "GiftCard";
            if (IsApp(name, type, categories)) return "Software";
            return "Game";
        }

        public static string GetDetailedBadge(string? name, string? type, IEnumerable<string>? categories = null)
        {
            if (IsApp(name, type, categories))
            {
                // Categories have highest precedence
                if (categories != null)
                {
                    foreach (var c in categories)
                    {
                        if (string.IsNullOrWhiteSpace(c)) continue;
                        var cNorm = RemoveDiacritics(c);
                        if (c.Contains("Đồ họa", StringComparison.OrdinalIgnoreCase) ||
                            c.Contains("3D", StringComparison.OrdinalIgnoreCase) ||
                            c.Contains("Thiết kế", StringComparison.OrdinalIgnoreCase) ||
                            cNorm.Contains("do hoa", StringComparison.OrdinalIgnoreCase) ||
                            cNorm.Contains("thiet ke", StringComparison.OrdinalIgnoreCase))
                        {
                            return "Phần mềm đồ họa";
                        }

                        if (c.Contains("Video", StringComparison.OrdinalIgnoreCase) ||
                            c.Contains("Âm thanh", StringComparison.OrdinalIgnoreCase) ||
                            c.Contains("Nhạc", StringComparison.OrdinalIgnoreCase) ||
                            cNorm.Contains("am thanh", StringComparison.OrdinalIgnoreCase))
                        {
                            return "Biên tập đa phương tiện";
                        }

                        if (c.Contains("Lập trình", StringComparison.OrdinalIgnoreCase) ||
                            c.Contains("Tiện ích", StringComparison.OrdinalIgnoreCase) ||
                            cNorm.Contains("lap trinh", StringComparison.OrdinalIgnoreCase) ||
                            cNorm.Contains("tien ich", StringComparison.OrdinalIgnoreCase))
                        {
                            return "Ứng dụng tiện ích";
                        }

                        if (c.Contains("Công cụ", StringComparison.OrdinalIgnoreCase) ||
                            c.Contains("Bản quyền", StringComparison.OrdinalIgnoreCase) ||
                            cNorm.Contains("cong cu", StringComparison.OrdinalIgnoreCase) ||
                            cNorm.Contains("ban quyen", StringComparison.OrdinalIgnoreCase))
                        {
                            return "Công cụ bản quyền";
                        }
                    }
                }

                // Name heuristic fallback
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var nNorm = RemoveDiacritics(name);
                    if (name.Contains("Photoshop", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("3D Modeler", StringComparison.OrdinalIgnoreCase) ||
                        nNorm.Contains("do hoa", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Phần mềm đồ họa";
                    }
                    if (name.Contains("Video Editor", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Music Studio", StringComparison.OrdinalIgnoreCase) ||
                        nNorm.Contains("bien tap", StringComparison.OrdinalIgnoreCase) ||
                        nNorm.Contains("am thanh", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Biên tập đa phương tiện";
                    }
                    if (name.Contains("DevStudio", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Visual Studio", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Code", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("IDE", StringComparison.OrdinalIgnoreCase) ||
                        nNorm.Contains("lap trinh", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Ứng dụng tiện ích";
                    }
                }

                return "Ứng dụng tiện ích";
            }

            if (IsGiftCard(name, type, categories))
            {
                return "Thẻ quà tặng";
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                if (name.Contains("Edition", StringComparison.OrdinalIgnoreCase) || 
                    name.Contains("Deluxe", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Ultimate", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Remake", StringComparison.OrdinalIgnoreCase))
                    return "Phiên bản đặc biệt";

                if (name.Contains("DLC", StringComparison.OrdinalIgnoreCase) || 
                    name.Contains("Expansion", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Pass", StringComparison.OrdinalIgnoreCase))
                    return "Gói bổ sung";
            }

            return "Trò chơi cơ bản";
        }

        public static string GetDetailedBadgeColor(string? name, string? type, IEnumerable<string>? categories = null)
        {
            if (IsApp(name, type, categories)) return "text-purple-400";
            if (IsGiftCard(name, type, categories)) return "text-amber-400";
            if (!string.IsNullOrWhiteSpace(name) && 
                (name.Contains("Edition", StringComparison.OrdinalIgnoreCase) || 
                 name.Contains("Deluxe", StringComparison.OrdinalIgnoreCase) ||
                 name.Contains("Ultimate", StringComparison.OrdinalIgnoreCase) ||
                 name.Contains("Remake", StringComparison.OrdinalIgnoreCase)))
            {
                return "text-amber-300";
            }
            return "text-white/70";
        }

        public static string GetBadge(string? name, string? type)
        {
            return GetDetailedBadge(name, type, null);
        }

        public static string GetNewsCover(int id, string? title, string? fallbackUrl)
        {
            if (!string.IsNullOrWhiteSpace(fallbackUrl) && !fallbackUrl.Contains("unsplash.com") && fallbackUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return fallbackUrl;
            }

            var heroCovers = new[]
            {
                "https://cdn.akamai.steamstatic.com/steam/apps/1091500/library_hero.jpg", // Cyberpunk 2077
                "https://cdn.akamai.steamstatic.com/steam/apps/271590/library_hero.jpg",  // GTA V
                "https://cdn.akamai.steamstatic.com/steam/apps/730/library_hero.jpg",     // CS2
                "https://cdn.akamai.steamstatic.com/steam/apps/2358720/library_hero.jpg", // Wukong
                "https://cdn.akamai.steamstatic.com/steam/apps/1245620/library_hero.jpg", // Elden Ring
                "https://cdn.akamai.steamstatic.com/steam/apps/2215430/library_hero.jpg", // Ghost of Tsushima
                "https://cdn.akamai.steamstatic.com/steam/apps/2322010/library_hero.jpg", // God of War
                "https://cdn.akamai.steamstatic.com/steam/apps/292030/library_hero.jpg",  // Witcher 3
                "https://cdn.akamai.steamstatic.com/steam/apps/1174180/library_hero.jpg", // RDR2
                "https://cdn.akamai.steamstatic.com/steam/apps/1086940/library_hero.jpg", // Baldur's Gate 3
                "https://cdn.akamai.steamstatic.com/steam/apps/870780/library_hero.jpg",  // Control
                "https://cdn.akamai.steamstatic.com/steam/apps/1145350/library_hero.jpg"  // Hades II
            };

            int idx = Math.Abs(id.GetHashCode()) % heroCovers.Length;
            return heroCovers[idx];
        }
    }

    public record MediaGalleryItem(string Url, string Type, string Title);
}
