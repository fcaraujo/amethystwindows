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
        IntPtr HandleMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);
    }

    public class Win32MessageService : IWin32MessageService
    {
        private readonly IDesktopService _desktopService;
        private readonly ILogger _logger;
        private readonly MainWindowViewModel _mainWindowVM;
        private readonly IVirtualDesktopWrapper _virtualDesktopWrapper;

        public Win32MessageService(IDesktopService desktopService,
                                   ILogger logger,
                                   MainWindowViewModel mainWindowVM,
                                   IVirtualDesktopWrapper virtualDesktopWrapper)
        {
            _desktopService = desktopService ?? throw new ArgumentNullException(nameof(desktopService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mainWindowVM = mainWindowVM ?? throw new ArgumentNullException(nameof(mainWindowVM));
            _virtualDesktopWrapper = virtualDesktopWrapper ?? throw new ArgumentNullException(nameof(virtualDesktopWrapper));
        }

        public IntPtr HandleMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var isHotkey = msg == (uint)User32.WindowMessage.WM_HOTKEY;
            if (isHotkey)
            {
                _logger.Debug("Handling shortcut {Msg}", msg);

                var hotkeyId = 0L;
                var command = CommandHotkey.None;

                try
                {
                    hotkeyId = wParam.ToInt64();
                    command = (CommandHotkey)hotkeyId;

                    var foregroundWindow = User32.GetForegroundWindow();
                    var currentMonitor = User32.MonitorFromWindow(foregroundWindow, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
                    var currentVirtualDesktop = _virtualDesktopWrapper.GetCurrent();
                    var currentPair = new Pair<VirtualDesktop, HMONITOR>(currentVirtualDesktop, currentMonitor);
                    var viewModelDesktopMonitor = _mainWindowVM.DesktopMonitors[currentPair];

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
                            _desktopService.Dispatch(command);
                            break;

                        case CommandHotkey.None:
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to run hotkey {HotkeyId} for {Command}.", hotkeyId, command);
                }
            }

            return IntPtr.Zero;
        }
    }
}
