using Serilog;
using System.Linq;
using WindowsDesktop;

namespace AmethystWindows.Services
{
    /// <summary>
    /// Encapsulates the VirtualDesktop class to make the VirtualDesktopService testable
    /// </summary>
    public interface IVirtualDesktopWrapper
    {
        void Add();
        VirtualDesktop? GetCurrent();
        VirtualDesktop[] GetAll();
        void RemoveLast();
    }

    public class VirtualDesktopWrapper : IVirtualDesktopWrapper
    {
        private readonly ILogger _logger;

        public VirtualDesktopWrapper(ILogger logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public void Add()
        {
            _logger.Debug("Performing {VirtualDesktopWrapperMethod} virtual desktop.", nameof(Add));

            VirtualDesktop.Create();
        }

        public VirtualDesktop? GetCurrent()
        {
            _logger.Debug("Performing {VirtualDesktopWrapperMethod} virtual desktop.", nameof(GetCurrent));
            return VirtualDesktop.Current;
        }

        public VirtualDesktop[] GetAll()
        {
            _logger.Debug("Performing {VirtualDesktopWrapperMethod} virtual desktop.", nameof(GetAll));
            return VirtualDesktop.GetDesktops();
        }

        public void RemoveLast()
        {
            _logger.Debug("Performing {VirtualDesktopWrapperMethod} virtual desktop.", nameof(RemoveLast));
            var lastVirtualDesktop = GetAll().ToList().Last();
            lastVirtualDesktop?.Remove();
        }
    }
}
