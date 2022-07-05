using AmethystWindows.Services;
using System;
using System.Windows;
using System.Windows.Interop;

namespace AmethystWindows
{
    public partial class MainWindow : Window
    {
        private readonly IWin32MessageService _win32MessageService;

        public MainWindow(IWin32MessageService win32MessageService)
        {
            InitializeComponent();
            _win32MessageService = win32MessageService ?? throw new ArgumentNullException(nameof(win32MessageService));
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwndSourceHookDelegate = (HwndSourceHook)_win32MessageService.HandleMessage;

            var wpfContentSource = PresentationSource.FromVisual(this) as HwndSource;
            wpfContentSource?.AddHook(hwndSourceHookDelegate);
        }
    }
}
