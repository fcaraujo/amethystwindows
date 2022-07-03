using Serilog;
using System;

namespace AmethystWindows.Services
{
    /// <summary>
    /// Responsible for controlling virtual desktops
    /// </summary>
    public interface IVirtualDesktopService
    {
        /// <summary>
        /// Adds/removes virtual desktops according to the current settings
        /// </summary>
        void SynchronizeDesktops();
    }

    public class VirtualDesktopService : IVirtualDesktopService
    {
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IVirtualDesktopWrapper _virtualDesktopFacade;

        public VirtualDesktopService(ILogger logger, ISettingsService settingsService, IVirtualDesktopWrapper virtualDesktopFacade)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _virtualDesktopFacade = virtualDesktopFacade ?? throw new ArgumentNullException(nameof(virtualDesktopFacade));
        }

        public void SynchronizeDesktops()
        {
            var currentDesktops = _virtualDesktopFacade.GetList();
            var currentLength = currentDesktops.Length;

            var settingsOptions = _settingsService.GetSettingsOptions();
            var desiredDesktops = settingsOptions.VirtualDesktops;

            _logger.Information(
                "Adjusting virtual desktops current/desired {CurrentDesktops}/{DesiredDesktops}.",
                currentLength,
                desiredDesktops);

            var desktopDifference = desiredDesktops - currentLength;
            switch (desktopDifference)
            {
                case < 0:
                    {
                        _logger.Debug("Removing desktop(s) due to {DesktopDifference} of difference.", desktopDifference);

                        for (int i = desktopDifference; i < 0; i++)
                        {
                            _virtualDesktopFacade.RemoveLast();
                        }
                    }
                    break;
                case > 0:
                    {
                        _logger.Debug("Adding desktop(s) due to {DesktopDifference} of difference.", desktopDifference);

                        for (int i = 0; i < desktopDifference; i++)
                        {
                            _virtualDesktopFacade.Add();
                        }
                    }
                    break;
                default:
                    {
                        _logger.Debug("No difference detected.");
                    }
                    break;
            }
        }
    }
}
