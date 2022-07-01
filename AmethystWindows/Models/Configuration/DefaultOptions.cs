using AmethystWindows.Models.Enums;
using System.Collections.Generic;

namespace AmethystWindows.Models.Configuration
{
    public static class DefaultOptions
    {
        public static readonly List<HotkeyOptions> Hotkeys = new()
        {
            new HotkeyOptions()
            {
                Command = CommandHotkey.RotateLayoutClockwise.ToString(),
                Keys = "Alt+Shift+Space",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.RotateLayoutAntiClockwise.ToString(),
                Keys = "Alt+Shift+Windows+Space",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.SetMainPane.ToString(),
                Keys = "Alt+Shift+Enter",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.SwapFocusedAnticlockwise.ToString(),
                Keys = "Alt+Shift+H",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.SwapFocusedClockwise.ToString(),
                Keys = "Alt+Shift+L",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.ChangeWindowFocusAntiClockwise.ToString(),
                Keys = "Alt+Shift+J",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.ChangeWindowFocusClockwise.ToString(),
                Keys = "Alt+Shift+K",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.MoveFocusPreviousScreen.ToString(),
                Keys = "Alt+Shift+P",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.MoveFocusNextScreen.ToString(),
                Keys = "Alt+Shift+N",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.ExpandMainPane.ToString(),
                Keys = "Alt+Shift+Windows+L",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.Shrink.ToString(),
                Keys = "Alt+Shift+Windows+H",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.MoveFocusedPreviousScreen.ToString(),
                Keys = "Alt+Shift+Windows+K",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.MoveFocusedNextScreen.ToString(),
                Keys = "Alt+Shift+Windows+J",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.Redraw.ToString(),
                Keys = "Alt+Shift+Z",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.MoveNextSpace.ToString(),
                Keys = "Alt+Shift+Windows+Right",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.MovePreviousSpace.ToString(),
                Keys = "Alt+Shift+Windows+Left",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.MoveFocusedToSpace1.ToString(),
                Keys = "Alt+Shift+Windows+1",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.MoveFocusedToSpace2.ToString(),
                Keys = "Alt+Shift+Windows+2",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.MoveFocusedToSpace3.ToString(),
                Keys = "Alt+Shift+Windows+3",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.MoveFocusedToSpace4.ToString(),
                Keys = "Alt+Shift+Windows+4",
            },
            new HotkeyOptions()
            {
                Command = CommandHotkey.MoveFocusedToSpace5.ToString(),
                Keys = "Alt+Shift+Windows+5",
            },
        };
    }
}