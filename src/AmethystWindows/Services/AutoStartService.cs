using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using Serilog;

namespace AmethystWindows.Services
{
    /// <summary>
    /// Responsible for adding the registry key/value pair to run in windows' startup
    /// </summary>
    public interface IAutoStartService
    {
        void SetStartup();
    }

    // TODO consider adding the registry key/pair in the setup process?
    public class AutoStartService : IAutoStartService
    {
        private const string KeyName = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string ValueName = "AmethystWindows.exe";
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;

        public AutoStartService(ILogger logger, ISettingsService settingsService)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        public void SetStartup()
        {
            var devOptions = _settingsService.GetDevOptions();
            if (!devOptions.RunAtStartup)
            {
                _logger.Debug("Skipping set program to run at startup.");
                return;
            }

            // TODO check elevated/write permissions to add it to the registry
            using (var key = Registry.CurrentUser.OpenSubKey(KeyName, true))
            {
                var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                    ?? throw new ArgumentNullException();
                var location = Path.Combine(assemblyDir, ValueName);
                var locationStr = $"\"{location}\"";

#if !DEBUG
                _logger.Debug("Adding assembly {RegValue} to {RegKeyName} registry.", locationStr, KeyName);
                key?.SetValue(ValueName, locationStr);
#endif
            }
        }
    }
}