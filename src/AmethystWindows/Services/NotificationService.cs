using System;
using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;

namespace AmethystWindows.Services
{
    public interface INotificationService
    {
        void Show(string title, string message, int? expirationInSeconds = null, bool? silent = true);
    }

    public class NotificationService : INotificationService
    {
        private const int DefaultExpirationInSeconds = 5;

        private readonly ILogger _logger;

        public NotificationService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Show(string title,
                         string message,
                         int? expirationInSeconds = null,
                         bool? silent = true)
        {
            _logger.Information("{Method} notification: {Title} - {Message}.",
                                nameof(Show),
                                title,
                                message);

            var audio = new ToastAudio
            {
                Silent = silent.GetValueOrDefault()
            };

            var expirationSeconds = expirationInSeconds ?? DefaultExpirationInSeconds;

            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .AddAudio(audio)
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTime.Now.AddSeconds(expirationSeconds);
                });
        }
    }
}