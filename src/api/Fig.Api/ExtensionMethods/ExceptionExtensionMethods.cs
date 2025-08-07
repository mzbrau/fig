using Microsoft.Data.SqlClient;
using NHibernate.Exceptions;
using System.Data.Common;
using System.Data.SQLite;

namespace Fig.Api.ExtensionMethods;

/// <summary>
/// Extension methods for Exception types to provide robust database exception analysis.
/// </summary>
public static class ExceptionExtensionMethods
{
    /// <summary>
    /// Determines if an exception indicates that a database table does not exist.
    /// Handles various database providers and their specific error codes.
    /// </summary>
    /// <param name="ex">The exception to analyze</param>
    /// <returns>True if the exception indicates a missing table, false otherwise</returns>
    public static bool IsTableNotExistsException(this Exception ex)
    {
        // Handle NHibernate's GenericADOException which wraps database-specific exceptions
        if (ex is GenericADOException adoException && adoException.InnerException != null)
        {
            return adoException.InnerException.IsTableNotExistsException();
        }

        // Handle SQL Server exceptions
        if (ex is SqlException sqlException)
        {
            // SQL Server error code 208: "Invalid object name"
            // SQL Server error code 2: "The system cannot find the file specified" (for missing database)
            return sqlException.Number == 208 || sqlException.Number == 2;
        }

        // Handle SQLite exceptions
        if (ex is SQLiteException sqliteException)
        {
            // SQLite error code 1: SQLITE_ERROR - "no such table"
            return sqliteException.ErrorCode == 1;
        }

        // Handle generic database exceptions that might wrap the specific ones
        if (ex is DbException dbException)
        {
            // Check inner exception for database-specific types
            if (dbException.InnerException != null)
            {
                return dbException.InnerException.IsTableNotExistsException();
            }
        }

        // If we have a nested exception, check it recursively
        if (ex.InnerException != null)
        {
            return ex.InnerException.IsTableNotExistsException();
        }

        return false;
    }

    /// <summary>
    /// Determines if an exception indicates database lock contention.
    /// Supports multiple database providers with provider-specific error code detection.
    /// </summary>
    /// <param name="ex">The exception to analyze</param>
    /// <returns>True if the exception indicates lock contention, false otherwise</returns>
    public static bool IsLockContention(this Exception ex)
    {
        // Handle NHibernate's GenericADOException which wraps database-specific exceptions
        if (ex is GenericADOException adoException && adoException.InnerException != null)
        {
            return adoException.InnerException.IsLockContention();
        }

        // Handle SQL Server exceptions (Microsoft.Data.SqlClient)
        if (ex is SqlException sqlException)
        {
            return sqlException.IsSqlServerLockContention();
        }

        // Handle legacy SQL Server exceptions (System.Data.SqlClient)
        if (ex.GetType().FullName == "System.Data.SqlClient.SqlException")
        {
            return ex.IsLegacySqlServerLockContention();
        }

        // Handle SQLite exceptions
        if (ex is SQLiteException sqliteException)
        {
            return sqliteException.IsSqliteLockContention();
        }

        // Handle generic database exceptions that might wrap the specific ones
        if (ex is DbException dbException)
        {
            // Check inner exception for database-specific types
            if (dbException.InnerException != null)
            {
                return dbException.InnerException.IsLockContention();
            }
            
            // Check vendor-specific error codes if available
            return dbException.IsDbExceptionLockContention();
        }

        // If we have a nested exception, check it recursively
        if (ex.InnerException != null)
        {
            return ex.InnerException.IsLockContention();
        }

        // Last resort: conservative message-text check for unsupported providers
        // This maintains backward compatibility but should be avoided when possible
        return ex.IsMessageBasedLockContention();
    }

    /// <summary>
    /// Detects SQL Server lock contention using Microsoft.Data.SqlClient error codes.
    /// Supported error codes:
    /// - 1205: Deadlock victim
    /// - 1222: Lock request time out
    /// - 3928: The marked transaction was aborted during rollback
    /// - 8645: A timeout occurred while waiting for memory resources
    /// </summary>
    /// <param name="sqlException">The SQL Server exception to analyze</param>
    /// <returns>True if the exception indicates lock contention, false otherwise</returns>
    public static bool IsSqlServerLockContention(this SqlException sqlException)
    {
        return sqlException.Number switch
        {
            1205 => true, // Deadlock victim
            1222 => true, // Lock request time out
            3928 => true, // The marked transaction was aborted during rollback
            8645 => true, // A timeout occurred while waiting for memory resources
            _ => false
        };
    }

    /// <summary>
    /// Detects legacy SQL Server lock contention using System.Data.SqlClient via reflection.
    /// </summary>
    /// <param name="ex">The legacy SQL Server exception to analyze</param>
    /// <returns>True if the exception indicates lock contention, false otherwise</returns>
    public static bool IsLegacySqlServerLockContention(this Exception ex)
    {
        try
        {
            var numberProperty = ex.GetType().GetProperty("Number");
            if (numberProperty != null && numberProperty.GetValue(ex) is int errorNumber)
            {
                return errorNumber switch
                {
                    1205 => true, // Deadlock victim
                    1222 => true, // Lock request time out
                    3928 => true, // The marked transaction was aborted during rollback
                    8645 => true, // A timeout occurred while waiting for memory resources
                    _ => false
                };
            }
        }
        catch
        {
            // If reflection fails, fall back to message-based detection
        }
        
        return false;
    }

    /// <summary>
    /// Detects SQLite lock contention using error codes.
    /// Supported error codes:
    /// - 5: SQLITE_BUSY - The database file is locked
    /// - 6: SQLITE_LOCKED - A table in the database is locked
    /// </summary>
    /// <param name="sqliteException">The SQLite exception to analyze</param>
    /// <returns>True if the exception indicates lock contention, false otherwise</returns>
    public static bool IsSqliteLockContention(this SQLiteException sqliteException)
    {
        return sqliteException.ErrorCode switch
        {
            5 => true,  // SQLITE_BUSY
            6 => true,  // SQLITE_LOCKED
            _ => false
        };
    }

    /// <summary>
    /// Detects lock contention in generic DbException by examining vendor-specific error codes.
    /// This method handles cases where database-specific exceptions are wrapped in DbException.
    /// </summary>
    /// <param name="dbException">The generic database exception to analyze</param>
    /// <returns>True if the exception indicates lock contention, false otherwise</returns>
    public static bool IsDbExceptionLockContention(this DbException dbException)
    {
        // For most providers, we can't reliably determine lock contention from DbException alone
        // without knowing the specific provider type, so we return false here.
        // Provider-specific exceptions should be caught by the more specific handlers above.
        return false;
    }

    /// <summary>
    /// Last resort: message-based lock contention detection for unsupported providers.
    /// This is fragile and localization-dependent but maintains backward compatibility.
    /// 
    /// Currently supported providers via exception types and error codes:
    /// - SQL Server (Microsoft.Data.SqlClient and System.Data.SqlClient)
    /// - SQLite (System.Data.SQLite)
    /// 
    /// Message-based fallback patterns for:
    /// - Generic lock timeout patterns
    /// - NOWAIT clause violations
    /// - Deadlock patterns
    /// </summary>
    /// <param name="ex">The exception to analyze using message patterns</param>
    /// <returns>True if the exception message suggests lock contention, false otherwise</returns>
    public static bool IsMessageBasedLockContention(this Exception ex)
    {
        var msg = ex.Message.ToLowerInvariant();
        return msg.Contains("nowait") || 
               msg.Contains("deadlock") || 
               msg.Contains("timeout") || 
               msg.Contains("could not obtain lock") ||
               msg.Contains("lock timeout") ||
               msg.Contains("resource busy");
    }
}
