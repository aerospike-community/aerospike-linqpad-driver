# Aerospike Database for LINQPad 7

## Description

Aerospike for LINQPad 7 is a data context dynamic driver for querying and updating an Aerospike database. This driver can be used to explore an Aerospike data model, data mining, prototyping, testing, etc.

You can perform the following:

-   query any [Aerospike Set](https://docs.aerospike.com/server/architecture/data-model) using any LINQ command.
-   use the driver’s extension methods to perform operations like Aerospike Expression, CRUD operations, import/export, or execute an Aerospike User Defined Function (UDF) without understanding the underlying Aerospike API.
-   use the Aerospike API to perform advance operations or just to test code segments used in your application.

The driver can dynamically detect the structure of records in Aerospike Sets resulting in an easy-to-understand view. The driver can also detect multiple data types for the same Aerospike Bin within a record. Below screenshot show how Aerospike Sets and Bins are represented in LinqPad:

![Example](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/DupBinTypeInRecordDialog.png?raw=true)

The LINQPad connection pane will display the different [Aerospike components](https://docs.aerospike.com/server/architecture/data-model) in an hierarchical manner where namespace is under Aerospike cluster connection. Aerospike Sets are under namespaces and bins are under Sets. Below screenshot shows the relationship between these components:  
  
![ComponentExample](<https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/ClusterNamespaceSetBinsDialog.png>)

Each component can be dragged-and-dropped onto the LINQPad Query pane to be executed by LINQPad. The execution behavior will depend on the component. For example, a Set or Secondary Index will present the records within that component. For other components, the properties are displayed. In all cases, you can always execute the driver’s extension methods. These extension methods, greatly simplify Aerospike API commands like [Get, Put, Query, Operate, etc](https://developer.aerospike.com/client/csharp). plus, the ability to perform things like importing or exporting data. Of course, you can always use LINQ against Aerospike Sets or Secondary Indexes. Below is an example of some of the driver extensions:  
  
![MethodsExample](<https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/NamespaceShowMethods.png>)

## Aerospike Namespace, Set, Records, Bins, and Secondary Indexes

The LINQPad connection pane will display the different [Aerospike components](https://docs.aerospike.com/server/architecture/data-model) in a hierarchical manner where namespace is under Aerospike cluster connection. Aerospike Sets are under namespaces and bins are under Sets. Below screenshot shows the relationship between these components:

![ComponentExample](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/ClusterNamespaceSetBinsDialog.png?raw=true)

Each component can be dragged-and-dropped onto the LINQPad Query pane to be executed by LINQPad. The execution behavior will depend on the component. For example, a Set or Secondary Index will present the records within that component. For other components, the properties are displayed. In all cases, you can always execute the driver’s extension methods and properties. These extensions, greatly simplify Aerospike API commands like [Get, Put, Query, Operate, etc](https://developer.aerospike.com/client/csharp). plus, the ability to perform things like importing or exporting data. Below is an example of some of the driver extensions:

![MethodsExample](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/NamespaceShowMethods.png?raw=true)

Aerospike Sets and records are very easy to use. The detected bins in a record are integrated into LINQPad and are treated as C\# properties. As such, features like Intellisense and Autocompletion just work. You can also access bins within a record by using the bin name.

Since Aerospike is a schemaless database, a record can consist of varying number of bins, or a bin can have different data types between records. The driver can handle these conditions seamlessly. This is done using extension methods and implicit data conversion.

Implicit data conversion eliminates the need to test and cast a bin’s value so that it can be used directly in any operation. Below is an example that shows how implicit conversion works. The set, “graphDeviceNodeIdSet”, has a bin named “nodeID” that consists of two different data type values. Some records have a list value while others have a string value. This example uses the “where” clause which compares each record in the set looking for a numeric value of 367 or the value “a” in the list values.   
![MethodsExample](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/ImplictConversionExample.png?raw=true)

## User-Defined Functions (UDFs)

The driver supports the execution of UDFs by calling the Execute extension method. The Execute method will reflect the actual arguments used in the UDF. Below is an example:

![MethodsExample](<https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/UDFExample.png?raw=true>)

### Aerospike API

At anytime you can use the underlying Aerospike API directly or a combination of API or driver extension methods. Below is an example:

![MethodsExample]([https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/AerospikeAPIExample.png?raw=true](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/UDFExample.png?raw=true))

# Prerequisites

-   [LINQPad 7](https://www.linqpad.net/LINQPad7.aspx): [.NET 7](https://dotnet.microsoft.com/download/dotnet/7.0)/[.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0)/[.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0)/[.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
-   [LINQPad 6](https://www.linqpad.net/LINQPad6.aspx): [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0)/[.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)

# Installation

## NuGet

-   Open LINQPad
-   Click `Add Connection` Link.
-   Click button `View more drivers…`
-   Click radio button `Show all drivers` and type `Aerospike`.
-   Install

## Manual

Obtain the latest driver from the `Driver` folder and download to your computer.

-   Open LINQPad
-   Click `Add Connection` Link.
-   Click button `View more drivers…`
-   Click button `Install driver from .LPX6 file…` and select downloaded lpx6 file.
