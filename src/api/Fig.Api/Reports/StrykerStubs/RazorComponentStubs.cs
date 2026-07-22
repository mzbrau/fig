using Fig.Api.Reports;
using Microsoft.AspNetCore.Components;

namespace Fig.Api.Reports.Rendering
{
    /// <summary>
    /// Stand-ins for Razor-generated component types when Stryker runs.
    /// Stryker cannot load the .NET 10 Razor source generator (ReferencesNewerCompiler),
    /// so these stubs keep Fig.Api compiling under STRYKER_RUNNING=1.
    /// </summary>
    public class ReportDocument : ComponentBase
    {
        [Parameter]
        public ReportRenderContext Context { get; set; } = default!;
    }
}

namespace Fig.Api.Reports.Rendering.Views
{
    public class AccessPrivilegeReportView : ComponentBase;
    public class AnomalyQuietPeriodReportView : ComponentBase;
    public class BlastRadiusReportView : ComponentBase;
    public class ChangeAnalyticsReportView : ComponentBase;
    public class ClientHistoryReportView : ComponentBase;
    public class ClientStatusReportView : ComponentBase;
    public class ClientUptimeReportView : ComponentBase;
    public class ClientVersionReportView : ComponentBase;
    public class ConfigurationInventoryReportView : ComponentBase;
    public class CustomActionOutcomesReportView : ComponentBase;
    public class DeferredScheduledWorkReportView : ComponentBase;
    public class ExternallyManagedOverridesReportView : ComponentBase;
    public class FigPlatformReportView : ComponentBase;
    public class FleetHealthReportView : ComponentBase;
    public class ImportExportActivityReportView : ComponentBase;
    public class IncidentCorrelationReportView : ComponentBase;
    public class InstanceEnvironmentMatrixReportView : ComponentBase;
    public class LookupUsageReportView : ComponentBase;
    public class RegistrationDriftReportView : ComponentBase;
    public class RestartLiveReloadDebtReportView : ComponentBase;
    public class SecretHygieneReportView : ComponentBase;
    public class SecurityAuditReportView : ComponentBase;
    public class SettingGroupsCoverageReportView : ComponentBase;
    public class SettingHistoryReportView : ComponentBase;
    public class StaleConfigReportView : ComponentBase;
    public class TimeMachineActivityReportView : ComponentBase;
    public class UserActivityReportView : ComponentBase;
    public class WebhookDeliveryReportView : ComponentBase;
}
