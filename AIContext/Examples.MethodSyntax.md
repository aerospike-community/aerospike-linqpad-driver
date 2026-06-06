### Query a generated set

```csharp
// Replace NamespaceName and SetName with generated names from the context.
NamespaceName.SetName
	.AsEnumerable()
	.Take(100)
	.Dump();
```

### Query with a bin/property filter

```csharp
// Prefer generated properties when available.
// Example: use r.status instead of r["status"] when the status property exists.
NamespaceName.SetName
	.AsEnumerable()
	.Where(r => r.status == "active")
	.Take(100)
	.Dump();

// Use string-indexer access only when no generated property exists or dynamic access is required.
NamespaceName.SetName
	.AsEnumerable()
	.Where(r => r["some-dynamic-bin"] == "active")
	.Take(100)
	.Dump();
```

### Filter with AValue TryApply

```csharp
// Prefer TryApply when an AValue-backed property may be null, missing, or mixed-type.
var customers = test.Customer
	.AsEnumerable()
	.Where(customer => customer.FirstName.TryApply<string, bool>(name => name.StartsWith("a")))
	.Take(100);

customers.Dump();
```

### Project with AValue Convert and CanConvert

```csharp
var customers = test.Customer
	.AsEnumerable()
	.Where(customer => customer.TotalPurchases.CanConvert<decimal>())
	.Select(customer => new
	{
		customer.{{DefaultASPIKeyName}},
		customer.FirstName,
		customer.LastName,
		TotalPurchases = customer.TotalPurchases.Convert<decimal>()
	})
	.Take(100);

customers.Dump();
```

### Use AValue Apply for type-specific operations

```csharp
var customers = test.Customer
	.AsEnumerable()
	.Select(customer => new
	{
		customer.{{DefaultASPIKeyName}},
		customer.FirstName,
		FirstNameLength = customer.FirstName.Apply<string, int>(name => name.Length),
		StartsWithA = customer.FirstName.Apply<string, bool>(name => name.StartsWith("a"))
	})
	.Take(100);

customers.Dump();
```

### Use LINQ collection operations with SetRecords

```csharp
// For LINQ methods such as Join, OrderBy, GroupBy, SelectMany, etc.,
// call AsEnumerable() on the Aerospike set first.
// Prefer generated properties over string-indexer bin access.
var ordered = NamespaceName.SetName
	.AsEnumerable()
	.OrderBy(r => r.status)
	.ThenBy(r => r.{{DefaultASPIKeyName}})
	.Take(100);

ordered.Dump();
```

### Join two Aerospike sets

```csharp
// Replace NamespaceName, Users, Orders, userid, and amount with actual generated names.
// Prefer generated properties when available.
var joined = NamespaceName.Users
	.AsEnumerable()
	.Join(
		NamespaceName.Orders.AsEnumerable(),
		user => user.userid,
		order => order.userid,
		(user, order) => new
		{
			UserId = user.userid,
			UserPK = user.{{DefaultASPIKeyName}},
			OrderPK = order.{{DefaultASPIKeyName}},
			OrderAmount = order.amount
		})
	.Take(100);

joined.Dump();
```

### Group records from an Aerospike set

```csharp
// Prefer generated properties when available.
var grouped = NamespaceName.SetName
	.AsEnumerable()
	.GroupBy(r => r.status)
	.Select(g => new
	{
		Status = g.Key,
		Count = g.Count()
	})
	.OrderByDescending(x => x.Count);

grouped.Dump();
```

### Access primary keys

```csharp
// Prefer the generated/default primary-key property when available.
var records = NamespaceName.SetName
	.AsEnumerable()
	.Take(100)
	.Select(r => new
	{
		PrimaryKey = r.{{DefaultASPIKeyName}},
		r.status
	});

records.Dump();

// Fallback if the generated {{DefaultASPIKeyName}} property is not available:
var recordsWithFallbackPK = NamespaceName.SetName
	.AsEnumerable()
	.Take(100)
	.Select(r => new
	{
		PrimaryKey = r.GetPK(),
		r.status
	});

recordsWithFallbackPK.Dump();
```

### Use aerospike server-side expression filters

```csharp
using Exp = Aerospike.Client.Exp;

Exp filterExpression =
	Exp.RegexCompare("^J", 0, Exp.StringBin("FirstName"));

var customers =
	(from customer in test.Customer.Query(filterExpression)
	 select customer)
	.Take(100);

customers.Dump();
```
