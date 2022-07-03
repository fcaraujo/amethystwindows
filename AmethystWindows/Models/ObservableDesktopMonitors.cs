using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Vanara.PInvoke;
using WindowsDesktop;

namespace AmethystWindows.Models
{
    public class ObservableDesktopMonitors : ObservableCollection<DesktopMonitorViewModel>
    {
        public ObservableDesktopMonitors(List<DesktopMonitorViewModel> list) : base(list)
        {
            foreach (DesktopMonitorViewModel viewModelDesktopMonitor in list)
            {
                viewModelDesktopMonitor.PropertyChanged += ItemPropertyChanged;
            }
            CollectionChanged += ObservableDesktopMonitors_CollectionChanged;
        }

        public ObservableDesktopMonitors()
        {
            CollectionChanged += ObservableDesktopMonitors_CollectionChanged;
        }

        private void ObservableDesktopMonitors_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (object item in e.NewItems)
                {
                    ((INotifyPropertyChanged)item).PropertyChanged += ItemPropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (object item in e.OldItems)
                {
                    ((INotifyPropertyChanged)item).PropertyChanged -= ItemPropertyChanged;
                }
            }
        }

        private void ItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender);
            OnCollectionChanged(args);
        }

        public DesktopMonitorViewModel this[Pair<VirtualDesktop, HMONITOR> desktopMonitor] => FindByDesktopMonitor(desktopMonitor);

        private DesktopMonitorViewModel FindByDesktopMonitor(Pair<VirtualDesktop, HMONITOR> desktopMonitor)
        {
            return this.First(viewModelDesktopMonitor =>
            {
                var virtualDesktop = viewModelDesktopMonitor.VirtualDesktop;

                if (virtualDesktop is null)
                {
                    return false;
                }

                return viewModelDesktopMonitor.Monitor.Equals(desktopMonitor.Value) && virtualDesktop.Equals(desktopMonitor.Key);
            });
        }
    }
}