using Fig.Web.Events;

namespace Fig.Web.Models;

public class SettingVerificationModel
{
    private readonly Func<SettingEventModel, Task<object>> _settingEvent;

    public SettingVerificationModel(Func<SettingEventModel, Task<object>> settingEvent,
        string name,
        string description,
        string verificationType,
        List<string> settingsVerified)
    {
        Name = name;
        Description = description;
        VerificationType = verificationType;
        SettingsVerified = settingsVerified;
        _settingEvent = settingEvent;
    }

    public string Name { get; }

    public string Description { get; }

    public string VerificationType { get; }

    public List<string> SettingsVerified { get; }

    public bool IsRunning { get; private set; }

    public bool? Succeeded { get; private set; }

    public bool IsHistoryVisible { get; set; }

    public string ResultMessage { get; private set; }

    public string ResultLog { get; set; }

    public DateTime ResultTime { get; private set; }

    public List<VerificationResultModel> History { get; set; }

    public async Task ShowHistory()
    {
        IsHistoryVisible = !IsHistoryVisible;
        
        var settingEvent = new SettingEventModel(Name, SettingEventType.VerificationHistoryRequested);
        var result = await _settingEvent(settingEvent);
        if (result is List<VerificationResultModel> history)
            History = history;
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
                Succeeded = verificationResult.Success;
                ResultMessage = verificationResult.Message;
                ResultLog = string.Join(Environment.NewLine, verificationResult.Logs);
                ResultTime = verificationResult.ExecutionTime;
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
        return new SettingVerificationModel(settingEvent, Name, Description, VerificationType, SettingsVerified);
    }
}