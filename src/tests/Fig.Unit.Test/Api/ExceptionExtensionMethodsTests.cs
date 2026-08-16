using System.Data.Common;
using System.Data.SQLite;
using System.Reflection;
using Fig.Api.ExtensionMethods;
using Microsoft.Data.SqlClient;
using NHibernate.Exceptions;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ExceptionExtensionMethodsTests
{
    [TestCaseSource(nameof(LockContentionCases))]
    public void IsLockContention_ShouldMatchExpected(Exception exception, bool expected)
    {
        Assert.That(exception.IsLockContention(), Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(TableNotExistsCases))]
    public void IsTableNotExistsException_ShouldMatchExpected(Exception exception, bool expected)
    {
        Assert.That(exception.IsTableNotExistsException(), Is.EqualTo(expected));
    }

    private static IEnumerable<TestCaseData> LockContentionCases()
    {
        yield return new TestCaseData(CreateSqlException(1205), true)
            .SetName("SqlServer_Deadlock_1205");
        yield return new TestCaseData(CreateSqlException(1222), true)
            .SetName("SqlServer_LockTimeout_1222");
        yield return new TestCaseData(CreateSqlException(3928), true)
            .SetName("SqlServer_MarkedTransactionAborted_3928");
        yield return new TestCaseData(CreateSqlException(8645), true)
            .SetName("SqlServer_MemoryResourceTimeout_8645");
        yield return new TestCaseData(CreateSqlException(208), false)
            .SetName("SqlServer_InvalidObject_NotLock");

        yield return new TestCaseData(new GenericADOException("wrapped", CreateSqlException(1205)), true)
            .SetName("NHibernate_Wraps_SqlServer_Deadlock");

        yield return new TestCaseData(new SQLiteException(SQLiteErrorCode.Busy, "database is locked"), true)
            .SetName("Sqlite_Busy_5");
        yield return new TestCaseData(new SQLiteException(SQLiteErrorCode.Locked, "table is locked"), true)
            .SetName("Sqlite_Locked_6");
        yield return new TestCaseData(new SQLiteException(SQLiteErrorCode.Error, "no such table"), false)
            .SetName("Sqlite_Error_NotLock");

        yield return new TestCaseData(new GenericADOException("wrapped", new SQLiteException(SQLiteErrorCode.Busy, "busy")), true)
            .SetName("NHibernate_Wraps_Sqlite_Busy");

        yield return new TestCaseData(new Exception("deadlock victim"), true)
            .SetName("MessageBased_Deadlock");
        yield return new TestCaseData(new Exception("lock timeout expired"), true)
            .SetName("MessageBased_LockTimeout");
        yield return new TestCaseData(new Exception("could not obtain lock on resource"), true)
            .SetName("MessageBased_CouldNotObtainLock");
        yield return new TestCaseData(new Exception("NOWAIT was specified"), true)
            .SetName("MessageBased_Nowait");
        yield return new TestCaseData(new Exception("resource busy and acquire with NOWAIT"), true)
            .SetName("MessageBased_ResourceBusy");
        yield return new TestCaseData(new Exception("database is locked"), true)
            .SetName("MessageBased_DatabaseIsLocked");
        yield return new TestCaseData(new Exception("SQLITE_BUSY"), true)
            .SetName("MessageBased_SqliteBusyToken");
        yield return new TestCaseData(new Exception("sqlite busy"), true)
            .SetName("MessageBased_SqliteBusyPhrase");
        yield return new TestCaseData(new Exception("timeout waiting for connection"), true)
            .SetName("MessageBased_Timeout");
        yield return new TestCaseData(new Exception("something unrelated went wrong"), false)
            .SetName("MessageBased_Unrelated");

        yield return new TestCaseData(new Exception("outer", new Exception("deadlock detected")), true)
            .SetName("Nested_MessageBased_Deadlock");
        yield return new TestCaseData(new TestDbException("db", CreateSqlException(1222)), true)
            .SetName("DbException_Inner_SqlServer_LockTimeout");
    }

    private static IEnumerable<TestCaseData> TableNotExistsCases()
    {
        yield return new TestCaseData(CreateSqlException(208), true)
            .SetName("SqlServer_InvalidObject_208");
        yield return new TestCaseData(CreateSqlException(2), true)
            .SetName("SqlServer_FileNotFound_2");
        yield return new TestCaseData(CreateSqlException(1205), false)
            .SetName("SqlServer_Deadlock_NotMissingTable");

        yield return new TestCaseData(new SQLiteException(SQLiteErrorCode.Error, "no such table: Foo"), true)
            .SetName("Sqlite_NoSuchTable_1");
        yield return new TestCaseData(new SQLiteException(SQLiteErrorCode.Busy, "database is locked"), false)
            .SetName("Sqlite_Busy_NotMissingTable");

        yield return new TestCaseData(new GenericADOException("wrapped", CreateSqlException(208)), true)
            .SetName("NHibernate_Wraps_SqlServer_MissingTable");
        yield return new TestCaseData(new GenericADOException("wrapped", new SQLiteException(SQLiteErrorCode.Error, "no such table")), true)
            .SetName("NHibernate_Wraps_Sqlite_MissingTable");

        yield return new TestCaseData(new Exception("outer", CreateSqlException(208)), true)
            .SetName("Nested_SqlServer_MissingTable");
        yield return new TestCaseData(new TestDbException("db", new SQLiteException(SQLiteErrorCode.Error, "no such table")), true)
            .SetName("DbException_Inner_Sqlite_MissingTable");
        yield return new TestCaseData(new Exception("plain failure"), false)
            .SetName("Unrelated_Exception");
    }

    private static SqlException CreateSqlException(int number)
    {
        var sqlError = CreateSqlError(number);
        var collectionCtor = typeof(SqlErrorCollection)
            .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)
            ?? throw new InvalidOperationException("Unable to find SqlErrorCollection constructor.");
        var errorCollection = (SqlErrorCollection)collectionCtor.Invoke(null)!;

        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(errorCollection, [sqlError]);

        var createException = typeof(SqlException)
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .FirstOrDefault(m =>
                m.Name == "CreateException" &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[0].ParameterType == typeof(SqlErrorCollection) &&
                m.GetParameters()[1].ParameterType == typeof(string))
            ?? throw new InvalidOperationException("Unable to find SqlException.CreateException.");

        return (SqlException)createException.Invoke(null, [errorCollection, "11.0.0"])!;
    }

    private static SqlError CreateSqlError(int number)
    {
        var constructors = typeof(SqlError)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

        foreach (var constructor in constructors.OrderByDescending(c => c.GetParameters().Length))
        {
            var parameters = constructor.GetParameters();
            var args = new object?[parameters.Length];
            var canInvoke = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                if (parameterType == typeof(int))
                    args[i] = number;
                else if (parameterType == typeof(byte))
                    args[i] = (byte)0;
                else if (parameterType == typeof(string))
                    args[i] = "test";
                else if (parameterType == typeof(uint))
                    args[i] = 0u;
                else if (typeof(Exception).IsAssignableFrom(parameterType))
                    args[i] = null;
                else if (!parameterType.IsValueType)
                    args[i] = null;
                else
                {
                    canInvoke = false;
                    break;
                }
            }

            if (!canInvoke)
                continue;

            try
            {
                return (SqlError)constructor.Invoke(args)!;
            }
            catch
            {
                // Try next constructor signature.
            }
        }

        throw new InvalidOperationException("Unable to construct SqlError for unit tests.");
    }

    private sealed class TestDbException : DbException
    {
        public TestDbException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
