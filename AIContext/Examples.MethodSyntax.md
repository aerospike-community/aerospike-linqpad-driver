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

### Query nested document/list/CDT values with method syntax

```csharp
// Use TryGetValue(..., AValue.Empty), AsEnumerable(), CanConvert<T>(), and Convert<T>()
// for nested CDT/map/list traversal. Avoid TryApply<IDictionary<...>> when TryGetValue can express the lookup.
var targetTrackIds = new HashSet<long> { 2955L, 1447L, 179L, 3169L };

var rows = test.CustInvsDoc
    .AsEnumerable()
    .Select(customer => new
    {
        Customer = customer,
        MatchingTrackIds = customer.Invoices
            .AsEnumerable()
            .SelectMany(invoice => invoice.TryGetValue("Lines", AValue.Empty).AsEnumerable())
            .Select(line => line.TryGetValue("TrackId", AValue.Empty))
            .Where(trackId => trackId.CanConvert<long>())
            .Select(trackId => trackId.Convert<long>())
            .Where(trackId => targetTrackIds.Contains(trackId))
            .Distinct()
            .ToList()
    })
    .Where(row => row.MatchingTrackIds.Any())
    .Select(row => new
    {
        CustomerPK = row.Customer.PK,
        row.Customer.FirstName,
        row.Customer.LastName,
        row.Customer.Email,
        row.Customer.Address,
        row.Customer.City,
        row.Customer.State,
        row.Customer.Country,
        MatchingTrackIds = row.MatchingTrackIds
    })
    .Take(100);

rows.Dump();
```

### Query `CustInvsDoc` TrackIds and enrich with Track, Album, and Artist by method syntax

```csharp
var targetTrackIds = new HashSet<long> { 2955L, 1447L, 179L, 3169L };

// Normalize generated PK and FK values to long before creating dictionaries or doing lookups.
var albumsById = test.Album
    .AsEnumerable()
    .Where(album => album.PK.CanConvert<long>() && album.ArtistId.CanConvert<long>())
    .Select(album => new
    {
        AlbumId = album.PK.Convert<long>(),
        album.Title,
        ArtistId = album.ArtistId.Convert<long>()
    })
    .ToDictionary(album => album.AlbumId);

var artistsById = test.Artist
    .AsEnumerable()
    .Where(artist => artist.PK.CanConvert<long>())
    .Select(artist => new
    {
        ArtistId = artist.PK.Convert<long>(),
        ArtistName = artist.Name
    })
    .ToDictionary(artist => artist.ArtistId);

var trackInfoById = test.Track
    .AsEnumerable()
    .Where(track => track.PK.CanConvert<long>() && track.AlbumId.CanConvert<long>())
    .Select(track => new
    {
        TrackId = track.PK.Convert<long>(),
        TrackName = track.Name,
        AlbumId = track.AlbumId.Convert<long>()
    })
    .Where(track => targetTrackIds.Contains(track.TrackId))
    .Select(track =>
    {
        var album = albumsById.TryGetValue(track.AlbumId, null);
        if (album == null)
            return null;

        var artist = artistsById.TryGetValue(album.ArtistId, null);

        return new
        {
            track.TrackId,
            track.TrackName,
            AlbumTitle = album.Title,
            ArtistName = artist?.ArtistName
        };
    })
    .Where(track => track != null)
    .ToDictionary(track => track.TrackId);

var results = test.CustInvsDoc
    .AsEnumerable()
    .Select(customer => new
    {
        Customer = customer,
        MatchingTrackIds = customer.Invoices
            .AsEnumerable()
            .SelectMany(invoice => invoice.TryGetValue("Lines", AValue.Empty).AsEnumerable())
            .Select(line => line.TryGetValue("TrackId", AValue.Empty))
            .Where(trackId => trackId.CanConvert<long>())
            .Select(trackId => trackId.Convert<long>())
            .Where(trackId => targetTrackIds.Contains(trackId))
            .Distinct()
            .ToList()
    })
    .Where(row => row.MatchingTrackIds.Any())
    .Select(row => new
    {
        Customer = new
        {
            CustomerPK = row.Customer.PK,
            row.Customer.FirstName,
            row.Customer.LastName,
            row.Customer.Email,
            row.Customer.Address,
            row.Customer.City,
            row.Customer.State,
            row.Customer.Country,
            row.Customer.PostalCode,
            row.Customer.Phone,
            row.Customer.Fax,
            row.Customer.SupportRepId
        },
        MatchingTracks = row.MatchingTrackIds
            .Select(trackId => trackInfoById.TryGetValue(trackId, null))
            .Where(enrichment => enrichment != null)
            .ToList()
    })
    .Take(100);

results.Dump();
```

### Query AValue-keyed map/CDT values with non-throwing TryGetValue helpers

```csharp
// Use this pattern when a CDT/map is represented as an AValue-keyed key/value sequence.
// The AValue-keyed TryGetValue helper performs exact matching on the AValue key
// and returns AValue.Empty when no matching key is found.
var rows = test.CustInvsDoc
    .AsEnumerable()
    .SelectMany(customer =>
        customer.Invoices
            .AsEnumerable()
            .SelectMany(invoice => invoice.TryGetValue("Lines", AValue.Empty).AsEnumerable())
            .Select(line => new
            {
                CustomerPK = customer.PK,
                TrackId = line.TryGetValue("TrackId", AValue.Empty)
            }))
    .Where(row => row.TrackId.CanConvert<long>())
    .Select(row => new
    {
        row.CustomerPK,
        TrackId = row.TrackId.Convert<long>()
    })
    .Take(100);

rows.Dump();
```


### Normalize nullable AValue/CDT values with ToAValue before traversal

```csharp
// Use ToAValue() when a generated property may be null or may already be AValue.
// If customer.Invoices is null, ToAValue() returns AValue.Empty.
// If customer.Invoices is already AValue, the original AValue is preserved.
var targetTrackIds = new HashSet<long> { 2955L, 1447L, 179L, 3169L };

var rows = test.CustInvsDoc
    .AsEnumerable()
    .Select(customer => new
    {
        Customer = customer,
        Invoices = customer.Invoices.ToAValue()
    })
    .Where(x => !x.Invoices.IsEmpty)
    .Select(x => new
    {
        x.Customer,
        MatchingTrackIds = x.Invoices
            .AsEnumerable()
            .SelectMany(invoice => invoice.TryGetValue("Lines", AValue.Empty).AsEnumerable())
            .Select(line => line.TryGetValue("TrackId", AValue.Empty))
            .Where(trackId => trackId.CanConvert<long>())
            .Select(trackId => trackId.Convert<long>())
            .Where(trackId => targetTrackIds.Contains(trackId))
            .Distinct()
            .ToList()
    })
    .Where(x => x.MatchingTrackIds.Any())
    .Select(x => new
    {
        CustomerPK = x.Customer.PK,
        x.Customer.FirstName,
        x.Customer.LastName,
        x.Customer.Email,
        x.Customer.Address,
        x.Customer.City,
        x.Customer.State,
        x.Customer.Country,
        MatchingTrackIds = x.MatchingTrackIds
    })
    .Take(100);

rows.Dump();
```
