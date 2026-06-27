using Frontend.Models.User;
using Frontend.Services;

namespace Frontend.ViewModels
{
    public class ProfileViewModel
    {
        private readonly UserService _userService;

        public UserProfileDto? Profile { get; private set; }
        public List<LibraryItemDto> Library { get; private set; } = new();
        public bool IsLoading { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? SuccessMessage { get; private set; }

        public event Action? StateChanged;

        public ProfileViewModel(UserService userService)
        {
            _userService = userService;
        }

        public async Task LoadProfileAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            StateChanged?.Invoke();

            Profile = await _userService.GetProfileAsync();

            IsLoading = false;
            StateChanged?.Invoke();
        }

        public async Task LoadLibraryAsync()
        {
            IsLoading = true;
            StateChanged?.Invoke();

            var library = await _userService.GetLibraryAsync();
            if (library != null)
            {
                Library = library;
            }

            IsLoading = false;
            StateChanged?.Invoke();
        }

        public async Task UpdateProfileAsync(string fullName, string phone, string avatarUrl)
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            StateChanged?.Invoke();

            var request = new UpdateProfileRequest
            {
                FullName = fullName,
                PhoneNumber = phone,
                AvatarUrl = avatarUrl
            };

            var success = await _userService.UpdateProfileAsync(request);
            if (success)
            {
                SuccessMessage = "Cập nhật hồ sơ thành công!";
                await LoadProfileAsync();
            }
            else
            {
                ErrorMessage = "Cập nhật hồ sơ thất bại.";
            }

            IsLoading = false;
            StateChanged?.Invoke();
        }
    }
}
