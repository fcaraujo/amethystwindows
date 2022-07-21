using AmethystWindows.Models;
using AmethystWindows.Models.Configuration;
using AmethystWindows.Models.Enums;
using AmethystWindows.Settings;
using DebounceThrottle;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using WindowsDesktop;

[assembly: InternalsVisibleTo("AmethystWindowsTests")]
namespace AmethystWindows.Services
{
    // TODO decouple services into desktop and windows management? (C'mon ~1k lines?!)
    public interface IDesktopService
    {
        ObservableCollection<DesktopWindow> ExcludedWindows { get; }
        string[] FixedExcludedFilters { get; }

        void AddWindow(DesktopWindow desktopWindow);
        void CollectWindows();
        void Dispatch(CommandHotkey command);
        void Draw();
        DesktopWindow? FindWindow(HWND hWND);
        List<DesktopWindow> GetWindowsByVirtualDesktop(VirtualDesktop virtualDesktop);
        IEnumerable<Rectangle> GridGenerator(int mWidth, int mHeight, int windowsCount, int factor, Layout layout, int layoutPadding);
        void Redraw();
        void RemoveWindow(DesktopWindow desktopWindow);
        void RepositionWindow(DesktopWindow oldDesktopWindow, DesktopWindow newDesktopWindow);
    }

    public class DesktopService : IDesktopService
    {
        private readonly ILogger _logger;
        private readonly INotificationService _notificationService;
        private readonly IVirtualDesktopService _virtualDesktopService;
        private readonly ISettingsService _settingsService;
        private readonly MainWindowViewModel _mainWindowViewModel;

        private Dictionary<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>> Windows { get; }
        private Dictionary<Pair<VirtualDesktop, HMONITOR>, bool> WindowsSubscribed = new Dictionary<Pair<VirtualDesktop, HMONITOR>, bool>();

        private DebounceDispatcher debounceDispatcher = new DebounceDispatcher(100);

        private readonly string[] FixedFilters = new string[] {
            "AmethystWindows",
            "AmethystWindowsPackaging",
            "Cortana",
            "Microsoft Spy++",
            "Task Manager",
        };

        private readonly string[] ModelViewPropertiesDraw = new string[] {
            "Padding",
            "LayoutPadding",
            "MarginTop",
            "MarginRight",
            "MarginBottom",
            "MarginLeft",
            "ConfigurableFilters",
            "ConfigurableAdditions",
        };

        private readonly string[] ModelViewPropertiesDrawMonitor = new string[] {
            "DesktopMonitors",
            "Windows",
        };

        private readonly string[] ModelViewPropertiesSaveSettings = new string[] {
            "Padding",
            "LayoutPadding",
            "MarginTop",
            "MarginRight",
            "MarginBottom",
            "MarginLeft",
            "VirtualDesktops",
            // "DesktopMonitors",
            "Additions",
            "Filters",
        };

        // TODO turn this private and restrict its access?
        public ObservableCollection<DesktopWindow> ExcludedWindows { get; }

        public string[] FixedExcludedFilters => new string[] {
            "Settings",
        };

        public DesktopService(ILogger logger,
                              INotificationService notificationService,
                              IVirtualDesktopService virtualDesktopService,
                              ISettingsService settingsService,
                              MainWindowViewModel mainWindowViewModel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _virtualDesktopService = virtualDesktopService ?? throw new ArgumentNullException(nameof(virtualDesktopService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(_mainWindowViewModel));

            Windows = new Dictionary<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>>();
            ExcludedWindows = new ObservableCollection<DesktopWindow>();

            ExcludedWindows.CollectionChanged += ExcludedWindows_CollectionChanged;
            _mainWindowViewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
        }

        public void AddWindow(DesktopWindow desktopWindow)
        {
            Pair<string, string> configurableFilter = _mainWindowViewModel.ConfigurableFilters.FirstOrDefault(f => f.Key == desktopWindow.AppName);

            if (!Windows.ContainsKey(desktopWindow.GetDesktopMonitor()) && !WindowsSubscribed.ContainsKey(desktopWindow.GetDesktopMonitor()))
            {
                WindowsSubscribed.Add(desktopWindow.GetDesktopMonitor(), false);
                Windows.Add(desktopWindow.GetDesktopMonitor(), new ObservableCollection<DesktopWindow>());
                SubscribeWindowsCollectionChanged(desktopWindow.GetDesktopMonitor(), true);
            }

            if (FixedFilters.All(s => !desktopWindow.AppName.StartsWith(s))
                && desktopWindow.AppName != string.Empty &&
                !Windows[desktopWindow.GetDesktopMonitor()].Contains(desktopWindow))
            {
                if (configurableFilter.Equals(null))
                {
                    Windows[desktopWindow.GetDesktopMonitor()].Insert(0, desktopWindow);
                }
                else
                {
                    if (configurableFilter.Value != "*" && configurableFilter.Value != desktopWindow.ClassName)
                    {
                        Windows[desktopWindow.GetDesktopMonitor()].Insert(0, desktopWindow);
                    }
                }
            }
        }

        // Previously from *.GENERATOR
        // TODO this apparently can be moved out of here...?
        public IEnumerable<Rectangle> GridGenerator(int mWidth, int mHeight, int windowsCount, int factor, Layout layout, int layoutPadding)
        {
            int i = 0;
            int j = 0;
            int horizStep;
            int vertStep;
            int tiles;
            int horizSize;
            int vertSize;
            bool isFirstLine;
            switch (layout)
            {
                case Layout.Column:
                    {
                        horizSize = mWidth / windowsCount;
                        j = 0;
                        for (i = 0; i < windowsCount; i++)
                        {
                            int lastPadding = i == windowsCount - 1 ? 0 : layoutPadding;
                            yield return new Rectangle(i * horizSize, j, horizSize - lastPadding, mHeight);
                        }
                    }
                    break;
                case Layout.Row:
                    {
                        vertSize = mHeight / windowsCount;
                        j = 0;
                        for (i = 0; i < windowsCount; i++)
                        {
                            int lastPadding = i == windowsCount - 1 ? 0 : layoutPadding;
                            yield return new Rectangle(j, i * vertSize, mWidth, vertSize - lastPadding);
                        }
                    }
                    break;
                case Layout.Horizontal:
                    {
                        horizStep = Math.Max((int)Math.Sqrt(windowsCount), 1);
                        vertStep = Math.Max(windowsCount / horizStep, 1);
                        tiles = horizStep * vertStep;
                        horizSize = mWidth / horizStep;
                        vertSize = mHeight / vertStep;
                        isFirstLine = true;

                        if (windowsCount != tiles || windowsCount == 3)
                        {
                            if (windowsCount == 3)
                            {
                                vertStep--;
                                vertSize = mHeight / vertStep;
                            }

                            while (windowsCount > 0)
                            {
                                int lastPaddingI = i == horizStep - 1 ? 0 : layoutPadding;
                                int lastPaddingJ = j == vertStep - 1 ? 0 : layoutPadding;
                                yield return new Rectangle(i * horizSize, j * vertSize, horizSize - lastPaddingI, vertSize - lastPaddingJ);
                                i++;
                                if (i >= horizStep)
                                {
                                    i = 0;
                                    j++;
                                }
                                if (j == vertStep - 1 && isFirstLine)
                                {
                                    horizStep++;
                                    horizSize = mWidth / horizStep;
                                    isFirstLine = false;
                                }
                                windowsCount--;
                            }
                        }
                        else
                        {
                            while (windowsCount > 0)
                            {
                                int lastPaddingI = i == horizStep - 1 ? 0 : layoutPadding;
                                int lastPaddingJ = j == vertStep - 1 ? 0 : layoutPadding;
                                yield return new Rectangle(i * horizSize, j * vertSize, horizSize - lastPaddingI, vertSize - lastPaddingJ);
                                i++;
                                if (i >= horizStep)
                                {
                                    i = 0;
                                    j++;
                                }
                                windowsCount--;
                            }
                        }
                    }
                    break;
                case Layout.Vertical:
                    {
                        vertStep = Math.Max((int)Math.Sqrt(windowsCount), 1);
                        horizStep = Math.Max(windowsCount / vertStep, 1);
                        tiles = horizStep * vertStep;
                        vertSize = mHeight / vertStep;
                        horizSize = mWidth / horizStep;
                        isFirstLine = true;

                        if (windowsCount != tiles || windowsCount == 3)
                        {
                            if (windowsCount == 3)
                            {
                                horizStep--;
                                horizSize = mWidth / horizStep;
                            }

                            while (windowsCount > 0)
                            {
                                int lastPaddingI = i == horizStep - 1 ? 0 : layoutPadding;
                                int lastPaddingJ = j == vertStep - 1 ? 0 : layoutPadding;
                                yield return new Rectangle(i * horizSize, j * vertSize, horizSize - lastPaddingI, vertSize - lastPaddingJ);
                                j++;
                                if (j >= vertStep)
                                {
                                    j = 0;
                                    i++;
                                }
                                if (i == horizStep - 1 && isFirstLine)
                                {
                                    vertStep++;
                                    vertSize = mHeight / vertStep;
                                    isFirstLine = false;
                                }
                                windowsCount--;
                            }
                        }
                        else
                        {
                            while (windowsCount > 0)
                            {
                                int lastPaddingI = i == horizStep - 1 ? 0 : layoutPadding;
                                int lastPaddingJ = j == vertStep - 1 ? 0 : layoutPadding;
                                yield return new Rectangle(i * horizSize, j * vertSize, horizSize - lastPaddingI, vertSize - lastPaddingJ);
                                j++;
                                if (j >= vertStep)
                                {
                                    j = 0;
                                    i++;
                                }
                                windowsCount--;
                            }
                        }
                    }
                    break;
                case Layout.FullScreen:
                    {
                        for (i = 0; i < windowsCount; i++)
                        {
                            yield return new Rectangle(0, 0, mWidth, mHeight);
                        }
                    }
                    break;
                case Layout.Wide:
                    {
                        if (windowsCount == 1) yield return new Rectangle(0, 0, mWidth, mHeight);
                        else
                        {
                            int size = mWidth / (windowsCount - 1);
                            for (i = 0; i < windowsCount - 1; i++)
                            {
                                int lastPaddingI = windowsCount == 1 ? 0 : layoutPadding;
                                int lastPaddingJ = i == windowsCount - 2 ? 0 : layoutPadding;

                                if (i == 0) yield return new Rectangle(0, 0, mWidth, mHeight / 2 + factor * _mainWindowViewModel.Step - lastPaddingI / 2);
                                yield return new Rectangle(i * size, mHeight / 2 + factor * _mainWindowViewModel.Step + lastPaddingI / 2, size - lastPaddingJ, mHeight / 2 - factor * _mainWindowViewModel.Step - lastPaddingI / 2);
                            }
                        }
                    }
                    break;
                case Layout.Tall:
                    {
                        if (windowsCount == 1) yield return new Rectangle(0, 0, mWidth, mHeight);
                        else
                        {
                            int size = mHeight / (windowsCount - 1);
                            for (i = 0; i < windowsCount - 1; i++)
                            {
                                int lastPaddingI = i == windowsCount - 2 ? 0 : layoutPadding;
                                int lastPaddingJ = windowsCount == 1 ? 0 : layoutPadding;

                                if (i == 0) yield return new Rectangle(0, 0, mWidth / 2 + factor * _mainWindowViewModel.Step - lastPaddingJ / 2, mHeight);
                                yield return new Rectangle(mWidth / 2 + factor * _mainWindowViewModel.Step + lastPaddingJ / 2, i * size, mWidth / 2 - factor * _mainWindowViewModel.Step - lastPaddingJ / 2, size - lastPaddingI);
                            }
                        }
                    }
                    break;
            }
        }

        // Previously from *.WINDOW
        public void Dispatch(CommandHotkey command)
        {
            var foregroundWindow = User32.GetForegroundWindow();
            var currentMonitor = User32.MonitorFromWindow(foregroundWindow, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
            var currentVirtualDesktop = _virtualDesktopService.GetCurrent();
            var currentPair = new Pair<VirtualDesktop, HMONITOR>(currentVirtualDesktop, currentMonitor);

            var selectedWindow = GetWindowByHandlers(foregroundWindow, currentMonitor, currentVirtualDesktop);
            if (selectedWindow is null)
            {
                _logger.Error("Selected window is not found.");
                return;
            }

            var findWindow = FindWindow(foregroundWindow);

            _logger.Debug($"Dispatch {{Command}}", command);

            if (findWindow is null)
            {
                _logger.Error("No window is found, is that a problem?");
                return;
            }

            switch (command)
            {
                // default => alt+shift+enter
                case CommandHotkey.SetMainPane:
                    SetMainPane(currentPair, selectedWindow);
                    break;
                // default => alt+shift+Z
                case CommandHotkey.Redraw:
                    Redraw();
                    break;
                // default => alt+shift+K
                case CommandHotkey.ChangeWindowFocusAntiClockwise:
                    ChangeWindowFocusAntiClockwise(currentPair, selectedWindow);
                    break;
                // default => alt+shift+J
                case CommandHotkey.ChangeWindowFocusClockwise:
                    ChangeWindowFocusClockwise(currentPair, selectedWindow);
                    break;
                // default => alt+shift+win+{1,2,3,4,5}
                case CommandHotkey.MoveFocusedToSpace1:
                case CommandHotkey.MoveFocusedToSpace2:
                case CommandHotkey.MoveFocusedToSpace3:
                case CommandHotkey.MoveFocusedToSpace4:
                case CommandHotkey.MoveFocusedToSpace5:
                    MoveWindowSpecificVirtualDesktop(findWindow, command);
                    break;
                // default => alt+shift+win+"arrow-right"
                case CommandHotkey.MoveNextSpace:
                    MoveWindowNextVirtualDesktop(findWindow);
                    break;
                // default => alt+shift+win+"arrow-left"
                case CommandHotkey.MovePreviousSpace:
                    MoveWindowPreviousVirtualDesktop(findWindow);
                    break;
                // default => alt+shift+H
                case CommandHotkey.SwapFocusedAnticlockwise:
                    RotateMonitorCounterClockwise(currentPair);
                    break;
                // default => alt+shift+L
                case CommandHotkey.SwapFocusedClockwise:
                    RotateMonitorClockwise(currentPair);
                    break;
                // TODO apparently it does nothing - check with more than one monitor?!
                // default => alt+shift+P
                case CommandHotkey.MoveFocusPreviousScreen:
                    MoveWindowCounterClockwise(currentPair, selectedWindow);
                    break;
                // TODO apparently it does nothing - check with more than one monitor?!
                // default => alt+shift+N
                case CommandHotkey.MoveFocusNextScreen:
                    MoveWindowClockwise(currentPair, selectedWindow);
                    break;
                // default => alt+shift+win+K
                case CommandHotkey.MoveFocusedPreviousScreen:
                    MoveWindowPreviousScreen(findWindow);
                    break;
                // default => alt+shift+win+J
                case CommandHotkey.MoveFocusedNextScreen:
                    MoveWindowNextScreen(findWindow);
                    break;
                // default => alt+shift+I
                case CommandHotkey.DisplayCurrentInfo:
                    DisplayCurrentInfo(currentVirtualDesktop);
                    break;
                // empty action
                case CommandHotkey.None:
                default:
                    break;
            }
        }

        private void DisplayCurrentInfo(VirtualDesktop? currentVirtualDesktop)
        {
            if (currentVirtualDesktop is null)
                return;

            var x = _mainWindowViewModel.DesktopMonitors.FirstOrDefault(x => x.VirtualDesktop?.Id == currentVirtualDesktop?.Id);
            if (x is null)
                return;

            var layout = x.Layout;

            _notificationService.Show("Current information", $"Layout: {layout}");
        }

        public void RemoveWindow(DesktopWindow desktopWindow)
        {
            Windows[desktopWindow.GetDesktopMonitor()].Remove(desktopWindow);
        }

        public void RepositionWindow(DesktopWindow oldDesktopWindow, DesktopWindow newDesktopWindow)
        {
            RemoveWindow(oldDesktopWindow);
            AddWindow(newDesktopWindow);
        }

        public List<DesktopWindow> GetWindowsByVirtualDesktop(VirtualDesktop virtualDesktop)
        {
            var desktopMonitorPairs = Windows
                .Keys
                .Where(desktopMonitor =>
                {
                    var k = desktopMonitor.Key ?? throw new ArgumentNullException();
                    var result = k.Equals(virtualDesktop);
                    return result;
                });

            return Windows.Where(windowsList => desktopMonitorPairs.Contains(windowsList.Key)).Select(windowsList => windowsList.Value).SelectMany(window => window).ToList();
        }

        public void CollectWindows()
        {
            User32.EnumWindowsProc filterDesktopWindows = delegate (HWND windowHandle, IntPtr lparam)
            {
                DesktopWindow desktopWindow = new DesktopWindow(windowHandle);
                desktopWindow.GetAppName();
                desktopWindow.GetClassName();

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
                    User32.ShowWindow(windowHandle, ShowWindowCommand.SW_RESTORE);
                    desktopWindow.GetInfo();

                    if (Windows.ContainsKey(desktopWindow.GetDesktopMonitor()))
                    {
                        if (!Windows[desktopWindow.GetDesktopMonitor()].Contains(desktopWindow))
                        {
                            AddWindow(desktopWindow);
                        }
                    }
                    else
                    {
                        Windows.Add(
                            desktopWindow.GetDesktopMonitor(),
                            new ObservableCollection<DesktopWindow>(new DesktopWindow[] { })
                            );
                        AddWindow(desktopWindow);
                    }
                }
                else
                {
                    if (desktopWindow.IsExcluded() && !ExcludedWindows.Contains(desktopWindow) && desktopWindow.AppName != "" && !FixedExcludedFilters.Contains(desktopWindow.AppName)) ExcludedWindows.Add(desktopWindow);
                }
                return true;
            };

            User32.EnumWindows(filterDesktopWindows, IntPtr.Zero);

            foreach (var desktopMonitor in Windows)
            {
                if (!WindowsSubscribed.ContainsKey(desktopMonitor.Key))
                {
                    WindowsSubscribed.Add(desktopMonitor.Key, false);
                }
                SubscribeWindowsCollectionChanged(desktopMonitor.Key, true);
            }
        }

        public void Redraw()
        {
            ClearWindows();
            CollectWindows();
            Draw();
        }

        public DesktopWindow? FindWindow(HWND hWND)
        {
            List<DesktopWindow> desktopWindows = new List<DesktopWindow>();
            foreach (var desktopMonitor in Windows)
            {
                desktopWindows.AddRange(Windows[new Pair<VirtualDesktop, HMONITOR>(desktopMonitor.Key.Key, desktopMonitor.Key.Value)].Where(window => window.Window == hWND));
            }
            return desktopWindows.FirstOrDefault();
        }

        private void ExcludedWindows_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!System.Windows.Application.Current.MainWindow.Equals(null))
            {
                _mainWindowViewModel.UpdateExcludedWindows();
            }
        }

        private void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var propertyName = e.PropertyName;

            _logger.Debug("PropertyChanged {PropertyName}.", propertyName);

            if (ModelViewPropertiesSaveSettings.Contains(propertyName))
            {
                _logger.Debug("» Save settings.");

                // TODO handle it
                var i = MySettings.Instance ?? throw new ArgumentNullException(nameof(MySettings.Instance));
                MySettings.Instance.Filters = _mainWindowViewModel.ConfigurableFilters;
                MySettings.Instance.Additions = _mainWindowViewModel.ConfigurableAdditions;
                MySettings.Instance.DesktopMonitors = _mainWindowViewModel.DesktopMonitors.ToList();
                MySettings.Save();


                // TODO check it's been called every layout rotation?
                _settingsService.SetSettingsOptions(_mainWindowViewModel);
                _settingsService.Save();
            }

            if (propertyName == "ConfigurableFilters" || propertyName == "ConfigurableAdditions")
            {
                _logger.Debug("» Configurable Filters/Additions.");

                ClearWindows();
                CollectWindows();
            }

            // When some visual settings is changed
            if (ModelViewPropertiesDraw.Contains(propertyName))
            {
                var monitor = _mainWindowViewModel.LastChangedDesktopMonitor;

                if (monitor.Key != null)
                {
                    _logger.Debug("» Properties draw with {MonitorKey} monitor.", monitor.Key);

                    debounceDispatcher.Debounce(() =>
                    {
                        _logger.Debug("» Dispatcher is running and it should call Draw method.");
                        Draw(monitor);
                    });
                }
                else
                {
                    // TODO discover when this is actually called...
                    _logger.Debug("» Debounce without monitor.");
                    debounceDispatcher.Debounce(() => Draw());
                }
            }

            // When a window is added/removed
            // TODO prob this logic should just consider the "current monitor"
            if (ModelViewPropertiesDrawMonitor.Contains(propertyName))
            {
                var monitor = _mainWindowViewModel.LastChangedDesktopMonitor;

                if (monitor.Key != null)
                {
                    _logger.Debug("» Window added/removed for {MonitorKey} monitor.", monitor.Key);
                    Draw(monitor);
                }
                else
                {
                    _logger.Warning("» Window added/removed BUT NO monitor found.");
                    Draw();
                }
            }

            if (propertyName == nameof(SettingsOptions.VirtualDesktops))
            {
                _logger.Debug("» Virtual desktops.");

                _virtualDesktopService.SynchronizeSpaces();
            }
        }

        private void RotateMonitorClockwise(Pair<VirtualDesktop, HMONITOR> currentDesktopMonitor)
        {
            var virtualDesktopMonitors = GetVirtualDesktopMonitors(currentDesktopMonitor);

            var nextMonitor = virtualDesktopMonitors
                .SkipWhile(x => x != currentDesktopMonitor.Value)
                .Skip(1)
                .DefaultIfEmpty(virtualDesktopMonitors[0])
                .FirstOrDefault();

            var nextDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(currentDesktopMonitor.Key, nextMonitor);
            SetForegroundWindow(nextDesktopMonitor);
        }

        private void RotateMonitorCounterClockwise(Pair<VirtualDesktop, HMONITOR> currentDesktopMonitor)
        {
            var virtualDesktopMonitors = GetVirtualDesktopMonitors(currentDesktopMonitor);

            var nextMonitor = virtualDesktopMonitors
                .TakeWhile(x => x != currentDesktopMonitor.Value)
                .Skip(1)
                .DefaultIfEmpty(virtualDesktopMonitors[0])
                .FirstOrDefault();

            var nextDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(currentDesktopMonitor.Key, nextMonitor);
            SetForegroundWindow(nextDesktopMonitor);
        }

        private List<HMONITOR> GetVirtualDesktopMonitors(Pair<VirtualDesktop, HMONITOR> currentDesktopMonitor)
        {
            return Windows
                .Keys
                .Where(desktopMonitor =>
                {
                    var desktopMonitorKey = desktopMonitor.Key ?? throw new ArgumentNullException(nameof(desktopMonitor.Key));
                    var result = desktopMonitorKey.Equals(currentDesktopMonitor.Key);
                    return result;
                })
                .Select(desktopMonitor => desktopMonitor.Value)
                .ToList();
        }

        private void SetForegroundWindow(Pair<VirtualDesktop, HMONITOR> nextDesktopMonitor)
        {
            var desktopWindow = Windows[nextDesktopMonitor].FirstOrDefault() ?? throw new ArgumentNullException();
            User32.SetForegroundWindow(desktopWindow.Window);
        }

        private void SubscribeWindowsCollectionChanged(Pair<VirtualDesktop, HMONITOR> desktopMonitor, bool enabled)
        {
            if (!enabled)
                Windows[desktopMonitor].CollectionChanged -= Windows_CollectionChanged;
            else if (!WindowsSubscribed[desktopMonitor])
                Windows[desktopMonitor].CollectionChanged += Windows_CollectionChanged;

            WindowsSubscribed[desktopMonitor] = enabled;
        }

        // Previously from *.DRAW
        public void Draw()
        {
            _logger.Debug("Performing {DesktopServiceMethod}.", nameof(Draw));

            if (_mainWindowViewModel.Disabled)
            {
                _logger.Warning("Skipping draw as it's disabled.");
                return;
            }

            foreach (var desktopMonitor in Windows)
            {
                int mX, mY;
                IEnumerable<Rectangle> gridGenerator;
                DrawMonitor(desktopMonitor, out mX, out mY, out gridGenerator);

                foreach (var w in desktopMonitor.Value.Select((value, i) => new Tuple<int, DesktopWindow>(i, value)))
                {
                    User32.ShowWindow(w.Item2.Window, ShowWindowCommand.SW_RESTORE);
                }

                HDWP hDWP1 = User32.BeginDeferWindowPos(Windows.Count);
                foreach (var w in desktopMonitor.Value.Select((value, i) => new Tuple<int, DesktopWindow>(i, value)))
                {
                    Rectangle adjustedSize = new Rectangle(
                        gridGenerator.ToArray()[w.Item1].X,
                        gridGenerator.ToArray()[w.Item1].Y,
                        gridGenerator.ToArray()[w.Item1].Width,
                        gridGenerator.ToArray()[w.Item1].Height
                    );

                    DrawWindow(mX, mY, adjustedSize, w, hDWP1, Windows.Count);
                }
                User32.EndDeferWindowPos(hDWP1.DangerousGetHandle());

                foreach (var w in desktopMonitor.Value.Select((value, i) => new Tuple<int, DesktopWindow>(i, value)))
                {
                    w.Item2.GetWindowInfo();
                }
            }
        }

        private void Draw(Pair<VirtualDesktop, HMONITOR> key)
        {
            _logger.Debug("Performing {DesktopServiceMethod} with {Monitor}.", nameof(Draw), key.Key);

            if (_mainWindowViewModel.Disabled)
            {
                _logger.Warning("Skipping draw as it's disabled.");
                return;
            }

            if (!Windows.TryGetValue(key, out _))
            {
                _logger.Error("The key was not found in windows, therefore it's impossible to continue.");
                return;
            }

            var windows = Windows[key];
            var desktopMonitor = new KeyValuePair<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>>(key, windows);

            int mX, mY;
            IEnumerable<Rectangle> gridGenerator;
            DrawMonitor(desktopMonitor, out mX, out mY, out gridGenerator);

            foreach (var w in windows.Select((value, i) => new Tuple<int, DesktopWindow>(i, value)))
            {
                User32.ShowWindow(w.Item2.Window, ShowWindowCommand.SW_RESTORE);
            }

            HDWP hDWP1 = User32.BeginDeferWindowPos(windows.Count);
            foreach (var w in windows.Select((value, i) => new Tuple<int, DesktopWindow>(i, value)))
            {
                Rectangle adjustedSize = new Rectangle(
                    gridGenerator.ToArray()[w.Item1].X,
                    gridGenerator.ToArray()[w.Item1].Y,
                    gridGenerator.ToArray()[w.Item1].Width,
                    gridGenerator.ToArray()[w.Item1].Height
                );

                DrawWindow(mX, mY, adjustedSize, w, hDWP1, windows.Count);
            }
            User32.EndDeferWindowPos(hDWP1.DangerousGetHandle());

            foreach (var w in desktopMonitor.Value.Select((value, i) => new Tuple<int, DesktopWindow>(i, value)))
            {
                w.Item2.GetWindowInfo();
            }
        }

        private void DrawMonitor(KeyValuePair<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>> desktopMonitor, out int mX, out int mY, out IEnumerable<Rectangle> gridGenerator)
        {
            HMONITOR m = desktopMonitor.Key.Value;
            int windowsCount = desktopMonitor.Value.Count;

            User32.MONITORINFO info = new User32.MONITORINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            User32.GetMonitorInfo(m, ref info);

            mX = info.rcWork.X + _mainWindowViewModel.MarginLeft;
            mY = info.rcWork.Y + _mainWindowViewModel.MarginTop;
            int mWidth = info.rcWork.Width - _mainWindowViewModel.MarginLeft - _mainWindowViewModel.MarginRight;
            int mHeight = info.rcWork.Height - _mainWindowViewModel.MarginTop - _mainWindowViewModel.MarginBottom;

            Layout mCurrentLayout;
            int mCurrentFactor;
            try
            {
                mCurrentLayout = _mainWindowViewModel.DesktopMonitors[desktopMonitor.Key].Layout;
                mCurrentFactor = _mainWindowViewModel.DesktopMonitors[desktopMonitor.Key].Factor;
            }
            // WTF exception based flow...?
            catch
            {
                var virtualDesktop = desktopMonitor.Key.Key ?? throw new ArgumentNullException();

                _mainWindowViewModel.DesktopMonitors
                    .Add(new DesktopMonitorViewModel(
                        desktopMonitor.Key.Value,
                        virtualDesktop,
                        0,
                        Layout.Tall
                    ));
                mCurrentLayout = _mainWindowViewModel.DesktopMonitors[desktopMonitor.Key].Layout;
                mCurrentFactor = _mainWindowViewModel.DesktopMonitors[desktopMonitor.Key].Factor;
            }

            gridGenerator = GridGenerator(mWidth, mHeight, windowsCount, mCurrentFactor, mCurrentLayout, _mainWindowViewModel.LayoutPadding);
        }

        private void DrawWindow(int mX, int mY, Rectangle adjustedSize, Tuple<int, DesktopWindow> w, HDWP hDWP, int windowsCount)
        {
            int X = mX + adjustedSize.X - w.Item2.BorderX / 2 + _mainWindowViewModel.Padding;
            int Y = mY + adjustedSize.Y - w.Item2.BorderY / 2 + _mainWindowViewModel.Padding;

            Y = Y <= mY ? mY : Y;

            User32.DeferWindowPos(
                hDWP,
                w.Item2.Window,
                HWND.HWND_NOTOPMOST,
                X,
                Y,
                adjustedSize.Width + w.Item2.BorderX - 2 * _mainWindowViewModel.Padding,
                adjustedSize.Height + w.Item2.BorderY - 2 * _mainWindowViewModel.Padding,
                User32.SetWindowPosFlags.SWP_NOACTIVATE |
                User32.SetWindowPosFlags.SWP_NOCOPYBITS |
                User32.SetWindowPosFlags.SWP_NOZORDER |
                User32.SetWindowPosFlags.SWP_NOOWNERZORDER
                );
        }

        private void ClearWindows()
        {
            foreach (var desktopMonitor in Windows)
            {
                desktopMonitor.Value.Clear();
            }
        }

        private void Windows_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action.Equals(NotifyCollectionChangedAction.Remove))
            {
                if (e.OldItems is null)
                {
                    throw new ArgumentNullException();
                }

                var oldItem = e.OldItems[0] ?? throw new ArgumentNullException();

                var desktopWindow = (DesktopWindow)oldItem;
            }
            else if (e.Action.Equals(NotifyCollectionChangedAction.Add))
            {
                if (e.NewItems is null)
                {
                    throw new ArgumentNullException();
                }

                var newItem = e.NewItems[0] ?? throw new ArgumentNullException();

                var desktopWindow = (DesktopWindow)newItem;

                var virtualDesktop = desktopWindow.GetDesktopMonitor().Key ?? throw new ArgumentNullException();

                if (virtualDesktop.Equals(null) || desktopWindow.GetDesktopMonitor().Value.Equals(null))
                    desktopWindow.GetInfo();
            }

            // TODO check exception - An exception of type 'System.InvalidOperationException' occurred in WindowsBase.dll but was not handled in user code: 'The calling thread cannot access this object because a different thread owns it.'
            // if (!System.Windows.Application.Current.MainWindow.Equals(null))
            // {
            _mainWindowViewModel.UpdateWindows();
            // }
        }

        private DesktopWindow? GetWindowByHandlers(HWND hWnd, HMONITOR hMONITOR, VirtualDesktop? desktop)
        {
            DesktopWindow? result = null;
            try
            {
                result = Windows[new Pair<VirtualDesktop, HMONITOR>(desktop, hMONITOR)].First(window => window.Window == hWnd);
            }
            catch (Exception)
            {
                _logger.Error("Failed to find window by handler.");
            }
            return result;
        }

        private void SetMainPane(Pair<VirtualDesktop, HMONITOR> desktopMonitor, DesktopWindow selectedWindow)
        {
            var windows = Windows[desktopMonitor];
            var oldIndex = windows.IndexOf(selectedWindow);
            windows.Move(oldIndex, 0);
        }

        private void ChangeWindowFocusClockwise(Pair<VirtualDesktop, HMONITOR> desktopMonitor, DesktopWindow window)
        {
            int currentIndex = Windows[desktopMonitor].IndexOf(window);
            int maxIndex = Windows[desktopMonitor].Count - 1;
            if (currentIndex == maxIndex)
            {
                User32.SetForegroundWindow(Windows[desktopMonitor][0].Window);
            }
            else
            {
                User32.SetForegroundWindow(Windows[desktopMonitor][++currentIndex].Window);
            }
        }

        private void ChangeWindowFocusAntiClockwise(Pair<VirtualDesktop, HMONITOR> desktopMonitor, DesktopWindow window)
        {
            int currentIndex = Windows[desktopMonitor].IndexOf(window);
            int maxIndex = Windows[desktopMonitor].Count - 1;
            if (currentIndex == 0)
            {
                User32.SetForegroundWindow(Windows[desktopMonitor][maxIndex].Window);
            }
            else
            {
                User32.SetForegroundWindow(Windows[desktopMonitor][--currentIndex].Window);
            }
        }

        private void MoveWindowClockwise(Pair<VirtualDesktop, HMONITOR> desktopMonitor, DesktopWindow window)
        {
            int currentIndex = Windows[desktopMonitor].IndexOf(window);
            int maxIndex = Windows[desktopMonitor].Count - 1;
            if (currentIndex == maxIndex)
            {
                Windows[desktopMonitor].Move(currentIndex, 0);
            }
            else
            {
                Windows[desktopMonitor].Move(currentIndex, ++currentIndex);
            }
        }

        private void MoveWindowCounterClockwise(Pair<VirtualDesktop, HMONITOR> desktopMonitor, DesktopWindow window)
        {
            int currentIndex = Windows[desktopMonitor].IndexOf(window);
            int maxIndex = Windows[desktopMonitor].Count - 1;
            if (currentIndex == 0)
            {
                Windows[desktopMonitor].Move(currentIndex, maxIndex);
            }
            else
            {
                Windows[desktopMonitor].Move(currentIndex, --currentIndex);
            }
        }

        private void MoveWindowNextScreen(DesktopWindow window)
        {
            // TODO Validate - sometimes it crashes?!
            var desktopMonitors = Windows.Keys.Where(IsCurrentVirtualDesktop()).ToList();
            var currentMonitorIndex = desktopMonitors.IndexOf(window.GetDesktopMonitor());
            var maxIndex = desktopMonitors.Count - 1;

            _mainWindowViewModel.LastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());

            if (currentMonitorIndex == maxIndex)
            {
                RemoveWindow(window);
                _mainWindowViewModel.LastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());
                window.MonitorHandle = desktopMonitors[0].Value;
                AddWindow(window);
            }
            else
            {
                RemoveWindow(window);
                _mainWindowViewModel.LastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());
                window.MonitorHandle = desktopMonitors[++currentMonitorIndex].Value;
                AddWindow(window);
            }
        }

        private void MoveWindowPreviousScreen(DesktopWindow window)
        {
            // TODO Validate - sometimes it crashes?!
            var desktopMonitors = Windows.Keys.Where(IsCurrentVirtualDesktop()).ToList();
            var currentMonitorIndex = desktopMonitors.IndexOf(window.GetDesktopMonitor());
            var maxIndex = desktopMonitors.Count - 1;

            _mainWindowViewModel.LastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());

            if (currentMonitorIndex == 0)
            {
                RemoveWindow(window);
                _mainWindowViewModel.LastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());
                window.MonitorHandle = desktopMonitors[maxIndex].Value;
                AddWindow(window);
            }
            else
            {
                RemoveWindow(window);
                _mainWindowViewModel.LastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());
                window.MonitorHandle = desktopMonitors[--currentMonitorIndex].Value;
                AddWindow(window);
            }
        }

        private Func<Pair<VirtualDesktop, HMONITOR>, bool> IsCurrentVirtualDesktop()
        {
            return desktopMonitorPair =>
            {
                var key = desktopMonitorPair.Key ?? throw new ArgumentNullException();
                var current = _virtualDesktopService.GetCurrent();
                var result = key.ToString() == current?.ToString();
                return result;
            };
        }

        private void MoveWindowNextVirtualDesktop(DesktopWindow window)
        {
            var targetDesktop = window.VirtualDesktop?.GetRight();

            // TODO consider having a toggling circular list here (to move to the first virtual desktop) 
            // var targetDesktop = (nextVirtualDesktop is not null)
            //     ? nextVirtualDesktop
            //     : _virtualDesktopService.GetFromIndex(0);

            if (targetDesktop is null)
            {
                _logger.Warning("Cannot move {Window} to the next virtual desktop. Target desktop is null, potentially window reached the last virtual desktop.");
                return;
            }

            RemoveWindow(window);
            var hWnd = window.Window.DangerousGetHandle();
            _virtualDesktopService.MoveTo(hWnd, targetDesktop);
            window.VirtualDesktop = targetDesktop;
            AddWindow(window);
            targetDesktop.Switch();
        }

        private void MoveWindowPreviousVirtualDesktop(DesktopWindow window)
        {
            var targetDesktop = window.VirtualDesktop?.GetLeft();

            // TODO consider having a toggling circular list here (to move to the last virtual desktop) 
            // var targetDesktop = (nextVirtualDesktop is not null)
            //     ? nextVirtualDesktop
            //     : _virtualDesktopService.GetLast();

            if (targetDesktop is null)
            {
                _logger.Warning("Cannot move {Window} to the previous virtual desktop. Target desktop is null, potentially window reached the first virtual desktop.");
                return;
            }

            RemoveWindow(window);
            var hWnd = window.Window.DangerousGetHandle();
            _virtualDesktopService.MoveTo(hWnd, targetDesktop);
            window.VirtualDesktop = targetDesktop;
            AddWindow(window);
            targetDesktop.Switch();
        }

        private void MoveWindowSpecificVirtualDesktop(DesktopWindow window, CommandHotkey command)
        {
            _logger.Information("Perform {Command} on {Window}", command, window);

            if (window is null)
            {
                _logger.Warning("Window is null, therefore we should skip.");
                return;
            }

            // TODO come up with a more sophisticated solution?
            var index = 0;
            switch (command)
            {
                case CommandHotkey.MoveFocusedToSpace1:
                    index = 0;
                    break;
                case CommandHotkey.MoveFocusedToSpace2:
                    index = 1;
                    break;
                case CommandHotkey.MoveFocusedToSpace3:
                    index = 2;
                    break;
                case CommandHotkey.MoveFocusedToSpace4:
                    index = 3;
                    break;
                case CommandHotkey.MoveFocusedToSpace5:
                    index = 4;
                    break;
                default:
                    throw new NotSupportedException("Invalid command.");
            }

            var targetDesktop = _virtualDesktopService.GetFromIndex(index);
            if (targetDesktop is null)
            {
                _logger.Warning("Virtual desktop was not found, therefore we should skip.");
                return;
            }

            var isWindowAlreadyPresent = targetDesktop == window.VirtualDesktop;
            if (isWindowAlreadyPresent)
            {
                _logger.Debug("Window is already present in {TargetDesktop}.", targetDesktop);
                return;
            }

            RemoveWindow(window);
            var hWnd = window.Window.DangerousGetHandle();
            _virtualDesktopService.MoveTo(hWnd, targetDesktop);
            window.VirtualDesktop = targetDesktop;
            AddWindow(window);
            targetDesktop.Switch();
        }
    }
}
