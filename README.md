# Integration Event Producer
This repository is a POC of how integration events could be created from SQL server tables.
This POC uses the [Change Data Capture (CDC)](https://docs.microsoft.com/en-us/sql/relational-databases/track-changes/about-change-data-capture-sql-server?view=sql-server-ver15)
functionality of SQL server.

## Enabling CDC on the SQL Server database
Before you can enable CDC for a table, you must enable it for the SQL Server database. A SQL Server administrator enables CDC by running a system stored procedure. 
System stored procedures can be run by using SQL Server Management Studio, or by using Transact-SQL.

Prerequisites
* You are a member of the sysadmin fixed server role for the SQL Server.
* You are a db_owner of the database.
* The SQL Server Agent is running.

Run the stored procedure sys.sp_cdc_enable_db to enable the database for CDC. After the database is enabled for CDC, a schema with the name cdc is created, along with a CDC user, metadata tables, and other system objects.

The following example shows how to enable CDC for the database Meniga:
```sql
USE Meniga
GO
EXEC sys.sp_cdc_enable_db
GO
```

## Enabling CDC on a SQL Server table
A SQL Server administrator must enable change data capture on the source tables that you want to capture. The database must already be enabled for CDC. To enable CDC on a table, 
a SQL Server administrator runs the stored procedure sys.sp_cdc_enable_table for the table. The stored procedures can be run by using SQL Server Management Studio, or by using Transact-SQL.
SQL Server CDC must be enabled for every table that you want to capture.

Prerequisites
* CDC is enabled on the SQL Server database.
* The SQL Server Agent is running.
* You are a member of the db_owner fixed database role for the database.

The following example shows how to enable CDC for the table usr.transactions on database *Meniga*:

```SQL
USE Meniga
GO
EXEC sys.sp_cdc_enable_table
@source_schema = N'usr',
@source_name   = N'transactions', 
@role_name     = N'public' // Use relevant database role
@supports_net_changes = 0
GO
```

## Using CDC
Following example sets `last_lsn` to a pointer reflecting the last captured row on the server:
```SQL
SET @last_lsn   = sys.fn_cdc_get_max_lsn()
```

This pointer can be stored to be able to capture all changes happening after that. Changes after this can be selected as follows, assuming `usr.transactions` table.