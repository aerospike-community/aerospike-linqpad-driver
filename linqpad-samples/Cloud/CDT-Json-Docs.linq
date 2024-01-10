<Query Kind="Program">
  <Connection>
    <ID>973104d1-5fc3-4e74-a869-59441d5e370d</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Driver Assembly="Aerospike.Database.LINQPadDriver" PublicKeyToken="no-strong-name">Aerospike.Database.LINQPadDriver.DynamicDriver</Driver>
    <Server>localhost</Server>
    <DisplayName>Aerospike Cluster (Demo)</DisplayName>
    <DriverData>
      <UseExternalIP>false</UseExternalIP>
      <Debug>false</Debug>
      <RecordView>Record</RecordView>
      <DocumentAPI>true</DocumentAPI>
    </DriverData>
  </Connection>
</Query>

#load ".\POCO"

/* 
This will show how to use CDT/Json/Documents with the driver. You can create the required set by running "Create CustInvsDoc set" script.
   
Note: this is not meant to be used in a production environment and there can be performance implications using this LinqPad driver!  
*/
void Main()
{
	//ORM -- Find all tracks for TrackId 2527 and return those customers who bought this track
	var fndTrackIdsInstances = from custInvoices in aerospike_cloud.CustInvsDoc.AsEnumerable()
							   let custInstance = custInvoices.Cast<Customer>()
							   where custInstance.Invoices
									   .Any(d => d.Lines.Any(l => l.TrackId == 2527))
							   select custInstance;
	fndTrackIdsInstances.Dump("Found Using ORM/POCO");

	//.Net CDTs -- Find all tracks for TrackId 2527 and return those customers who bought this track
	// BTW you can configure how documents from Aerospike are presented.
	//	The default is to treat documents as JObject but you can configure this (via the connection properties)
	//	to present them as .Net CDTs (i.e., List and Dictionary).
	var fndTrackIdsCDT = from custInvoices in aerospike_cloud.CustInvsDoc.AsEnumerable()
						 let custInvoiceLines = custInvoices.Invoices.ToCDT() //Not required if document mode was disabled
						 where custInvoiceLines
								   .SelectMany(il => ((IList<object>)il["Lines"]).Cast<IDictionary<string, object>>())
								 .Any(l => (long)l["TrackId"] == 2527)
						 select custInvoices;

	fndTrackIdsCDT.Dump("Found Using Linq CDT");

	//.Net CDTs using AValues -- Find all tracks for TrackId 2527 and return those customers who bought this track
	// Note: Using AValues reduces type checking and casting...
	var fndTrackIdsAValueCDT = from custInvoices in aerospike_cloud.CustInvsDoc.AsEnumerable()
							   where custInvoices
										.Invoices.AsEnumerable()//Get the list of invoices as an AValues
										.Any(il => il.TryGetValue("Lines", returnEmptyAValue: true) //Get invoice lines as an AValue. If the property "Lines" doens't exist, a Null AValue is returned
														.AsEnumerable() //Get List of invoice lines where each element ia an AValue. If there are no "Lines" this returns an empty collection.
														.Any(i => i.TryGetValue<int>("TrackId") == 2527)) //Find invoice line
							   select custInvoices;

	fndTrackIdsAValueCDT.Dump("Found Using Linq CDT using AValues");

	//JObject -- Find all tracks for TrackId 2527 and return those customers
	var fndTrackIdsJObj = from custInvoices in aerospike_cloud.CustInvsDoc.AsEnumerable()
						  let custInvoiceLines = custInvoices.ToJson()["Invoices"]
													  .Children()["Lines"].SelectMany(a => a)
						  where custInvoiceLines.Any(l => l.Value<int>("TrackId") == 2527)
						  select custInvoices;

	fndTrackIdsJObj.Dump("Found Using Linq JObject");
	
	//Json Pathing -- Find all tracks for TrackId 2527 and return those customers
	var fndTrackIdsJPath = from custInvoices in aerospike_cloud.CustInvsDoc.AsEnumerable()
						   where custInvoices.Invoices.ToJArray().SelectToken("$..Lines[?(@.TrackId == 2527)]") != null
						   select custInvoices;
	fndTrackIdsJPath.Dump("Found Using Json Path");

	{
		//Using Aerospike CDT/Expressions

		// 1) add (append) a new invoice to the CustnvsDoc set Invoice bin list
		// 2) Add 1 to that new invoice's Total,
		// 3) remove the newly created invoice from the list (last item in the list)
		
		var customer = fndTrackIdsJPath.First(); //Just use the first customer in fndTrackIdsJPath from above
		var invoiceId = DateTime.Now.Ticks;
		var invoiceLine = new InvoiceLine(invoiceId, 10, 1, 0.93m);
		var invoice = new Invoice(customer.Address,
									customer.City,
									customer.Country,
									customer.PostalCode,
									customer.State,
									DateTime.Now.Date,
									invoiceLine.UnitPrice,
									new List<InvoiceLine>() { invoiceLine });
									
		//Append the new invoice as an atomic operation using Aerospike Operate Expressions
		aerospike_cloud.CustInvsDoc.Operate(customer.Aerospike.PrimaryKey,
									ListOperation.Append(ListPolicy.Default,
															"Invoices", //Bin Name
															invoice.ToAerospikeValue() //Need to make sure to convert the invoice instance into an Aerospike Map Value (ORM)
								)) 
								.Dump($"Append Result Using Aerospike Operate Expression for Customer {customer.Aerospike.PrimaryKey}");
		
		aerospike_cloud.CustInvsDoc.Get(customer.Aerospike.PrimaryKey)
				.Dump("Check Append Result (should be last item in the list of invoices)");


		//Add 1 to the total of the newly created invoice (last item in the list) using Aerospike Operate Expressions which are atomic...		
		aerospike_cloud.CustInvsDoc.Operate(customer.Aerospike.PrimaryKey,
									MapOperation.Increment(MapPolicy.Default,
															"Invoices", //Bin Name
															Value.Get("Total"), //Property Name
															Value.Get(1), //Add 1
															CTX.ListIndex(-1) //Get last item in the list
								)).Dump($"Add 1 to the \"Total\" of the Last Invoice Using Aerospike Operate Expression for Customer {customer.Aerospike.PrimaryKey}");
		
		aerospike_cloud.CustInvsDoc.Get(customer.Aerospike.PrimaryKey)
				.Dump("Check Adding 1 to Total Result (should be last item in the list of invoices)");

		//Remove the newly created invoice from the list off invoices as an atomic operation using Aerospike Operate Expressions
		aerospike_cloud.CustInvsDoc.Operate(customer.Aerospike.PrimaryKey,
									ListOperation.RemoveByIndex("Invoices",  //Bin Name
																	-1, //Get Last Item
																	ListReturnType.VALUE //Return removed item
																)) 
									.Dump($"Remove Last Invoice Result Using Aerospike Operate Expression for Customer {customer.Aerospike.PrimaryKey}");
		
		aerospike_cloud.CustInvsDoc.Get(customer.Aerospike.PrimaryKey)
				.Dump("Check Removing of the Last Item Result");				
	}
	
	//You can convert Json to or from any record, set, or namespace by using the ToJson and FromJson methods.
	//	The driver is able to import json string with Json Type Tags. So importing from DB's like MonagoDB is easy.
	//	Also, any Json object (i.e., JObect, JArray, JProperty, JValue) are converted to the proper Aerospike DB Type/CDT/Document.
	//Note there is a different from using the Import/Export feature compared to importing/exporting Json strings.
	
	//Load Json from a MongoDB collection and put it into an Aerospike Set called jsonTest
	//aerospike_cloud.Truncate("jsonTest"); //Not supported in the Cloud
	
	JSONValues.MonogoDB.Dump("MongoDB Json String", 0);
	
	aerospike_cloud.FromJson("jsonTest", //Set Name
					JSONValues.MonogoDB, //Json String
					pkPropertyName: "_id" //The Primary Key's associated Property Name in Json.
				).Dump("Number of Records Inserted");
				
	aerospike_cloud.GetRecords("jsonTest").Dump("Newly inserted records in the Json Set");
	
	//Return the Json from the newly updated set...
	aerospike_cloud.ToJson("jsonTest").ToString().Dump("Json String");
}

public static class JSONValues
{
	public const string MonogoDB = @"
	{
	""_id"": 10006546,	
    ""accountid"": 794875,
    ""transcnt"": 6,
    ""bucketstartdte"": {""$date"": {
          ""$numberLong"": ""1545886800000""}},
    ""bucketenddte"": {""$date"": 1473120000000},
    ""transactions"": [
      {
        ""date"": {""$date"": 1325030400000},
        ""amount"": 1197,
        ""transcode"": ""buy"",
        ""symbol"": ""nvda"",
        ""price"": ""12.7330024299341033611199236474931240081787109375"",
        ""total"": ""15241.40390863112172326054861""
      },
      {
         ""date"": {""$date"": 1465776000000},
         ""amount"": 8797,
         ""transcode"": ""buy"",
         ""symbol"": ""nvda"",
         ""price"": ""46.53873172406391489630550495348870754241943359375"",
         ""total"": ""409401.2229765902593427995271""
      },
      {
         ""date"": {""$date"": 1472601600000},
         ""amount"": 6146,
         ""transcode"": ""sell"",
         ""symbol"": ""ebay"",
         ""price"": ""32.11600884852845894101847079582512378692626953125"",
         ""total"": ""197384.9903830559086514995215""
      },
      {
         ""date"": {""$date"": 1101081600000},
         ""amount"": 253,
         ""transcode"": ""buy"",
         ""symbol"": ""amzn"",
         ""price"": ""37.77441226157566944721111212857067584991455078125"",
         ""total"": ""9556.926302178644370144411369""
      },
      {
         ""date"": {""$date"": 1022112000000},
         ""amount"": 4521,
         ""transcode"": ""buy"",
         ""symbol"": ""nvda"",
         ""price"": ""10.763069758141103449133879621513187885284423828125"",
         ""total"": ""48659.83837655592869353426977""
      },
      {
         ""date"": {""$date"": 936144000000},
         ""amount"": 955,
         ""transcode"": ""buy"",
         ""symbol"": ""csco"",
         ""price"": ""27.992136535152877030441231909207999706268310546875"",
         ""total"": ""26732.49039107099756407137647""
      }
    ]
  }
";
}
