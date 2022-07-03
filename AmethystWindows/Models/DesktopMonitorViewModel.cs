using AmethystWindows.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Vanara.PInvoke;
using WindowsDesktop;

namespace AmethystWindows.Models
{
    public class DesktopMonitorViewModel : INotifyPropertyChanged
    {
        private HMONITOR _monitor;
        private VirtualDesktop? _virtualDesktop;
        private int _factor;
        private Layout _layout;

        public DesktopMonitorViewModel(HMONITOR monitor, VirtualDesktop virtualDesktop, int factor, Layout layout)
        {
            Monitor = monitor;
            VirtualDesktop = virtualDesktop;
            Factor = factor;
            Layout = layout;
        }

        public HMONITOR Monitor
        {
            get => _monitor;

            set
            {
                _monitor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Monitor)));
            }
        }

        public VirtualDesktop? VirtualDesktop
        {
            get => _virtualDesktop;

            set
            {
                _virtualDesktop = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VirtualDesktop)));
            }
        }

        public int Factor
        {
            get => _factor;

            set
            {
                _factor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Factor)));
            }
        }

        public Layout Layout
        {
            get => _layout;

            set
            {
                _layout = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Layout)));
            }
        }



        public event PropertyChangedEventHandler? PropertyChanged;

        public void Shrink()
        {
            --Factor;
        }

        public void Expand()
        {
            ++Factor;
        }

        public void RotateLayoutClockwise()
        {
            IEnumerable<Layout> values = Enum.GetValues(typeof(Layout)).Cast<Layout>();
            if (Layout == values.Max())
            {
                Layout = Layout.Horizontal;
            }
            else
            {
                ++Layout;
            }
        }

        public void RotateLayoutAntiClockwise()
        {
            IEnumerable<Layout> values = Enum.GetValues(typeof(Layout)).Cast<Layout>();
            if (Layout == 0)
            {
                Layout = Layout.Tall;
            }
            else
            {
                --Layout;
            }
        }

        public override bool Equals(object? obj)
        {
            return obj is DesktopMonitorViewModel monitor &&
                   EqualityComparer<HMONITOR>.Default.Equals(Monitor, monitor.Monitor) &&
                   EqualityComparer<VirtualDesktop>.Default.Equals(VirtualDesktop, monitor.VirtualDesktop) &&
                   Factor == monitor.Factor &&
                   Layout == monitor.Layout;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Monitor, VirtualDesktop, Factor, Layout);
        }

        public Pair<VirtualDesktop, HMONITOR> GetPair()
        {
            return new Pair<VirtualDesktop, HMONITOR>(VirtualDesktop, Monitor);
        }
    }
}