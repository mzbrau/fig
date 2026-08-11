using System;
using System.Threading;
using Fig.Contracts.Status;

namespace Fig.Client.StatusProperties
{
    public static class CustomStatusPropertiesBridge
    {
        private static Func<CustomStatusPropertiesDataContract?>? _getSnapshot;

        public static Func<CustomStatusPropertiesDataContract?>? GetSnapshot => Volatile.Read(ref _getSnapshot);

        internal static void Register(Func<CustomStatusPropertiesDataContract?> getSnapshot)
        {
            Interlocked.Exchange(ref _getSnapshot, getSnapshot);
        }

        internal static void ClearIfRegistered(Func<CustomStatusPropertiesDataContract?> getSnapshot)
        {
            Interlocked.CompareExchange(ref _getSnapshot, null, getSnapshot);
        }
    }
}
