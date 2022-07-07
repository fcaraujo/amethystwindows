using System.Collections.Generic;

namespace AmethystWindows.Models.Configuration
{
    public class AppSettings
    {
        public DevOptions DevOptions { get; set; } = new();
        public IEnumerable<FiltersOptions?>? FiltersOptions { get; set; }
        public IEnumerable<HotkeyOptions?> HotkeyOptions { get; set; } = DefaultOptions.Hotkeys;
        public SettingsOptions SettingsOptions { get; set; } = new();
    }
}