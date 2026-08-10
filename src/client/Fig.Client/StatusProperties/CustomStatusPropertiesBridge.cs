using System;
using System.Threading;
using Fig.Contracts.Status;

namespace Fig.Client.StatusProperties
{
    public static class CustomStatusPropertiesBridge
    {
        public static Func<CustomStatusPropertiesDataContract?>? GetSnapshot;

        internal static void Register(Func<CustomStatusPropertiesDataContract?> getSnapshot)
        {
            Interlocked.Exchange(ref GetSnapshot, getSnapshot);
        }

        internal static void ClearIfRegistered(Func<CustomStatusPropertiesDataContract?> getSnapshot)
        {
            Interlocked.CompareExchange(ref GetSnapshot, null, getSnapshot);
        }
    }
}
