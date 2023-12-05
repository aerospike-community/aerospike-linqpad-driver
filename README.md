# Aerospike Database for LINQPad 7+

[Description](#_Toc135216264)

[Aerospike Namespace, Set, Records, Bins, and Secondary Indexes](#_Toc135216265)

[User-Defined Functions (UDFs)](#_Toc135216266)

[Aerospike API](#_Toc135216267)

[Serialization/Object-Mapper](#_Toc135216268)

[Json Support](#_Toc135216269)

[Document API](#_Toc135216270)

[Importing/Exporting](#_Toc135216271)

[Examples](#_Toc135216272)

[Prerequisites](#prerequisites)

[Installation of LINQPad Driver](#_Toc135216274)

[LINQPad NuGet Manager](#_Toc135216275)

[Manual](#_Toc135216276)

[Installation of the Aerospike Database](#installation-of-the-aerospike-database)

[Other Resources](#other-resources)

# 

# Description

[Aerospike](https://aerospike.com/) for LINQPad 7+ is a data context dynamic driver for interactively querying and updating an Aerospike database using "[LINQPad](https://www.linqpad.net/)". LINQPad is a Graphical Development Tool designed for rapid prototyping, interactive testing, data modeling, data mining, drag-and-drop execution, interactive debugging, etc. The Aerospike driver for LINQPad is designed to support all LINQPad capabilities including the enhanced ability to learn and use the [Aerospike API directly](https://developer.aerospike.com/client/csharp).

Here is a subset of what you can perform using the driver:

-   Query any [Aerospike Set or Secondary Index](https://docs.aerospike.com/server/architecture/overview) using any LINQ command (including joins), interactively.
-   Use the driver’s extension methods to perform operations like Aerospike Expression, CRUD operations, etc. without understanding the underlying Aerospike API.
-   Serialize and deserialize any C\# object via the Object-Mapper (POCO). The driver supports all C\# data types, nested classes, and collections.
-   Full JSON support using [Json.NET](https://www.newtonsoft.com/json).
-   Be able to execute [UDF](https://docs.aerospike.com/server/guide/udf)s directly and display their underlying code. UDFs are treated like C\# methods with intellisense and code completion.
-   Export or Import Sets directly or by means of an [Aerospike Filter](https://docs.aerospike.com/server/operations/configure/cross-datacenter/filters).
-   Provides metadata about the cluster which includes active/inactive nodes, Aerospike server version, etc.
-   Use the Aerospike API directly to perform advance operations or instantly test snippets used in your application code.

The driver can, also, dynamically detect the structure of records within an Aerospike Set resulting in an easy-to-understand view much like a relational table with enhanced capabilities. Some of these capabilities are:

-   detection of bins with the same name but have different data types between records within a Set
-   records with different Bin structures within a Set
-   implicit data type conversion without the need to cast or check a Bin's data type for quick data operation
-   enhanced [Aerospike CDT](https://docs.aerospike.com/server/guide/data-types/cdt) handling
-   driver extension methods to programmatically interrogate Namespaces, Sets, records, Bins, Bin data types, etc.

The screenshot below show how Aerospike Sets and Bins are represented in LinqPad:

![Example](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/DupBinTypeInRecordDialog.png?raw=true)

The LINQPad connection pane will display the different [Aerospike components](https://docs.aerospike.com/server/architecture/data-model) in an hierarchical manner where namespace is under Aerospike cluster connection. Aerospike Sets are under namespaces and bins are under Sets. Below screenshot shows the relationship between these components:

![ComponentExample](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/ClusterNamespaceSetBinsDialog.png)

Each component can be dragged-and-dropped onto the LINQPad Query pane to be executed by LINQPad. The execution behavior will depend on the component. For example, a Set or Secondary Index will present the records within that component. For other components, the properties are displayed. In all cases, you can always execute the driver’s extension methods. These extension methods, greatly simplify Aerospike API commands like [Get, Put, Query, Operate, etc](https://developer.aerospike.com/client/csharp). plus, the ability to perform things like importing or exporting data. Of course, you can always use LINQ against Aerospike Sets or Secondary Indexes. Below is an example of some of the driver extensions:

![MethodsExample](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/NamespaceShowMethods.png)

# Aerospike Namespace, Set, Records, Bins, and Secondary Indexes

The LINQPad connection pane will display the different [Aerospike components](https://docs.aerospike.com/server/architecture/data-model) in a hierarchical manner where namespace is under Aerospike cluster connection. Aerospike Sets are under namespaces and bins are under Sets. Below screenshot shows the relationship between these components:

![ComponentExample](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/ClusterNamespaceSetBinsDialog.png?raw=true)

Each component can be dragged-and-dropped onto the LINQPad Query pane to be executed by LINQPad. The execution behavior will depend on the component. For example, a Set or Secondary Index will present the records within that component. For other components, the properties are displayed. In all cases, you can always execute the driver’s extension methods and properties. These extensions, greatly simplify Aerospike API commands like [Get, Put, Query, Operate, etc](https://developer.aerospike.com/client/csharp). plus, the ability to perform things like importing or exporting data. Below is an example of some of the driver extensions:

![MethodsExample](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/NamespaceShowMethods.png?raw=true)

Aerospike Sets and records are very easy to use. The detected bins in a record are integrated into LINQPad and are treated as C\# properties. As such, features like Intellisense and Autocompletion just work. You can also access bins within a record by using the bin name.

Since Aerospike is a schemaless database, a record can consist of varying number of bins, or a bin can have different data types between records. The driver can handle these conditions seamlessly. This is done using extension methods and implicit data conversion.

Implicit data conversion eliminates the need to test and cast a bin’s value so that it can be used directly in any operation. Below is an example that shows how implicit conversion works. The set, “graphDeviceNodeIdSet”, has a bin named “nodeID” that consists of two different data type values. Some records have a list value while others have a string value. This example uses the “where” clause which compares each record in the set looking for a numeric value of 367 or the value “a” in the list values.  
![MethodsExample](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/ImplictConversionExample.png?raw=true)

# User-Defined Functions (UDFs)

The driver supports the execution of UDFs by calling the Execute extension method. The Execute method will reflect the actual arguments used in the UDF. Below is an example:

![UDFExample](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/UDFExample.png?raw=true)

# Aerospike API

At any time, you can use the underlying Aerospike API directly or a combination of API or driver extension methods. Below is an example:

```
void Main() 
{ 
    //Using Aerospike API 
     
    var cPolicy = new ClientPolicy(); 
    using var client = new AerospikeClient(cPolicy, "localhost", 3000); 

    //Insert 3 records, with MapPolicy KEY_ORDERED 

    var key1 = new Key("test", "s1", 1); 
    var key2 = new Key("test", "s1", 2); 
    var key3 = new Key("test", "s1", 3); 
    var policy = new WritePolicy(); 
    policy.recordExistsAction = RecordExistsAction.UPDATE; 

    client.Put(policy, key1, new Bin("id", "groupID1")); 
    client.Put(policy, key2, new Bin("id", "groupID2")); 
    client.Put(policy, key3, new Bin("id", "groupID3")); 

    for (int i = 0; i < 25; i++) 
    { 
        client.Operate(null, key1, 
                        ListOperation.Insert("myList", 0, Value.get(value)), 
                        ListOperation.Trim("myList", 0, 20) ); 
    } 

}
```

# Serialization/Object-Mapper

The driver supports serialize and deserialize any C\# object to/from an Aerospike record. Native C\# types are stored as Aerospike data type. Unsupported types like DateTime, DateTimeOffset, Timespan, etc. are serialized as an ISO string or a numeric value based on behavior defined in the connection dialog. This behavior can also be changed programmability by the driver’s API or by providing a custom serializer.

C\# collections will be serialized into an Aerospike CDT. C\# public properties or fields will be serialized into an Aerospike bin where the bin name is the same name as the property/field. Fields can be ignored, and bin names can be different based on the use of C\# attributes (Aerospike.Client.BinIgnore, Aerospike.Client.BinName). Any nested class will be serialized as an Aerospike JSON document.

The driver can deserialize an Aerospike record into a C\# class. Just like serialization, bins are mapped to properties and fields using attributes as defined above. The driver will determine the best possible constructor to instantiate the class. This behavior can be changed by using the “Aerospike.Client.Constructor” attribute. Below is an example:

```
//This is from the POCO Aerospike LINQPad Example script
void Main()
{
	//If true, the read record is re-written with a different PK to demo how PICO are written back to the DB.
	var reWriteAsDiffPK = true;
	
	var customerInvoices = Demo.CustInvsDoc
							.AsEnumerable()
							.Select(cid => cid.Cast<Customer>(cid.PK)).ToArray();

	customerInvoicescustomerInvoices.OrderBy(i => i.LastName)
									.ThenBy(i => i.LastName)
									.ThenBy(i => i.Id)
									.Dump("Customer Invoices class instances created from the DB", 0);
	
	
	if(reWriteAsDiffPK)
	{
		var newRecs = new List<long>();
		
		foreach (var element in customerInvoices)
		{
			var newPK = element.Id * 1000; //Change the PK
			
			//Create DB Records from the Customer instances
			Demo.CustInvsDoc.WriteObject(newPK, element);
			newRecs.Add(newPK); //Removed new record later...
		}
		
		/Note that bin "Fax" is present in the DB (and list as a known bin in the Set's Bin list pane) but not as a property in the Customer Class
		//	Also bin "Company" is present in the DB and wasn't detected by the Set's Bin list pane and isn't a property either. 
		//		As such, records that have defined "Company" bin, will have an ExpandoObject value indicating that records has additional bins.
		Demo.CustInvsDoc.AsEnumerable()
						.OrderBy(cid => cid.LastName)
						.ThenBy(cid => cid.FirstName)
						.ThenBy(cid => cid.PK)
						.Dump("Customer Invoices Docs set From DB (rewritten with new PKs)", 1);

		LINQPad.Util.ReadLine("Press <Enter> to continue and remove newly written records!".Dump());

		foreach (var removePK in newRecs)
		{
			Demo.CustInvsDoc.Delete(removePK);
		}
	}
}

//Class definations for Customer and Invoice
public class Customer
{	
	/// <summary>
	/// The constructor used to create the object. 
	/// Note that the property "Invoices" will be set using the accessor. 
	/// </summary>
	[Aerospike.Client.Constructor]
	public Customer(long id,
					string address,
	                string city,
					string country,
					string email,
					string firstName,
					string lastName,
					string phone,
					string postalCode,
					string state, 
					int supportRepId)
	{
		this.Id = id;
		this.Address = address;
		this.City = city;
		this.Country = country;
		this.Email = email;
		this.FirstName = firstName;
		this.LastName = lastName;
		this.Phone = phone;
		this.PostalCode = postalCode;
		this.State = state;
		this.SupportRepId = supportRepId;
	}
	
	/// <summary>
	/// This property will contain the primary key value but will not be written in the set as a bin. 
	/// </summary>
	[Aerospike.Client.PrimaryKey]
	[Aerospike.Client.BinIgnore]
	public long Id { get; }
	public string Address{ get; }	
	public string City { get; }	
	public string Country { get; }	
	public string Email { get; }
	public string FirstName { get; }
	public string LastName	{ get; }
	public string Phone { get; }
	public string PostalCode { get; }
	public string State { get; }
	public int SupportRepId { get; }
	public List<Invoice> Invoices { get; set; }
}

public class Invoice
{	
	[Aerospike.Client.Constructor]
	public Invoice(string billingAddress,
					string billingCity,
					string billingCountry,
					string billingCode,
					string billingState,
					DateTime invoiceDate,
					decimal total,
					List<InvoiceLine> lines)
	{
		this.BillingAddress = billingAddress;
		this.BillingCity = billingCity;
		this.BillingCode = billingCode;
		this.BillingState = billingState;
		this.BillingCountry = billingCountry;
		this.InvoiceDate = invoiceDate;
		this.Total = total;
		this.Lines = lines;
	}
	
	/// <summary>
	/// Uses the bin name BillingAddr instead of the property name.
	/// </summary>
	[Aerospike.Client.BinName("BillingAddr")]
	public string BillingAddress { get;}
	public string BillingCity { get; }		
	[Aerospike.Client.BinName("BillingCtry")]
	public string BillingCountry { get; }
	public string BillingCode { get; }
	public string BillingState { get; }
	/// <summary>
	/// Notice that the driver will convert the DB value into the targed value automatically.
	/// The value is stored as a sting in the DB but converted to a date/time. Upon write it will be converted from back to a native DB type (e.g., string or long depending on configuration).
	/// </summary>
	public DateTime InvoiceDate { get; }
	/// <summary>
	/// This is stored as a double in the DB but is automatically converted to a decimal.
	/// </summary>
	public Decimal Total { get; }
	public IList<InvoiceLine> Lines { get; }
}

public class InvoiceLine
{
	[Aerospike.Client.Constructor]
	public InvoiceLine(long invoiceId,
						long quantity,
						long trackId,
						decimal unitPrice)
	{
		this.InvoiceId = invoiceId;
		this.Quantity = quantity;
		this.TrackId = trackId;
		this.UnitPrice = unitPrice;
	}

	public long InvoiceId { get; }
	public long Quantity { get; }
	public long TrackId { get; }
	public decimal UnitPrice { get; }
}
```

Below is the output from LINQPad:

![SerializationObjectMapper-Output](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/SerializationObjectMapper-Output.png?raw=true)

# Json Support

You can read and write Json to or from an Aerospike namespace, set, or record by means of the “ToJson” and “FromJson“ methods. The driver supports embedded JSON data types which are compatible with Json generated from multiple databases.

# Document API

The driver supports the use of the Aerospike Document API. This feature can be turned on or off from within the connection dialog. Below is an example where we obtain all artist recording track 2527 purchased by a customer.

```
    //ORM -- Find all tracks for TrackId 2527 and return those customers who bought this track
	var fndTrackIdsInstances = from custInvoices in Demo.CustInvsDoc.AsEnumerable()
							   let custInstance = custInvoices.Cast<Customer>()
							   where custInstance.Invoices
									   .Any(d => d.Lines.Any(l => l.TrackId == 2527))
							   select custInstance;
	fndTrackIdsInstances.Dump("Found Using ORM/POCO", 0);

	//.Net CDTs -- Find all tracks for TrackId 2527 and return those customers who bought this track
	// BTW you can configure how documents from Aerospike are presented.
	//	The default is to treat documents as JObject but you can configure this (via the connection properties)
	//	to present them as .Net CDTs (i.e., List and Dictionary).
	var fndTrackIdsCDT = from custInvoices in Demo.CustInvsDoc.AsEnumerable()
						 let custInvoiceLines = custInvoices.Invoices.ToCDT() //Not required if document mode was disabled
						 where custInvoiceLines
								   .SelectMany(il => ((IList<object>)il["Lines"]).Cast<IDictionary<string, object>>())
								 .Any(l => (long)l["TrackId"] == 2527)
						 select custInvoices;
	fndTrackIdsCDT.Dump("Found Using Linq CDT", 0);

	//JObject -- Find all tracks for TrackId 2527 and return those customers
	var fndTrackIdsJObj = from custInvoices in Demo.CustInvsDoc.AsEnumerable()
						  let custInvoiceLines = custInvoices.ToJson()["Invoices"]
													  .Children()["Lines"].Children()
						  where custInvoiceLines.Any(l => l["TrackId"].Value<int>() == 2527)
						  select custInvoices;

	fndTrackIdsJObj.Dump("Found Using Linq JObject", 0);

	//Json Pathing -- Find all tracks for TrackId 2527 and return those customers
	var fndTrackIdsJPath = from custInvoices in Demo.CustInvsDoc.AsEnumerable()
						   where custInvoices.Invoices.ToJArray().SelectToken("$..Lines[?(@.TrackId == 2527)]") != null
						   select custInvoices;
	fndTrackIdsJPath.Dump("Found Using Json Path", 0);
```

Below is the output from LINQPad:

![DocumentAPI-Output](https://github.com/aerospike-community/aerospike-linqpad-driver/blob/main/docs/DocumentAPI-Output.png?raw=true)

# Importing/Exporting

The driver can import a valid JSON file into an Aerospike set. The set can be an existing set or a new set which will be created. Each JSON property will be mapped to an Aerospike bin. Any JSON collection types will be transformed into the corresponding Aerospike CDT. Nested JSON objects will be treated as Aerospike JSON documents.

The driver can also export an Aerospike set into a JSON file. Below is an example of an export from the “players” Aerospike set.

```
test.players.Export(@"c:\users\randersen_aerospike\Desktop\player.json");
```

Below is an example of importing a JSON file:

```
test.players.Import(@"c:\users\randersen_aerospike\Desktop\player.json"); 
test.Import(@"c:\users\randersen_aerospike\Desktop\player.json", "players");
```
# Encryption and Authentication
Support for TLS encryption and authentication is fully supported by enabling these options in the connection dialog. This includes the LIINQPad Password Manager intergation. If the password manager is not used, all password are encryped using the Windows Data Protection API. 
# Examples

Sample scripts can be found in the [LINQPad Sample tree view tab](https://www.linqpad.net/nugetsamples.aspx) under “nuget” or in the “linqpad-samples” folder in GitHub.

The sample scripts are:

-   ReadMeFirst.linq – This script should be reviewed first. It will load the data into the “Demo” namespace which needs to exist. To create this namespace, please follow these [instructions](https://docs.aerospike.com/server/operations/manage/namespaces).
-   Basic Data Types.linq - Review some of the capabilities of working with Bins from within the driver plus show how to programmatically access sets and bins
-   Record Display View.linq - This demonstrates how "Record Display View" works
-   Linq Join Customer and Invoice.linq – Shows how to perform a client side join of two sets
-   LinqWhere-AerospikePK.linq – Shows how to use primary keys with Linq and Aerospike API
-   LinqWhere-AerospikeExpressions.linq – Shows how to use a Linq Where clause and the use of Aerospike Filter [Expressions](https://docs.aerospike.com/server/guide/expressions)
-   POCO.linq – Show the use of the ORM between complete class structures and the Aerospike DB
-   Put-Aerospike.linq – Shows the use of the how to insert or update a record within a set using a primary key.
-   CDT-Json-Docs.linq – Show the use of CDTs (Collection Data Types), Json, and documents by means of Linq and Aerospike [Expressions](https://docs.aerospike.com/server/guide/expressions)

# Prerequisites
-	[LINQPad 8](https://www.linqpad.net/LINQPad8.aspx): [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)/[.NET 7](https://dotnet.microsoft.com/download/dotnet/7.0)/[.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0)
-   [LINQPad 7](https://www.linqpad.net/LINQPad7.aspx): [.NET 7](https://dotnet.microsoft.com/download/dotnet/7.0)/[.NET 6](https://dotnet.microsoft.com/download/dotnet/6.0)/[.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0)/[.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)
-   [LINQPad 6](https://www.linqpad.net/LINQPad6.aspx): [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0)/[.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)

# Installation of LINQPad Driver

## LINQPad NuGet Manager

-   Open LINQPad
-   Click `Add Connection` Link.
-   Click button `View more drivers…`
-   Click radio button `Show all drivers` and type `Aerospike`.
-   Click Install

## Manual

Obtain the latest driver from the `Driver` folder and [download](https://github.com/aerospike-community/aerospike-linqpad-driver/tree/main/Driver) to your computer.

-   Open LINQPad
-   Click `Add Connection` Link.
-   Click button `View more drivers…`
-   Click button `Install driver from .LPX6 file…` and select downloaded lpx6 file.

# Installation of the Aerospike Database

There are multiple ways to install Aerospike DB.

-   [Docker, Cloud, and Linux](https://docs.aerospike.com/server/operations/install)
-   [AeroLab](https://github.com/aerospike/aerolab)

## Other Resources

-   https://www.linqpad.net/
-   <https://aerospike.com/>
-   <https://developer.aerospike.com/>
-   <https://developer.aerospike.com/blog>
-   <https://github.com/aerospike>
-   <https://github.com/aerospike-community>
