# Aerospike Database for LINQPad 7

## Description

Aerospike DB for LINQPad 7 is a data context dynamic driver for querying and updating an Aerospike database. This driver can be used to explore an Aerospike data model, data mining, prototyping, etc.

You can perform the following:

-   query any [Aerospike Set](https://docs.aerospike.com/server/architecture/data-model) using any LINQ command.
-   use the driver’s extension methods to perform operations like Aerospike Expression or CRUD operations without understanding the underlying Aerospike API.
-   use the Aerospike API to perform advance operations or just to test code segments used in your application.

The driver can dynamically detect the structure of records in Aerospike Sets resulting in an easy-to-understand view. The driver can also detect multiple data types for the same Aerospike Bin within a record. Below screenshot show how Aerospike Sets and Bins are represented in LinqPad:

![Example](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/DupBinTypeInRecordDialog.png)
