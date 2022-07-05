using AmethystWindows.Models;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AmethystWindows.DependencyInjection
{
    public class DIContainer
    {
        private static IServiceProvider? _provider;

        public static IServiceProvider BuildProvider(IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _provider = services.BuildServiceProvider();
            return _provider;
        }

        public static T GetService<T>()
        {
            if (_provider is null)
            {
                throw new ArgumentNullException(nameof(_provider), "Ioc provider cannot be null.");
            }

            var service = _provider.GetService(typeof(T)) ?? throw new ArgumentNullException("Service");

            return (T)service;
        }

        /// <summary>
        /// View model locator to pass statically the view model to WPF
        /// </summary>
        public MainWindowViewModel MainWindowViewModel
        {
            get => GetService<MainWindowViewModel>();
        }
    }
}
