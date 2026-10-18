using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using System.Diagnostics;
using GameStore.API.Data;
using GameStore.API.Entities;
using GameStore.API.Services;

namespace GameStore.API.Endpoints
{
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class AiAssistantConfigDto
    {
        public string ConfigKey { get; set; } = string.Empty;
        public string ConfigValue { get; set; } = string.Empty;
    }

    public class AiAssistantTestRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? Provider { get; set; }
        public string? GeminiApiKey { get; set; }
        public string? GeminiModel { get; set; }
        public double? GeminiTemperature { get; set; }
        public int? GeminiMaxTokens { get; set; }
        public string? OllamaBaseUrl { get; set; }
        public string? OllamaModel { get; set; }
        public string? CustomSystemPrompt { get; set; }
    }

    public static class AssistantEndpoints
    {
        public static void MapAssistantEndpoints(this IEndpointRouteBuilder app, string apiPrefix = "/api")
        {
            var group = app.MapGroup($"{apiPrefix}/assistant");

            // ==========================================
            // 1. PUBLIC CHAT ENDPOINT
            // ==========================================
            group.MapPost("/chat", async (
                ChatRequest req,
                GameStoreDbContext db,
                ClaimsPrincipal userClaim,
                GeminiService geminiService,
                IConfiguration config,
                IHttpClientFactory httpClientFactory,
                ILoggerFactory loggerFactory) =>
            {
                if (string.IsNullOrWhiteSpace(req.Message) || req.Message.Length > 500)
                    return Results.BadRequest(new { message = "Tin nhắn không được để trống và không vượt quá 500 ký tự." });

                var logger = loggerFactory.CreateLogger("AssistantEndpoint");

                // Load dynamic configurations from DB
                var dbConfigs = await db.AiAssistantConfigs
                    .ToDictionaryAsync(c => c.ConfigKey, c => c.ConfigValue, StringComparer.OrdinalIgnoreCase);

                string GetConfig(string key, string defaultValue)
                {
                    if (dbConfigs.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                        return val;
                    return config[key] ?? defaultValue;
                }

                // Check if AI Assistant is enabled
                var isEnabled = GetConfig("AiAssistant:IsEnabled", "true");
                if (isEnabled.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Ok(new
                    {
                        reply = "Trợ lý ảo Nexus Assistant hiện đang tạm ngưng phục vụ để bảo trì. Quý khách vui lòng liên hệ bộ phận hỗ trợ nếu cần giúp đỡ!",
                        provider = "Disabled"
                    });
                }

                // Try to identify user if logged in
                string userName = "Khách";
                decimal walletBalance = 0;
                var ownedGames = new List<string>();

                var userId = userClaim.GetUserId();
                if (userId != null)
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user != null)
                    {
                        userName = user.Username;
                        walletBalance = user.WalletBalance;

                        // Get owned games
                        ownedGames = await db.Set<License>()
                            .Include(l => l.Product)
                            .Where(l => l.UserId == userId)
                            .Select(l => l.Product.Name)
                            .ToListAsync();
                    }
                }

                // Get Hot / Trending Games
                var hotGames = await db.Products
                    .Where(p => p.Status == "Active")
                    .Take(5)
                    .Select(p => $"{p.Name} (Giá: {p.BasePrice:N0}đ)")
                    .ToListAsync();

                // Custom system prompt from config or default
                var customPromptTemplate = GetConfig("AiAssistant:SystemPrompt", string.Empty);
                string systemPrompt;

                if (!string.IsNullOrWhiteSpace(customPromptTemplate))
                {
                    systemPrompt = customPromptTemplate
                        .Replace("{UserName}", userName)
                        .Replace("{WalletBalance}", walletBalance.ToString("N0"))
                        .Replace("{OwnedGames}", ownedGames.Any() ? string.Join(", ", ownedGames) : "Chưa có")
                        .Replace("{HotGames}", string.Join("\n- ", hotGames));
                }
                else
                {
                    systemPrompt = $@"Bạn là Nexus Assistant - Trợ lý ảo AI CHUYÊN NGHIỆP của cửa hàng game NexusKeys.
TỪ CHỐI TẤT CẢ CÁC CÂU HỎI KHÔNG LIÊN QUAN ĐẾN: Game, Ứng dụng, Mua bán, Tài khoản, hoặc Cửa hàng NexusKeys.
Nếu người dùng hỏi về chính trị, toán học, lập trình (ngoài game), hoặc bất cứ chủ đề nào ngoài lề, hãy trả lời: 'Xin lỗi, tôi chỉ là trợ lý của NexusKeys nên chỉ có thể hỗ trợ bạn các vấn đề về game, sản phẩm và dịch vụ của cửa hàng.'

BẠN PHẢI TRẢ LỜI NGẮN GỌN, LỊCH SỰ BẰNG TIẾNG VIỆT, KHÔNG DÀI DÒNG. Dùng cấu trúc Markdown để in đậm tên game.

THÔNG TIN NGƯỜI DÙNG HIỆN TẠI:
- Tên: {userName}
- Số dư ví: {walletBalance:N0} VNĐ
- Các game đã sở hữu: {(ownedGames.Any() ? string.Join(", ", ownedGames) : "Chưa có")}

THÔNG TIN CỬA HÀNG (SẢN PHẨM HOT):
- {string.Join("\n- ", hotGames)}

NHIỆM VỤ CỦA BẠN:
1. Trả lời câu hỏi của người dùng CẦN BÁM SÁT THÔNG TIN VỀ GAME VÀ CỬA HÀNG.
2. Nếu người dùng hỏi số dư ví, hãy báo cho họ biết chính xác.
3. Nếu người dùng nhờ gợi ý game, hãy gợi ý từ danh sách SẢN PHẨM HOT và KHÔNG gợi ý game họ đã sở hữu.
4. LUÔN LUÔN TỪ CHỐI TRẢ LỜI CÁC CÂU HỎI NGOÀI LỀ. Trả lời súc tích, dưới 120 chữ.";
                }

                var provider = GetConfig("AiAssistant:Provider", "Auto");

                // 1. Try Google Gemini if Provider is "Gemini" or "Auto"
                if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) ||
                    provider.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var geminiApiKey = GetConfig("AiAssistant:Gemini:ApiKey", string.Empty);
                        var geminiModel = GetConfig("AiAssistant:Gemini:Model", "gemini-3.6-flash");
                        double.TryParse(GetConfig("AiAssistant:Gemini:Temperature", "0.7"), out var temp);
                        int.TryParse(GetConfig("AiAssistant:Gemini:MaxTokens", "2048"), out var tokens);

                        var geminiReply = await geminiService.GenerateReplyAsync(
                            systemPrompt, 
                            req.Message, 
                            string.IsNullOrWhiteSpace(geminiApiKey) ? null : geminiApiKey, 
                            geminiModel, 
                            temp > 0 ? temp : 0.7, 
                            tokens > 0 ? tokens : 2048);

                        if (!string.IsNullOrWhiteSpace(geminiReply))
                        {
                            return Results.Ok(new { reply = geminiReply.Trim(), provider = "Gemini" });
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to get reply from Google Gemini. Attempting fallback if allowed.");
                    }

                    // If strictly Gemini mode was selected and failed
                    if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
                    {
                        return Results.Ok(new { reply = "Xin lỗi, không thể kết nối tới Google Gemini AI. Vui lòng kiểm tra lại API Key hoặc kết nối mạng.", provider = "Gemini" });
                    }
                }

                // 2. Try Ollama Local if Provider is "Ollama" or fallback from "Auto"
                var ollamaBaseUrl = GetConfig("AiAssistant:Ollama:BaseUrl", "http://localhost:11434");
                var ollamaModel = GetConfig("AiAssistant:Ollama:Model", "qwen2.5:0.5b");

                try
                {
                    var httpClient = httpClientFactory.CreateClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(20);

                    var ollamaReq = new
                    {
                        model = ollamaModel,
                        messages = new[]
                        {
                            new { role = "system", content = systemPrompt },
                            new { role = "user", content = req.Message }
                        },
                        stream = false,
                        options = new
                        {
                            temperature = 0.5,
                            top_p = 0.9,
                            num_predict = 200
                        }
                    };

                    var response = await httpClient.PostAsJsonAsync($"{ollamaBaseUrl.TrimEnd('/')}/api/chat", ollamaReq);
                    if (response.IsSuccessStatusCode)
                    {
                        var resultString = await response.Content.ReadAsStringAsync();
                        using var jsonDoc = JsonDocument.Parse(resultString);
                        var reply = jsonDoc.RootElement.GetProperty("message").GetProperty("content").GetString();

                        return Results.Ok(new { reply = reply?.Trim() ?? string.Empty, provider = "Ollama" });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to connect to local Ollama.");
                }

                // 3. Ultimate Friendly Fallback
                return Results.Ok(new
                {
                    reply = "Xin chào! Hiện tại hệ thống AI đang nâng cấp hoặc chưa được cấu hình API Key. Bạn có thể duyệt các tựa game hot trên trang chủ NexusKeys hoặc liên hệ đội ngũ hỗ trợ bất cứ lúc nào nhé! 🎮",
                    provider = "Fallback"
                });
            }).RequireRateLimiting("chat");

            // ==========================================
            // 2. ADMIN CONFIGURATION ENDPOINTS
            // ==========================================
            var adminGroup = app.MapGroup($"{apiPrefix}/assistant")
                .RequireAuthorization(policy => policy.RequireRole("Administrator"));

            // GET Configurations
            adminGroup.MapGet("/configurations", async (GameStoreDbContext db, IConfiguration config) =>
            {
                var dbConfigs = await db.AiAssistantConfigs.ToListAsync();
                var dbDict = dbConfigs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue, StringComparer.OrdinalIgnoreCase);

                string GetMerged(string key, string fallback) =>
                    dbDict.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : (config[key] ?? fallback);

                var rawApiKey = GetMerged("AiAssistant:Gemini:ApiKey", "");
                var maskedApiKey = !string.IsNullOrEmpty(rawApiKey) && rawApiKey.Length > 4
                    ? new string('*', Math.Max(4, rawApiKey.Length - 4)) + rawApiKey[^4..]
                    : rawApiKey;

                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["AiAssistant:IsEnabled"] = GetMerged("AiAssistant:IsEnabled", "true"),
                    ["AiAssistant:Provider"] = GetMerged("AiAssistant:Provider", "Auto"),
                    ["AiAssistant:Gemini:ApiKey"] = maskedApiKey,
                    ["AiAssistant:Gemini:Model"] = GetMerged("AiAssistant:Gemini:Model", "gemini-3.6-flash"),
                    ["AiAssistant:Gemini:Temperature"] = GetMerged("AiAssistant:Gemini:Temperature", "0.7"),
                    ["AiAssistant:Gemini:MaxTokens"] = GetMerged("AiAssistant:Gemini:MaxTokens", "2048"),
                    ["AiAssistant:Ollama:BaseUrl"] = GetMerged("AiAssistant:Ollama:BaseUrl", "http://localhost:11434"),
                    ["AiAssistant:Ollama:Model"] = GetMerged("AiAssistant:Ollama:Model", "qwen2.5:0.5b"),
                    ["AiAssistant:SystemPrompt"] = GetMerged("AiAssistant:SystemPrompt", ""),
                    ["AiAssistant:IncludeUserInfo"] = GetMerged("AiAssistant:IncludeUserInfo", "true"),
                    ["AiAssistant:IncludeHotGames"] = GetMerged("AiAssistant:IncludeHotGames", "true")
                };

                return Results.Ok(result);
            });

            // PUT / Update Configurations
            adminGroup.MapPut("/configurations", async (
                List<AiAssistantConfigDto> req,
                GameStoreDbContext db,
                ClaimsPrincipal userClaim) =>
            {
                if (req == null || !req.Any())
                    return Results.BadRequest("Configuration list cannot be empty");

                var adminId = userClaim.GetUserId() ?? 0;
                var adminUser = await db.Users.FindAsync(adminId);
                var adminUsername = adminUser?.Username ?? "Admin";

                foreach (var dto in req)
                {
                    if (string.IsNullOrWhiteSpace(dto.ConfigKey)) continue;

                    if (dto.ConfigKey.Equals("AiAssistant:Ollama:BaseUrl", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(dto.ConfigValue))
                    {
                        if (IsForbiddenSsrfUrl(dto.ConfigValue, out var ssrfError))
                        {
                            return Results.BadRequest(new { message = ssrfError ?? "URL Ollama không hợp lệ hoặc thuộc dải mạng bị cấm." });
                        }
                    }

                    if (dto.ConfigKey.Equals("AiAssistant:Gemini:ApiKey", StringComparison.OrdinalIgnoreCase) && (dto.ConfigValue?.Contains("****") == true))
                    {
                        continue;
                    }

                    var entity = await db.AiAssistantConfigs
                        .FirstOrDefaultAsync(c => c.ConfigKey == dto.ConfigKey);

                    if (entity != null)
                    {
                        entity.ConfigValue = dto.ConfigValue ?? string.Empty;
                        entity.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        db.AiAssistantConfigs.Add(new AiAssistantConfig
                        {
                            ConfigKey = dto.ConfigKey,
                            ConfigValue = dto.ConfigValue ?? string.Empty,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }

                // Audit Log
                db.AuditLogs.Add(new AuditLog
                {
                    AdminId = adminId,
                    AdminUsername = adminUsername,
                    Action = "Update",
                    EntityType = "AiAssistantConfig",
                    EntityId = "Global",
                    Details = $"Cập nhật {req.Count} cấu hình Trợ lý AI.",
                    CreatedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            // POST Live Test Chat Sandbox
            adminGroup.MapPost("/test-chat", async (
                AiAssistantTestRequest req,
                GeminiService geminiService,
                IConfiguration config,
                IHttpClientFactory httpClientFactory) =>
            {
                if (string.IsNullOrWhiteSpace(req.Message))
                    return Results.BadRequest("Message is required for test.");

                var sw = Stopwatch.StartNew();
                var systemPrompt = !string.IsNullOrWhiteSpace(req.CustomSystemPrompt)
                    ? req.CustomSystemPrompt
                    : "Bạn là Nexus Assistant - Trợ lý ảo AI của cửa hàng NexusKeys. Hãy trả lời ngắn gọn, lịch sự.";

                var provider = req.Provider ?? "Auto";

                if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) ||
                    provider.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var reply = await geminiService.GenerateReplyAsync(
                            systemPrompt, 
                            req.Message, 
                            req.GeminiApiKey, 
                            req.GeminiModel ?? "gemini-3.6-flash", 
                            req.GeminiTemperature ?? 0.7, 
                            req.GeminiMaxTokens ?? 2048);

                        sw.Stop();
                        if (!string.IsNullOrWhiteSpace(reply))
                        {
                            return Results.Ok(new
                            {
                                success = true,
                                reply = reply.Trim(),
                                provider = "Gemini",
                                latencyMs = sw.ElapsedMilliseconds
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
                        {
                            return Results.Ok(new
                            {
                                success = false,
                                reply = $"Lỗi kết nối Gemini: {ex.Message}",
                                provider = "Gemini",
                                latencyMs = sw.ElapsedMilliseconds
                            });
                        }
                    }
                }

                if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ||
                    provider.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var baseUrl = !string.IsNullOrWhiteSpace(req.OllamaBaseUrl) ? req.OllamaBaseUrl : "http://localhost:11434";
                        if (IsForbiddenSsrfUrl(baseUrl, out var ssrfError))
                        {
                            return Results.BadRequest(new { success = false, message = ssrfError ?? "Địa chỉ đích bị cấm truy cập." });
                        }

                        var model = !string.IsNullOrWhiteSpace(req.OllamaModel) ? req.OllamaModel : "qwen2.5:0.5b";

                        var client = httpClientFactory.CreateClient();
                        client.Timeout = TimeSpan.FromSeconds(15);

                        var ollamaReq = new
                        {
                            model = model,
                            messages = new[]
                            {
                                new { role = "system", content = systemPrompt },
                                new { role = "user", content = req.Message }
                            },
                            stream = false
                        };

                        var response = await client.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/api/chat", ollamaReq);
                        sw.Stop();
                        if (response.IsSuccessStatusCode)
                        {
                            var resultStr = await response.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(resultStr);
                            var reply = doc.RootElement.GetProperty("message").GetProperty("content").GetString();

                            return Results.Ok(new
                            {
                                success = true,
                                reply = reply?.Trim() ?? string.Empty,
                                provider = "Ollama",
                                latencyMs = sw.ElapsedMilliseconds
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        return Results.Ok(new
                        {
                            success = false,
                            reply = $"Lỗi kết nối Ollama: {ex.Message}",
                            provider = "Ollama",
                            latencyMs = sw.ElapsedMilliseconds
                        });
                    }
                }

                sw.Stop();
                return Results.Ok(new
                {
                    success = false,
                    reply = "Không thể kết nối với AI provider nào. Vui lòng kiểm tra lại API Key hoặc địa chỉ server.",
                    provider = "None",
                    latencyMs = sw.ElapsedMilliseconds
                });
            });
        }

        private static bool IsForbiddenSsrfUrl(string url, out string? errorMessage)
        {
            errorMessage = null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUri) ||
                (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps))
            {
                errorMessage = "URL không hợp lệ. Chỉ chấp nhận giao thức http hoặc https.";
                return true;
            }

            var host = parsedUri.Host.ToLowerInvariant();

            // Cloud metadata endpoints
            if (host == "169.254.169.254" || host.StartsWith("169.254.") ||
                host == "metadata.google.internal" || host.EndsWith(".metadata.google.internal") ||
                host == "metadata.azure.internal" || host == "instance-data")
            {
                errorMessage = "Địa chỉ đích bị cấm truy cập (Cloud Metadata).";
                return true;
            }

            // IP address checks
            if (IPAddress.TryParse(host, out var ip))
            {
                if (ip.IsIPv4MappedToIPv6)
                {
                    ip = ip.MapToIPv4();
                }

                var bytes = ip.GetAddressBytes();
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    // 10.0.0.0/8
                    if (bytes[0] == 10)
                    {
                        errorMessage = "Địa chỉ IP mạng nội bộ (10.x.x.x) bị cấm truy cập.";
                        return true;
                    }
                    // 172.16.0.0/12 (172.16.0.0 - 172.31.255.255)
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    {
                        errorMessage = "Địa chỉ IP mạng nội bộ (172.16-31.x.x) bị cấm truy cập.";
                        return true;
                    }
                    // 192.168.0.0/16
                    if (bytes[0] == 192 && bytes[1] == 168)
                    {
                        errorMessage = "Địa chỉ IP mạng nội bộ (192.168.x.x) bị cấm truy cập.";
                        return true;
                    }
                    // 169.254.0.0/16 (Link-local)
                    if (bytes[0] == 169 && bytes[1] == 254)
                    {
                        errorMessage = "Địa chỉ IP link-local bị cấm truy cập.";
                        return true;
                    }
                }
                else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                {
                    // fe80::/10 (link-local), fc00::/7 (unique local)
                    if (ip.IsIPv6LinkLocal || (bytes[0] & 0xfe) == 0xfc)
                    {
                        errorMessage = "Địa chỉ IPv6 mạng nội bộ bị cấm truy cập.";
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
