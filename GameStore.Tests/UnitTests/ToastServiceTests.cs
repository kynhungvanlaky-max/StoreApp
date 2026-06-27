using Frontend.Services;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GameStore.Tests.UnitTests
{
    public class ToastServiceTests
    {
        [Fact]
        public void ShowSuccess_ShouldAddToastToQueue()
        {
            // Arrange
            var toastService = new ToastService();
            
            // Act
            toastService.ShowSuccess("Success Message");
            
            // Assert
            var toasts = toastService.GetToasts();
            Assert.Single(toasts);
            Assert.Equal("Success Message", toasts[0].Message);
            Assert.Equal(ToastLevel.Success, toasts[0].Level);
        }

        [Fact]
        public void ShowError_ShouldAddToastToQueue()
        {
            // Arrange
            var toastService = new ToastService();
            
            // Act
            toastService.ShowError("Error Message");
            
            // Assert
            var toasts = toastService.GetToasts();
            Assert.Single(toasts);
            Assert.Equal("Error Message", toasts[0].Message);
            Assert.Equal(ToastLevel.Error, toasts[0].Level);
        }

        [Fact]
        public void RemoveToast_ShouldRemoveSpecificToast()
        {
            // Arrange
            var toastService = new ToastService();
            toastService.ShowInfo("Message 1");
            toastService.ShowInfo("Message 2");
            var toasts = toastService.GetToasts();
            var idToRemove = toasts[0].Id;

            // Act
            toastService.RemoveToast(idToRemove);

            // Assert
            var remainingToasts = toastService.GetToasts();
            Assert.Single(remainingToasts);
            Assert.Equal("Message 2", remainingToasts[0].Message);
        }

        [Fact]
        public async Task OldToasts_ShouldBeRemovedAutomatically()
        {
            // Arrange
            var toastService = new ToastService();
            
            // Act
            toastService.ShowInfo("Message 1");
            var toasts = toastService.GetToasts();
            
            // Manually set timestamp to old value for testing (older than 1.5s)
            toasts[0].TimeStamp = System.DateTime.Now.AddSeconds(-2);
            
            // Wait for timer to tick (timer ticks every 200ms)
            await Task.Delay(300);

            // Assert
            Assert.Empty(toastService.GetToasts());
        }
    }
}
