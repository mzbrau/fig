using Fig.Api.SettingVerification.Exceptions;
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.SettingVerification.Plugin;

public class SettingPluginVerification : ISettingPluginVerification
{
    public Task<VerificationResultDataContract> RunVerification(SettingPluginVerificationBusinessEntity verification, IDictionary<string, object?> settings)
    {
        var arguments = verification.PropertyArguments.Select(argument =>
        {
            if (settings.ContainsKey(argument))
                return settings[argument];

            throw new InvalidSettingNameException($"Settings did not contain required argument '{argument}'");
        }).ToArray();

        throw new NotImplementedException();
    }
}