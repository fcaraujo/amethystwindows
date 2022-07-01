using AmethystWindows.DesktopWindowsManager;
using System;
using System.Globalization;
using System.Windows.Data;

namespace AmethystWindows.Models
{
    public class ViewModelConfigurableFilterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Pair<string, string> selectedConfigurableFilter)
            {
                return true;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}