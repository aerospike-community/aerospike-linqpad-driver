# Getting started

This guide takes you from an installed driver to a first bounded query.

## Before you begin

You need:

- LINQPad 9 or later.
- An Aerospike cluster that your computer can reach.
- At least one seed host and service port.
- Credentials and TLS information when required by the cluster.

The current project targets .NET 8.

## Install from LINQPad

1. Open LINQPad.
2. Select **Add connection**.
3. Select **View more drivers**.
4. Choose **Show all drivers**.
5. Search for `Aerospike` and install the driver.

## Create a connection

1. Add a new Aerospike connection.
2. Enter one or more comma-separated seed hosts.
3. Enter the service port; `3000` is the common default.
4. Enter authentication or TLS settings when required.
5. Select **Test**.
6. Give the connection a friendly name and select **OK**.

For public cloud or NAT-based access, enable the alternate/public address option only when the cluster advertises addresses that are not directly reachable from LINQPad. See [Connection configuration](connection-configuration.md).

## Understand the connection tree

After connection and metadata discovery, LINQPad shows a hierarchy similar to:

```text
Connection
└── Namespace
    ├── Configuration and metadata
    ├── Set
    │   ├── Generated bin properties
    │   └── Secondary indexes
    └── UDFs
```

![Connection tree with Aerospike namespaces, sets, and bins](../media/2590c33dc0562b6c0f3583edb1e4f91c.png)

You can drag many objects from the connection tree into a query. A set normally produces a record sequence; a metadata object displays its properties.

## Load the demo data

The samples use a `test` namespace and a demo connection by default.

1. Open `linqpad-samples/Demo/ReadMeFirst.linq`.
2. Read the comments at the top of the script.
3. Attach the query to your Aerospike connection if LINQPad asks.
4. Run the setup steps.
5. Refresh the connection after the sample creates or changes sets so generated properties reflect the current data.

The sample catalog explains dependencies and suggested order: [linqpad-samples/README.md](../linqpad-samples/README.md).

## Run a first query

```csharp
var rows = test.Customer.Take(20);
rows.Dump("First 20 customers");
```

`Take(...)` is a useful first operation because it bounds the result.

## Add a client-side LINQ filter

```csharp
var rows =
    from customer in test.Customer.AsEnumerable()
    where customer.State == "CA"
    orderby customer.LastName, customer.FirstName
    select customer;

rows.Take(100).Dump("California customers");
```

This predicate runs in LINQPad after records are returned. For a large set, prefer an Aerospike expression or another server-side selection mechanism.

## Add a server-side expression

```csharp
var filterExpression = Aerospike.Client.Exp.EQ(
    Aerospike.Client.Exp.StringBin("State"),
    Aerospike.Client.Exp.Val("CA"));

test.Customer
    .Query(filterExpression)
    .Take(100)
    .Dump("California customers");
```

Expression APIs use the raw Aerospike bin name (`"State"`), not the generated C# property.

## Next steps

- Learn the query execution choices in [Querying and records](querying-and-records.md).
- Learn safe mixed-type handling in [Auto Values](auto-values/README.md).
- Review writes and other mutating operations in [Data operations](data-operations.md).
- Try the [AI samples](../linqpad-samples/AI/README.md) when LINQPad AI is configured.

[Back to the documentation index](README.md)
