using System;
using System.Threading.Tasks;
using Fig.Contracts.LookupTable;

namespace Fig.Client.LookupTable;

/// <summary>
/// Obsolete. Use <see cref="Fig.Client.ConfigurationProvider.FigClientBridgeRegistry"/> instead.
/// This class is retained for backward compatibility only.
/// </summary>
[Obsolete("LookupTableBridge is obsolete. Lookup table registration is now handled automatically via FigClientBridgeRegistry. This class has no effect and will be removed in a future version.")]
public static class LookupTableBridge
{
    [Obsolete("LookupTableBridge.RegisterLookupTable is obsolete and has no effect. Lookup table registration is now handled automatically via FigClientBridgeRegistry.")]
    public static Func<LookupTableDataContract, Task>? RegisterLookupTable;
}
