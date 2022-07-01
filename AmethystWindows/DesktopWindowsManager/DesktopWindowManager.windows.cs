using AmethystWindows.Models.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Vanara.PInvoke;
using WindowsDesktop;

namespace AmethystWindows.DesktopWindowsManager
{
    // consider public method to be private if the actions are controlled by Dispatch...
    // TODO extract interface when only public methods are exposed
    partial class DesktopWindowsManager
    {
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
                && desktopWindow.AppName != String.Empty &&
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
            return desktopWindows.FirstOrDefault();
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
            if (!App.Current.MainWindow.Equals(null))
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
