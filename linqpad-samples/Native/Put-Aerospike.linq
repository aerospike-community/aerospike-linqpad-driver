<Query Kind="Statements">
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
      <DBType>Native</DBType>
      <UsePasswordManager>false</UsePasswordManager>
      <Port>3000</Port>
    </DriverData>
  </Connection>
</Query>

/*
This example updates the Customer set using the Aerospike Put functions based on the Primary Key.

Note: this is not meant to be used in a production environment and there can be performance implications using either this LinqPad driver and expressions!
*/
test.Customer.Get(20).Dump("Original Values");
test.Customer.PutRec(20, Phone: "+1 (650) 123-4567", additionalBinValues: new Dictionary<string, object>() { { "MyBin", "Hello" }});
test.Customer.Get(20).Dump("Edited Phone Number is now +1 (650) 123-4567 and the new Bin (MyBin) is listed in the newly display \"Values\" properties");
test.Customer.Put(20, new Dictionary<string, object>() { { "Phone", "+1 (650) 644-3358"}, { "MyBin", null }});
test.Customer.Get(20).Dump("Phone Number restored to original value and \"MyBin\" should have been removed and the \"Values\" property is not visible");
