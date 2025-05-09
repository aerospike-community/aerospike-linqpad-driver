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

/* 
This will demo some of the capabilities of working with set properties and auto-values (AValue) from within the driver.

Warning: You must run "Basic Data Types" script first, otherwise this script will not compile!!
	
Note: this is not meant to be used in a production environment and there can be performance implications using this LinqPad driver!   
		If this has compile errors, you need to run "Basic Data Types" tocreate the set.
*/
void Main()
{	
	test.DataTypes.Dump("DataTypes Set");
	
	//We are going to add new rows where "BinA" will now contain a list
	test.DataTypes.Put("List1", "BinA", new List<object>() { "BinA123", 456, "List1Bin"});
	test.DataTypes.Put("List2", "BinA", new List<object>() { "BinA123", 7.89, "List2Bin"});

	//Adding Dictionary (Map) and a List as new rows to "BinB"
	test.DataTypes.Put("Map1", "BinB", new Dictionary<string, object>() { { "Key2", "BinB123" }, { "Key3", 456 }, { "Key4", "Map1Bin" } });
	test.DataTypes.Put("Map2", "BinB", new Dictionary<string, object>() { { "key1", "BinA123" }, { "Key3", 7 }, { "Map4", "Map2Bin" }, { "Map5", "ABKey3CD" } });
	test.DataTypes.Put("Map3", "BinB", new Dictionary<string, object>() { { "key1", "BinA456" }, { "Map7", "Map2Bin" }, { "Map8", "ABKey3CD" } });
	test.DataTypes.Put("Map4", "BinB", new Dictionary<string, object>() { { "key12", "BinA456" }, { "Map9", "Map2Bin" }, { "Map10", "Key3" }, { "Map11", "Key3" } });
	test.DataTypes.Put("List3", "BinB", new List<object>() { "BinB123", 89, "List3Bin", "Key3" });
	test.DataTypes.Put("Key3", new Dictionary<string, object>() { { "BinA", "BinA123" }, { "BinB", "Key3" }, { "BinC", "BinCKey3" } });

	test.DataTypes.Dump("DataTypes Set with New Records");
	
	//Find all records where value "BinA123" is found within bin BinA
	test.DataTypes.Where(dt => dt.BinA.Contains("BinA123")) //We can use contains method to find the value regardless of the actual data types
			.Dump("All Records with value \"BinA123\"");
	
	test.DataTypes.Where(dt => dt.BinA.IsList && dt.BinA.Contains("BinA123"))
			.Dump("Records with value \"BinA123\" within a list");

	test.DataTypes.Where(dt => dt.BinA.IsString && dt.BinA.Contains("BinA123"))
			.Dump("Records with value \"BinA123\" is a string");

	test.DataTypes.Where(dt => dt.BinExists("BinB") && dt.BinA.Contains("BinA123"))
			.Dump("Records with value \"BinA123\" and BinB exists in the record");

	test.DataTypes.Where(dt => dt.BinA.Contains("BinA123"))
			.Dump("Records with value \"BinA123\" in All record");
			
	test.DataTypes.Where(dt => dt.BinA == "BinA123") //With Auto-Values we are not required to cast to match the data type from the DB
			.Dump("Records with value \"BinA123\" using ==");

	test.DataTypes.Where(dt => dt.BinB == 1001) //Auto-Values can also handle nulls
			.Dump("Records with value 1001 in BinB");
	
	var dateTimeOffset = DateTimeOffset.Parse("5/9/2023 2:42:40 PM -07:00");

	//This will only match the string value of a datetime
	test.DataTypes.Where(dt => dt.BinC == "5/9/2023 2:42:40 PM -07:00") //We are storing dB Datetimes as string, so we can use a string or actual DateTime object...
			.Dump("Records using DateTime string");

	//When using a DateTimeOffset object, it will match all records based on this object's actual DateTime...
	test.DataTypes.Where(dt => dt.BinC == dateTimeOffset) //We are storing dB Datetimes as string, so we can use a string or actual DateTime object...
			.Dump("Records using DateTimeOffset object");

	//When using a DateTime object, it will match all records based on this object's actual DateTime...
	test.DataTypes.Where(dt => dt.BinC == dateTimeOffset.DateTime) //We are storing dB Datetimes as string, so we can use a string or actual DateTime object...
			.Dump("Records using DateTime object");
			
	//Using Contains
	test.DataTypes.Where(dt => dt.BinB.Contains("Key3"))
			.Dump("Records where BinB has value \"Key3\" as a value or within a collection");
	test.DataTypes.GetBinBValues().FindAll("Key3")
			.Dump("Values in BinB with value \"Key3\" as a value or within a collection");
	test.DataTypes.Where(dt => dt.BinB.Contains("Key3", AValue.MatchOptions.Any))
			.Dump("Records where BinB has value \"Key3\" as a value or anywhere (can be an element, Key, or Value) within collection");
	test.DataTypes.GetBinBValues().FindAll("Key3", AValue.MatchOptions.Any)
			.Dump("Values in BinB with value \"Key3\" as a value or anywhere within a collection");
	test.DataTypes.Where(dt => dt.BinB.Contains("Key3", AValue.MatchOptions.Any | AValue.MatchOptions.SubString))
			.Dump("Records where BinB has value \"Key3\" as a substring within a value or anywhere (canbe an element, Key, or Value) within collection");
}
