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
	  <ConnectionTimeout>5000</ConnectionTimeout>
      <TotalTimeout>5000</TotalTimeout>
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
	aerospike_cloud.DataTypes.Dump("DataTypes Set");
	
	//We are going to add a row where "BinA" will now contain a list (currently has double and string values)
	aerospike_cloud.DataTypes.Put("List1", "BinA", new List<object>() { "BinA123", 456, "List1Bin"});
	aerospike_cloud.DataTypes.Put("List2", "BinA", new List<object>() { "BinA123", 7.89, "List2Bin"});
	
	aerospike_cloud.DataTypes.Dump("DataTypes Set with New Records");
	
	//Find all records where value "BinA123" is found within bin BinA
	aerospike_cloud.DataTypes.Where(dt => dt.BinA.Contains("BinA123")) //We can use contains method to find the value regardless of the actual data types
			.Dump("All Records with value \"BinA123\"");
	
	aerospike_cloud.DataTypes.Where(dt => dt.BinA.IsList && dt.BinA.Contains("BinA123"))
			.Dump("Records with value \"BinA123\" within a list");

	aerospike_cloud.DataTypes.Where(dt => dt.BinA.IsString && dt.BinA.Contains("BinA123"))
			.Dump("Records with value \"BinA123\" is a string");

	aerospike_cloud.DataTypes.Where(dt => dt.BinExists("BinB") && dt.BinA.Contains("BinA123"))
			.Dump("Records with value \"BinA123\" and BinB exists in the record");

	aerospike_cloud.DataTypes.Where(dt => dt.BinA == "BinA123") //With Auto-Values we are not required to cast to match the data type from the DB
			.Dump("Records with value \"BinA123\" using ==");

	aerospike_cloud.DataTypes.Where(dt => dt.BinB == 1001) //Auto-Values can also handle nulls
			.Dump("Records with value 1001 in BinB");
	
	var dateTimeOffset = DateTimeOffset.Parse("5/9/2023 2:42:40 PM -07:00");

	//This will only match the string value of a datetime
	aerospike_cloud.DataTypes.Where(dt => dt.BinC == "5/9/2023 2:42:40 PM -07:00") //We are storing dB Datetimes as string, so we can use a string or actual DateTime object...
			.Dump("Records using DateTime string");

	//When using a DateTimeOffset object, it will match all records based on this object's actual DateTime...
	aerospike_cloud.DataTypes.Where(dt => dt.BinC == dateTimeOffset) //We are storing dB Datetimes as string, so we can use a string or actual DateTime object...
			.Dump("Records using DateTimeOffset object");

	//When using a DateTime object, it will match all records based on this object's actual DateTime...
	aerospike_cloud.DataTypes.Where(dt => dt.BinC == dateTimeOffset.DateTime) //We are storing dB Datetimes as string, so we can use a string or actual DateTime object...
			.Dump("Records using DateTime object");
}
