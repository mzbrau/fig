using Fig.Contracts.Status;

namespace Fig.Client.StatusProperties
{
    /// <summary>
    /// Non-generic bridge used by the status poller when a typed store is registered.
    /// </summary>
    internal interface IFigStatusPropertiesSnapshotProvider
    {
        CustomStatusPropertiesDataContract? CreateSnapshot();
    }

    internal sealed class FigStatusPropertiesSnapshotProviderAdapter<T> : IFigStatusPropertiesSnapshotProvider
        where T : class, new()
    {
        private readonly FigStatusProperties<T> _store;

        public FigStatusPropertiesSnapshotProviderAdapter(FigStatusProperties<T> store)
        {
            _store = store;
        }

        public CustomStatusPropertiesDataContract? CreateSnapshot() => _store.CreateSnapshot();
    }
}
