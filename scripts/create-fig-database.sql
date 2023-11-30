IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'fig')
BEGIN
    CREATE DATABASE fig;
    PRINT 'Database "fig" created.';
END
ELSE
BEGIN
    PRINT 'Database "fig" already exists.';
END

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'fig_user')
BEGIN
    CREATE LOGIN fig_login WITH PASSWORD = '<figDbPassword>', CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;
    PRINT 'Login "fig_login" created.';
    
    CREATE USER fig_user FOR LOGIN fig_login;
    PRINT 'User "fig_user" created.';
    
    EXEC sp_addrolemember 'db_owner', 'fig_user';
    PRINT 'User "fig_user" added as db_owner for database "fig".';
END
ELSE
BEGIN
    PRINT 'User "fig_user" already exists.';
END

