using System.Runtime.CompilerServices;
using Fig.Client.Abstractions.CustomActions;
using Fig.Client.Abstractions.Data;

namespace Fig.Examples.AspNetApi;

public class FailoverAction : ICustomAction
{
    public string Name => "Failover";

    public string ButtonName => "Perform Failover";

    public string Description => "Fail over to another instance of the service.";

    public IEnumerable<string> SettingsUsed => [nameof(Settings.Location)]; // For example.

    public Classification Classification => Classification.Technical;

    public async IAsyncEnumerable<CustomActionResultModel> Execute([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Delay(3000, cancellationToken);
        
        yield return ResultBuilder.CreateSuccessResult("Failover Result")
            .WithTextResult("The failover action has been initiated successfully.");
    }
}