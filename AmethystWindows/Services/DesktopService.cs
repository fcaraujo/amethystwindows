using AmethystWindows.Models;
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
    // TODO decouple services into desktop and windows management
    public class DesktopService
    {
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;

        // TODO turn this private and restrict its access?
        public readonly MainWindowViewModel _mainWindowViewModel;

        public Dictionary<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>> Windows { get; }
        public Dictionary<Pair<VirtualDesktop, HMONITOR>, bool> WindowsSubscribed = new Dictionary<Pair<VirtualDesktop, HMONITOR>, bool>();

        public ObservableCollection<DesktopWindow> ExcludedWindows { get; }

        private DebounceDispatcher debounceDispatcher = new DebounceDispatcher(100);

        private readonly string[] FixedFilters = new string[] {
            "AmethystWindows",
            "AmethystWindowsPackaging",
            "Cortana",
            "Microsoft Spy++",
            "Task Manager",
        };

        public readonly string[] FixedExcludedFilters = new string[] {
            "Settings",
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
            "DesktopMonitors",
            "Windows",
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
            "DesktopMonitors",
            "Additions",
            "Filters",
            "Hotkeys",
        };

        public DesktopService(ILogger logger, ISettingsService settingsService, MainWindowViewModel amainWindowViewModel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _mainWindowViewModel = amainWindowViewModel ?? throw new ArgumentNullException(nameof(_mainWindowViewModel));

            Windows = new Dictionary<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>>();
            ExcludedWindows = new ObservableCollection<DesktopWindow>();

            ExcludedWindows.CollectionChanged += ExcludedWindows_CollectionChanged;
            _mainWindowViewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
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
            _logger.Debug($"ModelViewChanged: {e.PropertyName}");

            if (e.PropertyName == "VirtualDesktops")
            {
                // TODO check how to use DI here...?
                App.InitVirtualDesktops();
            }

            // TODO check if saving hotkeys on the event is enough
            //if (e.PropertyName == "Hotkeys")
            //{
            //    // TODO add dependency of HooksService in this class retrieve it
            //    var _hooksService = IocProvider.GetService<HooksService>();
            //    _hooksService.ClearHotkeys();
            //    _hooksService.SetKeyboardHook();

            //    _settingsService.SetHotkeyOptions(mainWindowViewModel);
            //    _settingsService.Save();
            //}

            if (ModelViewPropertiesSaveSettings.Contains(e.PropertyName))
            {
                // TODO handle
                MySettings.Instance.Filters = _mainWindowViewModel.ConfigurableFilters;
                MySettings.Instance.Additions = _mainWindowViewModel.ConfigurableAdditions;
                MySettings.Instance.DesktopMonitors = _mainWindowViewModel.DesktopMonitors.ToList();

                MySettings.Save();


                _settingsService.SetSettingsOptions(_mainWindowViewModel);

                // TODO check it's been called every layout rotation?
                _settingsService.Save();
            }

            if (e.PropertyName == "ConfigurableFilters" || e.PropertyName == "ConfigurableAdditions")
            {
                ClearWindows();
                CollectWindows();
            }

            if (ModelViewPropertiesDraw.Contains(e.PropertyName))
            {
                if (ModelViewPropertiesDrawMonitor.Contains(e.PropertyName) && _mainWindowViewModel.LastChangedDesktopMonitor.Key != null) debounceDispatcher.Debounce(() => Draw(_mainWindowViewModel.LastChangedDesktopMonitor));
                else debounceDispatcher.Debounce(() => Draw());
            }
        }

        private void RotateMonitorClockwise(Pair<VirtualDesktop, HMONITOR> currentDesktopMonitor)
        {
            List<HMONITOR> virtualDesktopMonitors = Windows
                .Keys
                .Where(desktopMonitor => desktopMonitor.Key.Equals(currentDesktopMonitor.Key))
                .Select(desktopMonitor => desktopMonitor.Value)
                .ToList();

            HMONITOR nextMonitor = virtualDesktopMonitors.SkipWhile(x => x != currentDesktopMonitor.Value).Skip(1).DefaultIfEmpty(virtualDesktopMonitors[0]).FirstOrDefault();
            Pair<VirtualDesktop, HMONITOR> nextDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(currentDesktopMonitor.Key, nextMonitor);

            User32.SetForegroundWindow(Windows[nextDesktopMonitor].FirstOrDefault().Window);
        }

        private void RotateMonitorCounterClockwise(Pair<VirtualDesktop, HMONITOR> currentDesktopMonitor)
        {
            List<HMONITOR> virtualDesktopMonitors = Windows
                .Keys
                .Where(desktopMonitor => desktopMonitor.Key.Equals(currentDesktopMonitor.Key))
                .Select(desktopMonitor => desktopMonitor.Value)
                .ToList();

            HMONITOR nextMonitor = virtualDesktopMonitors.TakeWhile(x => x != currentDesktopMonitor.Value).Skip(1).DefaultIfEmpty(virtualDesktopMonitors[0]).FirstOrDefault();
            Pair<VirtualDesktop, HMONITOR> nextDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(currentDesktopMonitor.Key, nextMonitor);

            User32.SetForegroundWindow(Windows[nextDesktopMonitor].FirstOrDefault().Window);
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
        public void Draw(Pair<VirtualDesktop, HMONITOR> key)
        {
            if (_mainWindowViewModel.Disabled)
                return;

            ObservableCollection<DesktopWindow> windows = Windows[key];
            KeyValuePair<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>> desktopMonitor = new KeyValuePair<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>>(key, windows);
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

        public void Draw()
        {
            if (_mainWindowViewModel.Disabled)
                return;

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
            catch
            {
                _mainWindowViewModel.DesktopMonitors.Add(new DesktopMonitorViewModel(
                    desktopMonitor.Key.Value,
                    desktopMonitor.Key.Key,
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


        // Previously from *.GENERATOR
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
                case Layout.Horizontal:
                    horizSize = mWidth / windowsCount;
                    j = 0;
                    for (i = 0; i < windowsCount; i++)
                    {
                        int lastPadding = i == windowsCount - 1 ? 0 : layoutPadding;
                        yield return new Rectangle(i * horizSize, j, horizSize - lastPadding, mHeight);
                    }
                    break;
                case Layout.Vertical:
                    vertSize = mHeight / windowsCount;
                    j = 0;
                    for (i = 0; i < windowsCount; i++)
                    {
                        int lastPadding = i == windowsCount - 1 ? 0 : layoutPadding;
                        yield return new Rectangle(j, i * vertSize, mWidth, vertSize - lastPadding);
                    }
                    break;
                case Layout.HorizGrid:
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
                    break;
                case Layout.VertGrid:
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
                    break;
                case Layout.Monocle:
                    for (i = 0; i < windowsCount; i++)
                    {
                        yield return new Rectangle(0, 0, mWidth, mHeight);
                    }
                    break;
                case Layout.Wide:
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
                    break;
                case Layout.Tall:
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
                    break;
            }
        }


        // Previously from *.WINDOW
        public void Dispatch(CommandHotkey command)
        {
            var foregroundWindow = User32.GetForegroundWindow();
            var currentMonitor = User32.MonitorFromWindow(foregroundWindow, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
            var currentVirtualDesktop = VirtualDesktop.Current;
            var currentPair = new Pair<VirtualDesktop, HMONITOR>(currentVirtualDesktop, currentMonitor);
            var selectedWindow = GetWindowByHandlers(foregroundWindow, currentMonitor, currentVirtualDesktop);
            var findWindow = FindWindow(foregroundWindow);

            _logger.Debug($"Dispatch {{Command}}", command);

            switch (command)
            {
                case CommandHotkey.SetMainPane:
                    SetMainPane(currentPair, selectedWindow);
                    break;
                case CommandHotkey.Redraw:
                    Redraw();
                    break;
                case CommandHotkey.ChangeWindowFocusAntiClockwise:
                    ChangeWindowFocusAntiClockwise(currentPair, selectedWindow);
                    break;
                case CommandHotkey.ChangeWindowFocusClockwise:
                    ChangeWindowFocusClockwise(currentPair, selectedWindow);
                    break;

                // TODO check all hotkeys below

                case CommandHotkey.MoveFocusedToSpace1:
                case CommandHotkey.MoveFocusedToSpace2:
                case CommandHotkey.MoveFocusedToSpace3:
                case CommandHotkey.MoveFocusedToSpace4:
                case CommandHotkey.MoveFocusedToSpace5:
                    MoveWindowSpecificVirtualDesktop(findWindow, findWindow.VirtualDesktop.Id);
                    break;

                case CommandHotkey.MoveNextSpace:
                    MoveWindowNextVirtualDesktop(findWindow);
                    break;
                case CommandHotkey.MovePreviousSpace:
                    MoveWindowPreviousVirtualDesktop(findWindow);
                    break;
                case CommandHotkey.SwapFocusedAnticlockwise:
                    RotateMonitorCounterClockwise(currentPair);
                    break;
                case CommandHotkey.SwapFocusedClockwise:
                    RotateMonitorClockwise(currentPair);
                    break;

                case CommandHotkey.MoveFocusPreviousScreen:
                    MoveWindowCounterClockwise(currentPair, selectedWindow);
                    break;
                case CommandHotkey.MoveFocusNextScreen:
                    MoveWindowClockwise(currentPair, selectedWindow);
                    break;
                case CommandHotkey.MoveFocusedPreviousScreen:
                    MoveWindowPreviousScreen(findWindow);
                    break;
                case CommandHotkey.MoveFocusedNextScreen:
                    MoveWindowNextScreen(findWindow);
                    break;

                case CommandHotkey.None:
                default:
                    break;
            }
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
            IEnumerable<Pair<VirtualDesktop, HMONITOR>> desktopMonitorPairs = Windows.Keys.Where(desktopMonitor => desktopMonitor.Key.Equals(virtualDesktop));
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

        private void ClearWindows()
        {
            foreach (var desktopMonitor in Windows)
            {
                desktopMonitor.Value.Clear();
            }
        }

        public DesktopWindow FindWindow(HWND hWND)
        {
            List<DesktopWindow> desktopWindows = new List<DesktopWindow>();
            foreach (var desktopMonitor in Windows)
            {
                desktopWindows.AddRange(Windows[new Pair<VirtualDesktop, HMONITOR>(desktopMonitor.Key.Key, desktopMonitor.Key.Value)].Where(window => window.Window == hWND));
            }

            // TODO check how to handle null pointer
            var window = desktopWindows.FirstOrDefault();
            return window;
        }

        private void Windows_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action.Equals(NotifyCollectionChangedAction.Remove))
            {
                DesktopWindow desktopWindow = (DesktopWindow)e.OldItems[0];
            }
            else if (e.Action.Equals(NotifyCollectionChangedAction.Add))
            {
                DesktopWindow desktopWindow = (DesktopWindow)e.NewItems[0];

                if (desktopWindow.GetDesktopMonitor().Key.Equals(null) || desktopWindow.GetDesktopMonitor().Value.Equals(null))
                    desktopWindow.GetInfo();
            }
            if (!System.Windows.Application.Current.MainWindow.Equals(null))
            {
                _mainWindowViewModel.UpdateWindows();
            }
        }

        private DesktopWindow GetWindowByHandlers(HWND hWND, HMONITOR hMONITOR, VirtualDesktop desktop)
        {
            DesktopWindow? desktopWindow = null;
            try
            {
                desktopWindow = Windows[new Pair<VirtualDesktop, HMONITOR>(desktop, hMONITOR)].First(window => window.Window == hWND);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to GetWindowByHandler");
            }
            return desktopWindow;
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
            List<Pair<VirtualDesktop, HMONITOR>> desktopMonitors = Windows.Keys.Where(dM => dM.Key.ToString() == VirtualDesktop.Current.ToString()).ToList();
            int currentMonitorIndex = desktopMonitors.IndexOf(window.GetDesktopMonitor());
            int maxIndex = desktopMonitors.Count - 1;
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
            List<Pair<VirtualDesktop, HMONITOR>> desktopMonitors = Windows.Keys.Where(dM => dM.Key.ToString() == VirtualDesktop.Current.ToString()).ToList();
            int currentMonitorIndex = desktopMonitors.IndexOf(window.GetDesktopMonitor());
            int maxIndex = desktopMonitors.Count - 1;
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

        private void MoveWindowNextVirtualDesktop(DesktopWindow window)
        {
            VirtualDesktop nextVirtualDesktop = window.VirtualDesktop.GetRight();
            if (nextVirtualDesktop != null)
            {
                RemoveWindow(window);
                VirtualDesktop.MoveToDesktop(window.Window.DangerousGetHandle(), nextVirtualDesktop);
                window.VirtualDesktop = nextVirtualDesktop;
                AddWindow(window);
                nextVirtualDesktop.Switch();
            }
        }

        private void MoveWindowPreviousVirtualDesktop(DesktopWindow window)
        {
            VirtualDesktop nextVirtualDesktop = window.VirtualDesktop.GetLeft();
            if (nextVirtualDesktop != null)
            {
                RemoveWindow(window);
                VirtualDesktop.MoveToDesktop(window.Window.DangerousGetHandle(), nextVirtualDesktop);
                window.VirtualDesktop = nextVirtualDesktop;
                AddWindow(window);
                nextVirtualDesktop.Switch();
            }
        }

        private void MoveWindowSpecificVirtualDesktop(DesktopWindow window, Guid desktopGuid)
        {
            var nextVirtualDesktop = VirtualDesktop.FromId(desktopGuid);
            if (nextVirtualDesktop != null)
            {
                RemoveWindow(window);
                VirtualDesktop.MoveToDesktop(window.Window.DangerousGetHandle(), nextVirtualDesktop);
                window.VirtualDesktop = nextVirtualDesktop;
                AddWindow(window);
                nextVirtualDesktop.Switch();
            }
        }
    }
}
