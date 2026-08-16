using System.Data.Common;
using NHibernate.Driver;

namespace Fig.Api.Datalayer;

/// <summary>
/// SQLite NHibernate driver that retries statement execution on lock contention.
/// </summary>
public class RetryingSQLiteDriver : SQLite20Driver
{
    public override DbCommand CreateCommand()
    {
        return new LockRetryDbCommand(base.CreateCommand());
    }

    public override DbCommand UnwrapDbCommand(DbCommand command)
    {
        if (command is LockRetryDbCommand wrapper)
            return base.UnwrapDbCommand(wrapper.Inner);

        return base.UnwrapDbCommand(command);
    }
}
