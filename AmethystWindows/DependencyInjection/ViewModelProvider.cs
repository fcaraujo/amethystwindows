using System;
using AmethystWindows.Models;

namespace AmethystWindows.DependencyInjection
{
    /// <summary>
    /// View model locator to pass statically the view model to WPF
    /// </summary>
    public class ViewModelProvider
    {
        public MainWindowViewModel MainWindowViewModel
        {
            get
            {
                return IocProvider.GetService<MainWindowViewModel>() ?? throw new ArgumentNullException(nameof(MainWindowViewModel));
            }
        }
    }
}
