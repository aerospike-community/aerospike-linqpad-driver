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
      <AlwaysUseAValues>false</AlwaysUseAValues>
    </DriverData>
  </Connection>
</Query>

// This LINQPad script will create the Aerospike sets needed to run the different samples/examples.
//
// For this to be able to run correctly, you are required to create an Aerospike namepsace named "Demo".
// To create an Aerospike namespace, follow the instructions in this link:
//			https://docs.aerospike.com/server/operations/manage/namespaces
//
// Once the "Demo" namespace is created, it should show up under the Aerospike Connection group.
//	If not, you will have to refresh te connection by right-clicking on connection object and selection "Refresh".
//
//Next Step is to run the "Create CustInvsDoc set" LINQPad script to create the CustInvsDoc Aerospike Set.
//
//Below is a list of the current sample code:
//	Create CustInvsDoc set -- Creates a document/sub-doc joined set used for the ORM/POCO, document, and CST samples
//	Basic Data Types -- This will demo some of the capiblities of working with Bins from within the driver plus show how to programmaticial access sets and bins. Don’t forget bins can be accessed directly from the set as a property of that set. 
//	CDT-Json-Docs -- This will show how to use CDT/Json/Documents with the driver including the use of Aerospike Expressions.
//	Linq Join Customer and Invoice -- Shows how to perform client side joins
//	LinqWhere-AerospikeExpressions -- This example compares the use of a Linq Where clause and Aerospike Expressions.
//	LinqWhere-AerospikePK -- This example compares the use of a Linq Where clause and Aerospike Get function to obtain a record based on the Primary Key.
//	POCO -- This program will read the CustInvsDoc (Customer-Invoice documents) set and cast the result into a .Net user class (Plain Old CLR Object).
//	Record Display View -- This demonstrates how "Record Display View" works.

void Main()
{
	//Import Aerospike Set Records...
	Demo.Import(@".\DemoDBJson\Aerospike.json")
		.Dump("Number of Records Imported");	
}

