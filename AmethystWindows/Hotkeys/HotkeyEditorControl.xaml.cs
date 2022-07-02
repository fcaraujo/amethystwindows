using AmethystWindows.DependencyInjection;
using AmethystWindows.Models;
using AmethystWindows.Models.Enums;
using AmethystWindows.Services;
using System.Windows;
using System.Windows.Input;

namespace AmethystWindows.Hotkeys
{
    public partial class HotkeyEditorControl
    {
        public static readonly DependencyProperty ViewModelHotkeyProperty =
            DependencyProperty.Register(nameof(ViewModelHotkey), typeof(HotkeyViewModel),
                typeof(HotkeyEditorControl),
                new FrameworkPropertyMetadata(default(HotkeyViewModel),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public HotkeyEditorControl()
        {
            InitializeComponent();
        }

        public CommandHotkey Command { get; set; }

        public HotkeyViewModel ViewModelHotkey
        {
            get => (HotkeyViewModel)GetValue(ViewModelHotkeyProperty);
            set => SetValue(ViewModelHotkeyProperty, value);
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            // When Alt is pressed e.Key is "System", let's use SystemKey instead
            var isAltPressed = key == Key.System;
            if (isAltPressed)
            {
                key = e.SystemKey;
            }

            // Win key is not detected on the modifiers so let's check/add them
            var isWinPressed = Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin);
            if (isWinPressed)
            {
                modifiers |= ModifierKeys.Windows;
            }

            // Tab navigation should work and not set an actual shortcut
            var isTabPressed = Keyboard.IsKeyDown(Key.Tab);
            var isShiftPressed = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
            if (isTabPressed || isShiftPressed && isTabPressed)
            {
                return;
            }

            // Prevent the event to pass further, as standard textbox shortcuts (except TAB) shouldn't work
            e.Handled = true;

            switch (key)
            {
                // If just this key is pressed - do nothing
                case Key.LeftCtrl:
                case Key.RightCtrl:
                case Key.LeftAlt:
                case Key.RightAlt:
                case Key.LeftShift:
                case Key.RightShift:
                case Key.LWin:
                case Key.RWin:
                case Key.Clear:
                case Key.OemClear:
                case Key.Apps:
                    return;

                // Clear hotkey
                case Key.None:
                case Key.Back:
                case Key.Delete:
                case Key.Escape:
                    ViewModelHotkey = new HotkeyViewModel(Command);
                    break;

                // Setup hotkey (if none of above caught it)
                default:
                    ViewModelHotkey = new HotkeyViewModel(Command, key, modifiers);
                    break;
            }

            // Persist setting
            var settingsService = IocProvider.GetService<ISettingsService>();
            settingsService?.SetHotkey(ViewModelHotkey);
            settingsService?.Save();

            // Apply hotkey
            var hotkeyService = IocProvider.GetService<HotkeyService>();
            hotkeyService?.ClearHotkeys();
            hotkeyService?.RegisterHotkeys();
        }
    }
}
