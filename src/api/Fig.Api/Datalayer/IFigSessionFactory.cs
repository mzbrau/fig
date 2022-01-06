using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer;

public interface IFigSessionFactory
{
    ISession OpenSession();
}