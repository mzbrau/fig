using Fig.Contracts.SettingVerification;
using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class SettingDynamicVerificationBusinessEntity : SettingVerificationBase
{
    private string? _settingsVerifiedAsJson;

    public virtual string? Description { get; set; }

    public virtual string? Code { get; set; }

    public virtual TargetRuntime TargetRuntime { get; set; }

    public virtual string CodeHash { get; set; } = default!;

    public virtual IList<string>? SettingsVerified { get; set; }

    public virtual string? SettingsVerifiedAsJson
    {
        get
        {
            if (SettingsVerified == null)
                return null;

            _settingsVerifiedAsJson = JsonConvert.SerializeObject(SettingsVerified);
            return _settingsVerifiedAsJson;
        }
        set
        {
            if (_settingsVerifiedAsJson != value)
                SettingsVerified =
                    value != null ? JsonConvert.DeserializeObject<List<string>>(value) : new List<string>();
        }
    }
}