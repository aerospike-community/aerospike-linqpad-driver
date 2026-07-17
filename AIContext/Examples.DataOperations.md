<!-- AIContext-Version: 2026.06.08.3; Change: runtime AI-context version source, LINQPad output display, and generated script provenance comments. -->

> **Example naming note:** Namespace, set, bin, and generated property names shown here, such as `test`, `Customer`, `Invoice`, `Track`, `Album`, `Artist`, `FirstName`, `CustomerId`, `AlbumId`, and `Name`, are examples. In generated code, substitute the actual names from the current AI context metadata or from the user's request.

### Data Mutation, Import, and Export Examples

Use these examples only when the user explicitly asks for insert, update, delete, import, export, copy, truncate, write, put, remove, or operate behavior.

Default generated code should remain safe, bounded, and read-only unless the user clearly requests a non-read-only operation.

---

### Export filtered records to CSV in LINQPad-driver mode

```csharp
var filePath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
    "customers-ca.csv");

var rows =
    test.Customer
        .AsEnumerable()
        .Where(customer => customer.State == "CA")
        .Take(1000)
        .Select(customer => new
        {
            CustomerPK = customer.PK,
            customer.FirstName,
            customer.LastName,
            customer.Email,
            customer.City,
            customer.State,
            customer.Country
        })
        .ToList();

// Fully qualify LINQPad.Util to avoid ambiguity with Aerospike.Client.Util.
LINQPad.Util.WriteCsv(rows, filePath);

filePath.Dump("CSV export path");
```

---

### Preview records before delete in LINQPad-driver mode

```csharp
var recordsToDelete =
    test.Customer
        .AsEnumerable()
        .Where(customer => customer.State == "ZZ")
        .Take(25)
        .ToList();

recordsToDelete.Dump("Preview records that match the delete criteria");

// Destructive operation intentionally left commented.
// Uncomment only after reviewing the preview and confirming the criteria.
// foreach (var customer in recordsToDelete)
// {
//     test.Customer.Delete(customer.PK);
// }
```

---

### Native API insert / upsert example

```csharp
using Aerospike.Client;

var namespaceName = "test";
var setName = "Customer";

using var client = new AerospikeClient(new ClientPolicy(), "localhost", 3000);

var key = new Key(namespaceName, setName, 123456L);

var writePolicy = new WritePolicy
{
    recordExistsAction = RecordExistsAction.UPDATE
};

client.Put(
    writePolicy,
    key,
    new Bin("FirstName", "Ada"),
    new Bin("LastName", "Lovelace"),
    new Bin("Email", "ada@example.com"));

"Inserted or updated native Customer record with PK 123456.".Dump();
```

---

### Native API delete with preview

```csharp
using Aerospike.Client;

var namespaceName = "test";
var setName = "Customer";
var targetPk = 123456L;

using var client = new AerospikeClient(new ClientPolicy(), "localhost", 3000);

var key = new Key(namespaceName, setName, targetPk);

var existing = client.Get(null, key);

existing?.bins.Dump("Preview record before delete");

// Destructive operation intentionally left commented.
// Uncomment only after reviewing the preview.
// var deleted = client.Delete(null, key);
// deleted.Dump("Deleted?");
```
