using AmethystWindows.DependencyInjection;
using AmethystWindows.Models;
using AmethystWindows.Models.Configuration;
using AmethystWindows.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Windows;

namespace AmethystWindows
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App()
        {
            InitializeComponent();

            var configuration = ConfigurationHelper.GetRoot();
            var services = ConfigureServices(configuration);
            _serviceProvider = DIContainer.BuildProvider(services);
        }

        private static IServiceCollection ConfigureServices(IConfiguration? configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var services = new ServiceCollection();

            // var t = configuration.GetSection("FiltersOptions").Get<List<Dictionary<string, string>>>();

            // Configuration
            services.Configure<DevOptions>(
                configuration.GetSection(nameof(DevOptions))
            );
            services.Configure<List<FiltersOptions>>(
                configuration.GetSection(nameof(FiltersOptions))
            );
            services.Configure<List<HotkeyOptions>>(
                configuration.GetSection(nameof(HotkeyOptions))
            );
            services.Configure<SettingsOptions>(
                configuration.GetSection(nameof(SettingsOptions))
            );
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IWin32MessageService, Win32MessageService>();

            // Basic Logging
            // TODO load it from configuration
            var seqUrl = "http://localhost:5341/";
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Seq(seqUrl);

            var logger = loggerConfig.CreateLogger();
            logger.Information("Configure services and start up");

            services.AddTransient<ILogger>(x => logger);
            services.AddSingleton<IAutoStartService, AutoStartService>();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<IVirtualDesktopWrapper, VirtualDesktopWrapper>();
            services.AddSingleton<IVirtualDesktopService, VirtualDesktopService>();
            services.AddSingleton<IDesktopService, DesktopService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<HotkeyService>();

            return services;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Show WPF window
            var mainWindow = _serviceProvider.GetService<MainWindow>() ?? throw new ArgumentNullException(nameof(MainWindow));
            mainWindow.Show();

            // Handle hotkeys
            var hotkeyService = _serviceProvider.GetService<HotkeyService>() ?? throw new ArgumentNullException(nameof(HotkeyService));
            hotkeyService.SetWindowsHook();
            hotkeyService.RegisterHotkeys();

            // Handle virtual desktop/windows
            var desktopService = _serviceProvider.GetService<IDesktopService>() ?? throw new ArgumentNullException(nameof(DesktopService));
            desktopService.CollectWindows();

            var virtualDesktopService = _serviceProvider.GetService<IVirtualDesktopService>() ?? throw new ArgumentNullException(nameof(VirtualDesktopService));
            virtualDesktopService.SubscribeChangedEvent((_, args) =>
            {
                desktopService.CollectWindows();
                desktopService.Draw();
            });
            virtualDesktopService.SynchronizeSpaces();

            var autoStartService = _serviceProvider.GetService<IAutoStartService>() ?? throw new ArgumentNullException(nameof(AutoStartService));
            autoStartService.SetStartup();

            var notificationService = _serviceProvider.GetService<INotificationService>() ?? throw new ArgumentNullException(nameof(NotificationService));
            notificationService.Show(
                "Hello world!",
                "AmethystWindows is running minimized and it can be opened by clicking on its icon at the system task."
            );
        }
    }
}