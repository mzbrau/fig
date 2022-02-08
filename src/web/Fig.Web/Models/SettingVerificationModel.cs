using Fig.Web.Events;

namespace Fig.Web.Models;

public class SettingVerificationModel
{
    private readonly Func<SettingEventModel, Task<object>> _settingEvent;


    public SettingVerificationModel(Func<SettingEventModel, Task<object>> settingEvent)
    {
        _settingEvent = settingEvent;
    }

    public string Name { get; set; }

    public string Description { get; set; }

    public string VerificationType { get; set; }

    public List<string> SettingsVerified { get; set; }

    public bool IsRunning { get; set; }

    public bool? Succeeded { get; set; }

    public bool IsHistoryVisible { get; set; }

    public string ResultMessage { get; set; }

    public string ResultLog { get; set; }

    public DateTime ResultTime { get; set; }

    public async Task ShowHistory()
    {
    }

    public async Task Verify()
    {
        IsRunning = true;
        try
        {
            var verificationRequest = new SettingEventModel(Name, SettingEventType.RunVerification);
            var result = await _settingEvent(verificationRequest);

            if (result is VerificationResultModel verificationResult)
            {
                Succeeded = verificationResult.Succeeded;
                ResultMessage = verificationResult.Message;
                ResultLog = verificationResult.Logs;
                ResultTime = DateTime.Now;
            }
        }
        catch (Exception ex)
        {
            Succeeded = false;
            ResultMessage = ex.Message;
        }
        finally
        {
            IsRunning = false;
        }
    }

    public SettingVerificationModel Clone(Func<SettingEventModel, Task<object>> settingEvent)
    {
        return new SettingVerificationModel(settingEvent)
        {
            Name = Name,
            Description = Description,
            VerificationType = VerificationType,
            SettingsVerified = SettingsVerified
        };
    }
}