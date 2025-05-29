using System;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fig.Client.CustomActions
{
    public class CustomActionRegistrar<TSettings, TAction> : IHostedService
        where TSettings : SettingsBase
        where TAction : ICustomAction
    {
        private readonly TSettings _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly IFigLogger<CustomActionRegistrar<TSettings, TAction>> _logger; // This is already IFigLogger

        public CustomActionRegistrar(TSettings settings, IServiceProvider serviceProvider, IFigLogger<CustomActionRegistrar<TSettings, TAction>> logger)
        {
            _settings = settings;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var action = scope.ServiceProvider.GetRequiredService<TAction>();
            _settings.CustomActions.Add(action);
            _logger.LogInformation($"Registered custom action '{action.Name}' for settings '{typeof(TSettings).Name}'.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class CustomActionRegistrar<TAction> : IHostedService
        where TAction : ICustomAction
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IFigLogger<CustomActionRegistrar<TAction>> _logger; // This is already IFigLogger

        public CustomActionRegistrar(IServiceProvider serviceProvider, IFigLogger<CustomActionRegistrar<TAction>> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<SettingsBase>();
            var action = scope.ServiceProvider.GetRequiredService<TAction>();
            settings.CustomActions.Add(action);
            _logger.LogInformation($"Registered custom action '{action.Name}' for application-wide settings.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
