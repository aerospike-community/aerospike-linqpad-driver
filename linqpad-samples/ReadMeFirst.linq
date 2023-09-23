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
  <Reference Relative="DemoDBJson\aerospike.json">&lt;MyDocuments&gt;\LINQPad Queries\Aerospike linqpad-samples\DemoDBJson\aerospike.json</Reference>
</Query>

/* 
This LINQPad script will create the Aerospike sets needed to run the different sample script. 

For this to be able to run correctly, you are required to create an Aerospike namepsace named "Demo". Also, the LINQPad connection object (“Aerospike Cluster (Demo)”) will need to be modified to point to your Aerospike cluster if it is not running locally. To modify the Aerospike connection, right-click and select properties. The connection dialog, also, has a “Test” feature to ensure you are able to connect to the cluster.

To create an Aerospike namespace, follow the instructions in this link:
https://docs.aerospike.com/server/operations/manage/namespaces 

Once the "Demo" namespace is created and you can connect to the Aerospike cluster, the “Demo” namespace should be displayed under the connection showing zero sets. If not, you will have to refresh te connection by right-clicking on connection object and selection "Refresh". 

Once the “Demo” namespace is present, execute this script to import the data into the namespace. There should be 13 Aerospike sets created.

Once completed, you are ready to continue reviewing the sample scripts. 

Below is a list of sample scripts: 
•	ReadMeFirst.linq – This script should be reviewed first. It will load the data into the “Demo” namespace which needs to exist. To create this namespace, please follow these instructions.
•	Basic Data Types.linq - Review some of the capabilities of working with Bins from within the driver plus show how to programmatically access sets and bins
•	Record Display View.linq - This demonstrates how "Record Display View" works
•	Linq Join Customer and Invoice.linq – Shows how to perform a client side join of two sets
•	LinqWhere-AerospikePK.linq – Shows how to use primary keys with Linq and Aerospike API
•	LinqWhere-AerospikeExpressions.linq – Shows how to use a Linq Where clause and the use of Aerospike Filter Expressions
•	POCO.linq – Show the use of the ORM between complete class structures and the Aerospike DB
•	Put-Aerospike.linq – Show the use of how to insert or update a record within a set using a primary key
•	CDT-Json-Docs.linq – Show the use of CDTs (Collection Data Types), Json, and documents by means of Linq and Aerospike Expressions

*/
void Main()
{
	//Clean up Namespace...
	Demo.Truncate();
	//Import Aerospike Set Records...	
	Demo.Import(LINQPad.Util.GetFullPath("aerospike.json"))
		.Dump("Number of Records Imported");
	Demo.Sets.Select(s => s.SetName).Dump("Sets in Demo Namespace");
}

