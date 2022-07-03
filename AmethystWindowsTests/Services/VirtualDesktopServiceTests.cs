using AmethystWindows.Models.Configuration;
using AmethystWindows.Services;
using Moq;
using Serilog;
using Xunit;

namespace AmethystWindowsTests.Services
{
    // TODO test this properly
    public class VirtualDesktopServiceTests
    {
        private readonly Mock<ILogger> loggerMock = new();
        private readonly Mock<ISettingsService> settingsServiceMock = new();
        private readonly Mock<IVirtualDesktopWrapper> virtualDesktopWrapperMock = new();

        private readonly VirtualDesktopService _sut;

        public VirtualDesktopServiceTests()
        {
            _sut = new(loggerMock.Object, settingsServiceMock.Object, virtualDesktopWrapperMock.Object);
        }

        [Fact]
        public void SynchronizeDesktops_WhenHasNoDiff_ShouldDoNothing()
        {
            // Arrange
            settingsServiceMock
                .Setup(x => x.GetSettingsOptions())
                .Returns(new SettingsOptions
                {
                    VirtualDesktops = 0,
                });

            // Act
            _sut.SynchronizeDesktops();

            // Assert
            // TODO implement here
        }
    }
}
