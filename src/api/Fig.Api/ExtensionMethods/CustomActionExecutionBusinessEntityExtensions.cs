using Fig.Contracts.CustomActions;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class CustomActionExecutionBusinessEntityExtensions
{
    public static ExecutionStatus GetStatus(this CustomActionExecutionBusinessEntity execution)
    {
        ExecutionStatus status;
        if (string.IsNullOrEmpty(execution.HandlingInstance))
        {
            status = ExecutionStatus.Submitted;
        }
        else if (execution.ExecutedAt is not null)
        {
            status = ExecutionStatus.Completed;
        }
        else
        {
            status = ExecutionStatus.SentToClient;
        }

        return status;
    }
}