using AmethystWindows.Models.Configuration;
using AmethystWindows.Models.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace AmethystWindows.Models
{

    public class ObservableHotkeys : ObservableCollection<HotkeyViewModel>
    {
        public ObservableHotkeys(List<HotkeyViewModel> list) : base(list)
        {
            foreach (HotkeyViewModel viewModelHotkey in list)
            {
                viewModelHotkey.PropertyChanged += ItemPropertyChanged;
            }
            CollectionChanged += ObservableHotkeys_CollectionChanged;
        }

        public ObservableHotkeys()
        {
            CollectionChanged += ObservableHotkeys_CollectionChanged;
        }

        public ObservableHotkeys(IEnumerable<HotkeyOptions> options)
        {
            foreach (var hotkey in options)
            {
                var command = (CommandHotkey)Enum.Parse(typeof(CommandHotkey), hotkey.Command);
                var hotkeyViewModel = new HotkeyViewModel(command, hotkey.Keys);
                Items.Add(hotkeyViewModel);
            }
            // TODO does it need to bind handlers?
        }

        private void ObservableHotkeys_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
    }
}