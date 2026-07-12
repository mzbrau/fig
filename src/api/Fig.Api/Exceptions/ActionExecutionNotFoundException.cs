namespace Fig.Api.Exceptions;

public class ActionExecutionNotFoundException 
    : Exception
{
    public ActionExecutionNotFoundException()
        : base("Action execution was not found.")
    {
    }

    public ActionExecutionNotFoundException(Guid executionId)
        : base($"Execution '{executionId}' was not found.")
    {
    }
}