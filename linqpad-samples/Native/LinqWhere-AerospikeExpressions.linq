<Query Kind="Statements">
  <Connection>
    <ID>973104d1-5fc3-4e74-a869-59441d5e370d</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Driver Assembly="Aerospike.Database.LINQPadDriver" PublicKeyToken="no-strong-name">Aerospike.Database.LINQPadDriver.DynamicDriver</Driver>
    <Server>localhost</Server>
    <Persist>false</Persist>
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
This example compares the use of a Linq Where clause and Aerospike Expressions.

Note: this is not meant to be used in a production environment and there can be performance implications using either this LinqPad driver and expresions! 
*/
test.Customer.Where(c => c.Country == "USA" && c.State == "CA").Dump("Using Linq");
test.Customer.Query(Exp.And(Exp.EQ(Exp.StringBin("Country"), Exp.Val("USA")), Exp.EQ(Exp.StringBin("State"), Exp.Val("CA")))).Dump("Using Aerospike Expressions");