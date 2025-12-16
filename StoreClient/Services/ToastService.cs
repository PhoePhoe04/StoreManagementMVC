using System.Timers;

namespace StoreClient.Services
{
    public enum ToastLevel
    {
        Success,
        Error
    }

    public class ToastMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Message { get; set; } = string.Empty;
        public ToastLevel Level { get; set; }
        public DateTime Posted { get; set; } = DateTime.Now;
    }

    public class ToastService : IDisposable
    {
        public event Action? OnChange;
        public List<ToastMessage> Messages { get; private set; } = new List<ToastMessage>();
        private System.Timers.Timer _countdown;

        public void ShowToast(string message, ToastLevel level)
        {
            var toast = new ToastMessage { Message = message, Level = level };
            Messages.Add(toast);

            // Khởi động bộ đếm để tự động xóa sau 3 giây
            StartTimer();
            OnChange?.Invoke();
        }

        private void StartTimer()
        {
            if (_countdown == null)
            {
                _countdown = new System.Timers.Timer(3000); // 3 giây
                _countdown.Elapsed += RemoveToast;
                _countdown.AutoReset = false;
            }

            // Reset lại thời gian mỗi khi có tin mới 
            _countdown.Stop();
            _countdown.Start();
        }

        private void RemoveToast(object? sender, ElapsedEventArgs e)
        {
            // Xóa tin nhắn cũ nhất hoặc xóa hết (ở đây xóa cái đầu tiên)
            if (Messages.Any())
            {
                Messages.RemoveAt(0);
                OnChange?.Invoke();

                // Nếu còn tin nhắn thì chạy tiếp timer cho cái tiếp theo
                if (Messages.Any()) _countdown.Start();
            }
        }

        public void Dispose()
        {
            _countdown?.Dispose();
        }
    }
}
