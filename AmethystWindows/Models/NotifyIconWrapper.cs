using AmethystWindows.DependencyInjection;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace AmethystWindows.Models
{
    public class NotifyIconWrapper : FrameworkElement, IDisposable
    {
        private static readonly DependencyProperty NotifyRequestProperty =
            DependencyProperty.Register("NotifyRequest", typeof(NotifyRequestRecord), typeof(NotifyIconWrapper),
                new PropertyMetadata(
                    (d, e) =>
                    {
                        var r = (NotifyRequestRecord)e.NewValue;
                        ((NotifyIconWrapper)d)._notifyIcon?.ShowBalloonTip(r.Duration, r.Title, r.Text, r.Icon);
                    }));

        private static readonly RoutedEvent OpenSelectedEvent = EventManager.RegisterRoutedEvent("OpenSelected",
            RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(NotifyIconWrapper));

        private static readonly RoutedEvent ExitSelectedEvent = EventManager.RegisterRoutedEvent("ExitSelected",
            RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(NotifyIconWrapper));

        private readonly NotifyIcon? _notifyIcon;

        private readonly MainWindowViewModel? _mainWindowViewModel;

        public NotifyIconWrapper()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            _mainWindowViewModel = IocProvider.GetService<MainWindowViewModel>() ?? throw new ArgumentNullException(nameof(_mainWindowViewModel));

            _notifyIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
                Visible = true,
                ContextMenuStrip = CreateContextMenu()
            };

            _notifyIcon.DoubleClick += OpenItem_Click;

            Application.Current.Exit += (obj, args) => { _notifyIcon.Dispose(); };
        }

        public NotifyRequestRecord NotifyRequest
        {
            get => (NotifyRequestRecord)GetValue(NotifyRequestProperty);
            set => SetValue(NotifyRequestProperty, value);
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
        }

        public event RoutedEventHandler OpenSelected
        {
            add => AddHandler(OpenSelectedEvent, value);
            remove => RemoveHandler(OpenSelectedEvent, value);
        }

        public event RoutedEventHandler ExitSelected
        {
            add => AddHandler(ExitSelectedEvent, value);
            remove => RemoveHandler(ExitSelectedEvent, value);
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var openItem = new ToolStripMenuItem("Open");
            openItem.Click += OpenItem_Click;

            var disableMenuItem = new ToolStripMenuItem("Disabled");
            disableMenuItem.CheckOnClick = true;
            disableMenuItem.Click += DisableMenuItem_Click;

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += ExitItem_Click;

            var contextMenu = new ContextMenuStrip
            {
                Items =
                {
                    openItem,
                    disableMenuItem,
                    exitItem
                }
            };
            return contextMenu;
        }

        private void DisableMenuItem_Click(object? sender, EventArgs eventArgs)
        {
            if (sender is null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            if (_mainWindowViewModel is null)
            {
                throw new ArgumentNullException(nameof(_mainWindowViewModel));
            }

            var menuItem = (ToolStripMenuItem)sender;
            _mainWindowViewModel.Disabled = menuItem.Checked;
        }

        private void OpenItem_Click(object? sender, EventArgs eventArgs)
        {
            var args = new RoutedEventArgs(OpenSelectedEvent);
            RaiseEvent(args);
            Application.Current.MainWindow.Activate();
        }

        private void ExitItem_Click(object? sender, EventArgs eventArgs)
        {
            var args = new RoutedEventArgs(ExitSelectedEvent);
            RaiseEvent(args);
        }
    }
}