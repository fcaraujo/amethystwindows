using AmethystWindows.Models.Configuration;
using AmethystWindows.Models.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AmethystWindows.Models
{
    /// <summary>
    /// Responsible to translate the settings options into a collection that's passed to the UI
    /// Probably there's no need to be an observable collection anymore as we're using the services to get/set
    /// </summary>
    public class ObservableHotkeys : ObservableCollection<HotkeyViewModel>
    {
        /// <summary>
        /// Used to create the collection from the settings service
        /// </summary>
        /// <param name="options"></param>
        public ObservableHotkeys(IEnumerable<HotkeyOptions> options)
        {
            foreach (var hotkey in options)
            {
                if (hotkey is null)
                {
                    throw new ArgumentNullException(nameof(hotkey));
                }

                var command = (hotkey.Command is not null)
                    ? (CommandHotkey)Enum.Parse(typeof(CommandHotkey), hotkey.Command)
                    : throw new ArgumentNullException(nameof(hotkey.Command));

                var keys = (!string.IsNullOrWhiteSpace(hotkey.Keys))
                    ? hotkey.Keys
                    : throw new ArgumentNullException(nameof(hotkey.Keys));

                var hotkeyViewModel = new HotkeyViewModel(command, keys);
                Items.Add(hotkeyViewModel);
            }
        }
    }
}