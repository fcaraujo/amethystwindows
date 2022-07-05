using AmethystWindows.Models;
using AmethystWindows.Models.Enums;
using Serilog;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Interop;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

namespace AmethystWindows.Services
{
    // TODO document it (when we actually understand it XD)
    public interface IHotkeyService
    {
        void ClearHotkeys();
        void RegisterHotkeys();
        void SetWindowsHook();
    }

    public class HotkeyService : IHotkeyService
    {
        private readonly ILogger _logger;
        private readonly IDesktopService _desktopWindowsManager;
        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly ISettingsService _settingsService;
        private readonly HWND _windowHandler;

        public HotkeyService(ILogger logger, IDesktopService desktopWindowsManager, MainWindow mainWindow, MainWindowViewModel mainWindowViewModel, ISettingsService settingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _desktopWindowsManager = desktopWindowsManager ?? throw new ArgumentNullException(nameof(desktopWindowsManager));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            _windowHandler = new WindowInteropHelper(mainWindow).Handle;
        }

        // TODO review this code...
        public void SetWindowsHook()
        {
            void WinEventHookAll(HWINEVENTHOOK hWinEventHook, uint winEvent, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
            {
                DesktopWindow desktopWindow = new DesktopWindow(hwnd);
                if (hwnd != HWND.NULL && idObject == ObjectIdentifiers.OBJID_WINDOW && idChild == 0 && desktopWindow.IsRuntimeValuable())
                {
                    switch (winEvent)
                    {
                        case EventConstants.EVENT_OBJECT_SHOW:
                        case EventConstants.EVENT_OBJECT_UNCLOAKED:
                        case EventConstants.EVENT_OBJECT_IME_SHOW:
                        case EventConstants.EVENT_SYSTEM_FOREGROUND:
                            ManageShown(hwnd);
                            break;
                        case EventConstants.EVENT_SYSTEM_MINIMIZEEND:
                            _logger.Debug($"window maximized");
                            desktopWindow.GetInfo();
                            _mainWindowViewModel.LastChangedDesktopMonitor = desktopWindow.GetDesktopMonitor();
                            Pair<string, string> configurableAddition = _mainWindowViewModel.ConfigurableAdditions.FirstOrDefault(f => f.Key == desktopWindow.AppName);
                            bool hasActiveAddition;
                            if (!(configurableAddition.Key == null))
                            {
                                hasActiveAddition = configurableAddition.Value.Equals("*") || configurableAddition.Value.Equals(desktopWindow.ClassName);
                            }
                            else
                            {
                                hasActiveAddition = false;
                            }
                            if (desktopWindow.IsRuntimePresent() || hasActiveAddition) _desktopWindowsManager.AddWindow(desktopWindow);
                            break;
                        case EventConstants.EVENT_SYSTEM_MINIMIZESTART:
                        case EventConstants.EVENT_OBJECT_HIDE:
                        case EventConstants.EVENT_OBJECT_IME_HIDE:
                            _logger.Debug($"window minimized/hide");
                            var removed = _desktopWindowsManager.FindWindow(hwnd);
                            if (removed != null)
                            {
                                _mainWindowViewModel.LastChangedDesktopMonitor = removed.GetDesktopMonitor();
                                _desktopWindowsManager.RemoveWindow(removed);
                            }
                            break;
                        case EventConstants.EVENT_SYSTEM_MOVESIZEEND:
                            _logger.Debug($"window move/size");
                            _mainWindowViewModel.LastChangedDesktopMonitor = new Pair<WindowsDesktop.VirtualDesktop, HMONITOR>(null, new HMONITOR());
                            var moved = _desktopWindowsManager.FindWindow(hwnd);
                            if (moved != null)
                            {
                                DesktopWindow newMoved = new DesktopWindow(hwnd);
                                newMoved.GetInfo();
                                if (!moved.Equals(newMoved))
                                {
                                    _desktopWindowsManager.RepositionWindow(moved, newMoved);
                                }
                            }
                            break;
                        case EventConstants.EVENT_OBJECT_DRAGCOMPLETE:
                            _logger.Debug($"window dragged");
                            break;
                        default:
                            break;
                    }
                }
            }

            var winEventHookAll = new WinEventProc(WinEventHookAll);
            var gchCreate = GCHandle.Alloc(winEventHookAll);
            var hookAll = SetWinEventHook(EventConstants.EVENT_MIN, EventConstants.EVENT_MAX, HINSTANCE.NULL, winEventHookAll, 0, 0, WINEVENT.WINEVENT_OUTOFCONTEXT | WINEVENT.WINEVENT_SKIPOWNPROCESS);
        }

        public void RegisterHotkeys()
        {
            var hotkeys = new ObservableHotkeys(_settingsService.GetHotkeyOptions());

            var commandHotkeys = (CommandHotkey[])Enum.GetValues(typeof(CommandHotkey));
            foreach (var commandHotkey in commandHotkeys)
            {
                // Ignore empty command hotkey
                if (commandHotkey == CommandHotkey.None)
                {
                    continue;
                }

                var hotkeyId = (int)commandHotkey;
                var hotkeyName = commandHotkey.ToString();

                var hotkeyModel = hotkeys.FirstOrDefault(x => x.Command.Equals(commandHotkey));

                if (hotkeyModel == null)
                {
                    _logger.Error($"Hotkey ({{CommandHotkeyId}}){{CommandHotkey}} not found.", hotkeyId, commandHotkey);
                    continue;
                }

                var fsModifiers = hotkeyModel.GetFsModifiers();
                var virtualKey = (uint)hotkeyModel.GetVirtualKey();

                _logger.Debug($"Set command hotkey ({{HotkeyId}}){{HotkeyName}} with {{Shortcut}}.", hotkeyId, commandHotkey, hotkeyModel.Keys);
                RegisterHotKey(_windowHandler, hotkeyId, fsModifiers, virtualKey);
            }
        }

        public void ClearHotkeys()
        {
            var commandHotkeys = (CommandHotkey[])Enum.GetValues(typeof(CommandHotkey));
            foreach (var commandHotkey in commandHotkeys)
            {
                var hotkeyId = (int)commandHotkey;
                var hotkeyName = commandHotkey.ToString();

                _logger.Debug($"Unset ({{HotkeyId}}){{HotkeyName}}.", hotkeyId, hotkeyName);
                UnregisterHotKey(_windowHandler, hotkeyId);
            }
        }

        // TODO review this code...
        private async void ManageShown(HWND hWND)
        {
            await Task.Delay(500);
            DesktopWindow desktopWindow = new DesktopWindow(hWND);
            desktopWindow.GetInfo();
            _mainWindowViewModel.LastChangedDesktopMonitor = desktopWindow.GetDesktopMonitor();
            Pair<string, string> configurableAddition = _mainWindowViewModel.ConfigurableAdditions.FirstOrDefault(f => f.Key == desktopWindow.AppName);
            bool hasActiveAddition;
            if (!(configurableAddition.Key == null))
            {
                hasActiveAddition = configurableAddition.Value.Equals("*") || configurableAddition.Value.Equals(desktopWindow.ClassName);
            }
            else
            {
                hasActiveAddition = false;
            }
            if (desktopWindow.IsRuntimePresent() || hasActiveAddition)
            {
                _logger.Debug($"window created");
                _desktopWindowsManager.AddWindow(desktopWindow);
            }
            else
            {
                // LOL WTF...
                if (desktopWindow.IsExcluded() && !_desktopWindowsManager.ExcludedWindows.Contains(desktopWindow) && desktopWindow.AppName != "" && !_desktopWindowsManager.FixedExcludedFilters.Contains(desktopWindow.AppName))
                    _desktopWindowsManager.ExcludedWindows.Add(desktopWindow);
            }
        }
    }
}
