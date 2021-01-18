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
