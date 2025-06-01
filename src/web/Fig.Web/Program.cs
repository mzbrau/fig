using Fig.Common;
using Fig.Common.Events;
using Fig.Common.NetStandard.Constants;
using Fig.Common.Timer;
using Fig.Web;
using Fig.Web.Builders;
using Fig.Web.Converters;
using Fig.Web.Facades;
using Fig.Web.Factories;
using Fig.Web.MarkdownReport;
using Fig.Web.Notifications;
using Fig.Web.Scripting;
using Fig.Web.Services;
using Fig.Web.Utils;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;
using Toolbelt.Blazor.Extensions.DependencyInjection;

var hostBuilder = WebAssemblyHostBuilder.CreateDefault(args);
var config = hostBuilder.Configuration.GetSection("WebSettings");

await BuildApplication(hostBuilder);

async Task BuildApplication(WebAssemblyHostBuilder builder)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");

    builder.Services.Configure<WebSettings>(config);
    var figUri = config.Get<WebSettings>()?.ApiUri;

    if (string.IsNullOrEmpty(figUri))
        throw new ApplicationException("ApiUri must be configured");

    builder.Services.AddHttpClient(HttpClientNames.FigApi, c =>
    {
        c.BaseAddress = new Uri(figUri);
        c.DefaultRequestHeaders.Add("Accept", "application/json");
    }); //.ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler()
    // { AutomaticDecompression = DecompressionMethods.GZip });
    
    builder.Services.AddHttpClient(HttpClientNames.WebApp, c =>
    {
        c.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
    });
    
    builder.Services.AddRadzenComponents();
    
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<IHttpService, HttpService>();
    builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
    builder.Services.AddScoped<ISettingsDefinitionConverter, SettingsDefinitionConverter>();
    builder.Services.AddScoped<IEventLogConverter, EventLogConverter>();
    builder.Services.AddScoped<IClientRunSessionConverter, ClientRunSessionConverter>();
    builder.Services.AddScoped<ISettingClientFacade, SettingClientFacade>();
    builder.Services.AddScoped<IUsersFacade, UsersFacade>();
    builder.Services.AddScoped<ITimeMachineFacade, TimeMachineFacade>();
    builder.Services.AddScoped<IUserConverter, UserConverter>();
    builder.Services.AddScoped<ICheckPointConverter, CheckPointConverter>();
    builder.Services.AddScoped<IEventsFacade, EventsFacade>();
    builder.Services.AddScoped<IDataFacade, DataFacade>();
    builder.Services.AddScoped<ISchedulingFacade, SchedulingFacade>();
    builder.Services.AddScoped<IClientStatusFacade, ClientStatusFacade>();
    builder.Services.AddScoped<IApiStatusFacade, ApiStatusFacade>();
    builder.Services.AddScoped<IWebHookFacade, WebHookFacade>();
    builder.Services.AddScoped<ICustomActionFacade, CustomActionFacade>(); // Added
    builder.Services.AddScoped<NotificationService>();
    builder.Services.AddScoped<INotificationFactory, NotificationFactory>();
    builder.Services.AddScoped<IWebHookTypeFactory, WebHookTypeFactory>();
    builder.Services.AddScoped<IImportTypeFactory, ImportTypeFactory>();
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
    builder.Services.AddScoped<IScriptRunner, ScriptRunner>();
    builder.Services.AddScoped<IInfiniteLoopDetector, InfiniteLoopDetector>();
    builder.Services.AddScoped<IBeautifyLoader, BeautifyLoader>();
    builder.Services.AddSingleton<IEventDistributor, EventDistributor>();
    builder.Services.AddHotKeys2();
    
    var host = builder.Build();

    AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(2));

    var accountService = host.Services.GetRequiredService<IAccountService>();
    await accountService.Initialize();

    await host.RunAsync();
}

