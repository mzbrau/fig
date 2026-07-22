using Fig.Api.Reports.Implementations;
using Fig.Api.Reports.Rendering;
using Fig.Api.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Fig.Api.Reports;

public static class ReportServiceCollectionExtensions
{
    public static IServiceCollection AddFigReports(this IServiceCollection services)
    {
        services.AddScoped<IReportRegistry, ReportRegistry>();
        services.AddScoped<IReportParameterBinder, ReportParameterBinder>();
        services.AddScoped<IReportExecutionService, ReportExecutionService>();
        services.AddScoped<IReportRenderer, HtmlReportRenderer>();
        services.AddScoped<HtmlRenderer>();

        services.AddReport<UserActivityReport>();
        services.AddReport<SettingHistoryReport>();
        services.AddReport<ClientStatusReport>();
        services.AddReport<ClientHistoryReport>();
        services.AddReport<ClientUptimeReport>();
        services.AddReport<ClientVersionReport>();
        services.AddReport<SecurityAuditReport>();
        services.AddReport<ConfigurationInventoryReport>();
        services.AddReport<AccessPrivilegeReport>();
        services.AddReport<SecretHygieneReport>();
        services.AddReport<ExternallyManagedOverridesReport>();
        services.AddReport<FleetHealthReport>();
        services.AddReport<RegistrationDriftReport>();
        services.AddReport<RestartLiveReloadDebtReport>();
        services.AddReport<InstanceEnvironmentMatrixReport>();
        services.AddReport<ChangeAnalyticsReport>();
        services.AddReport<DeferredScheduledWorkReport>();
        services.AddReport<TimeMachineActivityReport>();
        services.AddReport<ImportExportActivityReport>();
        services.AddReport<WebhookDeliveryReport>();
        services.AddReport<CustomActionOutcomesReport>();
        services.AddReport<SettingGroupsCoverageReport>();
        services.AddReport<LookupUsageReport>();
        services.AddReport<BlastRadiusReport>();
        services.AddReport<AnomalyQuietPeriodReport>();
        services.AddReport<StaleConfigReport>();
        services.AddReport<IncidentCorrelationReport>();
        services.AddReport<FigPlatformReport>();

        return services;
    }

    public static IServiceCollection AddReport<TReport>(this IServiceCollection services)
        where TReport : class, IReport, IAuthenticatedService
    {
        services.AddScoped<TReport>();
        services.AddScoped<IReport>(sp => sp.GetRequiredService<TReport>());
        services.AddScoped<IAuthenticatedService>(sp => sp.GetRequiredService<TReport>());
        return services;
    }
}
