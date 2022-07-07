using Serilog;
using System;
using System.Linq;
using WindowsDesktop;

namespace AmethystWindows.Services
{
    /// <summary>
    /// Encapsulates the VirtualDesktop class to make the VirtualDesktopService testable
    /// </summary>
    public interface IVirtualDesktopWrapper
    {
        // TODO add docs
        void Add();
        VirtualDesktop? GetCurrent();
        VirtualDesktop[] GetAll();
        void MoveToDesktop(IntPtr hWnd, VirtualDesktop targetDesktop);
        void RemoveLast();
        void SubscribeChangedEvent(EventHandler<VirtualDesktopChangedEventArgs> eventHandler);
    }

    public class VirtualDesktopWrapper : IVirtualDesktopWrapper
    {
        private readonly ILogger _logger;

        /// <inheritdoc />
        public VirtualDesktopWrapper(ILogger logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public void Add()
        {
            _logger.Debug("Performing {VirtualDesktopWrapperMethod} virtual desktop.", nameof(Add));

            VirtualDesktop.Create();
        }

        /// <inheritdoc />
        public VirtualDesktop? GetCurrent()
        {
            _logger.Debug("Performing {VirtualDesktopWrapperMethod} virtual desktop.", nameof(GetCurrent));
            return VirtualDesktop.Current;
        }

        /// <inheritdoc />
        public VirtualDesktop[] GetAll()
        {
            _logger.Debug("Performing {VirtualDesktopWrapperMethod} virtual desktop.", nameof(GetAll));
            return VirtualDesktop.GetDesktops();
        }

        /// <inheritdoc />
        public void MoveToDesktop(IntPtr hWnd, VirtualDesktop targetDesktop)
        {
            _logger.Debug("Performing {VirtualDesktopWrapperMethod} {WindowHandler} targeting {TargetDesktop}.",
                          nameof(RemoveLast),
                          hWnd,
                          targetDesktop);

            VirtualDesktop.MoveToDesktop(hWnd, targetDesktop);
        }

        /// <inheritdoc />
        public void RemoveLast()
        {
            _logger.Debug("Performing {VirtualDesktopWrapperMethod} virtual desktop.", nameof(RemoveLast));
            var lastVirtualDesktop = GetAll().ToList().Last();
            lastVirtualDesktop?.Remove();
        }

        /// <inheritdoc />
        public void SubscribeChangedEvent(EventHandler<VirtualDesktopChangedEventArgs> eventHandler)
        {
            VirtualDesktop.CurrentChanged += eventHandler;
        }

    }
}
