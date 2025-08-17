using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fig.Client.Abstractions.LookupTable;

/// <summary>
/// Used to provide lookup tables where the options are dynamic or only known at runtime.
/// </summary>
public interface ILookupProvider
{
    /// <summary>
    /// The name of the lookup table. This must match the name used in the LookupTable attribute on settings.
    /// </summary>
    string LookupName { get; }

    /// <summary>
    /// The key of the dictionary is the option value, and the value is an optional alias.
    /// </summary>
    /// <returns></returns>
    Task<Dictionary<string, string?>> GetItems();
}