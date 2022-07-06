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

        public void Dispatch(CommandHotkey command)
        {
            // TODO prob consider moving into its own service decoupled from this view model
            switch (command)
            {
                case CommandHotkey.RotateLayoutClockwise:
                    this.RotateLayoutClockwise();
                    break;
                case CommandHotkey.RotateLayoutAntiClockwise:
                    this.RotateLayoutAntiClockwise();
                    break;
                case CommandHotkey.ExpandMainPane:
                    this.Expand();
                    break;
                case CommandHotkey.Shrink:
                    this.Shrink();
                    break;
                default:
                    break;
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
        private void Shrink()
        {
            --Factor;
        }

        private void Expand()
        {
            ++Factor;
        }

        private void RotateLayoutClockwise()
        {
            var layouts = Enum.GetValues(typeof(Layout)).Cast<Layout>();
            if (Layout == layouts.Max())
            {
                Layout = Layout.Horizontal;
            }
            else
            {
                ++Layout;
            }
        }

        private void RotateLayoutAntiClockwise()
        {
            if (Layout == 0)
            {
                Layout = Layout.Tall;
            }
            else
            {
                --Layout;
            }
        }
    }
}