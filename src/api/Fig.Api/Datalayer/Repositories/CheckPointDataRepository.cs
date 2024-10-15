using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Datalayer;
using Fig.Datalayer.BusinessEntities;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class CheckPointDataRepository : RepositoryBase<CheckPointDataBusinessEntity>, ICheckPointDataRepository
{
    private readonly IEncryptionService _encryptionService;

    public CheckPointDataRepository(ISession session, IEncryptionService encryptionService) : base(session)
    {
        _encryptionService = encryptionService;
    }

    public CheckPointDataBusinessEntity? GetData(Guid id)
    {
        var result = Get(id, false);
        result?.Decrypt(_encryptionService);
        return result;
    }

    public Guid Add(CheckPointDataBusinessEntity data)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        data.Encrypt(_encryptionService);
        return Save(data);
    }
}