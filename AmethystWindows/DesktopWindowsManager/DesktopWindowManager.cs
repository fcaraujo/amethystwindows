using AmethystWindows.Models;
using AmethystWindows.Services;
using AmethystWindows.Settings;
using DebounceThrottle;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Vanara.PInvoke;
using WindowsDesktop;

[assembly: InternalsVisibleTo("AmethystWindowsTests")]
namespace AmethystWindows.DesktopWindowsManager
{
    public partial class DesktopWindowsManager
    {
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;

        // TODO turn this private and restrict its access?
        public readonly MainWindowViewModel _mainWindowViewModel;

        public Dictionary<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>> Windows { get; }
        public Dictionary<Pair<VirtualDesktop, HMONITOR>, bool> WindowsSubscribed = new Dictionary<Pair<VirtualDesktop, HMONITOR>, bool>();

        public ObservableCollection<DesktopWindow> ExcludedWindows { get; }

        private DebounceDispatcher debounceDispatcher = new DebounceDispatcher(100);

        private readonly string[] FixedFilters = new string[] {
            "AmethystWindows",
            "AmethystWindowsPackaging",
            "Cortana",
            "Microsoft Spy++",
            "Task Manager",
        };

        public readonly string[] FixedExcludedFilters = new string[] {
            "Settings",
        };

        private readonly string[] ModelViewPropertiesDraw = new string[] {
            "Padding",
            "LayoutPadding",
            "MarginTop",
            "MarginRight",
            "MarginBottom",
            "MarginLeft",
            "ConfigurableFilters",
            "ConfigurableAdditions",
            "DesktopMonitors",
            "Windows",
        };

        private readonly string[] ModelViewPropertiesDrawMonitor = new string[] {
            "DesktopMonitors",
            "Windows",
        };

        private readonly string[] ModelViewPropertiesSaveSettings = new string[] {
            "Padding",
            "LayoutPadding",
            "MarginTop",
            "MarginRight",
            "MarginBottom",
            "MarginLeft",
            "VirtualDesktops",
            "DesktopMonitors",
            "Additions",
            "Filters",
            "Hotkeys",
        };

        public DesktopWindowsManager(ILogger logger, ISettingsService settingsService, MainWindowViewModel amainWindowViewModel)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _mainWindowViewModel = amainWindowViewModel ?? throw new ArgumentNullException(nameof(_mainWindowViewModel));

            Windows = new Dictionary<Pair<VirtualDesktop, HMONITOR>, ObservableCollection<DesktopWindow>>();
            ExcludedWindows = new ObservableCollection<DesktopWindow>();

            ExcludedWindows.CollectionChanged += ExcludedWindows_CollectionChanged;
            _mainWindowViewModel.PropertyChanged += MainWindowViewModel_PropertyChanged;
        }

        private void ExcludedWindows_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!System.Windows.Application.Current.MainWindow.Equals(null))
            {
                _mainWindowViewModel.UpdateExcludedWindows();
            }
        }

        private void MainWindowViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            _logger.Debug($"ModelViewChanged: {e.PropertyName}");

            if (e.PropertyName == "VirtualDesktops")
            {
                // TODO check how to use DI here...?
                App.InitVirtualDesktops();
            }

            // TODO check if saving hotkeys on the event is enough
            //if (e.PropertyName == "Hotkeys")
            //{
            //    // TODO add dependency of HooksService in this class retrieve it
            //    var _hooksService = IocProvider.GetService<HooksService>();
            //    _hooksService.ClearHotkeys();
            //    _hooksService.SetKeyboardHook();

            //    _settingsService.SetHotkeyOptions(mainWindowViewModel);
            //    _settingsService.Save();
            //}

            if (ModelViewPropertiesSaveSettings.Contains(e.PropertyName))
            {
                // TODO handle
                MySettings.Instance.Filters = _mainWindowViewModel.ConfigurableFilters;
                MySettings.Instance.Additions = _mainWindowViewModel.ConfigurableAdditions;
                MySettings.Instance.DesktopMonitors = _mainWindowViewModel.DesktopMonitors.ToList();

                MySettings.Save();


                _settingsService.SetSettingsOptions(_mainWindowViewModel);

                // TODO check it's been called every layout rotation?
                _settingsService.Save();
            }

            if (e.PropertyName == "ConfigurableFilters" || e.PropertyName == "ConfigurableAdditions")
            {
                ClearWindows();
                CollectWindows();
            }

            if (ModelViewPropertiesDraw.Contains(e.PropertyName))
            {
                if (ModelViewPropertiesDrawMonitor.Contains(e.PropertyName) && _mainWindowViewModel.LastChangedDesktopMonitor.Key != null) debounceDispatcher.Debounce(() => Draw(_mainWindowViewModel.LastChangedDesktopMonitor));
                else debounceDispatcher.Debounce(() => Draw());
            }
        }

        private void RotateMonitorClockwise(Pair<VirtualDesktop, HMONITOR> currentDesktopMonitor)
        {
            List<HMONITOR> virtualDesktopMonitors = Windows
                .Keys
                .Where(desktopMonitor => desktopMonitor.Key.Equals(currentDesktopMonitor.Key))
                .Select(desktopMonitor => desktopMonitor.Value)
                .ToList();

            HMONITOR nextMonitor = virtualDesktopMonitors.SkipWhile(x => x != currentDesktopMonitor.Value).Skip(1).DefaultIfEmpty(virtualDesktopMonitors[0]).FirstOrDefault();
            Pair<VirtualDesktop, HMONITOR> nextDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(currentDesktopMonitor.Key, nextMonitor);

            User32.SetForegroundWindow(Windows[nextDesktopMonitor].FirstOrDefault().Window);
        }

        private void RotateMonitorCounterClockwise(Pair<VirtualDesktop, HMONITOR> currentDesktopMonitor)
        {
            List<HMONITOR> virtualDesktopMonitors = Windows
                .Keys
                .Where(desktopMonitor => desktopMonitor.Key.Equals(currentDesktopMonitor.Key))
                .Select(desktopMonitor => desktopMonitor.Value)
                .ToList();

            HMONITOR nextMonitor = virtualDesktopMonitors.TakeWhile(x => x != currentDesktopMonitor.Value).Skip(1).DefaultIfEmpty(virtualDesktopMonitors[0]).FirstOrDefault();
            Pair<VirtualDesktop, HMONITOR> nextDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(currentDesktopMonitor.Key, nextMonitor);

            User32.SetForegroundWindow(Windows[nextDesktopMonitor].FirstOrDefault().Window);
        }

        private void SubscribeWindowsCollectionChanged(Pair<VirtualDesktop, HMONITOR> desktopMonitor, bool enabled)
        {
            if (!enabled)
                Windows[desktopMonitor].CollectionChanged -= Windows_CollectionChanged;
            else if (!WindowsSubscribed[desktopMonitor])
                Windows[desktopMonitor].CollectionChanged += Windows_CollectionChanged;

            WindowsSubscribed[desktopMonitor] = enabled;
        }

    }
}
