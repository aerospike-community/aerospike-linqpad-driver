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

### Important LINQ Syntax Preference

- Preferred LINQ style: `method syntax`.
- Prefer chained methods such as `.Where(...)`, `.OrderBy(...)`, `.Select(...)`, `.Join(...)`, and `.GroupBy(...)`.
- Use query syntax only when explicitly requested by the user or when it is materially clearer.


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

### Important Record Property Rule

- When accessing Aerospike bin values from generated record objects, prefer generated C# properties when available.
- Example: use `customer.userid` instead of `customer["userid"]` when the `userid` property exists.
- Use `record["binName"]` only when no generated property exists, the bin name is not a valid C# identifier, or dynamic access is specifically required.
- Prefer property access in projections, filters, joins, sorts, and groups.
- The set-level bin metadata below lists the raw Aerospike bin name and, when available, the generated C# property name.

### Important AValue / AutoValue Rule

- The Aerospike LINQPad driver may expose bin values through `AValue` / AutoValue behavior.
- Current connection setting `Always use AValue`: `{{AlwaysUseAValues}}`.
- When `Always use AValue` is true, generated record properties may represent Aerospike values using the driver's `AValue` abstraction instead of plain CLR primitive types.
- Prefer generated record properties first, but write comparisons and projections in a way that respects the property's generated type.
- Do not assume an `AValue`-backed property is a raw `string`, `int`, `long`, `double`, `bool`, `DateTime`, list, or dictionary unless the context metadata clearly says so.
- Avoid unsafe casts from `AValue`-backed values to CLR primitive types.
- Use the driver's `AValue`-friendly comparison, conversion, or value-access patterns when needed.

### Important AValue-backed Map Key Rule

- Some map, dictionary, JSON, and CDT structures may expose keys as `AValue` instances rather than plain CLR key types.
- When searching `IEnumerable<KeyValuePair<TKey,TValue>>` where `TKey : AValue`, use the AValue-aware key helpers `ContainsKey(...)` and `GetByKey(...)`.
- Use `ContainsKey(matchKey, matchOptions)` to test whether any AValue key matches.
- Use `GetByKey(key, matchOptions)` to retrieve the first matching value.
- Use `AValue.MatchOptions` when key matching needs exact, equality, substring, regex, or broader AValue matching behavior.
- Use `ContainsKey(...)` before `GetByKey(...)` when missing keys are expected because `GetByKey(...)` throws `KeyNotFoundException` if no key matches.
- Do not assume dictionary/map keys are always plain strings.
- Prefer these helpers over manually converting every key with `.Convert<string>()` unless the key type is known and conversion is required.

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
