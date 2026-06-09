### Native Aerospike C# Client API Mode

When the user asks to translate driver/LINQPad-driver code to the **native Aerospike C# client API**, native mode overrides all LINQPad-driver examples and rules.

Return a complete runnable LINQPad C# Statements script using only the native Aerospike C# client API.

Do **not** use any Aerospike LINQPad driver API in native-mode output.

Do not generate:

```csharp
test.Customer
test.Customer.Query(...)
test.Customer.AsEnumerable()
SetRecords
SetRecords<T>
AValue
APrimaryKey
PK
GetPK()
generated record properties such as customer.FirstName
```

Use only native Aerospike C# client objects and APIs, such as:

```csharp
AerospikeClient
ClientPolicy
ScanPolicy
QueryPolicy
Statement
Filter
RecordSet
Key
Record
Bin
Value
Exp
RegexFlag
CDTExp
CTX
ListExp
MapExp
```

Use raw Aerospike namespace, set, and bin names:

```csharp
var namespaceName = "test";
var setName = "Customer";
record.GetValue("FirstName")
Exp.StringBin("FirstName")
```

For native server-side filter expressions:

- Build the expression into the native policy with `Exp.Build(...)`.
- Assign the built expression to `ScanPolicy.filterExp` or `QueryPolicy.filterExp`.
- Use `client.ScanAll(...)` or `client.Query(...)`.
- Do not call `test.Customer.Query(...)`.
- Do not pass a raw `Exp` directly to a LINQPad-driver `SetRecords.Query(...)`.
- Use raw bin names inside `Exp.StringBin(...)`, `Exp.IntBin(...)`, `Exp.FloatBin(...)`, `Exp.BoolBin(...)`, `Exp.ListBin(...)`, `Exp.MapBin(...)`, `CDTExp`, `ListExp`, `MapExp`, and related native expression builders.

Use this enum form:

```csharp
RegexFlag.NONE
```

Do **not** generate:

```csharp
Exp.RegexFlag.NONE
```

When using the native Aerospike client in LINQPad C# Statements, prefer a normal namespace import:

```csharp
using Aerospike.Client;
```

Then write:

```csharp
Exp.RegexCompare("^J.*", RegexFlag.NONE, Exp.StringBin("FirstName"))
```

Do not add this alias unless absolutely necessary:

```csharp
using Exp = Aerospike.Client.Exp;
```

---

### Using the native Aerospike client to create a connection

```csharp
using Aerospike.Client;
using System.Collections.Concurrent;

var host = "172.18.174.125";
var port = 3000;

var clientPolicy = new ClientPolicy();

using var clientConnection = new AerospikeClient(clientPolicy, host, port);
```

---

### Native server-side nested CDT expression rule

Aerospike server-side expressions support nested CDT traversal. For nested list/map/document paths, prefer `CDTExp.SelectByPath(...)` with `CTX` selectors instead of inventing arbitrary `ListExp` / `MapExp` chains.

Use this pattern to extract values from nested list/map structures:

```csharp
var extractedValues =
    CDTExp.SelectByPath(
        Exp.Type.LIST,
        SelectFlag.VALUE,
        Exp.ListBin("TopLevelListBin"),
        CTX.AllChildren(),
        CTX.MapKey(Value.Get("NestedListField")),
        CTX.AllChildren(),
        CTX.MapKey(Value.Get("TargetField")));
```

Then test whether the extracted list contains a target value:

```csharp
var policy = new QueryPolicy
{
    filterExp = Exp.Build(
        ListExp.GetByValue(
            ListReturnType.EXISTS,
            Exp.Val(targetValue),
            extractedValues))
};
```

For multiple target values, use `Exp.Or(...)` with one `ListExp.GetByValue(ListReturnType.EXISTS, ...)` expression per target value:

```csharp
filterExp = Exp.Build(
    Exp.Or(
        ListExp.GetByValue(ListReturnType.EXISTS, Exp.Val(1447L), extractedValues),
        ListExp.GetByValue(ListReturnType.EXISTS, Exp.Val(179L), extractedValues),
        ListExp.GetByValue(ListReturnType.EXISTS, Exp.Val(3169L), extractedValues)))
```

Use `Exp.ListBin("BinName")` when the top-level bin is a list. Use `Exp.MapBin("BinName")` when the top-level bin is a map.

Do not generate invalid or speculative expression code such as:

```csharp
Exp.Val(ListReturnType.VALUE)
ListExp.ValRange(...)
ListExp.ValRange(Value.Get("TrackId"))
Exp.Bin("Invoices")
Exp.RegexFlag.NONE
```

---

### Use the native Aerospike client to transform this LINQ query using client-side filtering

Original LINQPad-driver query:

```csharp
from customer in test.Customer.AsEnumerable()
where customer.FirstName.TryApply<string, bool>(name => name.StartsWith("J"))
select customer
```

Native Aerospike C# client equivalent:

```csharp
using Aerospike.Client;
using System.Collections.Concurrent;

var host = "172.18.174.125";
var port = 3000;

var namespaceName = "test";
var setName = "Customer";

var clientPolicy = new ClientPolicy();

using var client = new AerospikeClient(clientPolicy, host, port);

var results = new ConcurrentBag<object>();

var scanPolicy = new ScanPolicy();

client.ScanAll(
    scanPolicy,
    namespaceName,
    setName,
    (key, record) =>
    {
        if (record == null)
            return;

        var firstNameValue = record.GetValue("FirstName");

        if (firstNameValue is not string firstName)
            return;

        if (!firstName.StartsWith("J"))
            return;

        results.Add(new
        {
            Digest = ToHex(key.digest),
            PrimaryKey = key.userKey?.Object,
            FirstName = firstName,
            LastName = record.GetValue("LastName") as string,
            Email = record.GetValue("Email") as string,
            Phone = record.GetValue("Phone") as string,
            Company = record.GetValue("Company") as string,
            City = record.GetValue("City") as string,
            State = record.GetValue("State") as string,
            Country = record.GetValue("Country") as string
        });
    },
    "FirstName",
    "LastName",
    "Email",
    "Phone",
    "Company",
    "City",
    "State",
    "Country");

results
    .Take(100)
    .Dump("Native API client-side filter: FirstName starts with J");

static string ToHex(byte[] bytes)
{
    if (bytes == null || bytes.Length == 0)
        return string.Empty;

    return "0x" + Convert.ToHexString(bytes).ToLowerInvariant();
}
```

---

### Use the native Aerospike client to transform this LINQ query using server-side filtering expressions

Original LINQPad-driver query:

```csharp
from customer in test.Customer.AsEnumerable()
where customer.FirstName.TryApply<string, bool>(name => name.StartsWith("J"))
select customer
```

Native Aerospike C# client equivalent:

```csharp
using Aerospike.Client;
using System.Collections.Concurrent;

var host = "172.18.174.125";
var port = 3000;

var namespaceName = "test";
var setName = "Customer";

var clientPolicy = new ClientPolicy();

using var client = new AerospikeClient(clientPolicy, host, port);

var results = new ConcurrentBag<object>();

var scanPolicy = new ScanPolicy
{
    filterExp = Exp.Build(
        Exp.And(
            Exp.BinExists("FirstName"),
            Exp.RegexCompare(
                "^J.*",
                RegexFlag.NONE,
                Exp.StringBin("FirstName"))))
};

client.ScanAll(
    scanPolicy,
    namespaceName,
    setName,
    (key, record) =>
    {
        if (record == null)
            return;

        results.Add(new
        {
            Digest = ToHex(key.digest),
            PrimaryKey = key.userKey?.Object,
            FirstName = record.GetValue("FirstName") as string,
            LastName = record.GetValue("LastName") as string,
            Email = record.GetValue("Email") as string,
            Phone = record.GetValue("Phone") as string,
            Company = record.GetValue("Company") as string,
            City = record.GetValue("City") as string,
            State = record.GetValue("State") as string,
            Country = record.GetValue("Country") as string
        });
    },
    "FirstName",
    "LastName",
    "Email",
    "Phone",
    "Company",
    "City",
    "State",
    "Country");

results
    .Take(100)
    .Dump("Native API server-side expression filter: FirstName starts with J");

static string ToHex(byte[] bytes)
{
    if (bytes == null || bytes.Length == 0)
        return string.Empty;

    return "0x" + Convert.ToHexString(bytes).ToLowerInvariant();
}
```

---

### Native Aerospike client: nested document search with safe client-side traversal

Use this pattern when the user asks for native Aerospike client code and either does not require server-side filtering or when a client-side traversal example is requested.

Original LINQPad-driver intent:

```text
Find customer records in test.CustInvsDoc where any invoice line has TrackId 1447, 179, or 3169.
The path is Invoices -> Lines -> TrackId.
```

User request shape:

```text
I want to obtain all customer records from CustInvDoc set where TrackId 1447, 179, or 3169.
"TrackId" is within the "Lines" map which is within "Invoices" map.

I want to translate this to use Aerospike native API.
```

Native Aerospike C# client equivalent with client-side traversal:

```csharp
using Aerospike.Client;
using System.Collections.Generic;
using System.Collections.Concurrent;

var host = "172.18.174.125";
var port = 3000;

var namespaceName = "test";
var setName = "CustInvsDoc";

var targetTrackIds = new HashSet<long> { 1447L, 179L, 3169L };

var clientPolicy = new ClientPolicy();

using var client = new AerospikeClient(clientPolicy, host, port);

var results = new ConcurrentBag<object>();

var scanPolicy = new ScanPolicy();

client.ScanAll(
    scanPolicy,
    namespaceName,
    setName,
    (key, record) =>
    {
        if (record == null)
            return;

        var invoices = record.GetValue("Invoices");

        if (!ContainsAnyTrackId(invoices, targetTrackIds))
            return;

        results.Add(new
        {
            Digest = ToHex(key.digest),
            PrimaryKey = key.userKey?.Object,
            FirstName = record.GetValue("FirstName") as string,
            LastName = record.GetValue("LastName") as string,
            Email = record.GetValue("Email") as string,
            Address = record.GetValue("Address") as string,
            City = record.GetValue("City") as string,
            State = record.GetValue("State") as string,
            Country = record.GetValue("Country") as string,
            Invoices = invoices
        });
    },
    "FirstName",
    "LastName",
    "Email",
    "Address",
    "City",
    "State",
    "Country",
    "Invoices");

results
    .Take(100)
    .Dump("Customers with matching nested TrackId");

static bool ContainsAnyTrackId(object invoicesValue, ISet<long> targetTrackIds)
{
    if (invoicesValue is not IEnumerable<object> invoices)
        return false;

    foreach (var invoice in invoices)
    {
        if (invoice is not IDictionary<object, object> invoiceMap)
            continue;

        if (!invoiceMap.TryGetValue("Lines", out var linesValue))
            continue;

        if (linesValue is not IEnumerable<object> lines)
            continue;

        foreach (var line in lines)
        {
            if (line is not IDictionary<object, object> lineMap)
                continue;

            if (!lineMap.TryGetValue("TrackId", out var trackIdValue))
                continue;

            if (TryToLong(trackIdValue, out var trackId)
                && targetTrackIds.Contains(trackId))
            {
                return true;
            }
        }
    }

    return false;
}

static bool TryToLong(object value, out long result)
{
    switch (value)
    {
        case long longValue:
            result = longValue;
            return true;

        case int intValue:
            result = intValue;
            return true;

        case short shortValue:
            result = shortValue;
            return true;

        case byte byteValue:
            result = byteValue;
            return true;

        case string textValue when long.TryParse(textValue, out var parsed):
            result = parsed;
            return true;

        default:
            result = 0;
            return false;
    }
}

static string ToHex(byte[] bytes)
{
    if (bytes == null || bytes.Length == 0)
        return string.Empty;

    return "0x" + Convert.ToHexString(bytes).ToLowerInvariant();
}
```

---

### Native Aerospike client: server-side nested CDT expression for `CustInvsDoc` TrackId

Use this pattern when the user asks for native Aerospike client code with server-side expressions for the nested document/list/map path:

```text
Invoices[*].Lines[*].TrackId
```

User request shape:

```text
I want to obtain all customer records from CustInvDoc set where TrackId 1447, 179, or 3169.
"TrackId" is within the "Lines" map which is within "Invoices" map.

I want to translate this to use Aerospike native API with server-side expressions.
```

Native Aerospike C# client equivalent with server-side nested CDT expression:

```csharp
using Aerospike.Client;
using System.Collections.Generic;

var host = "172.18.174.125";
var port = 3000;

var namespaceName = "test";
var setName = "CustInvsDoc";

var clientPolicy = new ClientPolicy();

using var client = new AerospikeClient(clientPolicy, host, port);

// Extract all nested TrackId values from:
// Invoices[*].Lines[*].TrackId
var trackIdsExpression =
    CDTExp.SelectByPath(
        Exp.Type.LIST,
        SelectFlag.VALUE,
        Exp.ListBin("Invoices"),
        CTX.AllChildren(),
        CTX.MapKey(Value.Get("Lines")),
        CTX.AllChildren(),
        CTX.MapKey(Value.Get("TrackId")));

// Check whether any extracted TrackId equals one of the target values.
var queryPolicy = new QueryPolicy
{
    filterExp = Exp.Build(
        Exp.Or(
            ListExp.GetByValue(
                ListReturnType.EXISTS,
                Exp.Val(1447L),
                trackIdsExpression),
            ListExp.GetByValue(
                ListReturnType.EXISTS,
                Exp.Val(179L),
                trackIdsExpression),
            ListExp.GetByValue(
                ListReturnType.EXISTS,
                Exp.Val(3169L),
                trackIdsExpression)))
};

var statement = new Statement();
statement.SetNamespace(namespaceName);
statement.SetSetName(setName);
statement.SetBinNames(
    "Address",
    "City",
    "Country",
    "Email",
    "FirstName",
    "LastName",
    "Phone",
    "PostalCode",
    "State",
    "SupportRepId",
    "Invoices");

var rows = new List<object>();

using var recordSet = client.Query(queryPolicy, statement);

while (recordSet.Next())
{
    var key = recordSet.Key;
    var record = recordSet.Record;

    rows.Add(new
    {
        Digest = ToHex(key.digest),
        PrimaryKey = key.userKey?.Object,
        Address = record.GetValue("Address"),
        City = record.GetValue("City"),
        Country = record.GetValue("Country"),
        Email = record.GetValue("Email"),
        FirstName = record.GetValue("FirstName"),
        LastName = record.GetValue("LastName"),
        Phone = record.GetValue("Phone"),
        PostalCode = record.GetValue("PostalCode"),
        State = record.GetValue("State"),
        SupportRepId = record.GetValue("SupportRepId"),
        Invoices = record.GetValue("Invoices")
    });
}

rows
    .Take(100)
    .Dump("Customers with TrackId 1447, 179, or 3169");

static string ToHex(byte[] bytes)
{
    if (bytes == null || bytes.Length == 0)
        return string.Empty;

    return "0x" + Convert.ToHexString(bytes).ToLowerInvariant();
}
```

---

### Native API mode checklist

When generating native client code, verify that the output:

- Creates or uses an `AerospikeClient`.
- Uses `ClientPolicy`, `ScanPolicy`, `QueryPolicy`, `Statement`, `Filter`, or native client operations.
- Uses raw namespace, set, and bin names.
- Uses `record.GetValue("BinName")` to read bins.
- Uses `Exp.Build(...)` when assigning filter expressions to native policies.
- Uses `RegexFlag.NONE`, not `Exp.RegexFlag.NONE`.
- Uses `CDTExp.SelectByPath(...)` with `CTX` selectors for nested list/map/document expression traversal.
- Uses `ListExp.GetByValue(ListReturnType.EXISTS, ...)` to test whether extracted list values contain a target value.
- Does not use `test.Customer`, `SetRecords`, `AValue`, `PK`, `GetPK()`, generated properties, or any LINQPad-driver query APIs.


---

### Native helper: enumerate CDT/list values safely

Use this pattern when native Aerospike API code needs to traverse a bin value that may be a list, array, or other non-string enumerable. Because this helper uses `yield return`, do not use `return objectList;` inside it.

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



---

### Native API example: `CustInvsDoc` TrackId search with server-side expression and native enrichment only

Use this pattern when the user asks for native Aerospike C# client API code, server-side expression filtering, and enrichment from related sets. This example intentionally does **not** use LINQPad-driver sets such as `test.Track.AsEnumerable()`.

```csharp
// Request summary:
// - Query test.CustInvsDoc with native Aerospike API and a server-side nested CDT expression.
// - Match customers whose Invoices[*].Lines[*].TrackId contains 2955, 1447, 179, or 3169.
// - Return customer fields without Invoices, plus matching TrackIds with artist name and album title.
// - Use native client access only for CustInvsDoc, Track, Album, and Artist.

using Aerospike.Client;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

var host = "<aerospike-host>";
var port = 3000;
var namespaceName = "test";

var customerSetName = "CustInvsDoc";
var trackSetName = "Track";
var albumSetName = "Album";
var artistSetName = "Artist";

var targetTrackIds = new HashSet<long> { 2955L, 1447L, 179L, 3169L };

var clientPolicy = new ClientPolicy();
using var client = new AerospikeClient(clientPolicy, host, port);

// Build the server-side expression for Invoices[*].Lines[*].TrackId.
var trackIdsExpression =
    CDTExp.SelectByPath(
        Exp.Type.LIST,
        SelectFlag.VALUE,
        Exp.ListBin("Invoices"),
        CTX.AllChildren(),
        CTX.MapKey(Value.Get("Lines")),
        CTX.AllChildren(),
        CTX.MapKey(Value.Get("TrackId")));

// Keep only customers whose nested TrackId list contains at least one target value.
var customerPolicy = new QueryPolicy
{
    filterExp = Exp.Build(
        Exp.Or(
            ListExp.GetByValue(ListReturnType.EXISTS, Exp.Val(2955L), trackIdsExpression),
            ListExp.GetByValue(ListReturnType.EXISTS, Exp.Val(1447L), trackIdsExpression),
            ListExp.GetByValue(ListReturnType.EXISTS, Exp.Val(179L), trackIdsExpression),
            ListExp.GetByValue(ListReturnType.EXISTS, Exp.Val(3169L), trackIdsExpression)))
};

var customerStatement = new Statement();
customerStatement.SetNamespace(namespaceName);
customerStatement.SetSetName(customerSetName);
customerStatement.SetBinNames(
    "Address",
    "City",
    "Country",
    "Email",
    "Fax",
    "FirstName",
    "LastName",
    "Phone",
    "PostalCode",
    "State",
    "SupportRepId",
    "Invoices");

// Query filtered customers natively.
var matchedCustomers = new List<object>();

using (var recordSet = client.Query(customerPolicy, customerStatement))
{
    while (recordSet.Next())
    {
        var key = recordSet.Key;
        var record = recordSet.Record;
        var bins = record?.bins;

        if (bins == null)
            continue;

        var invoices = GetBin(bins, "Invoices");

        // Traverse nested CDT client-side only to shape the matching TrackIds for the output.
        var matchingTrackIds =
            (from invoice in AsObjectEnumerable(invoices)
             let lines = TryGetMapValue(invoice, "Lines")
             from line in AsObjectEnumerable(lines)
             let trackId = ToInt64(TryGetMapValue(line, "TrackId"))
             where trackId.HasValue && targetTrackIds.Contains(trackId.Value)
             select trackId.Value)
            .Distinct()
            .ToList();

        if (matchingTrackIds.Count == 0)
            continue;

        matchedCustomers.Add(new
        {
            PK = key?.userKey?.Object,
            Address = ToStringSafe(GetBin(bins, "Address")),
            City = ToStringSafe(GetBin(bins, "City")),
            Country = ToStringSafe(GetBin(bins, "Country")),
            Email = ToStringSafe(GetBin(bins, "Email")),
            Fax = ToStringSafe(GetBin(bins, "Fax")),
            FirstName = ToStringSafe(GetBin(bins, "FirstName")),
            LastName = ToStringSafe(GetBin(bins, "LastName")),
            Phone = ToStringSafe(GetBin(bins, "Phone")),
            PostalCode = ToStringSafe(GetBin(bins, "PostalCode")),
            State = ToStringSafe(GetBin(bins, "State")),
            SupportRepId = ToInt64(GetBin(bins, "SupportRepId")),
            MatchingTrackIds = matchingTrackIds
        });
    }
}

// Enrich via native scans of related sets. Do not use generated LINQPad driver sets in native mode.
var tracksById = LoadTracksById(client, namespaceName, trackSetName, targetTrackIds);
var albumIds = tracksById.Values.Select(x => x.AlbumId).Where(x => x.HasValue).Select(x => x.Value).ToHashSet();
var albumsById = LoadAlbumsById(client, namespaceName, albumSetName, albumIds);
var artistIds = albumsById.Values.Select(x => x.ArtistId).Where(x => x.HasValue).Select(x => x.Value).ToHashSet();
var artistsById = LoadArtistsById(client, namespaceName, artistSetName, artistIds);

var results =
    matchedCustomers
        .Select(customer => new
        {
            customer,
            Matches =
                ((IEnumerable<long>)customer.MatchingTrackIds)
                .Select(trackId =>
                {
                    tracksById.TryGetValue(trackId, out var track);
                    var album = track?.AlbumId is long albumId && albumsById.TryGetValue(albumId, out var a) ? a : null;
                    var artist = album?.ArtistId is long artistId && artistsById.TryGetValue(artistId, out var ar) ? ar : null;

                    return new
                    {
                        TrackId = trackId,
                        TrackName = track?.TrackName,
                        AlbumTitle = album?.AlbumTitle,
                        ArtistName = artist?.ArtistName
                    };
                })
                .ToList()
        })
        .Select(x => new
        {
            x.customer.PK,
            x.customer.FirstName,
            x.customer.LastName,
            x.customer.Email,
            x.customer.Phone,
            x.customer.Address,
            x.customer.City,
            x.customer.State,
            x.customer.PostalCode,
            x.customer.Country,
            x.customer.Fax,
            x.customer.SupportRepId,
            Matches = x.Matches
        })
        .ToList();

results.Dump("CustInvsDoc customers matching requested TrackIds");

static object GetBin(IDictionary<string, object> bins, string name)
{
    return bins.TryGetValue(name, out var value) ? value : null;
}

static object TryGetMapValue(object source, string key)
{
    if (source is IDictionary<string, object> stringDict && stringDict.TryGetValue(key, out var stringValue))
        return stringValue;

    if (source is IDictionary<object, object> objectDict && objectDict.TryGetValue(key, out var objectValue))
        return objectValue;

    if (source is IDictionary dict && dict.Contains(key))
        return dict[key];

    return null;
}

static IEnumerable<object> AsObjectEnumerable(object value)
{
    if (value is IEnumerable<object> objectList)
    {
        foreach (var item in objectList)
            yield return item;

        yield break;
    }

    if (value is IEnumerable enumerable && value is not string)
    {
        foreach (var item in enumerable)
            yield return item;
    }
}

static long? ToInt64(object value)
{
    if (value == null)
        return null;

    if (value is long longValue)
        return longValue;

    if (value is int intValue)
        return intValue;

    if (value is short shortValue)
        return shortValue;

    if (value is byte byteValue)
        return byteValue;

    return long.TryParse(value.ToString(), out var parsed) ? parsed : null;
}

static string ToStringSafe(object value)
{
    return value?.ToString();
}

static Dictionary<long, TrackInfo> LoadTracksById(AerospikeClient client, string namespaceName, string setName, ISet<long> targetTrackIds)
{
    var results = new Dictionary<long, TrackInfo>();

    client.ScanAll(
        new ScanPolicy(),
        namespaceName,
        setName,
        (key, record) =>
        {
            if (record?.bins == null)
                return;

            var trackId = ToInt64(key?.userKey?.Object);
            var albumId = ToInt64(record.GetValue("AlbumId"));

            if (trackId.HasValue && targetTrackIds.Contains(trackId.Value))
            {
                results[trackId.Value] = new TrackInfo(
                    trackId.Value,
                    ToStringSafe(record.GetValue("Name")),
                    albumId);
            }
        },
        "Name",
        "AlbumId");

    return results;
}

static Dictionary<long, AlbumInfo> LoadAlbumsById(AerospikeClient client, string namespaceName, string setName, ISet<long> albumIds)
{
    var results = new Dictionary<long, AlbumInfo>();

    client.ScanAll(
        new ScanPolicy(),
        namespaceName,
        setName,
        (key, record) =>
        {
            if (record?.bins == null)
                return;

            var albumId = ToInt64(key?.userKey?.Object);
            var artistId = ToInt64(record.GetValue("ArtistId"));

            if (albumId.HasValue && albumIds.Contains(albumId.Value))
            {
                results[albumId.Value] = new AlbumInfo(
                    albumId.Value,
                    ToStringSafe(record.GetValue("Title")),
                    artistId);
            }
        },
        "Title",
        "ArtistId");

    return results;
}

static Dictionary<long, ArtistInfo> LoadArtistsById(AerospikeClient client, string namespaceName, string setName, ISet<long> artistIds)
{
    var results = new Dictionary<long, ArtistInfo>();

    client.ScanAll(
        new ScanPolicy(),
        namespaceName,
        setName,
        (key, record) =>
        {
            if (record?.bins == null)
                return;

            var artistId = ToInt64(key?.userKey?.Object);

            if (artistId.HasValue && artistIds.Contains(artistId.Value))
            {
                results[artistId.Value] = new ArtistInfo(
                    artistId.Value,
                    ToStringSafe(record.GetValue("Name")));
            }
        },
        "Name");

    return results;
}

record TrackInfo(long TrackId, string TrackName, long? AlbumId);
record AlbumInfo(long AlbumId, string AlbumTitle, long? ArtistId);
record ArtistInfo(long ArtistId, string ArtistName);
```
