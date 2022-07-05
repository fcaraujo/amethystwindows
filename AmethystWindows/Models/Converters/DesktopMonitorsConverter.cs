using AmethystWindows.DependencyInjection;
using AmethystWindows.Models.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using WindowsDesktop;

namespace AmethystWindows.Models.Converters
{
    public class DesktopMonitorsConverter : JsonConverter
    {
        // Constants used for store/convert
        private const string DESKTOPID = "DesktopID";
        private const string FACTOR = "Factor";
        private const string MONITORX = "MonitorX";
        private const string MONITORY = "MonitorY";

        private readonly ILogger _logger;

        public DesktopMonitorsConverter()
        {
            _logger = DIContainer.GetService<ILogger>() ?? throw new ArgumentNullException(nameof(_logger));
        }

        public override bool CanConvert(Type objectType)
            => objectType == typeof(List<DesktopMonitorViewModel>);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var list = value as List<DesktopMonitorViewModel>
                ?? new();

            writer.WriteStartArray();
            foreach (DesktopMonitorViewModel desktopMonitor in list)
            {
                var info = new User32.MONITORINFO();
                info.cbSize = (uint)Marshal.SizeOf(info);
                User32.GetMonitorInfo(desktopMonitor.Monitor, ref info);

                writer.WriteStartObject();

                writer.WritePropertyName(DESKTOPID);
                writer.WriteValue(desktopMonitor.VirtualDesktop?.Id);

                writer.WritePropertyName(MONITORX);
                writer.WriteValue(info.rcMonitor.X);

                writer.WritePropertyName(MONITORY);
                writer.WriteValue(info.rcMonitor.Y);

                writer.WritePropertyName(nameof(Layout));
                writer.WriteValue(desktopMonitor.Layout);

                writer.WritePropertyName(FACTOR);
                writer.WriteValue(desktopMonitor.Factor);

                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var list = new List<DesktopMonitorViewModel>()
                ?? new();

            var array = JArray.Load(reader);

            foreach (JObject desktopMonitor in array.Children())
            {
                try
                {
                    if (desktopMonitor is null)
                    {
                        throw new ArgumentNullException();
                    }

                    var layoutJToken = desktopMonitor.GetValue(nameof(Layout));
                    var layout = layoutJToken is not null
                        ? (Layout)layoutJToken.Value<int>()
                        : default;

                    var factorJToken = desktopMonitor.GetValue(FACTOR);
                    var factor = factorJToken is not null
                        ? factorJToken.Value<int>()
                        : default;

                    var monitorXJToken = desktopMonitor.GetValue(MONITORX);
                    var monitorX = monitorXJToken is not null
                        ? monitorXJToken.Value<int>()
                        : default;

                    var monitorYJToken = desktopMonitor.GetValue(MONITORY);
                    var monitorY = monitorYJToken is not null
                        ? monitorYJToken.Value<int>()
                        : default;

                    var point = new Point(monitorX + 100, monitorY + 100);
                    var monitor = User32.MonitorFromPoint(point, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);

                    var desktopIdJToken = desktopMonitor.GetValue(DESKTOPID);
                    var desktopId = desktopIdJToken is not null
                        ? desktopIdJToken.Value<string>()
                        : default;
                    var desktopGuid = new Guid(desktopId ?? "empty");
                    var savedDesktop = VirtualDesktop.GetDesktops()
                        .First(vD => vD.Id.Equals(desktopGuid));

                    // TODO check if i's required somewhere? 
                    // var savedMonitor = monitor;

                    list.Add(new DesktopMonitorViewModel(monitor, savedDesktop, factor, layout));
                }
                catch (Exception ex)
                {
                    const string ExMessage = $"Failed to convert {nameof(DesktopMonitorViewModel)} reloading " +
                        $"from settings. Most probably monitor or virtual desktop do not exist anymore.";

                    _logger.Error(ex, ExMessage);
                }
            }

            return list;
        }
    }
}
