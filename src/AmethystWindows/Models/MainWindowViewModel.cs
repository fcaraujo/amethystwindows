using AmethystWindows.DependencyInjection;
using AmethystWindows.Models.Configuration;
using AmethystWindows.Services;
using AmethystWindows.Settings;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Vanara.PInvoke;
using WindowsDesktop;

namespace AmethystWindows.Models
{
    public class MainWindowViewModel : ObservableRecipient
    {
        private NotifyRequestRecord? _notifyRequest;
        private bool _showInTaskbar;
        private WindowState _windowState;
        private List<ViewModelDesktopWindow> _windows;
        private List<ViewModelDesktopWindow> _excludedWindows;
        private ViewModelDesktopWindow? _selectedWindow;
        private ViewModelDesktopWindow? _selectedExcludedWindow;

        // Settings
        private bool _disabled;
        private int _layoutPadding;
        private int _padding;
        private int _marginTop;
        private int _marginRight;
        private int _marginBottom;
        private int _marginLeft;
        private int _step;
        private int _virtualDesktops;

        private ObservableHotkeys _hotkeys;
        private ICollection<FiltersOptions> _filters;
        private FiltersOptions? _selectedFilter;

        private List<Pair<string, string>> _configurableFilters;
        private Pair<string, string> _selectedConfigurableFilter;

        private List<Pair<string, string>> _configurableAdditions;
        private Pair<string, string> _selectedConfigurableAddition;

        private Pair<VirtualDesktop, HMONITOR> _lastChangedDesktopMonitor;
        private ObservableDesktopMonitors _desktopMonitors;

        private readonly ISettingsService _settingsService;
        private readonly ILogger _logger;

        public MainWindowViewModel(ILogger logger, ISettingsService settingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            // General Settings
            var settingsOptions = _settingsService.GetSettingsOptions();

            _disabled = settingsOptions.Disabled;
            _step = settingsOptions.Step;
            _virtualDesktops = settingsOptions.VirtualDesktops;
            _padding = settingsOptions.Padding;
            _layoutPadding = settingsOptions.LayoutPadding;
            _marginTop = settingsOptions.MarginTop;
            _marginLeft = settingsOptions.MarginLeft;
            _marginBottom = settingsOptions.MarginBottom;
            _marginRight = settingsOptions.MarginRight;

            // Other configurations
            var hotkeyOptions = _settingsService.GetHotkeyOptions();
            _hotkeys = new ObservableHotkeys(hotkeyOptions);
            _filters = _settingsService.GetFiltersOptions();



            // TODO move this settings into the service
            MySettings.Load();
            _configurableFilters = MySettings.Instance?.Filters ?? new();
            _configurableAdditions = MySettings.Instance?.Additions ?? new();
            _desktopMonitors = new ObservableDesktopMonitors(MySettings.Instance?.DesktopMonitors ?? new());




            // Prob this is the only view model props needed
            _windows = new List<ViewModelDesktopWindow>();
            _excludedWindows = new List<ViewModelDesktopWindow>();
            _desktopMonitors.CollectionChanged += _desktopMonitors_CollectionChanged;
            _lastChangedDesktopMonitor = new Pair<VirtualDesktop, HMONITOR>(null, new HMONITOR());


            // Commands
            LoadedCommand = new RelayCommand(Loaded);
            ClosingCommand = new RelayCommand<CancelEventArgs>(Closing);
            NotifyIconOpenCommand = new RelayCommand(() => { WindowState = WindowState.Normal; });
            NotifyIconExitCommand = new RelayCommand(() => { Application.Current.Shutdown(); });
            UpdateWindowsCommand = new RelayCommand(UpdateWindows);
            FilterAppCommand = new RelayCommand(FilterApp);
            FilterClassWithinAppCommand = new RelayCommand(FilterClassWithinApp);
            RemoveFilterCommand = new RelayCommand(RemoveFilter);
            AddAppCommand = new RelayCommand(AddApp);
            AddClassWithinAppCommand = new RelayCommand(AddClassWithinApp);
            RemoveAdditionCommand = new RelayCommand(RemoveAddition);
            RedrawCommand = new RelayCommand(Redraw);
        }

        private void Redraw()
        {
            // DWM is a circular dependency, let's think how to simplify this shit
            var dwm = DIContainer.GetService<IDesktopService>();
            dwm.Redraw();
        }

        private void _desktopMonitors_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var desktopMonitorViewModel = e.NewItems?.Count > 0
                ? e.NewItems[0]
                : null;

            if (desktopMonitorViewModel is null)
            {
                // TODO log warn?
                return;
            }

            var viewModelDesktopMonitor = (DesktopMonitorViewModel)desktopMonitorViewModel;
            LastChangedDesktopMonitor = viewModelDesktopMonitor.GetPair();
            OnPropertyChanged("DesktopMonitors");
        }

        public ICommand LoadedCommand { get; }
        public ICommand ClosingCommand { get; }
        public ICommand NotifyIconOpenCommand { get; }
        public ICommand NotifyIconExitCommand { get; }
        public ICommand UpdateWindowsCommand { get; }
        public ICommand FilterAppCommand { get; }
        public ICommand FilterClassWithinAppCommand { get; }
        public ICommand RemoveFilterCommand { get; }
        public ICommand AddAppCommand { get; }
        public ICommand AddClassWithinAppCommand { get; }
        public ICommand RemoveAdditionCommand { get; }
        public ICommand RedrawCommand { get; }
        public WindowState WindowState
        {
            get => _windowState;
            set
            {
                ShowInTaskbar = true;
                SetProperty(ref _windowState, value);
                ShowInTaskbar = value != WindowState.Minimized;
            }
        }

        public bool ShowInTaskbar
        {
            get => _showInTaskbar;
            set => SetProperty(ref _showInTaskbar, value);
        }

        public List<ViewModelDesktopWindow> Windows
        {
            get => _windows;
            set => SetProperty(ref _windows, value);
        }

        public List<ViewModelDesktopWindow> ExcludedWindows
        {
            get => _excludedWindows;
            set => SetProperty(ref _excludedWindows, value);
        }

        public ViewModelDesktopWindow? SelectedWindow
        {
            get => _selectedWindow;
            set => SetProperty(ref _selectedWindow, value);
        }

        public ViewModelDesktopWindow? SelectedExcludedWindow
        {
            get => _selectedExcludedWindow;
            set => SetProperty(ref _selectedExcludedWindow, value);
        }

        public int LayoutPadding
        {
            get => _layoutPadding;
            set => SetProperty(ref _layoutPadding, value);
        }

        public int Padding
        {
            get => _padding;
            set => SetProperty(ref _padding, value);
        }

        public int MarginTop
        {
            get => _marginTop;
            set => SetProperty(ref _marginTop, value);
        }

        public int MarginRight
        {
            get => _marginRight;
            set => SetProperty(ref _marginRight, value);
        }

        public int MarginBottom
        {
            get => _marginBottom;
            set => SetProperty(ref _marginBottom, value);
        }

        public int MarginLeft
        {
            get => _marginLeft;
            set => SetProperty(ref _marginLeft, value);
        }

        public int VirtualDesktops
        {
            get => _virtualDesktops;
            set => SetProperty(ref _virtualDesktops, value);
        }

        public int Step
        {
            get => _step;
            set => SetProperty(ref _step, value);
        }

        public bool Disabled
        {
            get => _disabled;
            set
            {
                _settingsService.SetDisabled(value);

                SetProperty(ref _disabled, value);
            }
        }

        public ObservableDesktopMonitors DesktopMonitors
        {
            get => _desktopMonitors;
            set => SetProperty(ref _desktopMonitors, value);
        }

        public ObservableHotkeys Hotkeys
        {
            get => _hotkeys;
            set => SetProperty(ref _hotkeys, value);
        }

        public Pair<VirtualDesktop, HMONITOR> LastChangedDesktopMonitor
        {
            get;
            set;
        }

        //public ICollection<FiltersOptions> Filters
        //{
        //    get => _filters;
        //    set => SetProperty(ref _filters, value);
        //}

        public List<Pair<string, string>> ConfigurableFilters
        {
            get => _configurableFilters;
            set => SetProperty(ref _configurableFilters, value);
        }

        public FiltersOptions SelectedFilter
        {
            get => _selectedFilter ?? throw new ArgumentNullException(nameof(_selectedFilter));
            set => SetProperty(ref _selectedFilter, value);
        }

        public Pair<string, string> SelectedConfigurableFilter
        {
            get => _selectedConfigurableFilter;
            set => SetProperty(ref _selectedConfigurableFilter, value);
        }

        public List<Pair<string, string>> ConfigurableAdditions
        {
            get => _configurableAdditions;
            set => SetProperty(ref _configurableAdditions, value);
        }

        public Pair<string, string> SelectedConfigurableAddition
        {
            get => _selectedConfigurableAddition;
            set => SetProperty(ref _selectedConfigurableAddition, value);
        }

        public NotifyRequestRecord? NotifyRequest
        {
            get => _notifyRequest;
            set => SetProperty(ref _notifyRequest, value);
        }

        public void Notify(string text, string title, int duration)
        {
            NotifyRequest = new NotifyRequestRecord
            {
                Title = title,
                Text = text,
                Duration = duration,
            };
        }

        public void UpdateWindows()
        {
            _logger.Debug("Performing {MainWindowViewModelMethod}.", nameof(UpdateWindows));

            // TODO use DI to get DWM
            var desktopService = DIContainer.GetService<IDesktopService>();

            var desktopWindows = desktopService.GetWindowsByVirtualDesktop(VirtualDesktop.Current);

            var windowsForComparison = desktopWindows
                .Select(window => new ViewModelDesktopWindow(window))
                .ToList();

            if (!windowsForComparison.SequenceEqual(Windows))
            {
                Windows = windowsForComparison;
            }
        }

        public void UpdateExcludedWindows()
        {
            // TODO use DI to get DWM
            var desktopService = DIContainer.GetService<IDesktopService>();

            var windowsForComparison = desktopService.ExcludedWindows
                .Select(window => new ViewModelDesktopWindow(
                    window.AppName,
                    window.ClassName
                ))
                .ToList();

            if (!windowsForComparison.SequenceEqual(ExcludedWindows))
            {
                ExcludedWindows = windowsForComparison;
            }
        }

        public void FilterApp()
        {
            //AddFilter(SelectedWindow.AppName, "*");

            if (SelectedWindow is null)
            {
                throw new ArgumentNullException();
            }
            ConfigurableFilters = ConfigurableFilters.Concat(new[] { new Pair<string, string>(SelectedWindow.AppName, "*") }).ToList();
        }

        public void FilterClassWithinApp()
        {
            //AddFilter(SelectedWindow.AppName, SelectedWindow.ClassName);

            if (SelectedWindow is null)
            {
                throw new ArgumentNullException();
            }
            ConfigurableFilters = ConfigurableFilters.Concat(new[] { new Pair<string, string>(SelectedWindow.AppName, SelectedWindow.ClassName) }).ToList();
        }

        private void AddFilter(string appName, string className)
        {
            var filter = new FiltersOptions
            {
                AppName = appName,
                ClassName = className,
            };
            //Filters.Add(filter);
            _settingsService.AddFilter(filter);
            _settingsService.Save();
            //Filters = _settingsService.GetFiltersOptions();
        }

        public void RemoveFilter()
        {
            //var filter = Filters.FirstOrDefault(f => f.AppName == SelectedFilter.AppName && f.ClassName == SelectedFilter.ClassName);
            //if (filter != null)
            //{
            //    Filters.Remove(filter);
            //    _settingsService.RemoveFilter(filter);
            //    _settingsService.Save();
            //}
            //Filters = _settingsService.GetFiltersOptions();

            ConfigurableFilters = ConfigurableFilters.Where(f => f.Key != SelectedConfigurableFilter.Key && f.Value != SelectedConfigurableFilter.Value).ToList();
        }

        public void AddApp()
        {
            if (SelectedExcludedWindow is null)
            {
                throw new ArgumentNullException();
            }

            ConfigurableAdditions = ConfigurableAdditions.Concat(new[] { new Pair<string, string>(SelectedExcludedWindow.AppName, "*") }).ToList();
        }

        public void AddClassWithinApp()
        {
            if (SelectedExcludedWindow is null)
            {
                throw new ArgumentNullException();
            }

            ConfigurableAdditions = ConfigurableAdditions.Concat(new[] { new Pair<string, string>(SelectedExcludedWindow.AppName, SelectedExcludedWindow.ClassName) }).ToList();
        }

        public void RemoveAddition()
        {
            ConfigurableAdditions = ConfigurableAdditions.Where(f => f.Key != SelectedConfigurableAddition.Key && f.Value != SelectedConfigurableAddition.Value).ToList();
        }

        private void Loaded()
        {
            WindowState = WindowState.Minimized;
        }

        private void Closing(CancelEventArgs? e)
        {
            _settingsService.Save();
            //MySettings.Save();
            if (e == null)
                return;
            e.Cancel = true;
            WindowState = WindowState.Minimized;
        }
    }
}
