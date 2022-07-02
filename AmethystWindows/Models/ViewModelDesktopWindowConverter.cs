using System;
using System.Globalization;
using System.Windows.Data;

namespace AmethystWindows.Models
{
    public class ViewModelDesktopWindowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var selectedWindow = value as ViewModelDesktopWindow;

            if (selectedWindow != null)
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