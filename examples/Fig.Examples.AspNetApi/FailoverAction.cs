using Fig.Client.CustomActions;

namespace Fig.Examples.AspNetApi;

public class FailoverAction : ICustomAction
{
    public string Name => "Failover";
    public string ButtonName => "Perform Failover";
    public string Description => "Fail over to another instance of the service.";
    public IEnumerable<string> SettingsUsed => ["Setting1"];
    public async Task<IEnumerable<CustomActionResultModel>> Execute(CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        return [new CustomActionResultModel("Failover Result")
        {
            TextResult = "Successfully Failed over to the other instance.",
        }];
    }
}