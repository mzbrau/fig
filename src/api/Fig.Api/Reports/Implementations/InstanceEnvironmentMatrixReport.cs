using System.Net;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Api.Secrets;
using Fig.Contracts.Reports;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class InstanceEnvironmentMatrixParameters
{
    [ReportParameter("Client", LookupKind = ReportParameterLookupKind.Clients)]
    public string ClientName { get; set; } = string.Empty;

    [ReportParameter("Instance")]
    public string? Instance { get; set; }
}

public class InstanceEnvironmentMatrixReportModel
{
    public string ClientName { get; set; } = string.Empty;
    public string? Notice { get; set; }
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<string> InstanceColumns { get; set; } = [];
    public IReadOnlyList<InstanceMatrixRow> Rows { get; set; } = [];
}

public class InstanceMatrixRow
{
    public string SettingName { get; set; } = string.Empty;
    public bool EnvironmentSpecific { get; set; }
    public bool Diverges { get; set; }
    public IReadOnlyList<string> CellValues { get; set; } = [];
}

public class InstanceEnvironmentMatrixReport : ReportBase<InstanceEnvironmentMatrixParameters, InstanceEnvironmentMatrixReportModel>
{
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly ISecretStoreHandler _secretStoreHandler;

    public InstanceEnvironmentMatrixReport(
        ISettingClientRepository settingClientRepository,
        ISecretStoreHandler secretStoreHandler)
    {
        _settingClientRepository = settingClientRepository;
        _secretStoreHandler = secretStoreHandler;
    }

    public override string Id => "instance-environment-matrix";
    public override string Name => "Instance / Environment Matrix";
    public override string Category => "Operations";
    public override string Description =>
        "Compares setting values across all instances of a client, highlighting environment-specific settings and divergent values.";
    public override Type BodyComponentType => typeof(InstanceEnvironmentMatrixReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(InstanceEnvironmentMatrixParameters parameters, CancellationToken cancellationToken = default)
    {
        var clientName = ReportDateRange.NormalizeOptionalClient(parameters.ClientName)
                         ?? throw new ReportParameterValidationException("Parameter 'ClientName' is required.");

        ThrowIfNoAccess(clientName);

        // Instance is intentionally ignored — load every instance of the selected client name.
        var instances = (await _settingClientRepository.GetAllInstancesOfClient(clientName, upgradeLock: false))
            .OrderBy(c => c.Instance ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (instances.Count == 0)
            throw new KeyNotFoundException($"Client '{clientName}' was not found.");

        foreach (var instance in instances)
            await _secretStoreHandler.HydrateSecrets(instance);

        var instanceSnapshots = instances.Select(c => new InstanceSnapshot(
            FormatInstanceLabel(c.Instance),
            c.Settings.ToList())).ToList();

        var rows = BuildRows(instanceSnapshots);

        var divergeCount = rows.Count(r => r.Diverges);
        var envSpecificCount = rows.Count(r => r.EnvironmentSpecific);
        string? notice = instances.Count < 2
            ? "Only one instance is registered for this client. Divergence highlighting requires two or more instances."
            : null;

        return new InstanceEnvironmentMatrixReportModel
        {
            ClientName = clientName,
            Notice = notice,
            Summary =
            [
                new SummaryCardItem("Instances", instances.Count.ToString()),
                new SummaryCardItem("Settings", rows.Count.ToString()),
                new SummaryCardItem("Divergent Settings", divergeCount.ToString()),
                new SummaryCardItem("Environment Specific", envSpecificCount.ToString())
            ],
            InstanceColumns = instanceSnapshots.Select(i => i.Label).ToList(),
            Rows = rows
        };
    }

    internal static IReadOnlyList<InstanceMatrixRow> BuildRows(IReadOnlyList<InstanceSnapshot> instanceSnapshots)
    {
        var settingNames = instanceSnapshots
            .SelectMany(i => i.Settings.Select(s => s.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return settingNames.Select(name =>
        {
            var settings = instanceSnapshots
                .Select(i => i.Settings.FirstOrDefault(s =>
                    string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var compareValues = settings
                .Select(s => s is null
                    ? string.Empty
                    : ReportValueFormatter.FormatSettingValue(s.Value))
                .ToList();

            var displayValues = settings
                .Select(s =>
                {
                    if (s is null)
                        return "—";
                    if (s.IsSecret)
                        return ReportDataGridHtml.SecretMask;
                    return WebUtility.HtmlEncode(ReportValueFormatter.FormatSettingValue(s.Value));
                })
                .ToList();

            var envSpecific = settings.Any(s => s?.EnvironmentSpecific == true);
            var diverges = compareValues.Distinct(StringComparer.Ordinal).Count() > 1;

            return new InstanceMatrixRow
            {
                SettingName = name,
                EnvironmentSpecific = envSpecific,
                Diverges = diverges,
                CellValues = displayValues
            };
        }).ToList();
    }

    private static string FormatInstanceLabel(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? "(default)" : instance;

    internal sealed record InstanceSnapshot(string Label, IReadOnlyList<SettingBusinessEntity> Settings);
}
