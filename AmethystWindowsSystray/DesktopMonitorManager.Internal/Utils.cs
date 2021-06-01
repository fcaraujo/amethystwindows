﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using WindowsDesktop;

namespace DesktopMonitorManager.Internal
{
    public struct MarginPaddingStruct
    {
        public MarginPaddingStruct(int marginTop, int marginBottom, int marginLeft, int marginRight, int layoutPadding, int windowPadding)
        {
            MarginTop = marginTop;
            MarginBottom = marginBottom;
            MarginLeft = marginLeft;
            MarginRight = marginRight;
            LayoutPadding = layoutPadding;
            WindowPadding = windowPadding;
        }

        public int MarginTop { get; }
        public int MarginBottom { get; }
        public int MarginLeft { get; }
        public int MarginRight { get; }
        public int LayoutPadding { get; }
        public int WindowPadding { get; }
    }

    public enum Layout : ushort
    {
        Horizontal = 0,
        Vertical = 1,
        HorizGrid = 2,
        VertGrid = 3,
        Monocle = 4,
        Wide = 5,
        Tall = 6
    }

    public struct Pair<K, V>
    {
        public K Item1 { get; set; }
        public V Item2 { get; set; }

        public Pair(K item1, V item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public override bool Equals(object obj)
        {
            return obj is Pair<K, V> pair &&
                   EqualityComparer<K>.Default.Equals(Item1, pair.Item1) &&
                   EqualityComparer<V>.Default.Equals(Item2, pair.Item2);
        }

        public override int GetHashCode()
        {
            int hashCode = -1030903623;
            hashCode = hashCode * -1521134295 + EqualityComparer<K>.Default.GetHashCode(Item1);
            hashCode = hashCode * -1521134295 + EqualityComparer<V>.Default.GetHashCode(Item2);
            return hashCode;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    public class DesktopMonitorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(DesktopMonitor);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var desktopMonitor = value as DesktopMonitor;

            writer.WriteStartObject();
            writer.WritePropertyName("Desktop");
            writer.WriteValue(VirtualDesktop.DesktopNameFromDesktop(desktopMonitor.VirtualDesktop));
            writer.WritePropertyName("MonitorX");
            writer.WriteValue(desktopMonitor.MonitorInfo.rcMonitor.X);
            writer.WritePropertyName("MonitorY");
            writer.WriteValue(desktopMonitor.MonitorInfo.rcMonitor.Y);
            writer.WritePropertyName("Layout");
            writer.WriteValue((int)desktopMonitor.Layout);
            writer.WritePropertyName("Factor");
            writer.WriteValue((int)desktopMonitor.Factor);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            DesktopMonitor desktopMonitor;
            JObject obj = JObject.Load(reader);

            int virtualDesktopIndex = VirtualDesktop.SearchDesktop(obj["Desktop"].ToString());

            if (virtualDesktopIndex != -1)
            {
                Point point = new Point(obj["MonitorX"].ToObject<int>() + 100, obj["MonitorY"].ToObject<int>() + 100);
                HMONITOR monitor = User32.MonitorFromPoint(point, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
                VirtualDesktop virtualDesktop = VirtualDesktop.FromIndex(virtualDesktopIndex);
                Layout layout = (Layout)obj["Layout"].ToObject<Layout>();
                int factor = (int)obj["Factor"].ToObject<int>();

                desktopMonitor = new DesktopMonitor(layout, factor, monitor, virtualDesktop);

                return desktopMonitor;
            }

            return null;     
        }
    }

    //public class LayoutsConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType) => objectType == typeof(List<KeyValuePair<Pair<VirtualDesktop, HMONITOR>, Layout>>);

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        var list = value as List<KeyValuePair<Pair<VirtualDesktop, HMONITOR>, Layout>>;

    //        writer.WriteStartArray();
    //        foreach (KeyValuePair<Pair<VirtualDesktop, HMONITOR>, Layout> pair in list)
    //        {
    //            User32.MONITORINFOEX info = new User32.MONITORINFOEX();
    //            info.cbSize = (uint)Marshal.SizeOf(info);
    //            User32.GetMonitorInfo(pair.Key.Item2, ref info);

    //            writer.WriteStartObject();
    //            writer.WritePropertyName("Desktop");
    //            writer.WriteValue(VirtualDesktop.DesktopNameFromDesktop(pair.Key.Item1));
    //            writer.WritePropertyName("MonitorX");
    //            writer.WriteValue(info.rcMonitor.X);
    //            writer.WritePropertyName("MonitorY");
    //            writer.WriteValue(info.rcMonitor.Y);
    //            writer.WritePropertyName("Layout");
    //            writer.WriteValue((int)pair.Value);
    //            writer.WriteEndObject();
    //        }
    //        writer.WriteEndArray();

    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        List<KeyValuePair<Pair<VirtualDesktop, HMONITOR>, Layout>> list = new List<KeyValuePair<Pair<VirtualDesktop, HMONITOR>, Layout>>();

    //        JArray array = JArray.Load(reader);

    //        VirtualDesktop virtualDesktop = null;
    //        HMONITOR hMONITOR = HMONITOR.NULL;
    //        Layout layout = Layout.Tall;

    //        foreach (JObject desktopMonitor in array.Children())
    //        {
    //            var properties = desktopMonitor.Properties().ToList();
    //            Point point = new Point(properties[1].Value.ToObject<int>() + 100, properties[2].Value.ToObject<int>() + 100);
    //            HMONITOR monitor = User32.MonitorFromPoint(point, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);

    //            int virtualDesktopIndex = VirtualDesktop.SearchDesktop(properties[0].Value.ToString());

    //            if (virtualDesktopIndex != -1)
    //            {
    //                virtualDesktop = VirtualDesktop.FromIndex(virtualDesktopIndex);
    //                hMONITOR = monitor;
    //                layout = (Layout)properties[3].Value.ToObject<int>();

    //                var key = new Pair<VirtualDesktop, HMONITOR>(virtualDesktop, hMONITOR);
    //                list.Add(new KeyValuePair<Pair<VirtualDesktop, HMONITOR>, Layout>(key, layout));
    //            }
    //        }

    //        return list;
    //    }
    //}

    //public class FactorsConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType) => objectType == typeof(List<KeyValuePair<Pair<VirtualDesktop, HMONITOR>, int>>);

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        var list = value as List<KeyValuePair<Pair<VirtualDesktop, HMONITOR>, int>>;

    //        writer.WriteStartArray();
    //        foreach (KeyValuePair<Pair<VirtualDesktop, HMONITOR>, int> pair in list)
    //        {
    //            User32.MONITORINFOEX info = new User32.MONITORINFOEX();
    //            info.cbSize = (uint)Marshal.SizeOf(info);
    //            User32.GetMonitorInfo(pair.Key.Item2, ref info);

    //            writer.WriteStartObject();
    //            writer.WritePropertyName("Desktop");
    //            writer.WriteValue(VirtualDesktop.DesktopNameFromDesktop(pair.Key.Item1));
    //            writer.WritePropertyName("MonitorX");
    //            writer.WriteValue(info.rcMonitor.X);
    //            writer.WritePropertyName("MonitorY");
    //            writer.WriteValue(info.rcMonitor.Y);
    //            writer.WritePropertyName("Factor");
    //            writer.WriteValue(pair.Value);
    //            writer.WriteEndObject();
    //        }
    //        writer.WriteEndArray();

    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        List<KeyValuePair<Pair<VirtualDesktop, HMONITOR>, int>> list = new List<KeyValuePair<Pair<VirtualDesktop, HMONITOR>, int>>();

    //        JArray array = JArray.Load(reader);

    //        VirtualDesktop virtualDesktop = null;
    //        HMONITOR hMONITOR = HMONITOR.NULL;
    //        int factor = 0;

    //        foreach (JObject desktopMonitor in array.Children())
    //        {
    //            var properties = desktopMonitor.Properties().ToList();
    //            Point point = new Point(properties[1].Value.ToObject<int>() + 100, properties[2].Value.ToObject<int>() + 100);
    //            HMONITOR monitor = User32.MonitorFromPoint(point, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);

    //            int virtualDesktopIndex = VirtualDesktop.SearchDesktop(properties[0].Value.ToString());

    //            if (virtualDesktopIndex != -1)
    //            {
    //                virtualDesktop = VirtualDesktop.FromIndex(virtualDesktopIndex);
    //                hMONITOR = monitor;
    //                factor = properties[3].Value.ToObject<int>();

    //                var key = new Pair<VirtualDesktop, HMONITOR>(virtualDesktop, hMONITOR);
    //                list.Add(new KeyValuePair<Pair<VirtualDesktop, HMONITOR>, int>(key, factor));
    //            }
    //        }

    //        return list;
    //    }
    //}
}
