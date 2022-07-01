using AmethystWindows.Models;
using AmethystWindows.Models.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using WindowsDesktop;

namespace AmethystWindows.DesktopWindowsManager
{
    partial class DesktopWindowsManager
    {
        public void Draw(Pair<VirtualDesktop, HMONITOR> key)
        {
            if (!_mainWindowViewModel.Disabled)
            {
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
        }

        public void Draw()
        {
            if (!_mainWindowViewModel.Disabled)
            {
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
                _mainWindowViewModel.DesktopMonitors.Add(new ViewModelDesktopMonitor(
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
    }
}
