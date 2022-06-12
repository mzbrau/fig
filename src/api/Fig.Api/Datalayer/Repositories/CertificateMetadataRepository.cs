using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class CertificateMetadataRepository : RepositoryBase<CertificateMetadataBusinessEntity>, ICertificateMetadataRepository
{
    public CertificateMetadataRepository(IFigSessionFactory sessionFactory)
        : base(sessionFactory)
    {
    }

    public CertificateMetadataBusinessEntity? GetInUse()
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<CertificateMetadataBusinessEntity>();
        criteria.Add(Restrictions.Eq("InUse", true));
        return criteria.UniqueResult<CertificateMetadataBusinessEntity>();
    }

    public CertificateMetadataBusinessEntity? GetCertificate(string thumbprint)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<CertificateMetadataBusinessEntity>();
        criteria.Add(Restrictions.Eq("Thumbprint", thumbprint));
        return criteria.UniqueResult<CertificateMetadataBusinessEntity>();
    }

    public void ReplaceInUse(CertificateMetadataBusinessEntity certificateMetadata)
    {
        var currentInUse = GetInUse();

        if (currentInUse != null)
        {
            currentInUse.InUse = false;
            Update(currentInUse);
        }

        certificateMetadata.InUse = true;
        Save(certificateMetadata);
    }

    public void AddCertificate(CertificateMetadataBusinessEntity certificateMetadata)
    {
        Save(certificateMetadata);
    }
}