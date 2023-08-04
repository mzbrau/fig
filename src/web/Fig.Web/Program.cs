using Fig.Client.Constants;
using Fig.Common;
using Fig.Common.Timer;
using Fig.Web;
using Fig.Web.Builders;
using Fig.Web.Converters;
using Fig.Web.Facades;
using Fig.Web.Factories;
using Fig.Web.Notifications;
using Fig.Web.Services;
using Fig.Web.Utils;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var webSettings = builder.Configuration.GetSection("WebSettings");
builder.Services.Configure<WebSettings>(webSettings);
var figUri = webSettings.Get<WebSettings>()?.ApiUri;

if (string.IsNullOrEmpty(figUri))
    throw new ApplicationException("ApiUri must be configured");

builder.Services.AddHttpClient(HttpClientNames.FigApi, c =>
{
    c.BaseAddress = new Uri(figUri);
    c.DefaultRequestHeaders.Add("Accept", "application/json");
}); //.ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler()
 // { AutomaticDecompression = DecompressionMethods.GZip });

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IHttpService, HttpService>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<ISettingsDefinitionConverter, SettingsDefinitionConverter>();
builder.Services.AddScoped<IEventLogConverter, EventLogConverter>();
builder.Services.AddScoped<IClientRunSessionConverter, ClientRunSessionConverter>();
builder.Services.AddScoped<ISettingClientFacade, SettingClientFacade>();
builder.Services.AddScoped<IUsersFacade, UsersFacade>();
builder.Services.AddScoped<IUserConverter, UserConverter>();
builder.Services.AddScoped<IEventsFacade, EventsFacade>();
builder.Services.AddScoped<IDataFacade, DataFacade>();
builder.Services.AddScoped<IClientStatusFacade, ClientStatusFacade>();
builder.Services.AddScoped<IApiStatusFacade, ApiStatusFacade>();
builder.Services.AddScoped<IWebHookFacade, WebHookFacade>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<INotificationFactory, NotificationFactory>();
builder.Services.AddScoped<IWebHookTypeFactory, WebHookTypeFactory>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<IWebHookAnalysisService, WebHookAnalysisService>();
builder.Services.AddScoped<ISettingGroupBuilder, SettingGroupBuilder>();
builder.Services.AddScoped<ISettingHistoryConverter, SettingHistoryConverter>();
builder.Services.AddScoped<ISettingVerificationConverter, SettingVerificationConverter>();
builder.Services.AddScoped<IFigConfigurationConverter, FigConfigurationConverter>();
builder.Services.AddScoped<IWebHookClientConverter, WebHookClientConverter>();
builder.Services.AddScoped<IWebHookConverter, WebHookConverter>();
builder.Services.AddScoped<IConfigurationFacade, ConfigurationFacade>();
builder.Services.AddScoped<ILookupTablesFacade, LookupTableFacade>();
builder.Services.AddScoped<ILookupTableConverter, LookupTableConverter>();
builder.Services.AddScoped<IApiStatusConverter, ApiStatusConverter>();
builder.Services.AddScoped<IMarkdownReportGenerator, MarkdownReportGenerator>();
builder.Services.AddScoped<IApiVersionFacade, ApiVersionFacade>();
builder.Services.AddScoped<ITimerFactory, TimerFactory>();
builder.Services.AddScoped<IVersionHelper, VersionHelper>();

var host = builder.Build();

var accountService = host.Services.GetRequiredService<IAccountService>();
await accountService.Initialize();

await host.RunAsync();