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
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

/*
This will demo some of the capabilities of working with the Aerospike's LINQPad AI context.

Note: It is recommended to read the ReadMeFirst file in the Native samples folder.
	Make sure the connection is pointing to a valid Aerospike cluster (check IP Address).
*/

async Task Main()
{
	//Load the Demo's Cluster Data if not Already Loaded
	if (!test.Exists("Customer"))
	{
		//Import Aerospike Set Records...
		test.Import(LINQPad.Util.GetFullPath("aerospike.json"))
				.Dump("Number of Records Imported");
	}

	var userRequest =
	"I would like a list of customers with invoices and associated artist they purchased";

	await AIContext.SubmitRequestAndCreateQueryAsync(userRequest);
}
