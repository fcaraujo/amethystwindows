using System.Collections.Generic;

using static AmethystWindows.Models.Enums.CommandHotkey;

// TODO consider change mod2 = "Alt+Control+Shift" to really match amethyst
namespace AmethystWindows.Models.Configuration
{
    public static class DefaultOptions
    {
        // TODO mod1 + M = (move) focus to main pane
        // TODO mod1 + I = (info) display current layout
        public static readonly List<HotkeyOptions> Hotkeys = new()
        {
            new HotkeyOptions
            {
                Command = RotateLayoutClockwise.ToString(),
                Keys = "Alt+Shift+Space",
            },
            new HotkeyOptions
            {
                Command = RotateLayoutAntiClockwise.ToString(),
                Keys = "Alt+Shift+Windows+Space",
            },
            new HotkeyOptions
            {
                Command = SetMainPane.ToString(),
                Keys = "Alt+Shift+Enter",
            },
            new HotkeyOptions
            {
                Command = SwapFocusedAnticlockwise.ToString(),
                Keys = "Alt+Shift+Windows+H",
            },
            new HotkeyOptions
            {
                Command = SwapFocusedClockwise.ToString(),
                Keys = "Alt+Shift+Windows+L",
            },
            new HotkeyOptions
            {
                Command = ChangeWindowFocusAntiClockwise.ToString(),
                Keys = "Alt+Shift+K",
            },
            new HotkeyOptions
            {
                Command = ChangeWindowFocusClockwise.ToString(),
                Keys = "Alt+Shift+J",
            },
            new HotkeyOptions
            {
                Command = MoveFocusPreviousScreen.ToString(),
                Keys = "Alt+Shift+Windows+P",
            },
            new HotkeyOptions
            {
                Command = MoveFocusNextScreen.ToString(),
                Keys = "Alt+Shift+Windows+N",
            },
            new HotkeyOptions
            {
                Command = ExpandMainPane.ToString(),
                Keys = "Alt+Shift+L",
            },
            new HotkeyOptions
            {
                Command = Shrink.ToString(),
                Keys = "Alt+Shift+H",
            },
            new HotkeyOptions
            {
                Command = MoveFocusedPreviousScreen.ToString(),
                Keys = "Alt+Shift+Windows+K",
            },
            new HotkeyOptions
            {
                Command = MoveFocusedNextScreen.ToString(),
                Keys = "Alt+Shift+Windows+J",
            },
            new HotkeyOptions
            {
                Command = Redraw.ToString(),
                Keys = "Alt+Shift+Z",
            },
            new HotkeyOptions
            {
                Command = MoveNextSpace.ToString(),
                Keys = "Alt+Control+Shift+Right",
            },
            new HotkeyOptions
            {
                Command = MovePreviousSpace.ToString(),
                Keys = "Alt+Control+Shift+Left",
            },
            new HotkeyOptions
            {
                Command = MoveFocusedToSpace1.ToString(),
                Keys = "Alt+Shift+Windows+1",
            },
            new HotkeyOptions
            {
                Command = MoveFocusedToSpace2.ToString(),
                Keys = "Alt+Shift+Windows+2",
            },
            new HotkeyOptions
            {
                Command = MoveFocusedToSpace3.ToString(),
                Keys = "Alt+Shift+Windows+3",
            },
            new HotkeyOptions
            {
                Command = MoveFocusedToSpace4.ToString(),
                Keys = "Alt+Shift+Windows+4",
            },
            new HotkeyOptions
            {
                Command = MoveFocusedToSpace5.ToString(),
                Keys = "Alt+Shift+Windows+5",
            },
        };
    }
}