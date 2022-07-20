using Serilog;
using System;
using System.Linq;
using WindowsDesktop;

namespace AmethystWindows.Services
{
    /// <summary>
    /// Responsible for controlling virtual desktops
    /// </summary>
    public interface IVirtualDesktopService
    {
        /// <summary>
        /// Gets the current virtual desktop
        /// </summary>
        VirtualDesktop? GetCurrent();

        /// <summary>
        /// Gets the virtual desktop in that particular order
        /// </summary>
        VirtualDesktop? GetFromIndex(int i);

        /// <summary>
        /// Gets the last virtual desktop (the most right)
        /// </summary>
        VirtualDesktop? GetLast();

        /// <summary>
        /// Move to a target desktop based on window's handler
        /// </summary>
        void MoveTo(IntPtr hWnd, VirtualDesktop targetDesktop);

        /// <summary>
        /// Binds event handler every time the virtual desktops are switched
        /// </summary>
        void SubscribeChangedEvent(EventHandler<VirtualDesktopChangedEventArgs> eventHandler);

        /// <summary>
        /// Adds/removes virtual desktops according to the current settings
        /// </summary>
        void SynchronizeSpaces();
    }

    public class VirtualDesktopService : IVirtualDesktopService
    {
        private readonly ILogger _logger;
        private readonly ISettingsService _settingsService;
        private readonly IVirtualDesktopWrapper _virtualDesktopWrapper;

        public VirtualDesktopService(ILogger logger,
                                     ISettingsService settingsService,
                                     IVirtualDesktopWrapper virtualDesktopWrapper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _virtualDesktopWrapper = virtualDesktopWrapper ?? throw new ArgumentNullException(nameof(virtualDesktopWrapper));
        }

        /// <inheritdoc />
        public VirtualDesktop? GetCurrent()
        {
            var result = _virtualDesktopWrapper.GetCurrent();
            return result;
        }

        /// <inheritdoc />
        public VirtualDesktop? GetFromIndex(int i)
        {
            var desktops = _virtualDesktopWrapper.GetAll();

            var result = desktops?.ElementAtOrDefault(i);

            return result;
        }

        /// <inheritdoc />
        public VirtualDesktop? GetLast()
        {
            var desktops = _virtualDesktopWrapper.GetAll();

            var result = desktops?.Last();

            return result;
        }

        /// <inheritdoc />
        public void MoveTo(IntPtr hWnd, VirtualDesktop targetDesktop)
        {
            _virtualDesktopWrapper.MoveToDesktop(hWnd, targetDesktop);
        }

        /// <inheritdoc />
        public void SubscribeChangedEvent(EventHandler<VirtualDesktopChangedEventArgs> eventHandler)
        {
            _virtualDesktopWrapper.SubscribeChangedEvent(eventHandler);
        }

        /// <inheritdoc />
        public void SynchronizeSpaces()
        {
            var currentDesktops = _virtualDesktopWrapper.GetAll();
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
                            _virtualDesktopWrapper.RemoveLast();
                        }
                    }
                    break;
                case > 0:
                    {
                        _logger.Debug("Adding desktop(s) due to {DesktopDifference} of difference.", desktopDifference);

                        for (int i = 0; i < desktopDifference; i++)
                        {
                            _virtualDesktopWrapper.Add();
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
