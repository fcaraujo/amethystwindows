using AmethystWindows.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using static Vanara.PInvoke.User32;

namespace AmethystWindows.Models
{
    /// <summary>
    /// View model responsible to pass hotkeys throughout the UI/configuration
    /// </summary>
    public class HotkeyViewModel
    {
        public CommandHotkey Command { get; private set; }
        public Hotkey Hotkey { get; private set; }
        public string Keys { get; private set; }

        // Used to set hotkey from the UI (HotkeyEditorControl)
        public HotkeyViewModel(CommandHotkey command, Key key, ModifierKeys modifiers)
        {
            Command = command;
            Keys = GetConverterdKeys(key, modifiers);
            Hotkey = CreateHotkey(Keys);
        }

        // Used to empty/unset the hotkey
        public HotkeyViewModel(CommandHotkey command)
            : this(command, Key.None, ModifierKeys.None)
        { }

        // Used when it's loading the settings from SettingsService
        public HotkeyViewModel(CommandHotkey command, string keys)
        {
            Command = command;
            Keys = keys;
            Hotkey = CreateHotkey(Keys);
        }

        /// <summary>
        /// Resolves hotkey from keys
        /// </summary>
        /// <param name="Keys"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static Hotkey CreateHotkey(string Keys)
        {
            if (string.IsNullOrWhiteSpace(Keys))
            {
                throw new ArgumentException($"'{nameof(Keys)}' cannot be null or whitespace.", nameof(Keys));
            }

            var key = default(Key);
            var modifiers = ModifierKeys.None;

            foreach (var k in Keys.Split('+'))
            {
                var lowerKey = k.ToLower();
                switch (lowerKey)
                {
                    case "alt":
                        modifiers |= ModifierKeys.Alt;
                        break;
                    case "control":
                        modifiers |= ModifierKeys.Control;
                        break;
                    case "shift":
                        modifiers |= ModifierKeys.Shift;
                        break;
                    case "windows":
                        modifiers |= ModifierKeys.Windows;
                        break;
                    default:
                        var keyFromString = new KeyConverter().ConvertFromString(k);
                        if (keyFromString is not null)
                        {
                            key = (Key)keyFromString;
                        }
                        break;
                }
            }

            return new Hotkey(key, modifiers);
        }

        /// <summary>
        /// From key and modifiers get the keys in plain text
        /// </summary>
        /// <param name="key"></param>
        /// <param name="modifiers"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string GetConverterdKeys(Key key, ModifierKeys modifiers)
        {
            var builder = new StringBuilder();

            // Add modifiers
            if (modifiers != ModifierKeys.None)
            {
                modifiers.ToString()
                    .Split(',')
                    .ToList()
                    .ForEach(m =>
                    {
                        builder.Append($"{m.Trim()}+");
                    });
            }

            // Add key
            var k = new KeyConverter().ConvertToString(key) ?? throw new ArgumentNullException(nameof(key));
            builder.Append(k);

            return builder.ToString();
        }

        /// <summary>
        /// Gets respective modifiers to be used in RegisterHotKey
        /// </summary>
        /// <returns></returns>
        public HotKeyModifiers GetFsModifiers()
        {
            var modifiers = HotKeyModifiers.MOD_NONE;

            var modifiersToConvert = new Dictionary<ModifierKeys, HotKeyModifiers>
            {
                {
                    ModifierKeys.Alt, HotKeyModifiers.MOD_ALT
                },
                {
                    ModifierKeys.Control, HotKeyModifiers.MOD_CONTROL
                },
                {
                    ModifierKeys.Shift, HotKeyModifiers.MOD_SHIFT
                },
                {
                    ModifierKeys.Windows, HotKeyModifiers.MOD_WIN
                },
            };

            foreach (var itemToConvert in modifiersToConvert)
            {
                var modifier = itemToConvert.Key;
                var hotkey = itemToConvert.Value;

                if (Hotkey.Modifiers.HasFlag(modifier))
                {
                    modifiers |= hotkey;
                }
            }

            return modifiers;
        }

        /// <summary>
        /// Gets respective virtual key to be used in RegisterHotKey
        /// </summary>
        /// <returns></returns>
        public int GetVirtualKey()
        {
            var hotKeyKey = Hotkey?.Key ?? default;
            var virtualKey = KeyInterop.VirtualKeyFromKey(hotKeyKey);
            return virtualKey;
        }
    }
}