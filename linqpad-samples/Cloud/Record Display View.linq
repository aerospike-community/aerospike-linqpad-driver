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
This demonstrates how "Record Display View" works.   

When there are no changes in the schema for a set, only the scanned bins are displayed in the grid. But if there are any changes in the schema (e.g., new bins added or removed) for a recod, a new grid column is added to the display (i.e., "Values") to indicate that this record’s schema has changed since the last scan of the set.
 
The "Values" column will always indicate that the record schema is different from the scanned Aerospike set.  

You can also programmatically detect these changes by reviwing the value of the "HasDifferentSchema" property of the record.  

The record object (ARecord) has many different properties and methods that can be used to programmatically explore the record and bin atributes. You can, for example, test to see if a bin exists, determine the bin's data type, obtain a bin's value, TTL, etc.  

Note that you can always force a refresh of the cluster by right clicking on the connection object and selecting "Refresh". You can programmaticilly refresh a set by calling the Refresh method on the namespace instance.

*/
void Main()
{
	
	aerospike_cloud.Artist.Get(188).Dump("Artist 188 Before Adding a new Bin to the Record");
	
	aerospike_cloud.Artist.Put(188, "Mybin", "Hello");
	
	aerospike_cloud.Artist.Get(188).Dump("Artist 188 After Adding a new Bin to the Record. Notice the new Column (Values) in the display grid");
	
	//Remove MyBin from this record by setting the value to null.
	aerospike_cloud.Artist.Put(188, "Mybin", (string) null); 
	
	aerospike_cloud.Artist.Get(188).Dump("Artist 188 After removing new bin.");
	
	
	aerospike_cloud.Customer.Get(20).Dump("This Customer's Record doesn't actual have a \"Company\" nor \"Fax\" bins since their values are null");
	
	aerospike_cloud.Customer.ChangeRecordView(ARecord.DumpTypes.Dynamic).Get(20).Dump("By changing the display view to Dynamic, we can see the actual bins returned from the DB in this record via the \"Values\" column in the display grid");
}
