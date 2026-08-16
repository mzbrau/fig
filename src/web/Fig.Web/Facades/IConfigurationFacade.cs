using Fig.Contracts.ApiSecret;
using Fig.Contracts.Configuration;
using Fig.Web.Models.Configuration;

namespace Fig.Web.Facades;

public interface IConfigurationFacade
{
    FigConfigurationModel ConfigurationModel { get; }

    long EventLogCount { get; }

    ApiSecretRotationStatusDataContract? ApiSecretRotationStatus { get; }

    /// <summary>
    /// True when <see cref="LoadWebFeatures"/> has completed successfully for the current session.
    /// </summary>
    bool WebFeaturesLoaded { get; }

    /// <summary>
    /// Whether JavaScript (display scripts and dashboards) is allowed.
    /// Defaults to false until features are loaded.
    /// </summary>
    bool AllowDisplayScripts { get; }

    event Action? WebFeaturesChanged;

    Task LoadConfiguration();

    Task LoadWebFeatures();

    Task SaveConfiguration();

    Task RefreshApiSecretRotationStatus();

    Task MigrateEncryptedData();

    Task<SecretStoreTestResultDataContract> TestKeyVault();

    Task<SecretStoreTestResultDataContract> TestFigAssistant();

    /// <summary>
    /// Enables JavaScript for an administrator (sets AllowDisplayScripts and saves full configuration).
    /// </summary>
    Task EnableDisplayScripts();
}
