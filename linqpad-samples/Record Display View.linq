<Query Kind="Program">
  <Connection>
    <ID>973104d1-5fc3-4e74-a869-59441d5e370d</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Driver Assembly="Aerospike.Database.LINQPadDriver" PublicKeyToken="no-strong-name">Aerospike.Database.LINQPadDriver.DynamicDriver</Driver>
    <Server>localhost</Server>
    <Persist>true</Persist>
    <DisplayName>Aerospike Cluster (Local)</DisplayName>
    <DriverData>
      <UseExternalIP>false</UseExternalIP>
      <Debug>false</Debug>
      <RecordView>Record</RecordView>
      <DocumentAPI>true</DocumentAPI>
    </DriverData>
  </Connection>
</Query>

/*
This demonstrates how "Record Display View" works.  
When there are no changes in detected schema for a set, only the expected bins are displayed in the grid. 
But if there are any changes in the schema (e.g., new bins added or removed) for a recod, a new grid column is added to the display (i.e., "Values") to indicated that this record schema changed.
The "Values" column will always indicate that the record schema is different from when the Aerospike set was scanned/refreshed by the driver. 

You can also programmaticilly detect these changes by reviwing the value of the "HasDifferentSchema" property of the record. 
The record has many different properties and methods to programmaticilly explore the record and bins. You can, for example, test to see if a bin exists, determine the bin's data type, obtain a bin's value, etc. 

Note that you can always force a refresh of the cluster by right clicking on the connection and selecting "Refresh". You can programmaticilly refresh a set by calling the Refresh method on the namespace instance. 
*/
void Main()
{
	//Refresh schema for set...
	Demo.RefreshSet("Artist");
	
	Demo.Artist.Get(188).Dump("Artist 188 Before Adding a new Bin to the Record");
	
	Demo.Artist.Put(188, "Mybin", "Hello");
	
	Demo.Artist.Get(188).Dump("Artist 188 After Adding a new Bin to the Record. Notice the new Column (Values) in the display grid");
	
	//Remove MyBin from this record by setting the value to null.
	Demo.Artist.Put(188, "Mybin", (string) null); 
	
	Demo.Artist.Get(188).Dump("Artist 188 After removing new bin.");
	
	
	Demo.Customer.Get(20).Dump("This Customer's Record doesn't actual have a \"Company\" nor \"Fax\" bins since their values are null");
	
	Demo.Customer.ChangeRecordView(ARecord.DumpTypes.Dynamic).Get(20).Dump("By changing the display view to Dynamic, we can see the actual bins in this record via the \"Values\" column in the display grid");
}
