using System;

namespace AmethystWindows.Models
{
    public class ViewModelDesktopWindow
    {
        public string Window { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string VirtualDesktop { get; set; } = string.Empty;
        public string Monitor { get; set; } = string.Empty;

        public ViewModelDesktopWindow(string appName, string className)
        {
            AppName = appName;
            ClassName = className;
        }

        public ViewModelDesktopWindow(string window, string appName, string className, string virtualDesktop, string monitor)
        {
            Window = window;
            AppName = appName;
            ClassName = className;
            VirtualDesktop = virtualDesktop;
            Monitor = monitor;
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