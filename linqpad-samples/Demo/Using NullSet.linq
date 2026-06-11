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
    </DriverData>
  </Connection>
</Query>

test.NullSet.Where(ns => ns.Aerospike.SetName == "Artist")
	.Dump("Recorset for Artist Set using the NullSet (record view is Dynamic)");
test[null].Where(ns => ns.Aerospike.SetName == "Artist")
	.Dump("Recorset for Artist Set using null (record view is Dynamic)");
test["NullSet"].Where(ns => ns.Aerospike.SetName == "Artist")
	.Dump("Recorset for Artist Set using 'NullSet' (record view is Dynamic)");
test.Artist
	.Dump("Recorset for Artist Set using the Set Directly (record view is Record)");