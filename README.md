Aerospike Database for LINQPad 6/7

## Description

Aerospike for LINQPad 6/7 is a data context dynamic driver for querying and updating an Aerospike database. This driver can be used to explore an Aerospike data model, data mining, prototyping, etc.

You can perform the following:

-   query any [Aerospike Set](https://docs.aerospike.com/server/architecture/data-model) using any LINQ command.
-   use the driver’s extension methods to perform operations like Aerospike Expression, CRUD operations, import/export, or execute an Aerospike User Defined Function (UDF) without understanding the underlying Aerospike API.
-   use the Aerospike API to perform advance operations or just to test code segments used in your application.

The driver can dynamically detect the structure of records in Aerospike Sets resulting in an easy-to-understand view. The driver can also detect multiple data types for the same Aerospike Bin within a record. Below screenshot show how Aerospike Sets and Bins are represented in LinqPad:

![Example](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/DupBinTypeInRecordDialog.png)

## Aerospike Namespace, Set, Records, Bins, and Secondary Indexes

The LINQPad connection pane will display the different [Aerospike components](https://docs.aerospike.com/server/architecture/data-model) in an hierarchical manner where namespace is under Aerospike cluster connection. Aerospike Sets are under namespaces and bins are under Sets. Below screenshot shows the relationship between these components:

![ComponentExample](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/ClusterNamespaceSetBinsDialog.png)

Each component can be dragged-and-dropped onto the LINQPad Query pane to be executed by LINQPad. The execution behavior will depend on the component. For example, a Set or Secondary Index will present the records within that component. For other components, the properties are displayed. In all cases, you can always execute the driver’s extension methods. These extension methods, greatly simplify Aerospike API commands like [Get, Put, Query, Operate, etc](https://developer.aerospike.com/client/csharp). plus, the ability to perform things like importing or exporting data. Of course, you can always use LINQ against Aerospike Sets or Secondary Indexes. Below is an example of some of the driver extensions:

![MethodsExample](<https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/NamespaceShowMethods.png>)

# Prerequisites

* [LINQPad 7](https://www.linqpad.net/LINQPad7.aspx): [.NET 7](https://dotnet.microsoft.com/download/dotnet/7.0)/[.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0)/[.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0)/[.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
* [LINQPad 6](https://www.linqpad.net/LINQPad6.aspx): [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0)/[.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)

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
