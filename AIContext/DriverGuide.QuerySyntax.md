<!-- AIContext-Version: 2026.06.08.4; Change: native dictionary lookup boundary to prevent LINQPad-driver TryGetValue helper leakage into native mode. -->

## Driver Usage Rules

- Generate C# statements intended to run inside LINQPad.
- Use `Dump()` to display results.
- Prefer generated namespace and set members when they are present.
- Keep scans and queries bounded with `Take(...)`, filters, or secondary indexes where possible.
- Do not assume every Aerospike record in a set has every bin.
- Do not assume a bin has only one type unless metadata clearly indicates it.
- Treat bin names as case-sensitive.
- Prefer read-only query/exploration code unless the user explicitly asks for writes.
- Ask before destructive deletes/truncates unless the user explicitly requested them.
- Use the native Aerospike client only when the high-level driver API does not cover the request.

### Important `Util` Type Ambiguity Rule

- The name `Util` can be ambiguous because both `LINQPad.Util` and `Aerospike.Client.Util` may be available.
- When calling a LINQPad utility method, fully qualify it as `LINQPad.Util`.
- When calling an Aerospike client utility method, fully qualify it as `Aerospike.Client.Util`.
- Do not generate unqualified `Util.SomeMethod(...)` unless the context clearly guarantees there is no ambiguity.
- For LINQPad output, input, file, markdown, CSV, or AI helper calls, prefer explicit `LINQPad.Util`.
- For Aerospike client utility calls, prefer explicit `Aerospike.Client.Util`.

Examples:

```csharp
LINQPad.Util.WriteCsv(rows, filePath);
LINQPad.Util.ReadLine("Enter a value:");
LINQPad.Util.Markdown(markdown).Dump("Rendered Markdown");
```

### Important native Aerospike C# client API override

When the user asks for "native Aerospike API", "native C# client", "total Aerospike native API", "no LINQPad driver API", or similar wording, native mode overrides all LINQPad-driver examples and rules.

In native API mode, do not use `test.Customer`, `test.Customer.Query(...)`, `test.Customer.AsEnumerable()`, `SetRecords`, `SetRecords<T>`, `AValue`, `APrimaryKey`, `PK`, `GetPK()`, or generated record properties such as `customer.FirstName`.

In native API mode, use `new AerospikeClient(...)`, `ClientPolicy`, `ScanPolicy`, `QueryPolicy`, `Statement`, `Filter`, `client.ScanAll(...)`, `client.Query(...)`, raw namespace/set/bin names, `record.GetValue("BinName")`, and `Exp.Build(...)` for native policy filter expressions.

Use `RegexFlag.NONE`, not `Exp.RegexFlag.NONE`.

Do not generate `test.Customer.Query(filterExpression)` in native API mode. That is the Aerospike LINQPad driver API, not the native Aerospike C# client API.

### Important Native Nested CDT Expression Safety Rule

- Native Aerospike expressions are strict server-side expression trees.
- Do not invent `ListExp` or `MapExp` method chains for complex nested document/list/map searches.
- For nested paths such as `Invoices -> Lines -> TrackId`, only generate a fully server-side native expression if the exact C# expression API pattern is known and verified.
- If the nested native expression pattern is uncertain, generate a correct native scan with client-side nested traversal, or use a simple/coarse server-side expression such as `Exp.BinExists("Invoices")` followed by client-side traversal.
- Prefer correct runnable code over speculative server-side expression code.
- Do not generate invalid expression calls such as:
  - `Exp.Val(ListReturnType.VALUE)`
  - `ListExp.ValRange(...)`
  - `ListExp.ValRange(Value.Get("TrackId"))`
  - `Exp.Bin("Invoices")`
  - `Exp.RegexFlag.NONE`
- Use `RegexFlag.NONE`, not `Exp.RegexFlag.NONE`.


### Important Generated Script Summary and Comment Rule

- For runnable generated C# scripts, start with a short comment block that summarizes the user's request before the first executable statement.
- The request summary should capture the target namespace/set, filter criteria, output shape, and any important mode choice such as LINQPad-driver mode, native API mode, server-side expression filtering, or client-side traversal.
- Keep the summary factual and concise; do not restate the entire prompt verbatim.
- The top request-summary comment is always allowed because it documents the generated script's intent.
{{InlineCommentGuidance}}



### Important C# Iterator Helper Rule

- When generating C# helper methods that use `yield return`, the method body is an iterator block.
- Do not generate `return someEnumerable;`, `return someValue;`, or `return objectList;` inside an iterator block. That does not compile when the method also contains `yield return`.
- To emit all items from an existing enumerable inside an iterator block, use `foreach` and `yield return` each item.
- When a first branch handles `IEnumerable<object>`, add `yield break;` after yielding those items so the broader `System.Collections.IEnumerable` branch does not emit the same items again.
- If the helper should directly return an enumerable, then do not use `yield return` anywhere in that helper; instead return `Enumerable.Empty<object>()`, `objectList`, or a projected enumerable consistently.

Preferred native/helper pattern:

```csharp
IEnumerable<object> AsObjectEnumerable(object value)
{
    if (value is IEnumerable<object> objectList)
    {
        foreach (var item in objectList)
            yield return item;

        yield break;
    }

    if (value is System.Collections.IEnumerable enumerable && value is not string)
    {
        foreach (var item in enumerable)
            yield return item;
    }
}
```

Avoid this invalid iterator pattern:

```csharp
IEnumerable<object> AsObjectEnumerable(object value)
{
    if (value is IEnumerable<object> objectList)
        return objectList;

    if (value is System.Collections.IEnumerable enumerable && value is not string)
    {
        foreach (var item in enumerable)
            yield return item;
    }
}
```



### Important Native API Purity Rule

When the selected mode is native Aerospike C# client API mode, **all** Aerospike data access in the generated script must use native client objects and methods.

Do not mix native customer filtering with LINQPad-driver enrichment. In native mode, do not use:

```csharp
test.CustInvsDoc
test.CustInvsDoc.AerospikeClient
test.Track.AsEnumerable()
test.Album.AsEnumerable()
test.Artist.AsEnumerable()
SetRecords
AValue
APrimaryKey
PK
GetPK()
generated record properties such as track.AlbumId or artist.Name
```

Use an explicit native client connection:

```csharp
using var client = new AerospikeClient(clientPolicy, host, port);
```

Then use native calls such as `client.Query(...)`, `client.ScanAll(...)`, `client.Get(...)`, `client.Put(...)`, `client.Delete(...)`, and `record.GetValue("BinName")` with raw namespace, set, and bin names.

For native enrichment across related sets, read `Track`, `Album`, and `Artist` through the same native `AerospikeClient`; do not switch back to generated LINQPad driver sets.


### Important Native Dictionary Lookup Boundary Rule

When the selected mode is native Aerospike C# client API mode, dictionary lookup code must use ordinary C# dictionary APIs. Do not use LINQPad-driver or AValue/default-value helper lookup patterns in native-mode output.

In native mode, do not generate patterns such as:

```csharp
let trackInfo = trackById.TryGetValue(trackId, default((string TrackName, long AlbumId)?))
let trackInfo = trackById.TryGetValue(trackId, null)
var trackInfo = trackById.TryGetValue(trackId, defaultValue)
source.TryGetValue("KeyName")
source.TryGetValue("KeyName", AValue.Empty)
```

Those patterns are LINQPad-driver/AValue helper-style lookups and are not valid for normal native C# dictionaries.

Use normal C# dictionary lookup with `out var` inside a scoped block, block lambda, or helper method:

```csharp
if (trackById.TryGetValue(trackId, out var trackInfo))
{
    // use trackInfo here
}
```

For native projection/enrichment code, use a block lambda when lookup state is needed:

```csharp
.Select(trackId =>
{
    trackById.TryGetValue(trackId, out var trackInfo);

    return new
    {
        TrackId = trackId,
        TrackName = trackInfo?.TrackName,
        AlbumId = trackInfo?.AlbumId
    };
})
```

This native rule overrides the LINQPad-driver dictionary lookup rule whenever native API mode is selected.


### Important LINQ Syntax Preference

- Preferred LINQ style: `query syntax`.
- Use query syntax as the default form for query logic.
- For filters, projections, sorting, joins, and grouping, use `from`, `where`, `select`, `orderby`, `join`, and `group`.
- Do not use method-chain forms such as `.Where(...)`, `.Select(...)`, `.OrderBy(...)`, `.Join(...)`, or `.GroupBy(...)` when an equivalent query-syntax form is available.
- Method syntax is allowed only for terminal or non-query-expression operations such as `.Take(100)`, `.Skip(...)`, `.ToList()`, `.Count()`, `.FirstOrDefault()`, `.Any()`, `.Dump()`, or operations that cannot be expressed cleanly in query syntax.
- For joins, generate `from left in Namespace.LeftSet.AsEnumerable() join right in Namespace.RightSet.AsEnumerable() on left.Key equals right.Key select ...`.
- Do not generate `.Join(...)` when a query-syntax `join` clause can express the same logic.

### Important C# Scoping Rule for `out var` and LINQ

When generating C# code that uses dictionaries and `TryGetValue(...)`, do not declare `out var` variables in one LINQ query clause and then reference those variables in later query clauses. This can produce invalid or fragile C#.

Avoid this pattern:

```csharp
from trackId in trackIds
let hasTrack = trackById.TryGetValue(trackId, out var trackInfo)
let albumId = hasTrack ? trackInfo.AlbumId : 0L
select new
{
    TrackId = trackId,
    TrackName = trackInfo.Name
}
```

This rule applies to all generated C# code, including LINQPad-driver queries, native Aerospike C# client code, helper methods, projections, enrichment logic, and post-processing code. Prefer correctness and clear C# scoping over forcing query syntax when `out var`, dictionary lookups, exception handling, or multi-step enrichment logic is involved.


### Important Dictionary Lookup Rule for LINQPad-driver Query Projections

- This rule applies only to LINQPad-driver code. It does not apply to native Aerospike C# client API mode.
- When generating LINQPad-driver code that enriches nested CDT/AValue results from a dictionary lookup, prefer the non-throwing default-value lookup helper instead of a `ContainsKey(...) ? dictionary[key] : null` expression.
- Preferred pattern: `let enrichment = lookupById.TryGetValue(id, null)`.
- Avoid this pattern inside LINQ query clauses: `let enrichment = lookupById.ContainsKey(id) ? lookupById[id] : null`.
- This avoids duplicated lookups, avoids indexer access in the `let` clause, and keeps lookup code consistent with the driver's AValue-safe / null-safe style.
- Only use the standard `TryGetValue(key, out var value)` form outside LINQ query clauses or inside a block lambda/local helper where the `out var` variable will not be referenced across LINQ query clauses.

Preferred:

```csharp
let enrichment = trackInfoById.TryGetValue(trackId, null)
select new
{
    TrackId = trackId,
    ArtistName = enrichment?.ArtistName,
    AlbumTitle = enrichment?.AlbumTitle
}
```

Avoid:

```csharp
let enrichment = trackInfoById.ContainsKey(trackId) ? trackInfoById[trackId] : null
```

### Important Record Property Rule

- When accessing Aerospike bin values from generated record objects, prefer generated C# properties when available.
- Example: use `customer.userid` instead of `customer["userid"]` when the `userid` property exists.
- Use `record["binName"]` only when no generated property exists, the bin name is not a valid C# identifier, or dynamic access is specifically required.
- Prefer property access in projections, filters, joins, sorts, and groups.
- The set-level bin metadata below lists the raw Aerospike bin name and, when available, the generated C# property name.

### Important Projection Naming Rule

- When projecting fields from multiple records, explicitly name projected properties that may collide.
- Do not rely on inferred anonymous-object property names when projecting the same property name from more than one source.
- Common collision-prone names include `PK`, `Name`, `Title`, `Id`, `Date`, `Total`, `Email`, `City`, `State`, and any repeated bin/property name across joined sets.
- Always alias primary keys from multiple records, such as `CustomerPK`, `InvoicePK`, `TrackPK`, `AlbumPK`, and `ArtistPK`.
- Always alias repeated descriptive fields, such as `TrackName = track.Name` and `ArtistName = artist.Name`.
- Prefer clear source-qualified names in projections from joins.

Avoid:

```csharp
select new
{
    customer.PK,
    invoice.PK,
    track.Name,
    artist.Name
}
```

Prefer:

```csharp
select new
{
    CustomerPK = customer.PK,
    InvoicePK = invoice.PK,
    TrackName = track.Name,
    ArtistName = artist.Name
}
```

### Important AValue / AutoValue Rule

- The Aerospike LINQPad driver may expose bin values through `AValue` / AutoValue behavior.
- Current connection setting `Always use AValue`: `{{AlwaysUseAValues}}`.
- When `Always use AValue` is true, generated record properties may represent Aerospike values using the driver's `AValue` abstraction instead of plain CLR primitive types.
- Prefer generated record properties first, but write comparisons and projections in a way that respects the property's generated type.
- Do not assume an `AValue`-backed property is a raw `string`, `int`, `long`, `double`, `bool`, `DateTime`, list, or dictionary unless the context metadata clearly says so.
- Avoid unsafe casts from `AValue`-backed values to CLR primitive types.
- Use the driver's `AValue`-friendly comparison, conversion, or value-access patterns when needed.

### Important Nested Document / CDT Navigation Rule

- Aerospike bins may contain nested documents, JSON objects, maps, lists, GeoJSON values, or Aerospike CDTs.
- When a generated property is a document/list/map/CDT value, do not assume requested fields exist directly at the first level.
- If the metadata shows a property type such as `JsonDocument`, `List<JsonDocument>`, `Dictionary`, `Map`, `List`, `CDT`, or another document-like value, inspect or navigate the nested structure before selecting fields.
- Use AValue-safe navigation methods such as `Contains(...)`, `ContainsKey(...)`, `TryGetValue(...)`, `AsEnumerable()`, `ToList()`, `ToDictionary()`, `ElementAtOrDefault(...)`, and `AValue.Empty`.
- When searching for a field that may occur inside nested arrays/lists, flatten candidate child collections with `SelectMany(...)`.
- Use `TryGetValue("childCollectionName", AValue.Empty).AsEnumerable()` to safely access nested child collections.
- Use `TryGetValue("fieldName", defaultValue)` to safely read nested scalar fields.
- Do not generate direct access to a nested field unless the context clearly says that field exists at that document level.
- When the user asks for records containing a nested value, return the parent record and optionally include the matching nested items.
- For nested document filters, prefer query syntax with `let` clauses to keep the traversal readable.


### Important AValue Null Normalization Rule

- When a generated property, nested document value, list, map, JSON value, or CDT value may already be `AValue` or may be null, prefer `value.ToAValue()` to normalize it before AValue/CDT navigation.
- `ToAValue()` returns the original `AValue` when the value is already an `AValue`; when the source value is null, it returns `AValue.Empty`.
- Do not replace nullable CDT/list/map/document values with CLR fallback containers such as `new List<System.Text.Json.JsonDocument>()` when the next operation is AValue navigation.
- Prefer `let invoices = customer.Invoices.ToAValue()` over `let invoices = customer.Invoices ?? new List<System.Text.Json.JsonDocument>()`.
- After normalization, use normal AValue-safe operations such as `IsEmpty`, `AsEnumerable()`, `TryGetValue(...)`, `CanConvert<T>()`, and `Convert<T>()`.
- Use CLR fallback containers only when the subsequent code truly requires a concrete CLR collection type rather than AValue/CDT navigation.

Preferred pattern:

```csharp
let invoices = customer.Invoices.ToAValue()
where !invoices.IsEmpty
from invoice in invoices.AsEnumerable()
from line in invoice.TryGetValue("Lines", AValue.Empty).AsEnumerable()
let trackId = line.TryGetValue("TrackId", AValue.Empty)
where trackId.CanConvert<long>()
select trackId.Convert<long>()
```

Avoid this pattern for AValue/CDT traversal:

```csharp
let invoices = customer.Invoices ?? new List<System.Text.Json.JsonDocument>()
```

### Important AValue-backed Map Key Rule

- Some map, dictionary, JSON, and CDT structures may expose keys as `AValue` instances rather than plain CLR key types.
- When searching `IEnumerable<KeyValuePair<TKey,TValue>>` where `TKey : AValue`, use AValue-aware helpers rather than assuming keys are plain strings.
- Prefer the non-throwing AValue-keyed `TryGetValue(...)` helper overloads when missing keys are normal.
- Use `source.TryGetValue("KeyName", defaultValue)` when the caller needs the original `TValue` type.
- Use `source.TryGetValue("KeyName")` when the caller wants the matched value as `AValue`; this returns `AValue.Empty` when no matching key is found.
- The AValue-keyed `TryGetValue(...)` helpers use `AValue.MatchOptions.Exact` against the source AValue key.
- Use `ContainsKey(...)` and `GetByKey(...)` only when separate existence testing is needed or throwing on missing keys is intentional.
- Use `ContainsKey(...)` before `GetByKey(...)` when missing keys are expected, because `GetByKey(...)` throws `KeyNotFoundException` if no key matches.
- Do not manually iterate key/value pairs or convert every key with `.Convert<string>()` unless the key type is known and conversion is required.
- For nested CDT/map traversal, prefer `line.TryGetValue("TrackId", AValue.Empty)` or `line.TryGetValue("TrackId")` over `TryApply<IDictionary<...>>(...)` or manual dictionary conversion.

### Important AValue-keyed Dictionary TryGetValue Rule

- Some map, dictionary, JSON, and CDT structures may expose keys as `AValue` instances rather than plain CLR key types.
- When the source is `IEnumerable<KeyValuePair<TKey,TValue>> where TKey : AValue`, prefer the non-throwing AValue-keyed `TryGetValue(...)` helper overloads for exact key matching.
- Use `source.TryGetValue("KeyName", defaultValue)` when the caller needs the original `TValue` type and has an appropriate default value.
- Use `source.TryGetValue("KeyName")` when the caller wants the matched value as `AValue`; this overload returns `AValue.Empty` when no matching key is found.
- These helpers use `AValue.MatchOptions.Exact` against the AValue key.
- Prefer these helpers over `ContainsKey(...)` plus `GetByKey(...)` when missing keys are normal, because `TryGetValue(...)` is non-throwing.
- Use `ContainsKey(...)` plus `GetByKey(...)` only when the code specifically needs separate existence testing or the throwing behavior is intentional.
- Do not manually iterate key/value pairs or convert every AValue key to string just to find a key when these helpers are available.

Preferred examples:

```csharp
var lines = invoice.TryGetValue("Lines", AValue.Empty).AsEnumerable();
var trackId = line.TryGetValue("TrackId", AValue.Empty);
```

When working with an AValue-keyed key/value sequence directly:

```csharp
var trackId = line.AsEnumerable<KeyValuePair<AValue, AValue>>()
                  .TryGetValue("TrackId");
```

Avoid this pattern when the non-throwing helper is available:

```csharp
var trackId = line.ContainsKey("TrackId")
    ? line.GetByKey("TrackId")
    : AValue.Empty;
```
### Important Aerospike Expression Rule

- AValue comparisons and LINQ `where` clauses are client-side after records are returned and materialized by the driver.
- Aerospike filter expressions are server-side and use raw Aerospike bin names plus Aerospike expression APIs.
- Use Aerospike expressions when the user asks for server-side filtering, expression filters, filter expressions, `Query(...)`, CDT/map/list expression filters, or reducing records at the server.
- Use raw bin names inside `Exp.StringBin(...)`, `Exp.IntBin(...)`, `Exp.FloatBin(...)`, `Exp.BoolBin(...)`, `Exp.Bin(...)`, `MapExp`, `ListExp`, and related expression builders.
- Do not use generated record properties inside server-side `Exp.*` expression builders.
- When using AValue expression helpers, use `value.ToExpBin(...)` for the bin reference side and `value.ToExpVal()` for the literal side.
- For straightforward server-side expressions, using raw bin names directly is usually simpler and clearer.
- Do not call `Exp.Build(...)` when passing an `Exp` filter expression to `SetRecords.Query(...)`; the driver builds it into the policy.
- Use operational expressions with `Operate(...)` and `ExpOperation.Read(...)` / `ExpOperation.Write(...)` only when the user asks for expression read/write operations.
- When using Aerospike expression APIs, prefer importing the native namespace with `using Aerospike.Client;` or via the LINQPad query header namespace import.
- Use `Exp.StringBin(...)`, `Exp.IntBin(...)`, `Exp.RegexCompare(...)`, and related expression builders directly after `Aerospike.Client` is imported.
- Use `RegexFlag.NONE` for regex flags. Do not generate `Exp.RegexFlag.NONE`.
- Only use `using Exp = Aerospike.Client.Exp;` if a type-name conflict requires it.
- In LINQPad-driver expression examples, pass the raw `Exp` expression to `SetRecords.Query(...)`; do not call `Exp.Build(...)`.
- In native Aerospike client examples, assign `Exp.Build(...)` to `ScanPolicy.filterExp` or `QueryPolicy.filterExp`.

#### Driver API versus native client API

There are two different expression patterns.

Driver API pattern:

```csharp
Exp filterExpression =
    Exp.RegexCompare("^J.*", RegexFlag.NONE, Exp.StringBin("FirstName"));

var results =
    from customer in test.Customer.Query(filterExpression)
    select customer;
```

Native Aerospike C# client API pattern:

```csharp
var scanPolicy = new ScanPolicy
{
    filterExp = Exp.Build(
        Exp.RegexCompare(
            "^J.*",
            RegexFlag.NONE,
            Exp.StringBin("FirstName")))
};

client.ScanAll(scanPolicy, "test", "Customer", callback);
```

Do not mix these patterns.

If the user asks for native Aerospike C# client API code, use the native pattern only.
If the user asks for LINQPad-driver code, use the driver pattern only.
Use `RegexFlag.NONE`, not `Exp.RegexFlag.NONE`.

### Important Primary Key Rule

- When the Aerospike primary key value is required, prefer the generated/default primary-key property when available.
- Example: use `record.{{DefaultASPIKeyName}}` when the `{{DefaultASPIKeyName}}` property exists.
- If no generated/default primary-key property is available, use `record.GetPK()`.
- Do not access the primary key through `record["{{DefaultASPIKeyName}}"]` or another string-indexer expression unless the context explicitly says the primary key is stored as a normal bin.

### Important LINQ Rule for SetRecords

- Generated Aerospike set objects are `SetRecords` / `SetRecords<T>` instances.
- When using LINQ extension methods that require `IEnumerable<T>` semantics, call `AsEnumerable()` on the set first.
- This applies to LINQ operations such as `Join`, `GroupJoin`, `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`, `GroupBy`, `SelectMany`, `Concat`, `Union`, `Distinct`, `Except`, `Intersect`, `ToDictionary`, and similar collection-style LINQ methods.
- The API has native `First`, `FirstOrDefault`, `Skip`, `Where`, `ToList`, and `ToArray` functions for set instances (`SetRecords`, `SetRecords<T>`) and those should be used directly, if possible, without using the `AsEnumerable()` pattern.
- With query syntax, use `from record in NamespaceName.SetName.AsEnumerable()`.
- With method syntax, use `NamespaceName.SetName.AsEnumerable()` as the LINQ source.
- Do not generate `NamespaceName.SetName.Join(...)`, `NamespaceName.SetName.OrderBy(...)`, or `NamespaceName.SetName.GroupBy(...)` directly.
- Instead generate query syntax such as `from record in NamespaceName.SetName.AsEnumerable()` when QuerySyntax is configured.
- Use method syntax such as `NamespaceName.SetName.AsEnumerable().Join(...)` only when MethodSyntax is configured or query syntax cannot express the operation cleanly.
