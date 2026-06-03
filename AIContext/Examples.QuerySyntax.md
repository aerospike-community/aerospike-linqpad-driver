### Query a generated set

```csharp
// Replace NamespaceName and SetName with generated names from the context.
var records =
	(from r in NamespaceName.SetName.AsEnumerable()
	 select r)
	.Take(100);

records.Dump();
```

### Query with a bin/property filter

```csharp
// Prefer generated properties when available.
// Example: use r.status instead of r["status"] when the status property exists.
var activeRecords =
	(from r in NamespaceName.SetName.AsEnumerable()
	 where r.status == "active"
	 select r)
	.Take(100);

activeRecords.Dump();

// Use string-indexer access only when no generated property exists or dynamic access is required.
var dynamicRecords =
	(from r in NamespaceName.SetName.AsEnumerable()
	 where r["some-dynamic-bin"] == "active"
	 select r)
	.Take(100);

dynamicRecords.Dump();
```

### Filter with AValue TryApply

```csharp
// Prefer TryApply when an AValue-backed property may be null, missing, or mixed-type.
// This safely converts FirstName to string and invokes StartsWith only when possible.
var customers =
	(from customer in test.Customer.AsEnumerable()
	 where customer.FirstName.TryApply<string, bool>(name => name.StartsWith("a"))
	 select customer)
	.Take(100);

customers.Dump();
```

### Project with AValue Convert and CanConvert

```csharp
// Use CanConvert<T>() before Convert<T>() when conversion may not be valid.
var customers =
	(from customer in test.Customer.AsEnumerable()
	 where customer.TotalPurchases.CanConvert<decimal>()
	 select new
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
// Use Apply when the AValue is expected to exist, but conversion or execution may fail.
var customers =
	(from customer in test.Customer.AsEnumerable()
	 select new
	 {
		 customer.{{DefaultASPIKeyName}},
		 customer.FirstName,
		 FirstNameLength = customer.FirstName.Apply<string, int>(name => name.Length),
		 StartsWithA = customer.FirstName.Apply<string, bool>(name => name.StartsWith("a"))
	 })
	.Take(100);

customers.Dump();
```

### Filter sparse or mixed-type bins with Auto-Values

```csharp
// Auto-Values handle missing bins and mixed types more safely than raw casts.
var californiaCustomers =
	(from customer in test.Customer.AsEnumerable()
	 where !customer.Company.IsEmpty && customer.State == "CA"
	 select customer)
	.Take(100);

californiaCustomers.Dump();
```

### Query collection, map, JSON, or CDT values

```csharp
// Use TryGetValue, Contains, AsEnumerable, ToList, ToDictionary, and ElementAtOrDefault for CDT exploration.
var customersWithEmail =
	(from customer in test.Customer.AsEnumerable()
	 let email = customer.Profile.TryGetValue("email", "<missing>")
	 where email != "<missing>"
	 select new
	 {
		 customer.{{DefaultASPIKeyName}},
		 customer.FirstName,
		 Email = email
	 })
	.Take(100);

customersWithEmail.Dump();
```

### Query by APrimaryKey or digest

```csharp
// APrimaryKey can compare against user key values or digest hex strings when appropriate.
var recordsByKeyOrDigest =
	(from record in test.DataTypes.AsEnumerable()
	 where record.{{DefaultASPIKeyName}} == "NoPKValueSaved"
		|| record.{{DefaultASPIKeyName}} == "0xc363ecde6a39ae0611c69ee2c7bd8a3b6930337b"
	 select record)
	.Take(100);

recordsByKeyOrDigest.Dump();
```

### Server-side Aerospike expression filter

```csharp
// Aerospike expressions run server-side. Use raw bin names inside Exp.*Bin(...).
Client.Exp filterExpression = Exp.And(
	Exp.EQ(Exp.StringBin("State"), Exp.Val("CA")),
	Exp.BinExists("Company"));

var customers =
	(from customer in test.Customer.Query(filterExpression)
	 select customer)
	.Take(100);

customers.Dump();
```

### Build a server-side expression with AValue helpers

```csharp
// Use ToExpBin() for the bin reference and ToExpVal() for the literal value.
var status = "active".ToAValue("Status", "Status");

Client.Exp filterExpression = Exp.EQ(
	status.ToExpBin(Exp.Type.STRING),
	status.ToExpVal());

test.Customer.Query(filterExpression).Take(100).Dump();
```

### Sort records from an Aerospike set

```csharp
// Use query syntax when practical, and call AsEnumerable() on the set first.
// Prefer generated properties over string-indexer bin access.
var ordered =
	(from r in NamespaceName.SetName.AsEnumerable()
	 orderby r.status, r.{{DefaultASPIKeyName}}
	 select new
	 {
		 r.{{DefaultASPIKeyName}},
		 r.status
	 })
	.Take(100);

ordered.Dump();
```

### Join two Aerospike sets

```csharp
// Replace NamespaceName, Users, Orders, userid, and amount with actual generated names.
// Prefer generated properties when available.
var joined =
	(from user in NamespaceName.Users.AsEnumerable()
	 join order in NamespaceName.Orders.AsEnumerable()
		on user.userid equals order.userid
	 select new
	 {
		 UserId = user.userid,
		 UserPK = user.{{DefaultASPIKeyName}},
		 OrderPK = order.{{DefaultASPIKeyName}},
		 OrderAmount = order.amount
	 })
	.Take(100);

joined.Dump();
```

### Query syntax join rule

```csharp
// Preferred when LinqSyntaxPreference is QuerySyntax:
var joined =
	(from customer in test.Customer.AsEnumerable()
	 join invoice in test.Invoice.AsEnumerable()
		on customer.{{DefaultASPIKeyName}} equals invoice.CustomerId
	 select new
	 {
		 CustomerPK = customer.{{DefaultASPIKeyName}},
		 customer.FirstName,
		 customer.LastName,
		 customer.Email,
		 InvoicePK = invoice.{{DefaultASPIKeyName}},
		 invoice.InvoiceDate,
		 invoice.Total,
		 invoice.BillingCity,
		 invoice.BillingCtry
	 })
	.Take(100);

joined.Dump();

// Avoid this method-syntax form when an equivalent query-syntax join is available:
// test.Customer.AsEnumerable().Join(test.Invoice.AsEnumerable(), ...)
```

### Group records from an Aerospike set

```csharp
// Prefer query syntax where practical.
var grouped =
	from r in NamespaceName.SetName.AsEnumerable()
	group r by r.status into g
	orderby g.Count() descending
	select new
	{
		Status = g.Key,
		Count = g.Count()
	};

grouped.Dump();
```

### Access primary keys

```csharp
// Prefer the generated/default primary-key property when available.
var records =
	(from r in NamespaceName.SetName.AsEnumerable()
	 select new
	 {
		 PrimaryKey = r.{{DefaultASPIKeyName}},
		 r.status
	 })
	.Take(100);

records.Dump();

// Fallback if the generated {{DefaultASPIKeyName}} property is not available:
var recordsWithFallbackPK =
	(from r in NamespaceName.SetName.AsEnumerable()
	 select new
	 {
		 PrimaryKey = r.GetPK(),
		 r.status
	 })
	.Take(100);

recordsWithFallbackPK.Dump();
```

