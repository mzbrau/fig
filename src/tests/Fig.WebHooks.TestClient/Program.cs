using Fig.WebHooks.Contracts;
using Fig.WebHooks.TestClient;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IDataRegurgitationService, DataRegurgitationService>();
builder.Services.AddControllers().AddNewtonsoftJson();

var app = builder.Build();

app.UseMiddleware<FigWebHookAuthMiddleware>();

app.MapPost("/NewClientRegistration",
    (ClientRegistrationDataContract dc, IDataRegurgitationService service) => service.Add(dc));

app.MapPost("/UpdatedClientRegistration",
    (ClientRegistrationDataContract dc, IDataRegurgitationService service) => service.Add(dc));

app.MapPost("/ClientStatusChanged",
    (ClientStatusChangedDataContract dc, IDataRegurgitationService service) => service.Add(dc));

app.MapPost("/SettingValueChanged",
    (SettingValueChangedDataContract dc, IDataRegurgitationService service) => service.Add(dc));

app.MapPost("/MinRunSessions",
    (MinRunSessionsDataContract dc, IDataRegurgitationService service) => service.Add(dc));

app.MapPost("/ConfigurationError",
    (ClientConfigurationErrorDataContract dc, IDataRegurgitationService service) => service.Add(dc));

app.MapPost("/HealthStatusChanged",
    (ClientHealthChangedDataContract dc, IDataRegurgitationService service) => service.Add(dc));

app.MapGet("/", 
    (DateTime fromTimeUtc, IDataRegurgitationService service) => service.GetAllFromDateTime(fromTimeUtc));

app.Run();
