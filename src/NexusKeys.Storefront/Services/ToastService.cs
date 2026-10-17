using System;
using System.Collections.Generic;
using System.Timers;

namespace Frontend.Services
{
    public enum ToastLevel
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class ToastInstance
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Message { get; set; } = string.Empty;
        public ToastLevel Level { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
    }

    public class ToastService : IDisposable
    {
        public event Action? OnChange;
        private List<ToastInstance> _toastList = new List<ToastInstance>();
        private System.Timers.Timer _timer;

        public ToastService()
        {
            _timer = new System.Timers.Timer(200); // Check every 200ms
            _timer.Elapsed += ClearOldToasts;
            _timer.AutoReset = true;
            _timer.Start();
        }

        public IReadOnlyList<ToastInstance> GetToasts()
        {
            return _toastList.AsReadOnly();
        }

        public void ShowInfo(string message) => ShowToast(message, ToastLevel.Info);
        public void ShowSuccess(string message) => ShowToast(message, ToastLevel.Success);
        public void ShowWarning(string message) => ShowToast(message, ToastLevel.Warning);
        public void ShowError(string message) => ShowToast(message, ToastLevel.Error);

        private void ShowToast(string message, ToastLevel level)
        {
            var toast = new ToastInstance
            {
                Message = message,
                Level = level,
                TimeStamp = DateTime.Now
            };

            lock (_toastList)
            {
                _toastList.Add(toast);
            }

            NotifyStateChanged();
        }

        public void RemoveToast(Guid id)
        {
            lock (_toastList)
            {
                _toastList.RemoveAll(x => x.Id == id);
            }
            NotifyStateChanged();
        }

        private void ClearOldToasts(object? source, ElapsedEventArgs e)
        {
            bool removed = false;
            lock (_toastList)
            {
                var now = DateTime.Now;
                int countBefore = _toastList.Count;
                _toastList.RemoveAll(x => (now - x.TimeStamp).TotalSeconds > 1.5); // Hide after 1.5 seconds
                removed = _toastList.Count < countBefore;
            }

            if (removed)
            {
                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
