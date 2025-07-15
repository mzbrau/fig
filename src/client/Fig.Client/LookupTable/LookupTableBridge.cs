using System;
using System.Threading.Tasks;
using Fig.Contracts.LookupTable;

namespace Fig.Client.LookupTable;

public static class LookupTableBridge
{
    public static Func<LookupTableDataContract, Task>? RegisterLookupTable;
}