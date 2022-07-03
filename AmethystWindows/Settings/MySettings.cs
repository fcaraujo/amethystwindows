using AmethystWindows.Models;
using AmethystWindows.Models.Converters;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AmethystWindows.Settings
{
    public class MySettings : SettingsManager<MySettings>
    {
        // TODO move these to settings service later...
        public List<Pair<string, string>> Filters { get; set; } = new List<Pair<string, string>>();

        public List<Pair<string, string>> Additions { get; set; } = new List<Pair<string, string>>();

        [JsonConverter(typeof(DesktopMonitorsConverter))]
        public List<DesktopMonitorViewModel> DesktopMonitors = new List<DesktopMonitorViewModel>();
    }
}
