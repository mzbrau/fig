using NHibernate;

namespace Fig.Api.Datalayer;

public interface IFigSessionFactory
{
    ISessionFactory SessionFactory { get; }
}