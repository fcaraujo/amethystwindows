using System.Collections.Generic;

namespace AmethystWindows.Models.Configuration
{
    public class AppSettings
    {
        public DevOptions? DevOptions { get; set; }
        public IEnumerable<FiltersOptions?>? FiltersOptions { get; set; }
        public IEnumerable<HotkeyOptions?>? HotkeyOptions { get; set; }
        public SettingsOptions? SettingsOptions { get; set; }
    }
}