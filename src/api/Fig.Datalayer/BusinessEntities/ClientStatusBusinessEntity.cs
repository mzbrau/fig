namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassNeverInstantiated.Global used by NHibernate
public class ClientStatusBusinessEntity : ClientBase
{
    public virtual DateTime? LastRunSessionDisconnected { get; set; }

    public virtual string? LastRunSessionMachineName { get; set; }
}