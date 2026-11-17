IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'fig')
BEGIN
    CREATE DATABASE fig;
    PRINT 'Database "fig" created.';
END
ELSE
BEGIN
    PRINT 'Database "fig" already exists.';
END

GO

USE fig;

IF SUSER_ID('fig_login') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'fig_user')
        THROW 50000, 'fig_user exists but fig_login does not. Resolve the orphaned database user before continuing.', 1;

    CREATE LOGIN fig_login WITH PASSWORD = '<figDbPassword>', CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;
    PRINT 'Login "fig_login" created.';
END
ELSE
    PRINT 'Login "fig_login" already exists.';

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'fig_user')
BEGIN
    CREATE USER fig_user FOR LOGIN fig_login;
    PRINT 'User "fig_user" created.';
END
ELSE
BEGIN
    PRINT 'User "fig_user" already exists.';
    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'fig_user' AND sid = SUSER_SID('fig_login'))
        THROW 50000, 'fig_user is not mapped to fig_login. Resolve the user mapping before continuing.', 1;
END

IF IS_ROLEMEMBER('db_owner', 'fig_user') <> 1
BEGIN
    ALTER ROLE db_owner ADD MEMBER fig_user;
    PRINT 'User "fig_user" added as db_owner for database "fig".';
END

