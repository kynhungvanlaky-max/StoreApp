using System.Text.Json;

namespace Backend.Extensions
{
    public static class HttpExtensions
    {
        public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static Task<T?> ReadJsonAsync<T>(this HttpContent content)
        {
            return content.ReadFromJsonAsync<T>(DefaultOptions);
        }
    }
}
