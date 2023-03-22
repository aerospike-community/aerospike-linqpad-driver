Aerospike Database for LINQPad 7

## Description

Aerospike DB for LINQPad 7 is a data context dynamic driver for querying and updating an Aerospike database. This driver can be used to explore an Aerospike data model, data mining, prototyping, etc.

You can perform the following:

-   query any [Aerospike Set](https://docs.aerospike.com/server/architecture/data-model) using any LINQ command.
-   use the driver’s extension methods to perform operations like Aerospike Expression, CRUD operations, import/export, or execute an Aerospike User Defined Function (UDF) without understanding the underlying Aerospike API.
-   use the Aerospike API to perform advance operations or just to test code segments used in your application.

The driver can dynamically detect the structure of records in Aerospike Sets resulting in an easy-to-understand view. The driver can also detect multiple data types for the same Aerospike Bin within a record. Below screenshot show how Aerospike Sets and Bins are represented in LinqPad:

![Example](<https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/DupBinTypeInRecordDialog.png>)

## Aerospike Namespace, Set, Records, Bins, and Secondary Indexes

The LINQPad connection pane will display the different [Aerospike components](https://docs.aerospike.com/server/architecture/data-model) in an hierarchical manner where namespace is under Aerospike cluster connection. Aerospike Sets are under namespaces and bins are under Sets. Below screenshot shows the relationship between these components:  
  
![ComponentExample](<https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/ClusterNamespaceSetBinsDialog.png>)

Each component can be dragged-and-dropped onto the LINQPad Query pane to be executed by LINQPad. The execution behavior will depend on the component. For example, a Set or Secondary Index will present the records within that component. For other components, the properties are displayed. In all cases, you can always execute the driver’s extension methods. These extension methods, greatly simplify Aerospike API commands like [Get, Put, Query, Operate, etc](https://developer.aerospike.com/client/csharp). plus, the ability to perform things like importing or exporting data. Of course, you can always use LINQ against Aerospike Sets or Secondary Indexes. Below is an example of some of the driver extensions:  
  
![MethodsExample](<https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/NamespaceShowMethods.png>)
