# Auto Values helper reference and guidance

> **Auto Values guide:** [Overview](README.md) · [Fundamentals](fundamentals.md) · [Conversion and comparison](conversion-and-comparison.md) · [Collections and search](collections-and-search.md) · [Expressions and querying](expressions-and-querying.md) · [Reference and mistakes](reference.md)

## AValueHelper Extension Methods

The driver includes helper/extension methods for working with AValues, APrimaryKeys, Aerospike expressions, and collections of AValues.

### ToAValue

Convert a normal value, nullable value, Aerospike `Value`, or Aerospike `Bin` to an `AValue`:

```csharp
var value = "active".ToAValue();

var namedValue = "active".ToAValue("Status", "Status");

AValue nullableValue = ((int?)null).ToAValue("Score", "Score");
```

#### ToAValue for null-normalization

Use `ToAValue()` before CDT/map/list traversal when the source may already be an `AValue` or may be null.

If the source is already an `AValue`, the original `AValue` is preserved. If the source is null, the result is `AValue.Empty`.

```csharp
let invoices = customer.Invoices.ToAValue()
where !invoices.IsEmpty
from invoice in invoices.AsEnumerable()
from line in invoice.TryGetValue("Lines", AValue.Empty).AsEnumerable()
let trackId = line.TryGetValue("TrackId", AValue.Empty)
where trackId.CanConvert<long>()
select trackId.Convert<long>()
```

Prefer this pattern over replacing nullable CDT/list/map/document values with CLR fallback containers such as `new List<System.Text.Json.JsonDocument>()` when the next operation is AValue navigation.

### ToAPrimaryKey

Convert an Aerospike `Key` or normal value to an `APrimaryKey`:

```csharp
var pk = 123.ToAPrimaryKey("test", "Customer");
```

### ToAValueList

Convert an `ARecord` or Aerospike `Record` to AValues:

```csharp
var values = customer.ToAValueList();

values.Dump("AValues for customer");
```

### ToDictionary

Convert a collection of AValues to a dictionary keyed by each AValue's `BinName`:

```csharp
var byBin = customer.ToAValueList().ToDictionary();

byBin["FirstName"].Dump();
```

### OfType

Return only values whose underlying value is exactly type `T`:

```csharp
var strings = customer.ToAValueList().OfType<string>();
```

Use `OfType<T>()` when you want exact type filtering.

### Cast

Strictly cast values to `T`:

```csharp
var strings = customer.ToAValueList().Cast<string>();
```

Use `Cast<T>()` when all values must be castable and failures should throw.

### Convert

Convert values to `T` where possible and ignore non-convertible values:

```csharp
var numbers = customer.ToAValueList().Convert<long>();
```

Use `Convert<T>()` when you want coercion-style conversion.

### Contains

Search across a collection of AValues:

```csharp
var hasChicago = customer.ToAValueList()
    .Contains("Chicago", AValue.MatchOptions.Any);
```

### FindAll

Find matching values across AValue collections:

```csharp
customer.ToAValueList()
    .FindAll("Key3", AValue.MatchOptions.Any)
    .Dump("Matching values");
```

### TryGetValue

Use collection `TryGetValue` overloads to search across multiple AValues and return either an AValue or converted value:

```csharp
if (customer.ToAValueList().TryGetValue<string, string>("Email", out var email))
{
    email.Dump("Email");
}
```

### TryApply

Use the null-safe extension wrapper around `AValue.Apply(...)`:

```csharp
bool startsWithA = customer.FirstName
    .TryApply<string, bool>(name => name.StartsWith("A"));
```

### CanConvert

Use the null-safe extension wrapper around conversion testing:

```csharp
if (customer.TotalPurchases.CanConvert<decimal>())
{
    customer.TotalPurchases.Convert<decimal>().Dump();
}
```

### ToExpBin and ToExpVal

Use these helpers when building server-side Aerospike expressions from AValues:

```csharp
var status = "active".ToAValue("Status", "Status");

Client.Exp filterExpression = Exp.EQ(
    status.ToExpBin(Exp.Type.STRING),
    status.ToExpVal());
```

***


## Common Mistakes to Avoid

### Mistake: Calling LINQ collection methods directly on SetRecords

Avoid:

```csharp
test.Customer.Join(...)
```

Prefer:

```csharp
from customer in test.Customer.AsEnumerable()
join invoice in test.Invoice.AsEnumerable()
    on customer.PK equals invoice.CustomerId
select new
{
    customer,
    invoice
}
```

### Mistake: Using string-indexer access when a generated property exists

Avoid:

```csharp
customer["FirstName"]
```

Prefer:

```csharp
customer.FirstName
```

### Mistake: Unsafe casts

Avoid:

```csharp
((string)customer.FirstName.Value).StartsWith("A")
```

Prefer:

```csharp
customer.FirstName.TryApply<string, bool>(
    name => name.StartsWith("A"))
```

### Mistake: Treating Aerospike expressions like AValue comparisons

Avoid this when writing server-side expressions:

```csharp
Exp.EQ(customer.FirstName, Exp.Val("Alice"))
```

Prefer raw bin names:

```csharp
Exp.EQ(Exp.StringBin("FirstName"), Exp.Val("Alice"))
```

Or use AValue expression helpers carefully:

```csharp
var alice = "Alice".ToAValue("FirstName", "FirstName");

Exp.EQ(alice.ToExpBin(Exp.Type.STRING), alice.ToExpVal());
```

### Mistake: Assuming mixed-type ordering is semantic

Avoid broad less-than/greater-than comparisons on mixed-type bins unless you understand the comparison behavior:

```csharp
record.BinB < 800
```

Prefer type-aware checks:

```csharp
record.BinB.IsInt && record.BinB < 800
```

### Mistake: Using ElementAt on non-CDT values

Avoid:

```csharp
record.Status.ElementAt(0)
```

unless `Status` is known to be a list/map/CDT.

Prefer:

```csharp
var first = record.Status.ElementAtOrDefault(0);

if (!first.IsEmpty)
{
    first.Dump();
}
```

***


## Practical Decision Tree

Use this when deciding how to write AValue-aware code:

1.  **Generated property exists?**  
    Use it: `customer.FirstName`.
2.  **Need simple equality?**  
    Use direct AValue comparison: `customer.Status == "active"`.
3.  **Need numeric/date ordering?**  
    Add a type check or `CanConvert<T>()`.
4.  **Need a string/date/numeric method?**  
    Use `TryApply<TValue,TResult>()` in filters.
5.  **Need a computed projection?**  
    Use `Apply<TValue,TResult>()`.
6.  **Need explicit conversion?**  
    Use `CanConvert<T>()` before `Convert<T>()`.
7.  **Need to search list/map/scalar values?**  
    Use `Contains(...)`, `FindAll(...)`, `TryGetValue(...)`, and `AValue.MatchOptions`.
8.  **Need to normalize scalar-or-list values?**  
    Use `ToListItem()`.
9.  **Need to inspect or enumerate CDT values?**  
    Use `AsEnumerable()`, `ToList()`, `ToDictionary()`, or `ElementAtOrDefault(...)`.
10. **Need to write/update an Aerospike bin from an AValue?**  
    Use `ToBin()`.
11. **Need server-side filtering?**  
    Use Aerospike expressions with raw bin names or `ToExpBin()` / `ToExpVal()` when appropriate.

***


## Recommended AI Prompt Guidance

When generating LINQPad code against AValue-backed properties:

-   Prefer generated properties over string-indexer access.
-   Prefer query syntax when practical.
-   Use `.AsEnumerable()` before LINQ collection operations on sets.
-   Use direct AValue comparison for simple equality.
-   Use type checks for numeric/date ordering on mixed bins.
-   Use `TryApply<TValue,bool>()` for type-specific filter methods.
-   Use `Apply<TValue,TResult>()` for safe computed projections.
-   Use `CanConvert<T>()` and `Convert<T>()` for explicit conversion.
-   Use `PK` for primary key when available; otherwise use `GetPK()`.
-   Use `Contains(...)`, `ContainsKey(...)`, `FindAll(...)`, and `TryGetValue(...)` for scalar/list/map/JSON searches.
-   Use `AsEnumerable()`, `ToList()`, `ToListItem()`, `ToDictionary()`, and `ElementAtOrDefault(...)` for CDT exploration.
-   Use `ToBin()` when turning an AValue back into an Aerospike bin for write operations.
-   Use `ToExpBin()` and `ToExpVal()` only for Aerospike expression-building scenarios.
-   Use raw bin names only for Aerospike expressions or dynamic bin access.

[Back to the Auto Values overview](README.md)
