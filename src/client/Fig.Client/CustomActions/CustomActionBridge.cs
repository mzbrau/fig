using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Contracts.CustomActions;

namespace Fig.Client.CustomActions;

internal static class CustomActionBridge
{
    public static Func<Task<IEnumerable<CustomActionPollResponseDataContract>?>>? PollForCustomActionRequests;

    public static Func<CustomActionExecutionResultsDataContract, Task>? SendCustomActionResults;

    public static Func<List<CustomActionDefinitionDataContract>, Task>? RegisterCustomActions;
}