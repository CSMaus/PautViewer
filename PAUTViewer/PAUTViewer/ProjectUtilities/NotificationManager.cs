using System;
using System.Windows;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace PAUTViewer.ProjectUtilities
{
    public class NotificationManager
    {
        private static Notifier _notifier;

        public static Notifier Notifier
        {
            get
            {
                if (_notifier == null)
                {
                    _notifier = new Notifier(cfg =>
                    {
                        cfg.PositionProvider = new WindowPositionProvider(
                            parentWindow: Application.Current.MainWindow,
                            corner: Corner.BottomRight,
                            offsetX: 10,
                            offsetY: 10);

                        cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                            notificationLifetime: TimeSpan.FromSeconds(3),
                            maximumNotificationCount: MaximumNotificationCount.FromCount(3));

                        cfg.Dispatcher = Application.Current.Dispatcher;
                    });
                }
                return _notifier;
            }
        }
    }
}
