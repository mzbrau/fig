using Fig.Api.SettingVerification.Converters;
using Fig.Api.SettingVerification.Exceptions;
using Fig.Api.SettingVerification.Sdk;
using Fig.Contracts.SettingVerification;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.SettingVerification;

public class SettingVerification : ISettingVerification
{
    private readonly ISettingVerificationResultConverter _resultConverter;
    private readonly IVerificationFactory _verificationFactory;

    public SettingVerification(IVerificationFactory verificationFactory,
        ISettingVerificationResultConverter resultConverter)
    {
        _verificationFactory = verificationFactory;
        _resultConverter = resultConverter;
    }

    public async Task<VerificationResultDataContract> RunVerification(
        SettingVerificationBusinessEntity verification, ICollection<SettingBusinessEntity> settings)
    {
        var settingNamesAndValues = settings.ToDictionary(a => a.Name, b => b.Value?.GetValue());
        return await RunVerification(verification, settingNamesAndValues);
    }

    private async Task<VerificationResultDataContract> RunVerification(
        SettingVerificationBusinessEntity verification, IDictionary<string, object?> settings)
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

        var verifier = _verificationFactory.GetVerifier(verification.Name);
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