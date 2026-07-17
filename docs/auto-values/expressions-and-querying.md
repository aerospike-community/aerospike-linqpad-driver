# Expressions and querying

> **Auto Values guide:** [Overview](README.md) · [Fundamentals](fundamentals.md) · [Conversion and comparison](conversion-and-comparison.md) · [Collections and search](collections-and-search.md) · [Expressions and querying](expressions-and-querying.md) · [Reference and mistakes](reference.md)

## Aerospike Expression Helpers

The latest AValue helper extensions include methods for building Aerospike expression pieces from AValues.

These helpers are useful when you want to bridge LINQPad-discovered AValues with server-side Aerospike expressions.

### ToExpBin

Use `ToExpBin()` to create an Aerospike bin expression from an AValue's `BinName`:

```csharp
var status = "active".ToAValue("Status", "Status");

var filterExpression = Exp.EQ(
    status.ToExpBin(Exp.Type.STRING),
    status.ToExpVal());
```

If the expression type is not supplied, the helper attempts to infer the `Exp.Type` from the AValue's runtime value.

Use this for expression bin references:

```csharp
var statusBin = customer.Status.ToExpBin();
```

### ToExpVal

Use `ToExpVal()` to create an Aerospike expression literal value from an AValue's underlying value:

```csharp
var active = "active".ToAValue("Status", "Status");

var filterExpression = Exp.EQ(
    Exp.StringBin("Status"),
    active.ToExpVal());
```

For maps, `ToExpVal` accepts a `MapOrder` argument:

```csharp
var mapValue = profileMap.ToAValue("Profile", "Profile");

var expressionValue = mapValue.ToExpVal(MapOrder.KEY_ORDERED);
```

### Expression Helper Guidance

Use `ToExpBin()` for the **bin reference** side of an expression.

Use `ToExpVal()` for the **literal value** side of an expression.

Example:

```csharp
var active = "active".ToAValue("Status", "Status");

Client.Exp filterExpression = Exp.EQ(
    active.ToExpBin(Exp.Type.STRING),
    active.ToExpVal());

test.Customer
    .Query(filterExpression)
    .Take(100)
    .Dump();
```

For straightforward server-side expressions, it is still fine to use raw bin names directly:

```csharp
Client.Exp filterExpression = Exp.EQ(
    Exp.StringBin("Status"),
    Exp.Val("active"));
```

***


## LINQ Query Syntax with AValues

For query logic, prefer query syntax when practical. Since `SetRecords<T>` instances should use `AsEnumerable()` for LINQ collection operations, start with:

```csharp
from customer in test.Customer.AsEnumerable()
select customer
```

### Filter with AValue Comparison

```csharp
var customers =
    from customer in test.Customer.AsEnumerable()
    where customer.Status == "active"
    select customer;

customers.Take(100).Dump();
```

### Filter with TryApply

```csharp
var customers =
    from customer in test.Customer.AsEnumerable()
    where customer.FirstName.TryApply<string, bool>(
        name => name.StartsWith("a"))
    select customer;

customers.Take(100).Dump();
```

### Sort with Type Safety

```csharp
var records =
    from record in test.DataTypes.AsEnumerable()
    where record.BinB.IsInt
    orderby record.BinB
    select new
    {
        record.PK,
        record.BinB
    };

records.Take(100).Dump();
```

### Join with AValue-Aware Properties

```csharp
var joined =
    from customer in test.Customer.AsEnumerable()
    join invoice in test.Invoice.AsEnumerable()
        on customer.PK equals invoice.CustomerId
    select new
    {
        CustomerPK = customer.PK,
        customer.FirstName,
        customer.LastName,
        InvoicePK = invoice.PK,
        invoice.InvoiceDate,
        invoice.Total
    };

joined.Take(100).Dump();
```

### Query CDT Values

```csharp
var profileValues =
    from customer in test.Customer.AsEnumerable()
    from item in customer.Profile.AsEnumerable()
    select new
    {
        customer.PK,
        Item = item
    };

profileValues.Take(100).Dump();
```

### Query with TryGetValue

```csharp
var customers =
    from customer in test.Customer.AsEnumerable()
    let email = customer.Profile.TryGetValue("email", "<missing>")
    where email != "<missing>"
    select new
    {
        customer.PK,
        customer.FirstName,
        Email = email
    };

customers.Take(100).Dump();
```

***


## LINQ/AValue Filters Versus Aerospike Expressions

AValue comparisons and LINQ `where` clauses are client-side after records are returned to LINQPad.

Aerospike expressions are server-side and use raw bin names and Aerospike expression APIs.

### Client-Side LINQ/AValue Filter

```csharp
test.DataTypes
    .Where(dt => dt.BinB.IsInt && dt.BinB < 800)
    .Dump("Client-side LINQ/AValue filter");
```

### Server-Side Aerospike Expression

```csharp
test.DataTypes
    .Query(Exp.LT(Exp.IntBin("BinB"), Exp.Val(800)))
    .Dump("Server-side Aerospike expression filter");
```

### Server-Side Expression with AValue Helpers

```csharp
var limit = 800.ToAValue("BinB", "BinB");

Client.Exp filterExpression = Exp.LT(
    limit.ToExpBin(Exp.Type.INT),
    limit.ToExpVal());

test.DataTypes
    .Query(filterExpression)
    .Dump("Server-side expression built with AValue helpers");
```

Use Aerospike expressions when the user explicitly wants server-side filtering or when reducing records at the server is important.

Use AValue/LINQ filters when exploring data interactively in LINQPad and driver-side conversion behavior is useful.

***

[Back to the Auto Values overview](README.md)
