using HCI_2025_Project_Template.Services;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace HCI_2025_Project_Template.Views
{
    public partial class NotificationHost : UserControl
    {
        public ObservableCollection<ToastItem> Toasts { get; } = new();

        public NotificationHost()
        {
            InitializeComponent();
            Items.ItemsSource = Toasts;

            Notifier.Raised += OnRaised;
            Unloaded += (_, _) => Notifier.Raised -= OnRaised;
        }

        private void OnRaised(Notification n)
        {
            var item = new ToastItem
            {
                Title = n.Title,
                Message = n.Message,
                Accent = new SolidColorBrush(n.Kind switch
                {
                    NotificationKind.Success => Color.FromRgb(0x10, 0xB9, 0x81),
                    NotificationKind.Warning => Color.FromRgb(0xF5, 0x9E, 0x0B),
                    NotificationKind.Error => Color.FromRgb(0xEF, 0x44, 0x44),
                    _ => Color.FromRgb(0x3B, 0x82, 0xF6),
                }),
                Icon = n.Kind switch
                {
                    NotificationKind.Success => PackIconKind.CheckCircle,
                    NotificationKind.Warning => PackIconKind.AlertCircle,
                    NotificationKind.Error => PackIconKind.CloseCircle,
                    _ => PackIconKind.Information,
                }
            };

            Toasts.Insert(0, item);
            while (Toasts.Count > 4)
                Toasts.RemoveAt(Toasts.Count - 1);

            // Greške stoje duže — korisnik treba vremena da ih pročita
            var seconds = n.Kind == NotificationKind.Error ? 9 : 4;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
            timer.Tick += (_, _) => { Toasts.Remove(item); timer.Stop(); };
            timer.Start();
        }

        private void OnDismiss(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ToastItem item)
                Toasts.Remove(item);
        }
    }

    public class ToastItem
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Brush Accent { get; set; } = Brushes.Gray;
        public PackIconKind Icon { get; set; } = PackIconKind.Information;

        public Visibility MessageVisibility =>
            string.IsNullOrWhiteSpace(Message) ? Visibility.Collapsed : Visibility.Visible;
    }
}