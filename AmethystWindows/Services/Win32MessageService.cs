using AmethystWindows.Models;
using AmethystWindows.Models.Enums;
using Serilog;
using System;
using Vanara.PInvoke;
using WindowsDesktop;

namespace AmethystWindows.Services
{
    public interface IWin32MessageService
    {
        IntPtr HandleMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);
    }

    public class Win32MessageService : IWin32MessageService
    {
        private readonly DesktopService _desktopWindowsManager;
        private readonly ILogger _logger;
        private readonly MainWindowViewModel _mainWindowViewModel;

        public Win32MessageService(DesktopService desktopWindowsManager, ILogger logger, MainWindowViewModel mainWindowViewModel)
        {
            _desktopWindowsManager = desktopWindowsManager ?? throw new ArgumentNullException(nameof(desktopWindowsManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
        }

        public IntPtr HandleMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var isHotkey = msg == (uint)User32.WindowMessage.WM_HOTKEY;
            if (isHotkey)
            {
                _logger.Debug($"Handling shortcut {{Msg}}", msg);

                var hotkeyId = 0L;
                var command = CommandHotkey.None;

                try
                {
                    hotkeyId = wParam.ToInt64();
                    command = (CommandHotkey)hotkeyId;

                    // TODO think in a way to get current desktop directly from the view model itself
                    var foregroundWindow = User32.GetForegroundWindow();
                    var currentMonitor = User32.MonitorFromWindow(foregroundWindow, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
                    var currentVirtualDesktop = VirtualDesktop.Current;
                    var currentPair = new Pair<VirtualDesktop, HMONITOR>(currentVirtualDesktop, currentMonitor);
                    var viewModelDesktopMonitor = _mainWindowViewModel.DesktopMonitors[currentPair];

                    switch (command)
                    {
                        // Layout operations - TODO prob move into its own service
                        case CommandHotkey.RotateLayoutClockwise:
                            viewModelDesktopMonitor?.RotateLayoutClockwise();
                            break;
                        case CommandHotkey.RotateLayoutAntiClockwise:
                            viewModelDesktopMonitor?.RotateLayoutAntiClockwise();
                            break;
                        case CommandHotkey.ExpandMainPane:
                            viewModelDesktopMonitor?.Expand();
                            break;
                        case CommandHotkey.Shrink:
                            viewModelDesktopMonitor?.Shrink();
                            break;

                        // Windows Management operations
                        case CommandHotkey.SetMainPane:
                        case CommandHotkey.Redraw:
                        case CommandHotkey.ChangeWindowFocusAntiClockwise:
                        case CommandHotkey.ChangeWindowFocusClockwise:
                        case CommandHotkey.MoveFocusedToSpace1:
                        case CommandHotkey.MoveFocusedToSpace2:
                        case CommandHotkey.MoveFocusedToSpace3:
                        case CommandHotkey.MoveFocusedToSpace4:
                        case CommandHotkey.MoveFocusedToSpace5:
                        case CommandHotkey.SwapFocusedAnticlockwise:
                        case CommandHotkey.SwapFocusedClockwise:
                        case CommandHotkey.MoveFocusPreviousScreen:
                        case CommandHotkey.MoveFocusNextScreen:
                        case CommandHotkey.MoveFocusedPreviousScreen:
                        case CommandHotkey.MoveFocusedNextScreen:
                        case CommandHotkey.MoveNextSpace:
                        case CommandHotkey.MovePreviousSpace:
                            _desktopWindowsManager.Dispatch(command);
                            break;

                        case CommandHotkey.None:
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to run hotkey {{HotkeyId}} for {{Command}}.", hotkeyId, command);
                }
            }

            return IntPtr.Zero;
        }
    }
}
