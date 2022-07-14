using Fig.Api.SettingVerification.Converters;
using Fig.Api.SettingVerification.Exceptions;
using Fig.Api.SettingVerification.Sdk;
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.SettingVerification.Plugin;

public class SettingPluginVerification : ISettingPluginVerification
{
    private readonly ISettingVerificationResultConverter _resultConverter;
    private readonly IVerificationPluginFactory _verificationPluginFactory;

    public SettingPluginVerification(IVerificationPluginFactory verificationPluginFactory,
        ISettingVerificationResultConverter resultConverter)
    {
        _verificationPluginFactory = verificationPluginFactory;
        _resultConverter = resultConverter;
    }

    public async Task<VerificationResultDataContract> RunVerification(
        SettingPluginVerificationBusinessEntity verification, IDictionary<string, object?> settings)
    {
        var arguments = (verification.PropertyArguments ?? new List<string>()).Select(argument =>
        {
            if (settings.ContainsKey(argument))
            {
                if (settings[argument] == null)
                    throw new ArgumentException($"setting {argument} had a null value");

                return settings[argument];
            }

            throw new InvalidSettingNameException($"Settings did not contain required argument '{argument}'");
        }).ToArray();

        var verifier = _verificationPluginFactory.GetVerifier(verification.Name);
        VerificationResult result;
        try
        {
            result = await Task.Run(() => verifier.RunVerification(arguments!));
        }
        catch (Exception ex)
        {
            result = new VerificationResult()
            {
                Success = false,
                Message = ex.Message,
            };
        }

        return _resultConverter.Convert(result);
    }
}