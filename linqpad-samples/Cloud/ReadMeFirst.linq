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
  <Reference Relative="..\DemoDBJson\aerospike.json">&lt;MyDocuments&gt;\LINQPad Queries\Aerospike linqpad-samples\DemoDBJson\aerospike.json</Reference>
</Query>

/* 
This LINQPad script will create the Aerospike sets needed to run the different sample script. 

Before you begin, you will be required to update the connection properties with the Database Hostname, API Key, and API Secert.
You can obtain this information from your Cloud Database Dashboard!
You may also need to increase the timeout settings depending on your connection to Aerospike Cloud.

Below is a list of sample scripts: 
•	ReadMeFirst.linq – This script should be reviewed first. It will load the data into the “Demo” namespace which needs to exist. To create this namespace, please follow these instructions.
•	Basic Data Types.linq - Review some of the capabilities of working with Bins from within the driver plus show how to programmatically access sets and bins
•	Basic Data Types 2.linq - Part 2 of the above
•	Record Display View.linq - This demonstrates how "Record Display View" works
•	Linq Join Customer and Invoice.linq – Shows how to perform a client side join of two sets
•	LinqWhere-AerospikePK.linq – Shows how to use primary keys with Linq and Aerospike API
•	LinqWhere-AerospikeExpressions.linq – Shows how to use a Linq Where clause and the use of Aerospike Filter Expressions
•	POCO.linq – Show the use of the ORM between complete class structures and the Aerospike DB
•	Put-Aerospike.linq – Show the use of how to insert or update a record within a set using a primary key
•	CDT-Json-Docs.linq – Show the use of CDTs (Collection Data Types), Json, and documents by means of Linq and Aerospike Expressions
•	Using NullSet.linq – Show the use the Null Set to obtain a record set for a Set dynamically

*/
void Main()
{

	int ImportRecords()
		=> aerospike_cloud.Import(LINQPad.Util.GetFullPath("aerospike.json"))
			.Dump("Number of Records Imported");

	//Import Aerospike Set Records...	
	try
	{		
		ImportRecords();
	}
	catch (AggregateException ex)
	{
		var exceptionSummary = ex.InnerExceptions
								.GroupBy(ie => new { ie.GetType().Name, ie.Message })
								.Select(ie => new { Exception = ie.Key.Name, Message = ie.Key.Message, Count = ie.Count() });

		if (exceptionSummary.Any(s => s.Exception == "OperationCanceledException" || s.Exception == "TaskCanceledException"))
		{
			var newTimeout = Math.Max(aerospike_cloud.DefaultWritePolicy.totalTimeout,
										aerospike_cloud.DefaultWritePolicy.socketTimeout) * 3;

            LINQPad.Util.WithStyle("An Aggregate Exception occurred during import. Since there are \"Canceled Tasks/Operations\", this MAY indicates a timeout.",
									"color: darkred").Dump();
			LINQPad.Util.WithStyle($"Increasing the timeout value to {newTimeout} and will try the import again.",
									"color: darkred").Dump();
			LINQPad.Util.WithStyle("If the import fails again with cancelled tasks, timeout, deadline exceeded, etc. exceptions. Try increasing the time out properties in the connection dialog",
									"color: darkred").Dump();
			LINQPad.Util.WithStyle("or change the import line code above to \"aerospike_cloud.Import(LINQPad.Util.GetFullPath(\"aerospike.json\", maxDegreeOfParallelism: 1)\" ",
									"color: darkred").Dump();
			exceptionSummary.Dump("Below is the summary of the exceptions.");

			aerospike_cloud.DefaultWritePolicy.SetTimeout(newTimeout);
			ImportRecords();
		}
		else
		{
			LINQPad.Util.WithStyle("An Aggregate Exception occurred during import.",
									"color: darkred").Dump();
			LINQPad.Util.WithStyle("Check your connection properties in the connection dialog. You may try increasing the timeput values",
									"color: darkred").Dump();
			LINQPad.Util.WithStyle("or change the import line code above to \"aerospike_cloud.Import(LINQPad.Util.GetFullPath(\"aerospike.json\", maxDegreeOfParallelism: 1)\" ",
									"color: darkred").Dump();
			exceptionSummary.Dump("Below is the summary of the exceptions.");
			throw;			
		}
	}
	aerospike_cloud.Sets.Select(s => s.SetName).Dump("Sets in aerospike_cloud Namespace");
}

