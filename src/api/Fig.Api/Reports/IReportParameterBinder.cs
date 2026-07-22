namespace Fig.Api.Reports;

public interface IReportParameterBinder
{
    object Bind(Type parametersType, IDictionary<string, object?> rawParameters);
}
