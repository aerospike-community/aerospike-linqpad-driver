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

//This example shows how to create a Multi-record Transaction (MRT) using the extension API.
//The target namespace must be a strong consistency namespace. For more information see https://aerospike.com/docs/server/guide/consistency.
//For more information on Multi-record Transaction (MRT) see https://aerospike.com/blog/multi-record-transactions-for-aerospike/
void Main()
{
	//Create a Multi-record Transaction (MRT) on set "SCCustomer" in the  strong consistency namespace "testsc".
	var testscCustomerMRT = testsc.CreateTransaction("SCCustomer");
	
	//All MRT operations are supported in the Aerospike LINQPad driver (e.g., Put, Get, Delete, Import, Batch, etc.).
	//This example is using the CopyRecords extension API.
	try
	{
		//Copy all records from set "Customer" in namespace "Demo" into the  strong consistency namespace "testsc" into "SCCustomer"
		//If an error occurs, rollback (abort) all copied records into ""SCCustomer". 
		Demo.Customer.CopyRecords(testscCustomerMRT);
		testscCustomerMRT.Commit();
	}
	catch (Exception ex)
	{
		testscCustomerMRT.Abort();
		ex.Dump();
	}
}

// You can define other methods, fields, classes and namespaces here
