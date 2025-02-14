<Query Kind="Program">
  <Connection>
    <ID>973104d1-5fc3-4e74-a869-59441d5e370d</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Driver Assembly="Aerospike.Database.LINQPadDriver" PublicKeyToken="no-strong-name">Aerospike.Database.LINQPadDriver.DynamicDriver</Driver>
    <Server>172.18.174.161</Server>
    <DisplayName>Aerospike Cluster (Demo)</DisplayName>
    <DriverData>
      <UseExternalIP>false</UseExternalIP>
      <Debug>false</Debug>
      <RecordView>Record</RecordView>
      <DocumentAPI>true</DocumentAPI>
      <DBType>Native</DBType>
      <UsePasswordManager>false</UsePasswordManager>
      <Port>3000</Port>
    </DriverData>
  </Connection>
</Query>

//This example shows how to create a Multi-record Transaction (MRT) using the extension API.
// 	You must follow the instuctions in the ReadMeFirst.linq file first.
//
//This example required a strong consistency namespace called DemoSC.
//	For more information see https://aerospike.com/docs/server/guide/consistency.
//
//You can update the /etc/aerospike/aeorspice.conf file on the dtatabse server with this namepsace:
//		namespace DemoSC {
//				replication - factor 1
//				strong-consistency true
//				strong-consistency-allow-expunge true
//				allow-ttl-without-nsup true
//				storage-engine memory {
//					data-size 2G
//				}
//		}
//
//For more information on Multi-record Transaction (MRT)
//	see https://aerospike.com/blog/aerospike8-transactions/
//
//Note that the extension API handles the Aerospike Commit retry logic in case of the "In-Doubt" state. 
//	For more information see https://aerospike.com/blog/aerospike8-developers/
//Also, you can receive failures for very long transactions (over 10 seconds by default) or too many updates for a transaction. 
//
void Main()
{
	//Copy the Customer set in the Demo Namespace to this namespace (DemoSC).
	//Note that this will create the Set "Customer" in DemoSC
	Demo.Customer.CopyRecords(DemoSC, "Customer");
	DemoSC["Customer"].AsEnumerable().Count().Dump("Customer Records");
	
	//Create two multiple-record transactions (mrt1 and mrt2). 
	// Transaction mrt1 will update customer record 47 
	// Once updated we will try different operations against mrt1, mrt2, and non-mrt DemoSC
	
	var mrt1 = DemoSC.CreateTransaction();
	var mrt2 = DemoSC.CreateTransaction();
	
	//Get orginal record from both mrts and non-mrt...
	//We can obtain this records since no updates have occurred...
	DemoSC.Get("Customer", 47).Dump("DemoSC Orginal Customer rec 47");
	mrt1.Get("Customer", 47).Dump("mrt1 Orginal Customer rec 47");
	mrt2.Get("Customer", 47).Dump("mrt2 Orginal Customer rec 47");
	
	//Use mrt1 to update customer 47's record with a new bin
	"Updating Record 47 using mrt1".Dump();
	mrt1.Put("Customer", 47, "newbin1", "newbin1value");
	//The record's state in mrt1, will be now updated
	mrt1.RecordState("Customer", 47).Dump("Get Record's Transaction state for mrt1");
	//The record's state in mrt2, will be read since it wasn't updated within mrt2.
	mrt2.RecordState("Customer", 47).Dump("Get Record's Transaction state for mrt2");
	
	//Let us try to read the record
	
	//DemoSC will succeed and return the orginal record.
	DemoSC.Get("Customer", 47).Dump("DemoSC obtains the orginal record");
	
	//Get record for mrt1
	mrt1.Get("Customer", 47).Dump("mrt1 obtains the modified record");
	
	//mrt2 will fail with a version mismatch.
	//  This is because we performed a read for this record for mrt2 above.
	//  If we didn't perform the read we would get "Transaction record blocked by a different transaction"
	try
	{
		mrt2.Get("Customer", 47).Dump("Should fail for read since mrt1 was updated.");
	}
	catch (AerospikeException ae) when (ae.Result == ResultCode.MRT_VERSION_MISMATCH)
	{
		ae.Message.Dump("mrt2 read failed (version mismatch)");
	}
	catch (AerospikeException ae) when (ae.Result == ResultCode.MRT_BLOCKED)
	{
		ae.Message.Dump("mrt2 read failed (blocked)");
	}

	//Let us try changing the record for mrt2 and DemoSC
	
	//Try adding a new bin with DemoSC
	try
	{
		"Try adding bin using DemoSC".Dump();
		DemoSC.Put("Customer", 47, "newbinDemoSC", "DemnoSCvalue");
	}
	catch (AerospikeException ae) when (ae.Result == ResultCode.MRT_BLOCKED)
	{
		ae.Message.Dump("DemoSC Put failed (blocked)");
	}

	//Try adding a new bin with mrt2
	try
	{
		"Try adding bin using mrt2".Dump();
		mrt2.Put("Customer", 47, "newbinmrt2", "Demnomrt2");
	}
	catch (AerospikeException ae) when (ae.Result == ResultCode.MRT_VERSION_MISMATCH)
	{
		ae.Message.Dump("mrt2 read failed (version mismatch)");
	}
	catch (AerospikeException ae) when (ae.Result == ResultCode.MRT_BLOCKED)
	{
		ae.Message.Dump("mrt2 Put failed (blocked)");
	}

	//Obtain the record's state for mrt1 and mrt2
	mrt1.RecordState("Customer", 47).Dump("Before Commit -- Get Record's Transaction state for mrt1");
	mrt2.RecordState("Customer", 47).Dump("Before Abort -- Get Record's Transaction state for mrt2");
	
	"Abort mrt2 and Commit mrt1".Dump();
	mrt2.Abort().Dump("Abort status mrt2");
	mrt1.Commit().Dump("Commit status mrt1");

	//Obtain the record's state for mrt1 and mrt2 after abort/commit
	mrt1.RecordState("Customer", 47).Dump("After Commit -- Get Record's Transaction state for mrt1");
	mrt2.RecordState("Customer", 47).Dump("After Abort -- Get Record's Transaction state for mrt2");

	//Get the saved changed record from DemoSC
	DemoSC.Get("Customer", 47).Dump("DemoSC obtains the modified/saved record");

	try
	{
		//Get record for mrt1 which will fail
		mrt1.Get("Customer", 47).Dump("mrt1 should fail");
	}
	catch(AerospikeException ae)
	{
		ae.Dump("Try Get after Commit");
	}
}

// You can define other methods, fields, classes and namespaces here
