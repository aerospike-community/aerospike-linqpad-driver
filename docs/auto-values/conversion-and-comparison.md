# Conversion and comparison

> **Auto Values guide:** [Overview](README.md) · [Fundamentals](fundamentals.md) · [Conversion and comparison](conversion-and-comparison.md) · [Collections and search](collections-and-search.md) · [Expressions and querying](expressions-and-querying.md) · [Reference and mistakes](reference.md)

## Type Safety and Auto Conversion

Auto Values can perform convenient conversions, but you should still be intentional when the operation requires a specific type.

### Convenient Comparison

This is concise and often fine for exploration:

```csharp
test.DataTypes
    .Where(dt => dt.BinB < 800)
    .Dump("BinB less than 800");
```

### Type-Safe Comparison

This is safer when a bin may contain mixed types:

```csharp
test.DataTypes
    .Where(dt => dt.BinB.IsInt && dt.BinB < 800)
    .Dump("BinB less than 800 where BinB is an integer");
```

Use type checks when the bin can contain strings, numbers, maps, lists, JSON, GeoJSON, byte arrays, or other mixed values.

### Numeric Comparisons

Use numeric comparisons when the bin is known to contain numeric-compatible values:

```csharp
from item in test.DataTypes.AsEnumerable()
where item.BinB.IsInt && item.BinB < 800
select item
```

For mixed numeric bins, prefer type checks or `CanConvert<T>()`:

```csharp
from item in test.DataTypes.AsEnumerable()
where item.BinB.CanConvert<long>() && item.BinB.Convert<long>() < 800
select item
```

### Mixed-Type Ordering

Greater-than and less-than comparisons are safest when both sides are numeric-compatible, date/time-compatible, or otherwise known to be comparable.

Prefer this:

```csharp
test.DataTypes
    .Where(dt => dt.BinB.IsInt && dt.BinB < 800)
    .Dump("Type-safe numeric comparison");
```

Over this, when `BinB` is known to contain mixed types:

```csharp
test.DataTypes
    .Where(dt => dt.BinB < 800)
    .Dump("Convenient but less type-specific comparison");
```

***


## Date and Time Comparisons

Aerospike does not have native `DateTime`, `DateTimeOffset`, or `TimeSpan` DB types. The driver can transform or convert values to make date/time use more natural in LINQPad.

For example:

```csharp
var dateTimeOffset = DateTimeOffset.Parse("5/9/2023 2:42:40 PM -07:00");

test.DataTypes
    .Where(dt => dt.BinC == dateTimeOffset)
    .Dump("Records using DateTimeOffset object");

test.DataTypes
    .Where(dt => dt.BinC == dateTimeOffset.DateTime)
    .Dump("Records using DateTime object");
```

This is a **client-side LINQ/AValue comparison**. It is different from an Aerospike server-side expression.

When using Aerospike expressions, use the raw stored Aerospike representation and the correct `Exp.*Bin(...)` function:

```csharp
test.DataTypes
    .Query(Exp.EQ(Exp.StringBin("BinC"), Exp.Val("5/9/2023 2:42:40 PM -07:00")))
    .Dump("Date/time comparison using Aerospike expression");
```

### Key Point

AValue comparisons run after records are materialized by the driver. Aerospike expressions run on the Aerospike server and must use raw bin names and the stored Aerospike data representation.

***


## CanConvert and Convert

Use `CanConvert<T>()` to test whether an `AValue` can be converted without throwing:

```csharp
from customer in test.Customer.AsEnumerable()
where customer.TotalPurchases.CanConvert<decimal>()
select customer
```

Use `Convert<T>()` when conversion is expected to be valid:

```csharp
var customers =
    from customer in test.Customer.AsEnumerable()
    where customer.TotalPurchases.CanConvert<decimal>()
    select new
    {
        customer.PK,
        customer.FirstName,
        customer.LastName,
        TotalPurchases = customer.TotalPurchases.Convert<decimal>()
    };

customers.Take(100).Dump();
```

Use this pattern when:

-   the bin may not exist,
-   the bin is mixed-type,
-   conversion may fail,
-   you want a specific CLR type,
-   you want to document your intended type.

`CanConvert<T>()` is provided as a null-safe helper extension, so it returns `false` for a null `AValue`.

***


## Apply and TryApply

Use `Apply<TValue,TResult>()` and `TryApply<TValue,TResult>()` when invoking type-specific .NET methods against an AValue-backed property.

### TryApply in Filters

Use `TryApply` when the value may be missing, null, mixed-type, or AValue-backed:

```csharp
var customers =
    from customer in test.Customer.AsEnumerable()
    where customer.FirstName.TryApply<string, bool>(
        name => name.StartsWith("a"))
    select customer;

customers.Take(100).Dump();
```

This avoids unsafe casts and avoids calling `StartsWith` unless the value can safely behave as a string.

### Apply in Projections

Use `Apply` when the AValue is expected to exist, but you still want safe conversion and default return behavior:

```csharp
var customers =
    from customer in test.Customer.AsEnumerable()
    select new
    {
        customer.PK,
        customer.FirstName,
        FirstNameLength = customer.FirstName.Apply<string, int>(
            name => name.Length),
        StartsWithA = customer.FirstName.Apply<string, bool>(
            name => name.StartsWith("a"))
    };

customers.Take(100).Dump();
```

### When to Use Each

Use `TryApply` in filters when a missing/null value should simply not match:

```csharp
where customer.FirstName.TryApply<string, bool>(name => name.StartsWith("a"))
```

Use `Apply` in projections when you want a computed value and default is acceptable if conversion fails:

```csharp
FirstNameLength = customer.FirstName.Apply<string, int>(name => name.Length)
```

***


## String Operations

Do not directly call string methods on an AValue unless the generated property is known to be a string.

Prefer:

```csharp
from customer in test.Customer.AsEnumerable()
where customer.FirstName.TryApply<string, bool>(
    name => name.StartsWith("A"))
select customer
```

Instead of:

```csharp
from customer in test.Customer.AsEnumerable()
where customer.FirstName.Value.ToString().StartsWith("A")
select customer
```

`TryApply` is more explicit and safer for null, missing, and mixed-type scenarios.

***

[Back to the Auto Values overview](README.md)
