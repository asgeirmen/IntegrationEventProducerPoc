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

The following example shows how to enable CDC for `usr.transactions` and `usr.accounts` tables on database *Meniga*:

```SQL
USE Meniga
GO
EXEC sys.sp_cdc_enable_table
@source_schema = N'usr',
@source_name   = N'transactions', 
@role_name     = N'public' -- Use relevant database role
@supports_net_changes = 0
GO
EXEC sys.sp_cdc_enable_table  
@source_schema = N'usr',  
@source_name   = N'accounts',  
@role_name     = N'public',  -- Use relevant database role
@supports_net_changes = 0  
GO
```
This will generate following table-valued functions: `cdc.fn_cdc_get_all_changes_usr_transactions` and `cdc.fn_cdc_get_all_changes_usr_accounts`.

## Configure poll interval
The CDC job configuration on the database can be viewed by running following command:
```SQL
EXEC sys.sp_cdc_help_jobs;
```
As you probably see, the `pollinginterval` is set to 5 seconds, which means up 5 seconds delay. This should be reduced to 1 second or event 0 as follows:
```SQL
EXECUTE sys.sp_cdc_change_job   
    @job_type = N'capture',  
    @pollinginterval  = 0;
```
You will need to restart SQL Server Agent to activete those changes.


## Using CDC
Following example selects a pointer reflecting the last captured row on the server:
```SQL
select sys.fn_cdc_get_max_lsn()
```
Let's assume following result: `0x0000161100000A200001`

This pointer can be stored to be able to capture all changes happening after that. The last commit and changes after this can be selected as follows, assuming the `usr.transactions` table.
```SQL
SELECT * FROM cdc.fn_cdc_get_all_changes_usr_transactions(0x0000161100000A200001, sys.fn_cdc_get_max_lsn(), N'all');
```

The results from this query is a table with the same columns as `usr.transactions` but has 4 additional colums as described [here](https://docs.microsoft.com/en-us/sql/relational-databases/system-functions/cdc-fn-cdc-get-all-changes-capture-instance-transact-sql?view=sql-server-ver15).
The `__$start_lsn` column for the last raw can be used as the from pointer in the next call to `cdc.fn_cdc_get_all_changes_usr_transactions`.

## Customer events, Debezium and Outbox pattern
Debezium is an open source distributed platform for change data capture, built on top of Apache Kafka. It has a [connector for SQL server](https://debezium.io/documentation/reference/1.4/connectors/sqlserver.html)
that makes use of CDC in a similar way as this POC. I recommend this video to understand Debezium: 
[Practical Change Data Streaming Use Cases with Apache Kafka & Debezium](https://www.infoq.com/presentations/data-streaming-kafka-debezium/)

An interesting article named [Reliable Microservices Data Exchange With the Outbox Pattern](https://debezium.io/blog/2019/02/19/reliable-microservices-data-exchange-with-the-outbox-pattern/)
describes how capturing from an Outbox table can be implemented. Following quote from this article is very interesting:

> This might be surprising at first. But it makes sense when remembering how log-based CDC works: it doesn’t examine the actual contents of the table in the database, but instead it tails the append-only transaction log. The calls to persist() and remove() will create an INSERT and a DELETE entry in the log once the transaction commits. After that, Debezium will process these events: for any INSERT, a message with the event’s payload will be sent to Apache Kafka. DELETE events on the other hand can be ignored, as the removal from the outbox table is a mere technicality that doesn’t require any propagation to the message broker. So we are able to capture the event added to the outbox table by means of CDC, but when looking at the contents of the table itself, it will always be empty. This means that no additional disk space is needed for the table (apart from the log file elements which will automatically be discarded at some point) and also no separate house-keeping process is required to stop it from growing indefinitely.

## Configuration
This POC implementation can capture from multiple tables on a single database. This is conigured in 
[appsettings.json](https://github.com/asgeirmen/IntegrationEventProducerPoc/blob/main/src/IntegrationEventProducerPoc/appsettings.json).
