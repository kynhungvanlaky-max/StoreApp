using Frontend.Models.Assistant;
using Frontend.Services;

namespace Frontend.ViewModels
{
    public class AssistantViewModel
    {
        private readonly AssistantService _service;
        
        public List<ChatMessage> Messages { get; private set; } = new();
        public bool IsTyping { get; private set; }
        public string InputText { get; set; } = "";
        
        public event Action? StateChanged;
        
        public AssistantViewModel(AssistantService service)
        {
            _service = service;
            // Welcome message
            Messages.Add(new ChatMessage 
            { 
                Role = "assistant", 
                Content = "Xin chào! Tôi là **Nexus Assistant** 🎮. Tôi có thể giúp bạn tìm game, kiểm tra đơn hàng hoặc tư vấn sản phẩm! Bạn cần tôi giúp gì nào?",
                Timestamp = DateTime.Now
            });
        }
        
        public async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText)) return;
            
            var userMessage = InputText;
            InputText = "";
            
            Messages.Add(new ChatMessage { Role = "user", Content = userMessage, Timestamp = DateTime.Now });
            IsTyping = true;
            StateChanged?.Invoke();
            
            var reply = await _service.SendMessageAsync(userMessage);
            
            Messages.Add(new ChatMessage { Role = "assistant", Content = reply, Timestamp = DateTime.Now });
            IsTyping = false;
            StateChanged?.Invoke();
        }
    }
}
