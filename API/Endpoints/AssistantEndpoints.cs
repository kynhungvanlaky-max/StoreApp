using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using GameStore.API.Data;
using GameStore.API.Entities;

namespace GameStore.API.Endpoints
{
    public static class AssistantEndpoints
    {
        public static void MapAssistantEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/assistant");

            group.MapPost("/chat", async (ChatRequest req, GameStoreDbContext db, ClaimsPrincipal userClaim, HttpContext ctx) =>
            {
                if (string.IsNullOrWhiteSpace(req.Message))
                    return Results.BadRequest("Message is required");

                // Try to identify user if logged in
                int? userId = null;
                string userName = "Khách";
                decimal walletBalance = 0;
                var ownedGames = new List<string>();

                var userIdStr = userClaim.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                                userClaim.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int uid))
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == uid);
                    if (user != null)
                    {
                        userId = uid;
                        userName = user.Username;
                        walletBalance = user.WalletBalance;

                        // Get owned games
                        ownedGames = await db.Set<License>()
                            .Include(l => l.Product)
                            .Where(l => l.UserId == uid)
                            .Select(l => l.Product.Name)
                            .ToListAsync();
                    }
                }

                // Get Hot / Trending Games
                var hotGames = await db.Products
                    .Where(p => p.Status == "Active")
                    .Take(5)
                    .Select(p => $"{p.Name} (Giá: {p.BasePrice}đ)")
                    .ToListAsync();

                // Build Context Prompt
                var systemPrompt = $@"Bạn là Nexus Assistant - Trợ lý ảo AI CHUYÊN NGHIỆP của cửa hàng game Nexus Store.
TỪ CHỐI TẤT CẢ CÁC CÂU HỎI KHÔNG LIÊN QUAN ĐẾN: Game, Ứng dụng, Mua bán, Tài khoản, hoặc Cửa hàng Nexus Store.
Nếu người dùng hỏi về chính trị, toán học, lập trình (ngoài game), hoặc bất cứ chủ đề nào ngoài lề, hãy trả lời: 'Xin lỗi, tôi chỉ là trợ lý của Nexus Store nên chỉ có thể hỗ trợ bạn các vấn đề về game, sản phẩm và dịch vụ của cửa hàng.'

BẠN PHẢI TRẢ LỜI NGẮN GỌN, LỊCH SỰ BẰNG TIẾNG VIỆT, KHÔNG DÀI DÒNG. Dùng cấu trúc Markdown để in đậm tên game.

THÔNG TIN NGƯỜI DÙNG HIỆN TẠI:
- Tên: {userName}
- Số dư ví: {walletBalance} VNĐ
- Các game đã sở hữu: {(ownedGames.Any() ? string.Join(", ", ownedGames) : "Chưa có")}

THÔNG TIN CỬA HÀNG (SẢN PHẨM HOT):
- {string.Join("\n- ", hotGames)}

NHIỆM VỤ CỦA BẠN:
1. Trả lời câu hỏi của người dùng CẦN BÁM SÁT THÔNG TIN VỀ GAME VÀ CỬA HÀNG.
2. Nếu người dùng hỏi số dư, hãy báo cho họ biết.
3. Nếu người dùng nhờ gợi ý game, hãy gợi ý từ danh sách SẢN PHẨM HOT và KHÔNG gợi ý game họ đã sở hữu.
4. LUÔN LUÔN TỪ CHỐI TRẢ LỜI CÁC CÂU HỎI NGOÀI LỀ. Trả lời dưới 100 chữ.";

                // Connect to Ollama Local
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var ollamaReq = new
                {
                    model = "qwen2.5:0.5b",
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

                try
                {
                    var response = await httpClient.PostAsJsonAsync("http://localhost:11434/api/chat", ollamaReq);
                    if (response.IsSuccessStatusCode)
                    {
                        var resultString = await response.Content.ReadAsStringAsync();
                        var jsonDoc = JsonDocument.Parse(resultString);
                        var reply = jsonDoc.RootElement.GetProperty("message").GetProperty("content").GetString();
                        
                        return Results.Ok(new { reply = reply });
                    }
                    else
                    {
                        // Fallback logic if Ollama fails
                        return Results.Ok(new { reply = "Xin lỗi, hệ thống AI đang bảo trì cục bộ. Tôi là Nexus Assistant (chế độ dự phòng). Bạn có thể xem các game hot trên trang chủ nhé!" });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ollama error: {ex.Message}");
                    // Fallback logic
                    return Results.Ok(new { reply = "Xin lỗi, hiện tại tôi không thể kết nối tới não bộ AI (Ollama). Vui lòng đảm bảo Ollama đang chạy." });
                }
            });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
