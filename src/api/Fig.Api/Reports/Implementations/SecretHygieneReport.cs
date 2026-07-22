using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Contracts.Reports;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class SecretHygieneParameters
{
    [ReportParameter("Client", LookupKind = ReportParameterLookupKind.Clients)]
    public string? ClientName { get; set; }

    [ReportParameter("Instance")]
    public string? Instance { get; set; }
}

public class SecretHygieneReportModel
{
    public string ScopeDisplay { get; set; } = string.Empty;
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<SecretSettingRow> SecretSettings { get; set; } = [];
    public IReadOnlyList<PreviousSecretWindowRow> PreviousSecretWindows { get; set; } = [];
    public IReadOnlyList<SummaryCardItem> ApiRotation { get; set; } = [];
}

public class SecretSettingRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public string SettingName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public DateTime? LastChanged { get; set; }
    public string Age { get; set; } = string.Empty;
}

public class PreviousSecretWindowRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public DateTime ExpiryUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SecretHygieneReport : ReportBase<SecretHygieneParameters, SecretHygieneReportModel>
{
    private static readonly TimeSpan RecentWindow = TimeSpan.FromDays(30);

    private readonly ISettingClientRepository _settingClientRepository;
    private readonly IApiSecretRotationStateRepository _apiSecretRotationStateRepository;

    public SecretHygieneReport(
        ISettingClientRepository settingClientRepository,
        IApiSecretRotationStateRepository apiSecretRotationStateRepository)
    {
        _settingClientRepository = settingClientRepository;
        _apiSecretRotationStateRepository = apiSecretRotationStateRepository;
    }

    public override string Id => "secret-hygiene";
    public override string Name => "Secret Hygiene Report";
    public override string Category => "Security";
    public override string Description =>
        "Reviews secret setting age, previous client-secret windows, and API secret rotation state. Secret values are never shown.";
    public override Type BodyComponentType => typeof(SecretHygieneReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(SecretHygieneParameters parameters, CancellationToken cancellationToken = default)
    {
        var clientName = ReportDateRange.NormalizeOptionalClient(parameters.ClientName);
        var instance = string.IsNullOrWhiteSpace(parameters.Instance) ? null : parameters.Instance.Trim();
        var now = DateTime.UtcNow;

        IList<SettingClientBusinessEntity> clients;
        if (clientName is not null)
        {
            ThrowIfNoAccess(clientName);
            var client = await _settingClientRepository.GetClient(clientName, instance)
                         ?? throw new KeyNotFoundException($"Client '{clientName}' was not found.");
            clients = [client];
        }
        else
        {
            clients = await _settingClientRepository.GetAllClients(RequireAuthenticatedUser());
        }

        var secretInventory = SettingInventoryProjector.ProjectAll(clients, secretsOnly: true);
        var secretRows = secretInventory
            .Select(r => new SecretSettingRow
            {
                ClientDisplay = r.ClientDisplay,
                SettingName = r.SettingName,
                Category = r.Category,
                Classification = r.Classification,
                LastChanged = r.LastChanged,
                Age = FormatAge(r.LastChanged, now)
            })
            .OrderByDescending(r => r.LastChanged.HasValue ? 0 : 1)
            .ThenBy(r => r.LastChanged ?? DateTime.MaxValue)
            .ThenBy(r => r.ClientDisplay, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var recentCutoff = now - RecentWindow;
        var previousSecretWindows = clients
            .Where(c => c.PreviousClientSecretExpiryUtc is not null &&
                        c.PreviousClientSecretExpiryUtc >= recentCutoff)
            .Select(c => new PreviousSecretWindowRow
            {
                ClientDisplay = ReportValueFormatter.FormatClientDisplay(c.Name, c.Instance),
                ExpiryUtc = c.PreviousClientSecretExpiryUtc!.Value,
                Status = c.PreviousClientSecretExpiryUtc > now ? "Active window" : "Recently expired"
            })
            .OrderByDescending(r => r.ExpiryUtc)
            .ToList();

        var rotation = await _apiSecretRotationStateRepository.GetLatest();
        var neverChanged = secretRows.Count(r => r.LastChanged is null);

        return new SecretHygieneReportModel
        {
            ScopeDisplay = clientName is null
                ? "All clients"
                : ReportValueFormatter.FormatClientDisplay(clientName, instance),
            Summary =
            [
                new SummaryCardItem("Secret Settings", secretRows.Count.ToString()),
                new SummaryCardItem("Never Changed", neverChanged.ToString()),
                new SummaryCardItem("Previous Secret Windows", previousSecretWindows.Count.ToString()),
                new SummaryCardItem("API Rotation Status", rotation?.Status ?? "None")
            ],
            SecretSettings = secretRows,
            PreviousSecretWindows = previousSecretWindows,
            ApiRotation = BuildApiRotationCards(rotation)
        };
    }

    private static IReadOnlyList<SummaryCardItem> BuildApiRotationCards(ApiSecretRotationStateBusinessEntity? rotation)
        => BuildApiRotationCardsCore(rotation);

    internal static IReadOnlyList<SummaryCardItem> BuildApiRotationCardsCore(ApiSecretRotationStateBusinessEntity? rotation)
    {
        if (rotation is null)
        {
            return
            [
                new SummaryCardItem("Status", "No rotation recorded")
            ];
        }

        return
        [
            new SummaryCardItem("Status", rotation.Status),
            new SummaryCardItem("Last Stage", rotation.LastCompletedStage ?? "—"),
            new SummaryCardItem("Processed Records", rotation.ProcessedRecords.ToString()),
            new SummaryCardItem("Updated", FormatTimestamp(rotation.UpdatedAtUtc)),
            new SummaryCardItem("Completed", FormatTimestamp(rotation.CompletedAtUtc)),
            new SummaryCardItem("Last Error", string.IsNullOrWhiteSpace(rotation.LastError) ? "None" : rotation.LastError!)
        ];
    }

    internal static string FormatAge(DateTime? lastChanged, DateTime now)
    {
        if (lastChanged is null)
            return "Never changed";

        var age = now - lastChanged.Value.ToUniversalTime();
        if (age.TotalDays >= 1)
            return $"{(int)age.TotalDays}d";
        if (age.TotalHours >= 1)
            return $"{(int)age.TotalHours}h";
        if (age.TotalMinutes >= 1)
            return $"{(int)age.TotalMinutes}m";
        return "<1m";
    }

    private static string FormatTimestamp(DateTime? value)
        => value is null
            ? "—"
            : value.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
}
