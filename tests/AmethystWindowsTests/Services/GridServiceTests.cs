using AmethystWindows.Models.Configuration;
using AmethystWindows.Models.Enums;
using AmethystWindows.Services;
using FluentAssertions;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Xunit;

namespace AmethystWindowsTests.Services
{
    public class GridServiceTests
    {
        // Dependencies
        private readonly Mock<ILogger> _loggerMock = new();
        private readonly Mock<ISettingsService> _settingsServiceMock = new();

        // Subject under testing
        private readonly IGridService _sut;

        // General grid configuration
        private const int Factor = 0;
        private const int LayoutPadding = 0;
        private const int MaxHeight = 1000;
        private const int MaxWidth = 1000;
        private Layout _layout;

        public GridServiceTests()
        {
            _settingsServiceMock
                .Setup(x => x.GetSettingsOptions())
                .Returns(new SettingsOptions { });

            _sut = new GridService(_loggerMock.Object, _settingsServiceMock.Object);
        }

        [Theory]
        [InlineData(Layout.Column)]
        [InlineData(Layout.Row)]
        [InlineData(Layout.Horizontal)]
        [InlineData(Layout.Vertical)]
        [InlineData(Layout.FullScreen)]
        [InlineData(Layout.Wide)]
        [InlineData(Layout.Tall)]
        public void Create_SingleWindow_ShouldReturnSingleRectangle(Layout layout)
        {
            // Arrange
            var windows = 1;

            // Act
            var rectangles = _sut.Create(MaxWidth, MaxWidth, windows, Factor, layout, LayoutPadding);

            // Assert
            rectangles.Should().ContainSingle()
                .And.BeEquivalentTo(new[] { new Rectangle(0, 0, 1000, 1000) });
        }

        [Theory]
        [InlineData(Layout.Column)]
        [InlineData(Layout.Row)]
        [InlineData(Layout.Horizontal)]
        [InlineData(Layout.Vertical)]
        [InlineData(Layout.FullScreen)]
        [InlineData(Layout.Wide)]
        [InlineData(Layout.Tall)]
        public void Create_2Windows_ShouldReturn2Rectangles(Layout layout)
        {
            // Arrange
            var windows = 2;

            // Act
            var rectangles = _sut.Create(MaxWidth, MaxWidth, windows, Factor, layout, LayoutPadding);

            // Assert
            rectangles.Should().NotBeEmpty()
                .And.HaveCount(2);

            var first = rectangles.First();
            var second = rectangles.Last();

            switch (layout)
            {
                case Layout.Column:
                    {
                        first.Should().BeEquivalentTo(new Rectangle(0, 0, 500, 1000));
                        second.Should().BeEquivalentTo(new Rectangle(500, 0, 500, 1000));
                    }
                    break;
                case Layout.Row:
                    {
                        first.Should().BeEquivalentTo(new Rectangle(0, 0, 1000, 500));
                        second.Should().BeEquivalentTo(new Rectangle(0, 500, 1000, 500));
                    }
                    break;
                case Layout.Horizontal:
                    {
                        first.Should().BeEquivalentTo(new Rectangle(0, 0, 1000, 500));
                        second.Should().BeEquivalentTo(new Rectangle(0, 500, 1000, 500));
                    }
                    break;
                case Layout.Vertical:
                    {
                        first.Should().BeEquivalentTo(new Rectangle(0, 0, 500, 1000));
                        second.Should().BeEquivalentTo(new Rectangle(500, 0, 500, 1000));
                    }
                    break;
                case Layout.FullScreen:
                    {
                        first.Should().BeEquivalentTo(new Rectangle(0, 0, 1000, 1000));
                        second.Should().BeEquivalentTo(new Rectangle(0, 0, 1000, 1000));
                    }
                    break;
                case Layout.Wide:
                    {
                        first.Should().BeEquivalentTo(new Rectangle(0, 0, 1000, 500));
                        second.Should().BeEquivalentTo(new Rectangle(0, 500, 1000, 500));
                    }
                    break;
                case Layout.Tall:
                    {
                        first.Should().BeEquivalentTo(new Rectangle(0, 0, 500, 1000));
                        second.Should().BeEquivalentTo(new Rectangle(500, 0, 500, 1000));
                    }
                    break;
                default:
                    break;
            }
        }

        // TODO follow standard below

        [Fact]
        public void GridGeneratorCountThree()
        {
            _layout = Layout.Column;
            IEnumerable<Rectangle> gridGenerator = _sut.Create(1000, 1000, 3, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 333, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(333, 0, 333, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(666, 0, 333, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);

            _layout = Layout.Row;
            gridGenerator = _sut.Create(1000, 1000, 3, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 333, 1000, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 666, 1000, 333));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);

            _layout = Layout.Vertical;
            gridGenerator = _sut.Create(1000, 1000, 3, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 500, 500, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);

            _layout = Layout.Horizontal;
            gridGenerator = _sut.Create(1000, 1000, 3, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 500, 500, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);

            _layout = Layout.FullScreen;
            gridGenerator = _sut.Create(1000, 1000, 3, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 0, 1000, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 0, 1000, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);

            _layout = Layout.Wide;
            gridGenerator = _sut.Create(1000, 1000, 3, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 500, 500, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);

            _layout = Layout.Tall;
            gridGenerator = _sut.Create(1000, 1000, 3, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 500, 500, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[3]);
        }

        [Fact]
        public void GridGeneratorCountFour()
        {
            _layout = Layout.Column;
            IEnumerable<Rectangle> gridGenerator = _sut.Create(1000, 1000, 4, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 250, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(250, 0, 250, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 0, 250, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(750, 0, 250, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);

            _layout = Layout.Row;
            gridGenerator = _sut.Create(1000, 1000, 4, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 250));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 250, 1000, 250));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 500, 1000, 250));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(0, 750, 1000, 250));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);

            _layout = Layout.Vertical;
            gridGenerator = _sut.Create(1000, 1000, 4, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 500, 500, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);

            _layout = Layout.Horizontal;
            gridGenerator = _sut.Create(1000, 1000, 4, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 500, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 500, 500, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);

            _layout = Layout.FullScreen;
            gridGenerator = _sut.Create(1000, 1000, 4, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 0, 1000, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 0, 1000, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(0, 0, 1000, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);

            _layout = Layout.Wide;
            gridGenerator = _sut.Create(1000, 1000, 4, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(333, 500, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(666, 500, 333, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);

            _layout = Layout.Tall;
            gridGenerator = _sut.Create(1000, 1000, 4, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 333, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 666, 500, 333));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[4]);
        }

        [Fact]
        public void GridGeneratorCountFive()
        {
            _layout = Layout.Column;
            IEnumerable<Rectangle> gridGenerator = _sut.Create(1000, 1000, 5, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 200, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(200, 0, 200, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(400, 0, 200, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(600, 0, 200, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(800, 0, 200, 1000));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[5]);

            _layout = Layout.Row;
            gridGenerator = _sut.Create(1000, 1000, 5, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 200));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 200, 1000, 200));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 400, 1000, 200));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(0, 600, 1000, 200));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(0, 800, 1000, 200));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[5]);

            _layout = Layout.Vertical;
            gridGenerator = _sut.Create(1000, 1000, 5, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 0, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 333, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(500, 666, 500, 333));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[5]);

            _layout = Layout.Horizontal;
            gridGenerator = _sut.Create(1000, 1000, 5, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 500, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(333, 500, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(666, 500, 333, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[5]);

            _layout = Layout.Wide;
            gridGenerator = _sut.Create(1000, 1000, 5, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 1000, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 250, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(250, 500, 250, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 500, 250, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(750, 500, 250, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[5]);

            _layout = Layout.Tall;
            gridGenerator = _sut.Create(1000, 1000, 5, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 1000));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 250));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(500, 250, 500, 250));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 500, 500, 250));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(500, 750, 500, 250));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[5]);
        }

        [Fact]
        public void GridGeneratorCountSix()
        {
            _layout = Layout.Vertical;
            IEnumerable<Rectangle> gridGenerator = _sut.Create(1000, 1000, 6, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(0, 500, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(333, 0, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(333, 500, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(666, 0, 333, 500));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[5], new Rectangle(666, 500, 333, 500));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[6]);

            _layout = Layout.Horizontal;
            gridGenerator = _sut.Create(1000, 1000, 6, 0, _layout, 0);
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[0], new Rectangle(0, 0, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[1], new Rectangle(500, 0, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[2], new Rectangle(0, 333, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[3], new Rectangle(500, 333, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[4], new Rectangle(0, 666, 500, 333));
            Assert.Equal<Rectangle>(gridGenerator.ToArray()[5], new Rectangle(500, 666, 500, 333));
            Assert.Throws<IndexOutOfRangeException>(() => gridGenerator.ToArray()[6]);
        }
    }
}