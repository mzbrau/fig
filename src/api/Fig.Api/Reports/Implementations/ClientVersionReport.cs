using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports.Implementations;

public class ClientVersionParameters
{
    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}

public class ClientVersionReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<ClientVersionSessionRow> Sessions { get; set; } = [];
    public IReadOnlyList<ChartSlice> FigVersionBreakdown { get; set; } = [];
    public IReadOnlyList<MultiVersionApplicationRow> MultiVersionApplications { get; set; } = [];
}

public class ClientVersionSessionRow
{
    public string ClientName { get; set; } = string.Empty;
    public string InstanceDisplay { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public string FigVersion { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
    public bool IsMultiVersion { get; set; }
}

public class MultiVersionApplicationRow
{
    public string ClientName { get; set; } = string.Empty;
    public string ApplicationVersions { get; set; } = string.Empty;
    public int VersionCount { get; set; }
    public int SessionCount { get; set; }
}

public class ClientVersionReport : ReportBase<ClientVersionParameters, ClientVersionReportModel>
{
    private readonly IClientStatusRepository _clientStatusRepository;

    public ClientVersionReport(IClientStatusRepository clientStatusRepository)
    {
        _clientStatusRepository = clientStatusRepository;
    }

    public override string Id => "client-version";
    public override string Name => "Client Version Report";
    public override string Category => "Operations";
    public override string Description =>
        "Lists unique clients that connected in the selected period with application and Fig client versions, highlighting apps running multiple application versions.";
    public override Type BodyComponentType => typeof(ClientVersionReportView);

    public override async Task<object> ExecuteAsync(ClientVersionParameters parameters, CancellationToken cancellationToken = default)
    {
        var (from, to) = ReportDateRange.Validate(parameters.From, parameters.To);
        var clients = await _clientStatusRepository.GetAllClients(RequireAuthenticatedUser());

        var sessionRows = clients
            .SelectMany(client => client.RunSessions
                .Where(s => SessionOverlaps(s, from, to))
                .Select(s => new SessionSnapshot(
                    client.Name,
                    FormatInstance(client.Instance),
                    s.Hostname ?? "—",
                    string.IsNullOrWhiteSpace(s.ApplicationVersion) ? "—" : s.ApplicationVersion,
                    string.IsNullOrWhiteSpace(s.FigVersion) ? "—" : s.FigVersion,
                    s.LastSeen)))
            .ToList();

        var multiVersionClientNames = BuildMultiVersionClientNames(sessionRows);

        var sessions = sessionRows
            .Select(s => new ClientVersionSessionRow
            {
                ClientName = s.ClientName,
                InstanceDisplay = s.InstanceDisplay,
                Hostname = s.Hostname,
                ApplicationVersion = s.ApplicationVersion,
                FigVersion = s.FigVersion,
                LastSeen = s.LastSeen,
                IsMultiVersion = multiVersionClientNames.Contains(s.ClientName)
            })
            .OrderBy(s => s.ClientName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.InstanceDisplay, StringComparer.OrdinalIgnoreCase)
            .ThenBy(s => s.Hostname, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var multiVersionApplications = sessionRows
            .Where(s => multiVersionClientNames.Contains(s.ClientName))
            .GroupBy(s => s.ClientName, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var versions = g.Select(x => x.ApplicationVersion)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return new MultiVersionApplicationRow
                {
                    ClientName = g.Key,
                    ApplicationVersions = string.Join(", ", versions),
                    VersionCount = versions.Count,
                    SessionCount = g.Count()
                };
            })
            .OrderBy(r => r.ClientName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var figVersionBreakdown = BuildFigVersionBreakdown(sessionRows);
        var uniqueClients = sessionRows
            .Select(s => s.ClientName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return new ClientVersionReportModel
        {
            Summary =
            [
                new SummaryCardItem("Unique Clients", uniqueClients.ToString()),
                new SummaryCardItem("Sessions", sessionRows.Count.ToString()),
                new SummaryCardItem("Fig Versions", figVersionBreakdown.Count.ToString()),
                new SummaryCardItem("Multi-Version Apps", multiVersionApplications.Count.ToString())
            ],
            Sessions = sessions,
            FigVersionBreakdown = figVersionBreakdown,
            MultiVersionApplications = multiVersionApplications
        };
    }

    internal static HashSet<string> BuildMultiVersionClientNames(IEnumerable<SessionSnapshot> sessions)
    {
        return sessions
            .GroupBy(s => s.ClientName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Select(s => s.ApplicationVersion).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    internal static IReadOnlyList<ChartSlice> BuildFigVersionBreakdown(IEnumerable<SessionSnapshot> sessions)
    {
        return sessions
            .GroupBy(s => s.FigVersion, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ChartSlice(g.Key, g.Count()))
            .ToList();
    }

    private static bool SessionOverlaps(ClientRunSessionBusinessEntity session, DateTime from, DateTime to)
        => session.StartTimeUtc <= to && session.LastSeen >= from;

    private static string FormatInstance(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? "(default)" : instance;

    internal sealed record SessionSnapshot(
        string ClientName,
        string InstanceDisplay,
        string Hostname,
        string ApplicationVersion,
        string FigVersion,
        DateTime LastSeen);
}
