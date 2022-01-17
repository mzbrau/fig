using Fig.Api.SettingVerification.Converters;
using Fig.Api.SettingVerification.Exceptions;
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.SettingVerification.Plugin;

public class SettingPluginVerification : ISettingPluginVerification
{
    private readonly IVerificationPluginFactory _verificationPluginFactory;
    private readonly ISettingVerificationResultConverter _resultConverter;

    public SettingPluginVerification(IVerificationPluginFactory verificationPluginFactory,
        ISettingVerificationResultConverter resultConverter)
    {
        _verificationPluginFactory = verificationPluginFactory;
        _resultConverter = resultConverter;
    }
    
    public async Task<VerificationResultDataContract> RunVerification(SettingPluginVerificationBusinessEntity verification, IDictionary<string, object?> settings)
    {
        var arguments = verification.PropertyArguments.Select(argument =>
        {
            if (settings.ContainsKey(argument))
            {
                if (settings[argument] == null)
                {
                    throw new ArgumentException($"setting {argument} had a null value");
                }
                
                return settings[argument] as object;
            }

            throw new InvalidSettingNameException($"Settings did not contain required argument '{argument}'");
        }).ToArray();

        var verifier = _verificationPluginFactory.GetVerifier(verification.Name);
        
        var result = await Task.Run(() => verifier.RunVerification(arguments!));
        return _resultConverter.Convert(result);
    }
}