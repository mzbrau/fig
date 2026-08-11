using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.Status;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fig.Client.StatusProperties
{
    internal sealed class FigStatusPropertiesWorker : IHostedService
    {
        private readonly IEnumerable<IFigStatusPropertiesSnapshotProvider> _providers;
        private readonly ILogger<FigStatusPropertiesWorker> _logger;
        private Func<CustomStatusPropertiesDataContract?>? _registered;

        public FigStatusPropertiesWorker(
            IEnumerable<IFigStatusPropertiesSnapshotProvider> providers,
            ILogger<FigStatusPropertiesWorker> logger)
        {
            _providers = providers;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var provider = _providers.FirstOrDefault();
            if (provider is null)
                return Task.CompletedTask;

            if (_providers.Skip(1).Any())
            {
                _logger.LogWarning(
                    "Multiple AddFigStatusProperties registrations found; only the first will be sent on status polls.");
            }

            _registered = () =>
            {
                try
                {
                    var snapshot = provider.CreateSnapshot();
                    if (snapshot is null)
                        return null;

                    if (!CustomStatusPropertiesValidator.TryValidate(snapshot, out var error))
                    {
                        _logger.LogWarning(
                            "Custom status properties exceeded limits and will be omitted from this poll: {Error}",
                            error);
                        return null;
                    }

                    return snapshot;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to create custom status properties snapshot");
                    return null;
                }
            };

            CustomStatusPropertiesBridge.Register(_registered);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_registered is not null)
                CustomStatusPropertiesBridge.ClearIfRegistered(_registered);
            return Task.CompletedTask;
        }
    }
}
