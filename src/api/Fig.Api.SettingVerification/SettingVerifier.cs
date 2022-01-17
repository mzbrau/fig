using Fig.Api.SettingVerification.Dynamic;
using Fig.Api.SettingVerification.Plugin;
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.SettingVerification;

public class SettingVerifier : ISettingVerifier
{
    private readonly ISettingDynamicVerifier _settingDynamicVerifier;
    private readonly ISettingPluginVerification _settingPluginVerification;

    public SettingVerifier(ISettingDynamicVerifier settingDynamicVerifier,
        ISettingPluginVerification settingPluginVerification)
    {
        _settingDynamicVerifier = settingDynamicVerifier;
        _settingPluginVerification = settingPluginVerification;
    }
    
    public async Task Compile(SettingDynamicVerificationBusinessEntity verification)
    {
        await _settingDynamicVerifier.Compile(verification);
    }

    public async Task<VerificationResultDataContract> Verify(SettingVerificationBase verification, IEnumerable<SettingBusinessEntity> settings)
    {
        var settingNamesAndValues = settings.ToDictionary(a => a.Name, b => b.Value);
        return verification switch
        {
            SettingDynamicVerificationBusinessEntity settingDynamicVerification => await DynamicVerification(
                settingDynamicVerification, settingNamesAndValues),
            SettingPluginVerificationBusinessEntity settingPluginVerification => await PluginVerification(
                settingPluginVerification, settingNamesAndValues),
            _ => throw new ArgumentException($"The verification type {verification.GetType()} is not supported")
        };
    }

    private async Task<VerificationResultDataContract> DynamicVerification(
        SettingDynamicVerificationBusinessEntity verification,
        IDictionary<string, object?> settings)
    {
        return await _settingDynamicVerifier.RunVerification(verification, settings);
    }

    private async Task<VerificationResultDataContract> PluginVerification(
        SettingPluginVerificationBusinessEntity verification, IDictionary<string, object?> settings)
    {
        return await _settingPluginVerification.RunVerification(verification, settings);
    }
}