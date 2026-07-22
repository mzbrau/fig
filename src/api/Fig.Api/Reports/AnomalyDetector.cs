using Fig.Common.Constants;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Reports;

public record AnomalyMetric(string Name, int PeriodCount, int BaselineCount, bool IsAnomaly);

public static class AnomalyDetector
{
    public const int MinimumBaseline = 3;
    public const double Multiplier = 2.0;

    public static IReadOnlyList<AnomalyMetric> Detect(
        IEnumerable<EventLogBusinessEntity> periodEvents,
        IEnumerable<EventLogBusinessEntity> baselineEvents)
    {
        var period = periodEvents.ToList();
        var baseline = baselineEvents.ToList();

        return
        [
            Evaluate("Failed Logins", Count(period, EventMessage.LoginFailed), Count(baseline, EventMessage.LoginFailed)),
            Evaluate("Invalid Client Secrets", Count(period, EventMessage.InvalidClientSecretAttempt), Count(baseline, EventMessage.InvalidClientSecretAttempt)),
            Evaluate("Setting Updates", Count(period, EventMessage.SettingValueUpdated) + Count(period, EventMessage.ExternallyManagedSettingUpdatedByUser),
                Count(baseline, EventMessage.SettingValueUpdated) + Count(baseline, EventMessage.ExternallyManagedSettingUpdatedByUser)),
            Evaluate("Session Flaps", Count(period, EventMessage.NewSession) + Count(period, EventMessage.ExpiredSession),
                Count(baseline, EventMessage.NewSession) + Count(baseline, EventMessage.ExpiredSession))
        ];
    }

    public static AnomalyMetric Evaluate(string name, int periodCount, int baselineCount)
    {
        var isAnomaly = baselineCount >= MinimumBaseline && periodCount >= Multiplier * baselineCount;
        return new AnomalyMetric(name, periodCount, baselineCount, isAnomaly);
    }

    private static int Count(IEnumerable<EventLogBusinessEntity> events, string eventType)
        => events.Count(e => string.Equals(e.EventType, eventType, StringComparison.Ordinal));
}
