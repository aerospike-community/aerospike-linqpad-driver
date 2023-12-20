<Query Kind="Program">
  <Connection>
    <ID>973104d1-5fc3-4e74-a869-59441d5e370d</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Driver Assembly="Aerospike.Database.LINQPadDriver" PublicKeyToken="no-strong-name">Aerospike.Database.LINQPadDriver.DynamicDriver</Driver>
    <Persist>false</Persist>    
    <DisplayName>Aerospike Cloud (Demo)</DisplayName>
    <DriverData>
      <DBType>Cloud</DBType>
      <Port>4000</Port>
      <TLSOnlyLogin>true</TLSOnlyLogin>
      <SetNamesCloud>PlaylistTrack Track InvoiceLine Album Invoice Artist Playlist CustInvsDoc Customer Genre MediaType Employee DataTypes</SetNamesCloud>
    </DriverData>
  </Connection>
</Query>

/* 
This will demo some of the capabilities of working with Bins from within the driver plus show how to programmatically access sets and bins. Don’t forget bins can be accessed directly from the set as a property of that set instance.  

Once a bin’s value is accessed the driver simplifies how you work with the value. Most of the time casting from one type to another is not required. You just work with the value and the driver will perform the casting and, if required, transformation of the values between the DB types and .Net. These values from the DB are called AValues (auto-values). 

The Aerospike DB only supports the following data types:  
    String,  
    Integer (Long),  
    Double,  
    Boolean,  
    Map (Dictionary),  
    List,  
    Bytes/Blob,  
    Geospatial,  
    HyperLogLog  
      
The LinqPad driver supports all DB Types plus can transform/convert types into any of the native C# data types.   
This includes C# DateTime, DateTimeOffset, and TimeSpan.   
Since these types do not exist in the DB they are converted into a string or numeric formats depending on the driver’s connection properties. These properties can be changed by right clicking the Aerospike driver connection and selecting properties. These settings can also be changed programmatically.    

Again, the driver will perform these casting/conversion operations automatically based on the DB type and the .Net type.
      
Note: this is not meant to be used in a production environment and there can be performance implications using this LinqPad driver!   

*/
void Main()
{

	var demoSet = "DataTypes"; //The name of the set in the aerospike_cloud nameospace.

	var dateTimeOffset = DateTimeOffset.Parse("5/9/2023 2:42:40 PM -07:00");

	//Add some rows to our demo set where the PK is of different data types and so are the bins... 
	//Note because we are using the Aerospike Bin class, we must convert the unsupported C# values (e.g., DateTime) to the correct DB type (e.g., string).  
	//Norminally the LinqPad driver's Put methods will have done all the conversions for us but I wanted to show the use of the underlying API... 
	//If the set doesn' exist, it will also be created...
	aerospike_cloud.Put(demoSet, 123, new Bin[] { new Bin("BinA", "BinA123"), new Bin("BinB", 123), new Bin("BinC", dateTimeOffset.DateTime.ToString()) });
	aerospike_cloud.Put(demoSet, "MyPK", new Bin[] { new Bin("BinA", 456), new Bin("BinB", 456), new Bin("BinC", dateTimeOffset.ToString()) });
	aerospike_cloud.Put(demoSet, 7.89, new Bin[] { new Bin("BinA", 7.89), new Bin("BinB", 789), new Bin("BinC", dateTimeOffset.ToUnixTimeMilliseconds() * 1000000) });
	aerospike_cloud.Put(demoSet, 10.01, new Bin[] { new Bin("BinA", "10.01"), new Bin("BinB", 1001), new Bin("BinC", dateTimeOffset.TimeOfDay.ToString()) });
	aerospike_cloud.Put(demoSet, "10.01", new Bin[] { new Bin("BinA", 10.01), new Bin("BinB", "1001"), new Bin("BinC", 123) });

	//Add a record where the PK's actual value will NOT be saved in the DB (digiest will only be used)
	{
		var writePolicy = new WritePolicy() { sendKey = false };
		aerospike_cloud.Put(demoSet, 
					"NoPKValueSaved", 
					new Bin[] { new Bin("BinA", 10.02), new Bin("BinB", "1002"), new Bin("BinC", 456) },
					writePolicy: writePolicy);					
	}

	//Once the refresh has completed, the LinqPad Connection Pane will update and the bins under the demo set will not reflect these changes.
	//Note that the bin names will now be repeated with an "*" next to the bin's data type indicating that this bin has multiple data types.
	//LINQPad.Util.ReadLine("Note changes in Connection Pane on the left. Press <Enter> once the Connection Pane has completly refreshed!".Dump());

	aerospike_cloud.GetRecords(demoSet).Dump("Added new records");

	//We have a mixture of data types as the PK and in Bins "BinA" and "BinC". 
	//Let us try some different queries. Note that we don't need to cast any of the operations...
	//Since we are using the set dynamically, the bins will need to be accessed dynamically plus the grid display will also be in "dynamic" mode. 
	aerospike_cloud[demoSet].Where(x => x["PK"] == "MyPK").Dump("\"MyPK\" Record using Where");
	aerospike_cloud[demoSet].Get("MyPK").Dump("\"MyPK\" Record using Get");
	
	var R123BinC = aerospike_cloud[demoSet].Where(x => x["PK"] == 123)
					.Dump("123 Record") //Dump Record
					.First() //Get first (only record)  BTW, could wrote as "aerospike_cloud[demoSet].First(x => x["PK"] == 123)"
					.GetValue("BinC") //Get Bin "BinC"'s value
					.Dump("123 Record BinC's Value"); //Dump Value
					
	aerospike_cloud[demoSet].Where(x => x["PK"] == 10.01M).Dump("10.01 Records");
	aerospike_cloud[demoSet].Where(x => x["PK"] == "10.01").Dump("\"10.01\" (string) Records");

	//Get the record where we didn't save the PK value. 
	aerospike_cloud[demoSet].Where(x => x["PK"] == "NoPKValueSaved").Dump("\"NoPKValueSaved\" Records");
	aerospike_cloud[demoSet].Get("NoPKValueSaved").Dump("\"NoPKValueSaved\" Record using Get");

	//This example returns the records based on a date-time. When using linq, The string from the DB is converted into a C# DateTime and that is used for the comparision. 
	//Since the DB doesn't support date.time, the date/time string must be used and match exactly when using expressions.
	aerospike_cloud[demoSet].Where(x => x["BinC"] == dateTimeOffset.DateTime).Dump("DateTime Records using where");
	aerospike_cloud[demoSet].Query(Exp.EQ(Exp.StringBin("BinC"), Exp.Val((string)R123BinC))).Dump("DateTime Record using Expressions");

	aerospike_cloud[demoSet].Where(x => x["BinC"] == dateTimeOffset.DateTime.ToUniversalTime()).Dump("Universal DateTime Records");
	aerospike_cloud[demoSet].Where(x => x["BinC"] == dateTimeOffset).Dump("DateTimeOffset Records");
	aerospike_cloud[demoSet].Where(x => x["BinC"] == 1683668560000000000).Dump("DateTimeOffset Long Records");

	//When comparing values for gtrater or less than values of different data types (excludes numeric types), the hash code of eachobject is used.
	//Also the results will always be consistent for the same query and data.
	aerospike_cloud[demoSet].Where(x => x["PK"] < 11).Dump("PK < 11 Records Where");

	//This shows the use of a where clause with and without a filter. 
	//When using a filter with a where clause we can produce the same results as using expressions.
	aerospike_cloud[demoSet].Where(x => x["BinB"] < 800).Dump("\"BinB <800\" Records using Where");
	aerospike_cloud[demoSet].Where(x => x["BinB"].IsInt && x["BinB"] < 800).Dump("\"BinB <800\" Records using Where and an Int Filter (same result as using Expressions)");
	aerospike_cloud[demoSet].Query(Exp.LT(Exp.IntBin("BinB"), Exp.Val(800))).Dump("\"BinB <800\" Records using Expressions");
	
	aerospike_cloud[demoSet].Where(x => x["BinA"] == "10.01" || x["BinB"] == "1001").Dump("\"BinA == \"10.01\" || BinB == \"1001\"\" Records using Where");
	aerospike_cloud[demoSet].Query(Exp.Or(Exp.EQ(Exp.StringBin("BinA"), Exp.Val("10.01")), Exp.EQ(Exp.StringBin("BinB"), Exp.Val("1001")))).Dump("\"BinA == \"10.01\" || BinB == \"1001\"\" Records using Expressions");
}
