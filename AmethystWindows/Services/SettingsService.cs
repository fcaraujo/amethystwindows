using AmethystWindows.Models;
using AmethystWindows.Models.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AmethystWindows.Services
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// 
        /// </summary>
        DevOptions GetDevOptions();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerable<HotkeyOptions> GetHotkeyOptions();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        SettingsOptions GetSettingsOptions();

        /// <summary>
        /// 
        /// </summary>
        void Save();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mainWindowViewModel"></param>
        void SetSettingsOptions(MainWindowViewModel mainWindowViewModel);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        void SetDisabled(bool value);

        void SetHotkeyOptions(ObservableHotkeys x);
        void SetHotkey(HotkeyViewModel x01);

        ICollection<FiltersOptions> GetFiltersOptions();
        void AddFilter(FiltersOptions filter);
        void RemoveFilter(FiltersOptions filter);
    }

    public class SettingsService : ISettingsService
    {
        private readonly ILogger _logger;
        private readonly IOptionsMonitor<DevOptions> _devOptions;
        private readonly IOptionsMonitor<ICollection<FiltersOptions>> _filtersOptions;
        private readonly IOptionsMonitor<IEnumerable<HotkeyOptions>> _hotkeyOptions;
        private readonly IOptionsMonitor<SettingsOptions> _settingsOptions;

        private readonly ICollection<FiltersOptions> _filters;
        private readonly ICollection<HotkeyOptions> _hotkeys;

        public SettingsService(ILogger logger, IOptionsMonitor<DevOptions> devOptions, IOptionsMonitor<List<FiltersOptions>> filtersOptions, IOptionsMonitor<SettingsOptions> settingsOptions, IOptionsMonitor<List<HotkeyOptions>> hotkeyOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _devOptions = devOptions ?? throw new ArgumentNullException(nameof(devOptions));
            _hotkeyOptions = hotkeyOptions ?? throw new ArgumentNullException(nameof(hotkeyOptions));
            _settingsOptions = settingsOptions ?? throw new ArgumentNullException(nameof(settingsOptions));
            _filtersOptions = filtersOptions ?? throw new ArgumentNullException(nameof(filtersOptions));

            _filters = filtersOptions.CurrentValue.Any()
                ? filtersOptions.CurrentValue
                : new();

            _hotkeys = hotkeyOptions.CurrentValue.Any()
                ? hotkeyOptions.CurrentValue
                : DefaultOptions.Hotkeys;
        }

        public DevOptions GetDevOptions()
        {
            var t = _devOptions.CurrentValue;

            t.IsActive = true;

            return t;
        }

        public IEnumerable<HotkeyOptions> GetHotkeyOptions()
        {
            // TODO get currentValue
            var t = _hotkeys;
            return t;
        }

        public SettingsOptions GetSettingsOptions()
        {
            var t = _settingsOptions.CurrentValue;
            return t;
        }

        public ICollection<FiltersOptions> GetFiltersOptions()
        {
            var t = _filters;
            return t;
        }

        public void AddFilter(FiltersOptions filter)
        {
            // TODO check key/value already exist?
            _filters.Add(filter);
        }

        public void RemoveFilter(FiltersOptions filter)
        {
            // TODO validation?
            _filters.Remove(filter);
        }

        public void Save()
        {
            var currentSettings = new AppSettings
            {
                DevOptions = _devOptions.CurrentValue,
                FiltersOptions = _filters,
                HotkeyOptions = _hotkeys,
                SettingsOptions = _settingsOptions.CurrentValue,
            };

            var settingsJson = JsonConvert.SerializeObject(currentSettings, Formatting.Indented);

            // TODO dynamic file path and write operation
            var filePath = "C:\\Users\\Fernando.Silva\\AppData\\Local\\AmethystWindows\\appsettings.json";
            File.WriteAllText(filePath, settingsJson);
        }

        public void SetDisabled(bool value)
        {
            var t = GetSettingsOptions();
            t.Disabled = value;
            Save();
        }

        public void SetHotkey(HotkeyViewModel x01)
        {
            if (x01 is null)
            {
                throw new ArgumentNullException(nameof(x01));
            }

            var hots = _hotkeys;
            var find = hots.FirstOrDefault(x => x.Command.Equals(x01.Command.ToString()));
            if (find == null)
            {
                // TODO error setting not found
                _logger.Error($"Hotkey not found");
            }
            else
            {
                find.Keys = x01.Keys;
            }
            // TODO review if needs to be saved here
            //Save();
        }

        public void SetHotkeyOptions(ObservableHotkeys x)
        {
            if (x is null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            foreach (var ii in x)
            {
                var xx = new HotkeyOptions
                {
                    Command = ii.Command.ToString(),
                    Keys = ii.Keys,
                };
                _hotkeys.Add(xx);
            }

            var hots = _hotkeyOptions.CurrentValue;

            foreach (var i in x)
            {
                var command = i.Command;
                var keys = i.Keys;

                var find = hots.FirstOrDefault(x => x.Command.Equals(command));
                if (find == null)
                {
                    // error setting not found

                }
                else
                {
                    find.Keys = keys;
                }
            }

            Save();
        }

        public void SetSettingsOptions(MainWindowViewModel mainWindowViewModel)
        {
            if (mainWindowViewModel is null)
            {
                throw new ArgumentNullException(nameof(mainWindowViewModel));
            }

            var settings = _settingsOptions.CurrentValue;

            settings.LayoutPadding = mainWindowViewModel.LayoutPadding;
            settings.Padding = mainWindowViewModel.Padding;
            settings.MarginTop = mainWindowViewModel.MarginTop;
            settings.MarginRight = mainWindowViewModel.MarginRight;
            settings.MarginBottom = mainWindowViewModel.MarginBottom;
            settings.MarginLeft = mainWindowViewModel.MarginLeft;
            settings.VirtualDesktops = mainWindowViewModel.VirtualDesktops;
        }
    }
}
