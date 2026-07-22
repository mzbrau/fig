using Fig.Api.Datalayer.Repositories;
using Fig.Api.Reports.Rendering.Components;
using Fig.Api.Reports.Rendering.Views;

namespace Fig.Api.Reports.Implementations;

public class LookupUsageParameters
{
}

public class LookupUsageReportModel
{
    public IReadOnlyList<SummaryCardItem> Summary { get; set; } = [];
    public IReadOnlyList<LookupUsageRow> UsedLookups { get; set; } = [];
    public IReadOnlyList<LookupUnusedRow> UnusedLookups { get; set; } = [];
    public IReadOnlyList<LookupKeyReferenceRow> KeySettingReferences { get; set; } = [];
}

public class LookupUsageRow
{
    public string LookupName { get; set; } = string.Empty;
    public string ClientDefined { get; set; } = string.Empty;
    public int EntryCount { get; set; }
    public int ConsumerCount { get; set; }
    public string Consumers { get; set; } = string.Empty;
}

public class LookupUnusedRow
{
    public string LookupName { get; set; } = string.Empty;
    public string ClientDefined { get; set; } = string.Empty;
    public int EntryCount { get; set; }
}

public class LookupKeyReferenceRow
{
    public string ClientDisplay { get; set; } = string.Empty;
    public string SettingName { get; set; } = string.Empty;
    public string LookupTableKey { get; set; } = string.Empty;
    public string LookupKeySettingName { get; set; } = string.Empty;
}

public class LookupUsageReport : ReportBase<LookupUsageParameters, LookupUsageReportModel>
{
    private readonly ILookupTablesRepository _lookupTablesRepository;
    private readonly ISettingClientRepository _settingClientRepository;

    public LookupUsageReport(
        ILookupTablesRepository lookupTablesRepository,
        ISettingClientRepository settingClientRepository)
    {
        _lookupTablesRepository = lookupTablesRepository;
        _settingClientRepository = settingClientRepository;
    }

    public override string Id => "lookup-usage";
    public override string Name => "Lookup Usage Report";
    public override string Category => "Compliance";
    public override string Description =>
        "Shows which lookup tables are referenced by settings, which are unused, and LookupKeySettingName references.";
    public override Type BodyComponentType => typeof(LookupUsageReportView);
    public override ReportPageOrientation PageOrientation => ReportPageOrientation.Landscape;

    public override async Task<object> ExecuteAsync(LookupUsageParameters parameters, CancellationToken cancellationToken = default)
    {
        var lookups = (await _lookupTablesRepository.GetAllItems())
            .OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var clients = await _settingClientRepository.GetAllClients(RequireAuthenticatedUser());
        var inventory = SettingInventoryProjector.ProjectAll(clients);

        var consumersByLookup = inventory
            .Where(r => !string.IsNullOrWhiteSpace(r.LookupTableKey))
            .GroupBy(r => r.LookupTableKey!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g
                    .Select(r => $"{r.ClientDisplay} · {r.SettingName}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        var used = new List<LookupUsageRow>();
        var unused = new List<LookupUnusedRow>();

        foreach (var lookup in lookups)
        {
            consumersByLookup.TryGetValue(lookup.Name, out var consumers);
            consumers ??= [];
            var entryCount = lookup.LookupTable?.Count ?? 0;

            if (consumers.Count == 0)
            {
                unused.Add(new LookupUnusedRow
                {
                    LookupName = lookup.Name,
                    ClientDefined = lookup.IsClientDefined ? "Yes" : "No",
                    EntryCount = entryCount
                });
            }
            else
            {
                used.Add(new LookupUsageRow
                {
                    LookupName = lookup.Name,
                    ClientDefined = lookup.IsClientDefined ? "Yes" : "No",
                    EntryCount = entryCount,
                    ConsumerCount = consumers.Count,
                    Consumers = string.Join("; ", consumers)
                });
            }
        }

        var keyReferences = inventory
            .Where(r => !string.IsNullOrWhiteSpace(r.LookupKeySettingName))
            .Select(r => new LookupKeyReferenceRow
            {
                ClientDisplay = r.ClientDisplay,
                SettingName = r.SettingName,
                LookupTableKey = r.LookupTableKey ?? string.Empty,
                LookupKeySettingName = r.LookupKeySettingName!
            })
            .OrderBy(r => r.ClientDisplay, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.SettingName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Settings that reference a lookup name not present in the lookup catalogue.
        var orphanedReferences = inventory
            .Where(r => !string.IsNullOrWhiteSpace(r.LookupTableKey) &&
                        !lookups.Any(l => string.Equals(l.Name, r.LookupTableKey, StringComparison.OrdinalIgnoreCase)))
            .Select(r => $"{r.ClientDisplay} · {r.SettingName} → {r.LookupTableKey}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return new LookupUsageReportModel
        {
            Summary =
            [
                new SummaryCardItem("Lookups", lookups.Count.ToString()),
                new SummaryCardItem("Used", used.Count.ToString()),
                new SummaryCardItem("Unused", unused.Count.ToString()),
                new SummaryCardItem("Key Setting Refs", keyReferences.Count.ToString()),
                new SummaryCardItem("Missing Lookup Refs", orphanedReferences.ToString())
            ],
            UsedLookups = used,
            UnusedLookups = unused,
            KeySettingReferences = keyReferences
        };
    }
}
