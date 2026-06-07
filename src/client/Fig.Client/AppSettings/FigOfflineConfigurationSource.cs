using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Fig.Client.AppSettings;

internal class FigOfflineConfigurationSource : IConfigurationSource
{
    private readonly List<IConfigurationSource> _preFigSources;
    private readonly IDpapiValueProcessor? _processorOverride;

    public FigOfflineConfigurationSource(List<IConfigurationSource> preFigSources)
        : this(preFigSources, null)
    {
    }

    internal FigOfflineConfigurationSource(
        List<IConfigurationSource> preFigSources,
        IDpapiValueProcessor? processorOverride)
    {
        _preFigSources = preFigSources;
        _processorOverride = processorOverride;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return _processorOverride != null
            ? new FigOfflineConfigurationProvider(_preFigSources, _processorOverride)
            : new FigOfflineConfigurationProvider(_preFigSources);
    }
}
