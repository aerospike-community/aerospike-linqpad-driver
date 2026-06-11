<!-- AValues-Readme-Version: 2026.06.11.1; Change: add LINQPad AI feature guidance and AI-generated AValue code patterns. -->

# Auto Values (`AValue`) in the Aerospike LINQPad Driver

`AValue` is the Aerospike LINQPad driver's **auto-value** abstraction. It is designed to make Aerospike records feel natural in LINQPad even though Aerospike is schemaless and stores a smaller set of native database types than .NET.

The goal of `AValue` is simple:

>   Let you work with Aerospike bin values and primary keys directly in LINQPad without constantly writing casts, null checks, conversion code, or raw Aerospike `Value` plumbing.

For example, with Auto Values enabled, you can often write:

```csharp
from customer in test.Customer.AsEnumerable()
where customer.FirstName == "Alice"
select customer
```

instead of writing code that manually extracts the bin value, checks for null, casts it, and handles mixed types.

`AValue` is most useful when:

-   A set contains mixed bin types.
-   Some records have missing or sparse bins.
-   You want LINQPad-friendly exploration.
-   You want to compare values without constantly casting.
-   You want to work with richer .NET values such as `DateTime`, `DateTimeOffset`, `TimeSpan`, JSON, maps, lists, and GeoJSON values.
-   You want safer helper methods such as `CanConvert<T>()`, `Convert<T>()`, `Apply<TValue,TResult>()`, and `TryApply<TValue,TResult>()`.
-   You want collection/map helpers that work across scalars, lists, dictionaries, JSON objects, and Aerospike CDTs.

The Native LINQPad samples under `linqpad-samples/Native` are a good source of examples, especially `Basic Data Types.linq` and `Basic Data Types 2.linq`.

---

## Auto Values and LINQPad AI

Auto Values are especially important for the Aerospike LINQPad AI features.

When LINQPad AI generates C# against the Aerospike LINQPad driver, it needs to produce code that works with Aerospike's schemaless data model. Records may be sparse, bins may be missing, and the same bin name may contain different types across records. `AValue` gives AI-generated code a safer and more natural way to work with that data.

For example, a natural-language request such as:

```text
Show me customers whose first name starts with J.
```

can generate AValue-aware LINQPad-driver code such as:

```csharp
var customers =
    (from customer in test.Customer.AsEnumerable()
     where customer.FirstName.TryApply<string, bool>(name => name.StartsWith("J"))
     select customer)
    .Take(100);

customers.Dump();
```

This is safer than directly casting `FirstName` to `string` because `TryApply<string, bool>(...)` only invokes `StartsWith` when the value can behave as a string. Missing, null, or mixed-type values simply do not match.

### AI-generated LINQPad-driver code

When AI generates LINQPad-driver code, it should prefer:

```csharp
customer.FirstName
customer.PK
customer.Total.CanConvert<decimal>()
customer.Total.Convert<decimal>()
customer.Profile.TryGetValue("email", AValue.Empty)
customer.Invoices.ToAValue().AsEnumerable()
```

over raw casts, raw Aerospike values, or string-indexer access when generated properties are available.

### AI-generated server-side expression code

Auto Values are client-side LINQPad-driver conveniences after records are materialized.

Server-side Aerospike expressions are different. When AI generates server-side expression filters, it should use raw bin names and Aerospike expression APIs:

```csharp
Client.Exp filterExpression = Exp.EQ(
    Exp.StringBin("State"),
    Exp.Val("CA"));

test.Customer
    .Query(filterExpression)
    .Take(100)
    .Dump();
```

For native Aerospike C# client API code, AI should use native APIs such as `AerospikeClient`, `ScanPolicy`, `QueryPolicy`, `Statement`, `Record`, `Exp.Build(...)`, and `record.GetValue("BinName")`. Native API code should not use `AValue`, generated LINQPad-driver sets, or generated record properties.

### AI guidance summary

When generating LINQPad-driver code, AI should:

- Prefer generated properties over string-indexer access.
- Use `IsEmpty` / `!IsEmpty` for AValue missing or empty checks.
- Use `TryApply<TValue,TResult>()` for type-specific methods on values that may be null, missing, or mixed-type.
- Use `CanConvert<T>()` before `Convert<T>()` when conversion may fail.
- Use `ToAValue()` before traversing nullable or document/CDT values.
- Use `TryGetValue(..., AValue.Empty)` for AValue/CDT map or document navigation.
- Avoid direct `System.Convert.*` calls on values that may be `AValue` or `APrimaryKey`.
- Avoid unsafe casts such as `(string)customer.FirstName.Value`.
- Use server-side Aerospike expressions when the request explicitly asks for server-side filtering.

This behavior is one of the main reasons Auto Values are a good fit for AI-assisted query generation in LINQPad.



***

## Why Auto Values Exist

Aerospike stores a limited set of native data types, including:

-   String
-   Integer / long
-   Double
-   Boolean
-   Map / dictionary
-   List
-   Bytes / blob
-   Geospatial
-   HyperLogLog

The LINQPad driver can expose those values as convenient .NET-facing values. In addition, the driver can transform or convert values into common .NET types that Aerospike does not natively store, such as:

-   `DateTime`
-   `DateTimeOffset`
-   `TimeSpan`
-   `decimal`
-   numeric variants
-   dictionaries
-   lists
-   byte arrays
-   JSON objects
-   GeoJSON objects

Most of the time users do not need to cast manually because the driver performs casting and transformation between Aerospike DB types and .NET types, seamlessly.

***

## AValue Versus Raw Bin Access

AValues gives the driver a consistent way to represent Aerospike bins in LINQPad, even when bins are different types across records, missing within a record, etc.

Let’s demonstrate the usefulness of AValues by examples. We have a “customer” set (in namespace “test”) and each customer may or may not have a “Company” bin (value is null). I want to obtain all customers who are associated with a company from the “State” of “CA”. Note, that the “State” bin can also be null (missing) and let’s assume this bin can contain different datatypes (e.g., strings and numbers).

Below is a sample of the customer set:

![](./docs/CompanyStateSampleRS.png)

There are multiple ways to obtain this result set. We will focus on four using the LINQPad Aerospike driver.

1.  Using AValues
2.  Using native .Net datatypes (disabling AValues)
3.  Using Aerospike Expression Filters
4.  Using native Aerospike API (not using Aerospike LINQPad driver)

### Example Using AValues

```csharp
from customer in test.Customer.AsEnumerable()
where !customer.Company.IsEmpty && customer.State == "CA"
	select customer
```

The result set:

![](./docs/CompanyStateCARS.png)

Even though bins “State” and “Company” maybe missing, AValues handled the checks.

Note: If “State” contained other datatype, instead of string, AValues would have taken care of the datatype checking and casting so that the equals operation would have worked correctly (can’t compare an int and a string, exception occurs).

### Example Using native .Net datatypes

In this example, AValues are disabled and assume “state” can contain multiple datatypes.

```csharp
from customer in test.Customer.AsEnumerable()
where !String.IsNullOrEmpty(customer.Company)
		&& customer.State is string s && s == "CA"
select customer
```

The result set:

![](./docs/CompanyStateCARS.png)

In this example we had to write more code and checks to ensure proper execution.

### Example Using Aerospike Expression Filters

In this example, we will use Aerospike Expression Filters with the Query method.

```csharp
from customer in test.Customer
					.Query(Aerospike.Client.Exp.And(
							Aerospike.Client.Exp.EQ(
								Aerospike.Client.Exp.StringBin("State"),
								Aerospike.Client.Exp.Val("CA")),
							Aerospike.Client.Exp.BinExists("Company")))
select customer
```

The result set:

![](./docs/CompanyStateCARS.png)

In this example we show how to write Aerospike Filter Expressions which results in server-side query execution.

Note: Working with complex filtering can result in difficulty writing and understanding Filter Expressions.

### Example Using native Aerospike API

In this example, we will use the native Aerospike API without using the Aerospike LINQPad driver.

```csharp
void Main()
{
    var host = "172.18.174.125";
    var port = 3000;

    var aerospikeNamespace = "test";
    var setName = "Customer";

    var clientPolicy = new ClientPolicy();
    using var client = new AerospikeClient(clientPolicy, host, port);

    var collector = new MatchingRecordCollector();

    var scanPolicy = new ScanPolicy();

    client.ScanAll(scanPolicy, aerospikeNamespace, setName, collector.OnRecord);
    
    var results = collector.Rows
        .OrderBy(r => r.Company)
        .ThenBy(r => r.State)
        .ThenBy(r => r.LastName)
        .ThenBy(r => r.FirstName)
        .ToList();

    results.Dump("Records where Company exists/string AND State == 'CA'");
}

sealed class MatchingRecordCollector
{
    private readonly ConcurrentBag<ResultRow> _rows = new();
    private long _scannedCount;

    public IReadOnlyCollection<ResultRow> Rows => _rows;
    public long ScannedCount => Interlocked.Read(ref _scannedCount);

    public void OnRecord(Key key, Record record)
    {
        Interlocked.Increment(ref _scannedCount);

        // Company must exist and be a string
        var companyValue = record.GetValue("Company");
        if (companyValue is not string company || string.IsNullOrWhiteSpace(company))
            return;

        // State must exist and normalize to "CA"
        var stateValue = record.GetValue("State");
        if (!IsCalifornia(stateValue))
            return;

        _rows.Add(new ResultRow
        {
            Digest = ToHex(key.digest),
            PrimaryKey = TryGetUserKey(key),
            Address = GetBinText(record, "Address"),
            City = GetBinText(record, "City"),
            Country = GetBinText(record, "Country"),
            Email = GetBinText(record, "Email"),
            FirstName = GetBinText(record, "FirstName"),
            LastName = GetBinText(record, "LastName"),
            Phone = GetBinText(record, "Phone"),
            PostalCode = GetBinText(record, "PostalCode"),
            State = NormalizeState(stateValue),
            SupportRepId = GetBinText(record, "SupportRepId"),
            Company = company,
            Fax = GetBinText(record, "Fax")
        });
    }

    private static bool IsCalifornia(object? value) =>
        string.Equals(NormalizeState(value), "CA", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeState(object? value) =>
        value switch
        {
            null => null,
            string s => s.Trim(),
            char c => c.ToString().Trim(),
            byte[] bytes => Encoding.UTF8.GetString(bytes).Trim(),
            _ => value.ToString()?.Trim()
        };

    private static string? GetBinText(Record record, string binName) =>
        NormalizeState(record.GetValue(binName));

    private static string? TryGetUserKey(Key key)
    {
        try
        {
            return key.userKey?.Object?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static string ToHex(byte[]? bytes) =>
        bytes is null ? "" : Convert.ToHexString(bytes);
}

sealed class ResultRow
{
    public string Digest { get; init; } = "";
    public string? PrimaryKey { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Phone { get; init; }
    public string? PostalCode { get; init; }
    public string? State { get; init; }
    public string? SupportRepId { get; init; }
    public string Company { get; init; } = "";
    public string? Fax { get; init; }
}
```

The result set:

![](./docs/CompanyStateCADigestRS.png)

This example really shows the complexity of using the native Aerospike C\# API driver. It took 137 lines of code versus at maximum 7 lines with the Aerospike LINQPad driver.

### Obtaining the Associated Aerospike Bin/Value Instance

The Aerospike Client API `Bin` instance can be obtained by means of the `ToBin()` method. For Aerospike server-side expression literal values, use `ToExpVal()`. For expression bin references, use `ToExpBin(...)`.

***

***

## APrimaryKey and Primary Key Values

`APrimaryKey` is the primary-key companion to `AValue`. It gives the driver a consistent way to represent Aerospike keys in LINQPad, even when primary keys are different types across records, user key values ([send key policy](https://aerospike.com/docs/database/learn/policies#send-key) is true), working with [Aerospike digests](https://aerospike.com/docs/database/learn/architecture/data-storage/data-model/#keys-and-digests), etc.

All the features of AValues apply to APrimaryKey. APrimaryKey is enhanced to support digest to user key value compare support, conversion from a user key value to digest, creation/conversion of keys between namespaces and/or sets, etc.

Below is an example where a record was inserted with the “send key” policy was set to false.

![](./docs/DigestSampleRS.png)

I want to be able to retrieve this record by means of the user key value and the digest (you can always use the digest to obtain any record when using the driver).

```csharp
test.DataTypes.Where(r => r.PK == "NoPKValueSaved") //using the user key value
```

Below is the result set:

![](./docs/DigestRSOnly.png)

```csharp
test.DataTypes.Where(r => r.PK == "0xc363ecde6a39ae0611c69ee2c7bd8a3b6930337b")
```

Below is the result set:

![](./docs/DigestRSOnly.png)

Note that the digest can be represented as a hex string, a byte array, APrimaryKey instance, etc.

### Obtaining the Associated Aerospike Client Key Instance

The Aerospike Client API Key instance can be obtained by means of the `AerospikeKey` property.

***

***

## Basic Comparisons

`AValue` is designed to make common LINQPad comparisons feel natural.

In many cases, you can compare an `AValue` directly to a normal .NET value:

```csharp
test.DataTypes
    .Where(dt => dt.BinA == "BinA123")
    .Dump("Records where BinA equals BinA123");

test.DataTypes
    .Where(dt => dt.BinB == 1001)
    .Dump("Records where BinB equals 1001");
```

This is a core Auto Values use case: you can often compare values without manually casting them to match the underlying Aerospike DB type.

### Equality Comparisons

Use direct equality when the intent is simple value equality:

```csharp
test.DataTypes
    .Where(dt => dt.BinA == "10.01")
    .Dump("String comparison");

test.DataTypes
    .Where(dt => dt.BinB == 1001)
    .Dump("Numeric comparison");
```

AValue equality is usually the most convenient comparison style for LINQPad exploration.

### CompareTo and Ordering Operators

`AValue` supports comparison operators and `CompareTo(...)` against other `AValue` instances, Aerospike `Value`, Aerospike `Key`, and normal objects:

```csharp
test.DataTypes
    .Where(dt => dt.BinB > 100)
    .Dump("BinB greater than 100");

test.DataTypes
    .Where(dt => dt.BinB.CompareTo(100) > 0)
    .Dump("BinB CompareTo example");
```

Greater-than and less-than comparisons are safest when both sides are numeric-compatible, date/time-compatible, or otherwise known to be comparable.

When comparing values of different non-numeric types, the driver may fall back to deterministic comparison behavior rather than semantic numeric/date/string ordering. Use type checks when the ordering meaning matters.

### Null and Missing Values

AValues are designed to make null and missing-bin scenarios easier to work with. However, for mixed or sparse sets, type-aware guards are still recommended when the next operation depends on a specific type.

For example:

```csharp
test.DataTypes
    .Where(dt => dt.BinExists("BinB") && dt.BinB == 1001)
    .Dump("Records where BinB exists and equals 1001");
```

Use `BinExists(...)` when the query depends on a bin being present.

***

## Type Inspection

Use `AValue` type-inspection properties when the operation depends on the underlying type:

```csharp
value.IsString
value.IsNumeric
value.IsInt
value.IsFloat
value.IsBool
value.IsList
value.IsMap
value.IsDictionary
value.IsCDT
value.IsJson
value.IsGeoJson
value.IsDateTime
value.IsDateTimeOffset
value.IsTimeSpan
value.IsKeyValuePair
value.IsEmpty
value.UnderlyingType
```

Examples:

```csharp
test.DataTypes
    .Where(dt => dt.BinB.IsInt && dt.BinB < 800)
    .Dump("BinB less than 800 where BinB is an integer");

test.DataTypes
    .Where(dt => dt.Profile.IsJson)
    .Dump("Records where Profile is JSON");
```

Use the exact properties available on your driver version.

***

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

## Collections, Maps, JSON, and CDTs

`AValue` includes helpers that make it easier to work with Aerospike CDTs, JSON values, dictionaries, lists, and scalar values.

### ToDictionary

Use `ToDictionary()` to safely view a map-like value as `IDictionary<object, object>`:

```csharp
var profileMap = customer.Profile.ToDictionary();

profileMap.Dump("Profile map");
```

If the value is not map-like, an empty dictionary is returned.

### ToDictionary\<K,V\>

Use `ToDictionary<K,V>(keySelector, valueSelector)` to project keys and values through `AValue` conversion logic:

```csharp
var typedProfile = customer.Profile.ToDictionary(
    key => key.Convert<string>(),
    value => value.Convert<object>());

typedProfile.Dump("Typed profile map");
```

This is useful when the source map may contain JSON, object dictionaries, string dictionaries, or AValue dictionaries.

### ToList

Use `ToList()` to safely view list-like, JSON-array, object-enumerable, or GeoJSON collection values as `IList<object>`:

```csharp
var tags = customer.Tags.ToList();

tags.Dump("Tags");
```

If the value is not list-like, an empty list is returned.

### ToListItem

Use `ToListItem()` when you always want a list. If the value is not a collection, it returns a one-item list containing the scalar value:

```csharp
var values = customer.Status.ToListItem();

values.Dump("Status as one-item list");
```

This is useful when you want to normalize scalar-or-list bins.

### ToCDT

Use `ToCDT()` when converting JSON documents or dictionary-like values into Aerospike CDT-style dictionaries:

```csharp
var cdtItems = customer.Profile.ToCDT();

cdtItems.Dump("Profile as CDT dictionaries");
```

This is useful when preparing JSON-like values for Aerospike map/list storage.

### AsEnumerable

Use `AsEnumerable()` to enumerate CDT values as `AValue` elements:

```csharp
var items =
    from item in customer.Tags.AsEnumerable()
    select item;

items.Dump("Tags as AValue items");
```

For a dictionary/map, the elements can be key/value-pair AValues.

### AsEnumerable<T>

Use `AsEnumerable<T>()` when the AValue should be converted to an array/enumerable of a specific type:

```csharp
var tagStrings = customer.Tags.AsEnumerable<string>();

tagStrings.Dump("Tag strings");
```

If conversion is not possible, the method can throw.

### ElementAt and ElementAtOrDefault

Use `ElementAt(index)` to get an AValue at a CDT index when you expect the value to be a CDT:

```csharp
var firstTag = customer.Tags.ElementAt(0);
```

Use `ElementAtOrDefault(index)` when the value may not be a CDT or the index may be out of range:

```csharp
var firstTag = customer.Tags.ElementAtOrDefault(0);

if (!firstTag.IsEmpty)
{
    firstTag.Dump("First tag");
}
```

### Count

Use `Count()` to count characters in a string or elements in a collection. If the value is neither a string nor collection, `-1` is returned:

```csharp
var tagCount = customer.Tags.Count();
var nameLength = customer.FirstName.Count();
```

### ToBin

Use `ToBin()` to convert an AValue back into an Aerospike `Bin` using `BinName` or `FldName` and Aerospike-compatible conversion:

```csharp
var bin = customer.Status.ToBin();
```

This is useful when preparing write operations from an AValue.

### DebugDump

Use `DebugDump()` when debugging AValue metadata such as `Value`, `BinName`, `FldName`, and type information:

```csharp
customer.Profile.DebugDump();
```

***

## Contains and Search Helpers

`AValue.Contains(...)` is useful because a bin may be a scalar, list, map, JSON object, or key/value pair.

```csharp
test.DataTypes
    .Where(dt => dt.BinA.Contains("BinA123"))
    .Dump("Records where BinA contains BinA123");
```

Use type checks to narrow behavior:

```csharp
test.DataTypes
    .Where(dt => dt.BinA.IsList && dt.BinA.Contains("BinA123"))
    .Dump("Records where BinA is a list containing BinA123");

test.DataTypes
    .Where(dt => dt.BinA.IsString && dt.BinA.Contains("BinA123"))
    .Dump("Records where BinA is a string containing BinA123");
```

Use match options when searching maps/lists more broadly:

```csharp
test.DataTypes
    .Where(dt => dt.BinB.Contains("Key3", AValue.MatchOptions.Any))
    .Dump("Records where BinB contains Key3 anywhere");

test.DataTypes
    .Where(dt => dt.BinB.Contains(
        "Key3",
        AValue.MatchOptions.Any | AValue.MatchOptions.SubString))
    .Dump("Records where BinB contains Key3 as a substring anywhere");
```

### MatchOptions

`AValue.MatchOptions` controls how `Contains(...)` and `FindAll(...)` search. Some key-specific helpers, such as AValue-keyed `TryGetValue(...)`, use exact matching internally:

-   `Value` searches normal values and keys depending on the underlying type.
-   `Equals` uses AValue-aware equality.
-   `Any` expands matching across dictionary keys and values.
-   `SubString` performs substring matching for strings.
-   `Exact` treats the whole value/collection as the thing being matched instead of searching inside it.
-   `Regex` applies a regular expression to `ToString()` output.

Example:

```csharp
test.DataTypes
    .Where(dt => dt.BinB.Contains(
        "key[0-9]+",
        AValue.MatchOptions.Any | AValue.MatchOptions.Regex))
    .Dump("Regex search across BinB");
```

### ContainsKey

Use `ContainsKey(...)` when the AValue is expected to represent a map, dictionary, JSON object, or key/value pair:

```csharp
test.Customer
    .Where(customer => customer.Profile.ContainsKey("email"))
    .Dump("Customers with profile email");
```

### Contains Key and Value

Use `Contains(key, value)` to test map/dictionary/key-value-pair values:

```csharp
test.Customer
    .Where(customer => customer.Profile.Contains("status", "active"))
    .Dump("Customers whose profile.status is active");
```

### FindAll

Use `FindAll(...)` to return all matching values as AValues:

```csharp
test.DataTypes
    .GetBinBValues()
    .FindAll("Key3")
    .Dump("Values in BinB containing Key3");
```

With match options:

```csharp
test.DataTypes
    .GetBinBValues()
    .FindAll("Key3", AValue.MatchOptions.Any)
    .Dump("Values in BinB containing Key3 anywhere");
```

`FindAll` exists both on a single `AValue` and as an extension over `IEnumerable<AValue>`.

### TryGetValue on AValue

Use `TryGetValue` to search scalar/list/map/string/JSON values and retrieve the matched value as either an AValue or converted CLR type.

Return an `AValue`:

```csharp
if (customer.Profile.TryGetValue("email", out AValue emailValue))
{
    emailValue.Dump("Email value");
}
```

Return a converted CLR value:

```csharp
if (customer.Profile.TryGetValue<string>("email", out var email))
{
    email.Dump("Email");
}
```

Return a default when no match is found or conversion fails:

```csharp
var email = customer.Profile.TryGetValue("email", defaultValue: "<missing>");
```

Return `AValue.Empty` instead of `null` when no match is found:

```csharp
var emailValue = customer.Profile.TryGetValue("email", returnEmptyAValue: true);

if (!emailValue.IsEmpty)
{
    emailValue.Dump("Email");
}
```

### TryGetValue on IEnumerable

The collection extension overloads search across multiple AValue items.

Return a converted value through an `out` parameter:

```csharp
if (customer.Profile.AsEnumerable().TryGetValue<string, string>("email", out var email))
{
    email.Dump("Email");
}
```

Return a converted value or a default:

```csharp
var email = customer.Profile
    .AsEnumerable()
    .TryGetValue<string, string>("email", "<missing>");
```

Return the matching AValue:

```csharp
if (customer.Profile.AsEnumerable().TryGetValue("email", out AValue emailValue))
{
    emailValue.Dump("Email AValue");
}
```

Return a matching AValue or `AValue.Empty`:

```csharp
var emailValue = customer.Profile
    .AsEnumerable()
    .TryGetValue("email", returnEmptyAValue: true);
```

### Contains on IEnumerable

Use the collection `Contains` extension to search across multiple AValue elements:

```csharp
var hasKey3 = test.DataTypes
    .GetBinBValues()
    .Contains("Key3", AValue.MatchOptions.Any);
```

### AValue-backed dictionary keys

Some Aerospike map/CDT or JSON-style structures may expose keys as `AValue` instances instead of plain CLR strings, integers, or other primitive key types.

For these cases, use the AValue-aware key helper methods on `IEnumerable<KeyValuePair<TKey,TValue>>` where `TKey : AValue`.

#### TryGetValue for AValue-backed keys

Prefer `TryGetValue(...)` when missing keys are normal and you want a non-throwing lookup.

Use this overload when you want to preserve the original value type and supply a default value:

```csharp
var email = keyValuePairs.TryGetValue("email", defaultValue: "<missing>");
```

Use this overload when you want the matched value returned as an `AValue`. If no key matches, it returns `AValue.Empty`:

```csharp
var emailValue = keyValuePairs.TryGetValue("email");

if (!emailValue.IsEmpty)
{
    emailValue.Dump("email");
}
```

These overloads perform exact matching against the `AValue` key using `AValue.MatchOptions.Exact`.

#### ContainsKey for AValue keys

Use `ContainsKey(...)` when dictionary/map keys are `AValue` instances and you only need to test whether a matching key exists:

```csharp
var hasEmailKey = profileMap.ContainsKey("email");
```

#### GetByKey for AValue-backed keys

Use `GetByKey(...)` when you have an `IEnumerable<KeyValuePair<TKey,TValue>>` where `TKey : AValue` and you want to retrieve the value whose key matches using AValue comparison behavior, and missing keys should be treated as an error.

```csharp
var value = keyValuePairs.GetByKey("email");
```

When missing keys are expected, prefer `TryGetValue(...)` over `ContainsKey(...)` plus `GetByKey(...)`.

***

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
