#!/bin/bash

DATABASE="master"
USER="sa"

# Check if the database "fig" already exists
if /opt/mssql-tools/bin/sqlcmd -S $DB_SERVER -d master -U sa -P $SA_PASSWORD -Q "SELECT 1 FROM sys.databases WHERE name = '$FIG_DB_NAME'" | grep -q 1; then
    echo "Database '$FIG_DB_NAME' already exists. Skipping creation."
else
    # SQL commands to create database, user, and set permissions
    SQL_COMMANDS=$(cat <<EOF
    USE master;
    CREATE DATABASE $FIG_DB_NAME;
    GO

    USE $FIG_DB_NAME;
    CREATE LOGIN $FIG_DB_NAME WITH PASSWORD = '$FIG_DB_PASSWORD';
    CREATE USER $FIG_DB_NAME FOR LOGIN $FIG_DB_NAME;
    ALTER ROLE db_owner ADD MEMBER $FIG_DB_NAME;
    GO
EOF
)

    # Run SQL commands using sqlcmd
    /opt/mssql-tools/bin/sqlcmd -S $DB_SERVER -d master -U sa -P $SA_PASSWORD -Q "$SQL_COMMANDS"
    echo "Database '$FIG_DB_NAME' created successfully."
fi


