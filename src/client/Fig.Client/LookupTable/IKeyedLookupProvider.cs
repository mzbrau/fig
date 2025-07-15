using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fig.Client.LookupTable;

/// <summary>
/// Used to provide lookup tables where the options are filtered by the value of another setting.
/// </summary>
public interface IKeyedLookupProvider
{
    /// <summary>
    /// The name of the lookup table. This must match the name used in the LookupTable attribute on settings.
    /// </summary>
    string LookupName { get; }

    /// <summary>
    /// Gets the items for the lookup table.
    /// The key of the outer dictionary is the value of the other setting.
    /// The key of the inner dictionary is the option value, and the value is an optional alias.
    /// </summary>
    /// <returns></returns>
    Task<Dictionary<string, Dictionary<string, string?>>> GetItems();
}