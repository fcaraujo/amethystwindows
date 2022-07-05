namespace AmethystWindows.Models.Configuration
{
    public class SettingsOptions
    {
        public int Padding { get; set; } = 0;
        public int Step { get; set; } = 25;
        public int LayoutPadding { get; set; } = 4;
        public int MarginTop { get; set; } = 6;
        public int MarginRight { get; set; } = 6;
        public int MarginBottom { get; set; } = 6;
        public int MarginLeft { get; set; } = 6;
        public int VirtualDesktops { get; set; } = 2;
        public bool Disabled { get; set; } = false;
    }
}