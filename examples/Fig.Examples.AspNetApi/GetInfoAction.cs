using Fig.Client.CustomActions;

namespace Fig.Examples.AspNetApi;

public class GetInfoAction : ICustomAction
{
    public string Name => "Get Info";
    public string ButtonName => "GET IT";
    public string Description => "This action retrieves some information from the server and displays it.";
    public IEnumerable<string> SettingsUsed => ["Setting1"];
    public async Task<IEnumerable<CustomActionResultModel>> Execute(CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);

        return [new CustomActionResultModel("My Result")
        {
            TextResult = "This is a text result from the GetInfoAction.",
            DataGridResult =
            [
                new()
                {
                    { "Column1", "Value1" },
                    { "Column2", 123 },
                    { "Column3", true }
                },

                new()
                {
                    { "Column1", "Value2" },
                    { "Column2", 456 },
                    { "Column3", false }
                }
            ]
        }];
    }
}