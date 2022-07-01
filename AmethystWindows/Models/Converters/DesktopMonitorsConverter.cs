using AmethystWindows.Models;
using AmethystWindows.Models.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using WindowsDesktop;

namespace AmethystWindows.Models.Converters
{
    public class DesktopMonitorsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(List<ViewModelDesktopMonitor>);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var list = value as List<ViewModelDesktopMonitor>;

            writer.WriteStartArray();
            foreach (ViewModelDesktopMonitor desktopMonitor in list)
            {
                User32.MONITORINFO info = new User32.MONITORINFO();
                info.cbSize = (uint)Marshal.SizeOf(info);
                User32.GetMonitorInfo(desktopMonitor.Monitor, ref info);

                writer.WriteStartObject();
                writer.WritePropertyName("DesktopID");
                writer.WriteValue(desktopMonitor.VirtualDesktop.Id);
                writer.WritePropertyName("MonitorX");
                writer.WriteValue(info.rcMonitor.X);
                writer.WritePropertyName("MonitorY");
                writer.WriteValue(info.rcMonitor.Y);
                writer.WritePropertyName("Layout");
                writer.WriteValue(desktopMonitor.Layout);
                writer.WritePropertyName("Factor");
                writer.WriteValue(desktopMonitor.Factor);
                writer.WriteEndObject();

            }
            writer.WriteEndArray();

        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            List<ViewModelDesktopMonitor> list = new List<ViewModelDesktopMonitor>();

            JArray array = JArray.Load(reader);

            foreach (JObject desktopMonitor in array.Children())
            {
                try
                {
                    Layout layout = (Layout)desktopMonitor.GetValue("Layout").Value<int>();
                    int factor = desktopMonitor.GetValue("Factor").Value<int>();

                    Point point = new Point(desktopMonitor.GetValue("MonitorX").Value<int>() + 100, desktopMonitor.GetValue("MonitorY").Value<int>() + 100);
                    HMONITOR monitor = User32.MonitorFromPoint(point, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);

                    VirtualDesktop savedDesktop = VirtualDesktop.GetDesktops().First(vD => vD.Id.Equals(new Guid(desktopMonitor.GetValue("DesktopID").Value<string>())));
                    HMONITOR savedMonitor = monitor;

                    list.Add(new ViewModelDesktopMonitor(monitor, savedDesktop, factor, layout));
                }
                catch
                {
                    Debug.WriteLine("WARNING: something was wrong in reloading your settings. Most probably monitor or virtual desktop do not exist anymore.");
                }
            }

            return list;
        }
    }
}
