using System.Collections.Generic;

namespace Fig.Contracts.Capabilities;

/// <summary>
/// Returned by GET /capabilities so clients can discover optional server features
/// and adapt their behaviour accordingly (e.g. request compression, deferred description upload).
/// </summary>
public class FigCapabilitiesDataContract
{
    public FigCapabilitiesDataContract(string apiVersion, IReadOnlyList<string> supportedFeatures)
    {
        ApiVersion = apiVersion;
        SupportedFeatures = supportedFeatures;
    }

    /// <summary>The running API version string.</summary>
    public string ApiVersion { get; }

    /// <summary>
    /// Stable feature tokens that clients may check.
    /// Current tokens: "deferredDescriptionRegistration", "requestCompression",
    /// "clientSettingUpdates", and "migrateFromClientTransforms" when custom migrate-from preview is enabled.
    /// </summary>
    public IReadOnlyList<string> SupportedFeatures { get; }
}
