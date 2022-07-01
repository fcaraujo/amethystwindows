using System;

namespace AmethystWindows.Models
{
    public class ViewModelDesktopWindow
    {
        public string Window { get; set; }
        public string AppName { get; set; }
        public string ClassName { get; set; }
        public string VirtualDesktop { get; set; }
        public string Monitor { get; set; }

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