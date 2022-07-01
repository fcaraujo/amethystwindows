using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Vanara.PInvoke;
using WindowsDesktop;

namespace AmethystWindows.Models
{
    public class ObservableDesktopMonitors : ObservableCollection<ViewModelDesktopMonitor>
    {
        public ObservableDesktopMonitors(List<ViewModelDesktopMonitor> list) : base(list)
        {
            foreach (ViewModelDesktopMonitor viewModelDesktopMonitor in list)
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

        private void ItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyCollectionChangedEventArgs args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, sender, sender);
            OnCollectionChanged(args);
        }

        public ViewModelDesktopMonitor this[Pair<VirtualDesktop, HMONITOR> desktopMonitor] => FindByDesktopMonitor(desktopMonitor);

        private ViewModelDesktopMonitor FindByDesktopMonitor(Pair<VirtualDesktop, HMONITOR> desktopMonitor)
        {
            return this.First(viewModelDesktopMonitor => viewModelDesktopMonitor.Monitor.Equals(desktopMonitor.Value) && viewModelDesktopMonitor.VirtualDesktop.Equals(desktopMonitor.Key));
        }
    }
}