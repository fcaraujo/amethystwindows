using Microsoft.Extensions.DependencyInjection;
using System;

namespace AmethystWindows.DependencyInjection
{
    public class IocProvider
    {
        private static IServiceProvider? _provider;

        public static IServiceProvider Build(IServiceCollection services)
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
    }
}
