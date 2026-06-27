namespace Frontend.Models.User
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public decimal WalletBalance { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class LibraryItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime AcquiredDate { get; set; }
        public string KeyCode { get; set; } = string.Empty;
    }
}
