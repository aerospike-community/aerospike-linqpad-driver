# Auto Values (`AValue`) overview

`AValue` is the driver's value abstraction for Aerospike bins. `APrimaryKey` applies the same model to record keys.

Aerospike sets are schemaless: a bin can be missing, null-like, or represented by different native types in different records. Auto Values let LINQPad queries inspect and work with those values without scattering casts and null checks throughout the query.

## Why use Auto Values

Without Auto Values, code often needs to:

1. Check that a bin exists.
2. Check for `null`.
3. Inspect the runtime type.
4. Cast or convert the value.
5. Handle conversion failure.
6. Repeat the same defensive logic for each query.

With Auto Values, common intent is direct:

```csharp
from customer in test.Customer.AsEnumerable()
where !customer.Company.IsEmpty && customer.State == "CA"
select customer
```

## Core operations

| Need | Preferred pattern |
|---|---|
| Missing or empty check | `value.IsEmpty` / `!value.IsEmpty` |
| Inspect type | `IsString`, `IsNumeric`, `IsList`, `IsMap`, `Type`, `ValueType` |
| Safe conversion | `value.CanConvert<T>()` then `value.Convert<T>()` |
| Apply a type-specific method | `value.TryApply<TValue,TResult>(...)` |
| Project with a known type | `value.Apply<TValue,TResult>(...)` |
| Traverse map/document data | `TryGetValue(...)`, `ToDictionary(...)`, `ToAValue()` |
| Traverse list/CDT data | `AsEnumerable()`, `ToList()`, `ElementAtOrDefault(...)` |
| Search heterogeneous values | `Contains(...)`, `FindAll(...)`, `MatchOptions` |
| Build expression operands | `ToExpBin()` and `ToExpVal()` |
| Work with a key | `APrimaryKey`, `ToAPrimaryKey()` |

## Quick examples

### Sparse string filter

```csharp
var rows =
    from customer in test.Customer.AsEnumerable()
    where customer.FirstName.TryApply<string, bool>(
        name => name.StartsWith("J", StringComparison.OrdinalIgnoreCase))
    select customer;
```

### Numeric conversion

```csharp
var rows =
    from invoice in test.Invoice.AsEnumerable()
    where invoice.Total.CanConvert<decimal>()
       && invoice.Total.Convert<decimal>() >= 25m
    select invoice;
```

### Map/document lookup

```csharp
var email = customer.Profile.TryGetValue("email", AValue.Empty);
```

### List traversal

```csharp
var lines = invoice.Lines.ToAValue().AsEnumerable();
```

## Client-side versus server-side

`AValue` is primarily used after records are materialized in the LINQPad process. It does not automatically translate an ordinary LINQ predicate into a server-side Aerospike expression.

```csharp
// Client-side LINQ/AValue predicate
from customer in test.Customer.AsEnumerable()
where customer.State == "CA"
select customer
```

```csharp
// Server-side Aerospike expression
var expression = Aerospike.Client.Exp.EQ(
    Aerospike.Client.Exp.StringBin("State"),
    Aerospike.Client.Exp.Val("CA"));

test.Customer.Query(expression);
```

Use server-side filters to reduce network transfer and client work; use AValue/LINQ for interactive exploration, conversion, complex projections, and cross-set operations.

## Read the detailed guide

1. [Fundamentals and primary keys](fundamentals.md)
2. [Conversion and comparison](conversion-and-comparison.md)
3. [Collections, maps, JSON, CDTs, and search](collections-and-search.md)
4. [LINQ and Aerospike expressions](expressions-and-querying.md)
5. [Helper reference, common mistakes, and AI guidance](reference.md)

The runnable examples `Basic Data Types.linq`, `Basic Data Types 2.linq`, and `CDT-Json-Docs.linq` are described in the [sample catalog](../../linqpad-samples/Demo/README.md).

[Back to the documentation index](../README.md)
