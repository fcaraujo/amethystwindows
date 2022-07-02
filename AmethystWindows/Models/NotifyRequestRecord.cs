using System.Windows.Forms;

namespace AmethystWindows.Models
{
    public class NotifyRequestRecord
    {
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public int Duration { get; set; } = 0;
        public ToolTipIcon Icon { get; set; } = ToolTipIcon.Info;
    }
}