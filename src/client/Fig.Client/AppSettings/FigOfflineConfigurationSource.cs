using System.Collections.Generic;
using Fig.Client.Contracts;
using Microsoft.Extensions.Configuration;

namespace Fig.Client.AppSettings;

internal class FigOfflineConfigurationSource : IConfigurationSource
{
    private readonly List<IConfigurationSource> _preFigSources;
    private readonly IAppSettingsEncryptionProvider? _encryptionProvider;

    internal FigOfflineConfigurationSource(
        List<IConfigurationSource> preFigSources,
        IAppSettingsEncryptionProvider? encryptionProvider = null)
    {
        _preFigSources = preFigSources;
        _encryptionProvider = encryptionProvider;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new FigOfflineConfigurationProvider(_preFigSources, _encryptionProvider);
    }
}
