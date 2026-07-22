using Fig.Api.Services;
using Fig.Contracts.Reports;

namespace Fig.Api.Reports;

public abstract class ReportBase<TParameters, TModel> : AuthenticatedService, IReport<TParameters>, IAuthenticatedService
    where TParameters : class, new()
    where TModel : class
{
    public abstract string Id { get; }

    public abstract string Name { get; }

    public abstract string Category { get; }

    public abstract string Description { get; }

    public Type ParametersType => typeof(TParameters);

    public abstract Type BodyComponentType { get; }

    public virtual ReportPageOrientation PageOrientation => ReportPageOrientation.Portrait;

    public IList<ReportParameterDataContract> GetParameterDefinitions()
        => ReportParameterMetadataFactory.Create(typeof(TParameters));

    public async Task<object> ExecuteAsync(object parameters, CancellationToken cancellationToken = default)
    {
        if (parameters is not TParameters typed)
            throw new ArgumentException($"Expected parameters of type {typeof(TParameters).Name}", nameof(parameters));

        return await ExecuteAsync(typed, cancellationToken);
    }

    public abstract Task<object> ExecuteAsync(TParameters parameters, CancellationToken cancellationToken = default);

    protected Task<object> Result(TModel model) => Task.FromResult<object>(model);
}
