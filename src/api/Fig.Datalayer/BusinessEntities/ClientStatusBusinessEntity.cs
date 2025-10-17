namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassNeverInstantiated.Global used by NHibernate
public class ClientStatusBusinessEntity : ClientBase
{
    public virtual ICollection<ClientRunSessionBusinessEntity> RunSessions { get; set; } =
        new List<ClientRunSessionBusinessEntity>();
}