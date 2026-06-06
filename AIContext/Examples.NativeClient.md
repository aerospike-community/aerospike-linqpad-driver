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
