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
This example compares the use of a Linq Where clause and Aerospike Get function to obtain a record based on the Primary Key.

Note: this is not meant to be used in a production environment and there can be performance implications using either this LinqPad driver and expresions! 
*/
Demo.Customer.Where(c => c.PK == 20).Dump("Using Linq");
Demo.Customer.Get(20).Dump("Using Aerospike Get");