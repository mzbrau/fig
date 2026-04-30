using System;

namespace Fig.Client.ConfigurationProvider;

internal class FigClientBridgeOptions
{
    public static FigClientBridgeOptions Default { get; } = new(
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30));

    public FigClientBridgeOptions(TimeSpan customActionPollInterval, TimeSpan lookupTableRegistrationDelay)
    {
        CustomActionPollInterval = customActionPollInterval;
        LookupTableRegistrationDelay = lookupTableRegistrationDelay;
    }

    public TimeSpan CustomActionPollInterval { get; }

    public TimeSpan LookupTableRegistrationDelay { get; }
}

