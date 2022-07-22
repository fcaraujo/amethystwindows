using AmethystWindows.Models.Enums;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace AmethystWindows.Services
{
    public interface IGridService
    {
        IEnumerable<Rectangle> Create(int mWidth, int mHeight, int windowsCount, int factor, Layout layout, int layoutPadding);
    }

    public class GridService : IGridService
    {
        private readonly ILogger _logger;
        private int _step = 0;

        public GridService(ILogger logger, ISettingsService settingsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (settingsService is null)
            {
                throw new ArgumentNullException(nameof(settingsService));
            }

            var opts = settingsService.GetSettingsOptions();
            _step = opts.Step;
        }

        public IEnumerable<Rectangle> Create(int mWidth, int mHeight, int windowsCount, int factor, Layout layout, int layoutPadding)
        {
            var i = 0;
            var j = 0;
            var horizontalStep = 0;
            var verticalStep = 0;
            var tiles = 0;
            var horizontalSize = 0;
            var verticalSize = 0;
            var isFirstLine = true;

            // int horizontalStep;
            // int verticalStep;
            // int tiles;
            // int horizontalSize;
            // int verticalSize;
            // bool isFirstLine;

            switch (layout)
            {
                case Layout.Column:
                    {
                        horizontalSize = mWidth / windowsCount;
                        j = 0;
                        for (i = 0; i < windowsCount; i++)
                        {
                            int lastPadding = i == windowsCount - 1 ? 0 : layoutPadding;
                            yield return new Rectangle(i * horizontalSize, j, horizontalSize - lastPadding, mHeight);
                        }
                    }
                    break;
                case Layout.Row:
                    {
                        verticalSize = mHeight / windowsCount;
                        j = 0;
                        for (i = 0; i < windowsCount; i++)
                        {
                            int lastPadding = i == windowsCount - 1 ? 0 : layoutPadding;
                            yield return new Rectangle(j, i * verticalSize, mWidth, verticalSize - lastPadding);
                        }
                    }
                    break;
                case Layout.Horizontal:
                    {
                        horizontalStep = Math.Max((int)Math.Sqrt(windowsCount), 1);
                        verticalStep = Math.Max(windowsCount / horizontalStep, 1);
                        tiles = horizontalStep * verticalStep;
                        horizontalSize = mWidth / horizontalStep;
                        verticalSize = mHeight / verticalStep;
                        // isFirstLine = true;

                        if (windowsCount != tiles || windowsCount == 3)
                        {
                            if (windowsCount == 3)
                            {
                                verticalStep--;
                                verticalSize = mHeight / verticalStep;
                            }

                            while (windowsCount > 0)
                            {
                                int lastPaddingI = i == horizontalStep - 1 ? 0 : layoutPadding;
                                int lastPaddingJ = j == verticalStep - 1 ? 0 : layoutPadding;
                                yield return new Rectangle(i * horizontalSize, j * verticalSize, horizontalSize - lastPaddingI, verticalSize - lastPaddingJ);
                                i++;
                                if (i >= horizontalStep)
                                {
                                    i = 0;
                                    j++;
                                }
                                if (j == verticalStep - 1 && isFirstLine)
                                {
                                    horizontalStep++;
                                    horizontalSize = mWidth / horizontalStep;
                                    isFirstLine = false;
                                }
                                windowsCount--;
                            }
                        }
                        else
                        {
                            while (windowsCount > 0)
                            {
                                int lastPaddingI = i == horizontalStep - 1 ? 0 : layoutPadding;
                                int lastPaddingJ = j == verticalStep - 1 ? 0 : layoutPadding;
                                yield return new Rectangle(i * horizontalSize, j * verticalSize, horizontalSize - lastPaddingI, verticalSize - lastPaddingJ);
                                i++;
                                if (i >= horizontalStep)
                                {
                                    i = 0;
                                    j++;
                                }
                                windowsCount--;
                            }
                        }
                    }
                    break;
                case Layout.Vertical:
                    {
                        verticalStep = Math.Max((int)Math.Sqrt(windowsCount), 1);
                        horizontalStep = Math.Max(windowsCount / verticalStep, 1);
                        tiles = horizontalStep * verticalStep;
                        verticalSize = mHeight / verticalStep;
                        horizontalSize = mWidth / horizontalStep;
                        // isFirstLine = true;

                        if (windowsCount != tiles || windowsCount == 3)
                        {
                            if (windowsCount == 3)
                            {
                                horizontalStep--;
                                horizontalSize = mWidth / horizontalStep;
                            }

                            while (windowsCount > 0)
                            {
                                int lastPaddingI = i == horizontalStep - 1 ? 0 : layoutPadding;
                                int lastPaddingJ = j == verticalStep - 1 ? 0 : layoutPadding;
                                yield return new Rectangle(i * horizontalSize, j * verticalSize, horizontalSize - lastPaddingI, verticalSize - lastPaddingJ);
                                j++;
                                if (j >= verticalStep)
                                {
                                    j = 0;
                                    i++;
                                }
                                if (i == horizontalStep - 1 && isFirstLine)
                                {
                                    verticalStep++;
                                    verticalSize = mHeight / verticalStep;
                                    isFirstLine = false;
                                }
                                windowsCount--;
                            }
                        }
                        else
                        {
                            while (windowsCount > 0)
                            {
                                int lastPaddingI = i == horizontalStep - 1 ? 0 : layoutPadding;
                                int lastPaddingJ = j == verticalStep - 1 ? 0 : layoutPadding;
                                yield return new Rectangle(i * horizontalSize, j * verticalSize, horizontalSize - lastPaddingI, verticalSize - lastPaddingJ);
                                j++;
                                if (j >= verticalStep)
                                {
                                    j = 0;
                                    i++;
                                }
                                windowsCount--;
                            }
                        }
                    }
                    break;
                case Layout.FullScreen:
                    {
                        for (i = 0; i < windowsCount; i++)
                        {
                            yield return new Rectangle(0, 0, mWidth, mHeight);
                        }
                    }
                    break;
                case Layout.Wide:
                    {
                        if (windowsCount == 1)
                        {
                            yield return new Rectangle(0, 0, mWidth, mHeight);
                        }
                        else
                        {
                            int size = mWidth / (windowsCount - 1);
                            for (i = 0; i < windowsCount - 1; i++)
                            {
                                int lastPaddingI = windowsCount == 1 ? 0 : layoutPadding;
                                int lastPaddingJ = i == windowsCount - 2 ? 0 : layoutPadding;

                                if (i == 0)
                                {
                                    yield return new Rectangle(0, 0, mWidth, mHeight / 2 + factor * _step - lastPaddingI / 2);
                                }

                                yield return new Rectangle(i * size, mHeight / 2 + factor * _step + lastPaddingI / 2, size - lastPaddingJ, mHeight / 2 - factor * _step - lastPaddingI / 2);
                            }
                        }
                    }
                    break;
                case Layout.Tall:
                    {
                        if (windowsCount == 1)
                        {
                            yield return new Rectangle(0, 0, mWidth, mHeight);
                        }
                        else
                        {
                            int size = mHeight / (windowsCount - 1);
                            for (i = 0; i < windowsCount - 1; i++)
                            {
                                int lastPaddingI = i == windowsCount - 2 ? 0 : layoutPadding;
                                int lastPaddingJ = windowsCount == 1 ? 0 : layoutPadding;

                                if (i == 0)
                                {
                                    yield return new Rectangle(0, 0, mWidth / 2 + factor * _step - lastPaddingJ / 2, mHeight);
                                }

                                yield return new Rectangle(mWidth / 2 + factor * _step + lastPaddingJ / 2, i * size, mWidth / 2 - factor * _step - lastPaddingJ / 2, size - lastPaddingI);
                            }
                        }
                    }
                    break;
            }
        }
    }
}