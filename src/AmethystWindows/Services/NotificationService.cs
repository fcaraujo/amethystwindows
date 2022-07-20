using System;
using Microsoft.Toolkit.Uwp.Notifications;

namespace AmethystWindows.Services
{
    public interface INotificationService
    {
        void Show(string title, string message, int? expirationInSeconds = null, bool? silent = true);
    }

    public class NotificationService : INotificationService
    {
        private const int DefaultExpirationInSeconds = 5;

        public void Show(string title, string message, int? expirationInSeconds = null, bool? silent = true)
        {
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