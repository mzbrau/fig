#!/bin/bash

DATABASE="master"
USER="sa"

# Check if the database "fig" already exists
if /opt/mssql-tools/bin/sqlcmd -S $DB_SERVER -d master -U $FIG_USER_NAME -P "$FIG_DB_PASSWORD" -Q "SELECT 1 FROM sys.databases WHERE name = '$FIG_DB_NAME'" | grep -q 1; then
    echo "Database '$FIG_DB_NAME' already exists. Skipping creation."
elif /opt/mssql-tools/bin/sqlcmd -S $DB_SERVER -d master -U sa -P "$SA_PASSWORD" -Q "SELECT 1 FROM sys.databases WHERE name = '$FIG_DB_NAME'" | grep -q 1; then
    echo "Database '$FIG_DB_NAME' already exists. Skipping creation."
else
    # SQL commands to create database, user, and set permissions
    SQL_COMMANDS=$(cat <<EOF
    USE master;
    PRINT 'Creating database "$FIG_DB_NAME".';      
    CREATE DATABASE $FIG_DB_NAME;
    GO

    USE $FIG_DB_NAME;

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '$FIG_USER_NAME')
    BEGIN
      CREATE LOGIN $FIG_USER_NAME WITH PASSWORD = '$FIG_DB_PASSWORD', CHECK_POLICY = OFF, CHECK_EXPIRATION = OFF;
      PRINT 'Login "$FIG_USER_NAME" created.';    
      CREATE USER $FIG_USER_NAME FOR LOGIN $FIG_USER_NAME;
      PRINT 'User "$FIG_USER_NAME" created.';
      ALTER ROLE db_owner ADD MEMBER $FIG_USER_NAME;
    END
    ELSE
    BEGIN
      PRINT 'User "$FIG_USER_NAME" already exist.';
    END
    GO
EOF
)

    # Run SQL commands using sqlcmd
    /opt/mssql-tools/bin/sqlcmd -S $DB_SERVER -d master -U sa -P $SA_PASSWORD -Q "$SQL_COMMANDS"
    retval=$?; if [ $retval -ne 0 ]; then exit $retval; fi
    echo "Database '$FIG_DB_NAME' created successfully."
fi


