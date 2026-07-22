using Fig.Contracts.Reports;

namespace Fig.Api.Reports;

public interface IReport
{
    string Id { get; }

    string Name { get; }

    string Category { get; }

    string Description { get; }

    Type ParametersType { get; }

    Type BodyComponentType { get; }

    ReportPageOrientation PageOrientation { get; }

    IList<ReportParameterDataContract> GetParameterDefinitions();

    Task<object> ExecuteAsync(object parameters, CancellationToken cancellationToken = default);
}

public interface IReport<TParameters> : IReport
{
    Task<object> ExecuteAsync(TParameters parameters, CancellationToken cancellationToken = default);
}
