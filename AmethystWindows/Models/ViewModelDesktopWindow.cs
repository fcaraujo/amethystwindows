using System;

namespace AmethystWindows.Models
{
    public class ViewModelDesktopWindow
    {
        public string AppName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Monitor { get; set; } = string.Empty;
        public string VirtualDesktop { get; set; } = string.Empty;
        public string Window { get; set; } = string.Empty;

        public ViewModelDesktopWindow(string appName, string className)
        {
            AppName = appName;
            ClassName = className;
        }

        public ViewModelDesktopWindow(string window, string appName, string className, string virtualDesktop, string monitor)
        {
            AppName = appName;
            ClassName = className;
            Monitor = monitor;
            VirtualDesktop = virtualDesktop;
            Window = window;
        }

        public ViewModelDesktopWindow(DesktopWindow desktopWindow)
        {
            AppName = desktopWindow.AppName;
            ClassName = desktopWindow.ClassName;
            Monitor = desktopWindow.Monitor.ToString() ?? "empty";
            VirtualDesktop = desktopWindow.VirtualDesktop.Id.ToString();
            Window = desktopWindow.Window.DangerousGetHandle().ToString();
        }

        public override bool Equals(object? obj)
        {
            return obj is ViewModelDesktopWindow window &&
                   Window == window.Window &&
                   AppName == window.AppName &&
                   ClassName == window.ClassName &&
                   VirtualDesktop == window.VirtualDesktop &&
                   Monitor == window.Monitor;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Window, AppName, ClassName, VirtualDesktop, Monitor);
        }
    }
}