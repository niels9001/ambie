﻿using System;
using Microsoft.Toolkit.Uwp.Notifications;

namespace AmbientSounds.Services.Uwp
{
    public class ToastService : IToastService
    {
        private const string BellUriString = "ms-appx:///Assets/SoundEffects/bell.wav";
        private readonly Uri _bellUri;
        private readonly Lazy<IToastButton> _dismissButton;

        public ToastService()
        {
            _bellUri = new Uri(BellUriString);
            _dismissButton = new Lazy<IToastButton>(() => new ToastButtonDismiss());
        }

        public void ClearScheduledToasts()
        {
            ToastNotifierCompat notifier = ToastNotificationManagerCompat.CreateToastNotifier();
            var scheduled = notifier.GetScheduledToastNotifications();

            if (scheduled != null)
            {
                foreach (var toRemove in scheduled)
                {
                    notifier.RemoveFromSchedule(toRemove);
                }
            }
        }

        public void ScheduleToast(
            DateTime scheduleDateTime,
            string title,
            string message,
            bool silent = false)
        {
            if (scheduleDateTime <= DateTime.Now)
            {
                return;
            }

            new ToastContentBuilder()
                .SetToastScenario(ToastScenario.Default)
                .AddAudio(_bellUri, silent: silent)
                .AddButton(_dismissButton.Value)
                .AddText(title)
                .AddText(message)
                .AddArgument("scheduledToast")
                .Schedule(scheduleDateTime, toast =>
                {
                    toast.ExpirationTime = new DateTimeOffset(scheduleDateTime).AddMinutes(1);
                });
        }

        public void SendToast(string title, string message)
        {
            new ToastContentBuilder()
                .AddButton(_dismissButton.Value)
                .AddText(title)
                .AddText(message)
                .AddArgument("singleToast")
                .Show(toast =>
                {
                    toast.ExpirationTime = DateTimeOffset.Now.AddMinutes(1);
                });
        }
    }
}
