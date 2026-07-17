# Collections, maps, JSON, CDTs, and search

> **Auto Values guide:** [Overview](README.md) · [Fundamentals](fundamentals.md) · [Conversion and comparison](conversion-and-comparison.md) · [Collections and search](collections-and-search.md) · [Expressions and querying](expressions-and-querying.md) · [Reference and mistakes](reference.md)

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

[Back to the Auto Values overview](README.md)
