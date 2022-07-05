using System.Windows.Input;

namespace AmethystWindows.Models
{
    /// <summary>
    /// Hotkey object that holds the Modifier {Alt,Shift,Ctrl,Win} and Key
    /// </summary>
    public class Hotkey
    {
        public Key Key { get; }

        public ModifierKeys Modifiers { get; }

        public Hotkey(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        // Used to show the hotkey as a string in the UI field (textbox on the HotkeyEditorControl)
        public override string ToString()
        {
            var result = Key == Key.None && Modifiers == ModifierKeys.None
                ? $"<{nameof(Key.None)}>"
                : HotkeyViewModel.GetConverterdKeys(Key, Modifiers);

            return result;
        }
    }
}
