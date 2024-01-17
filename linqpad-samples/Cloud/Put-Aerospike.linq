<Query Kind="Statements">
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
</Query>

/*
This example updates the Customer set using the Aerospike Put functions based on the Primary Key.

Note: this is not meant to be used in a production environment and there can be performance implications using either this LinqPad driver and expresions!
*/
aerospike_cloud.Customer.Get(20).Dump("Orginal Values");
aerospike_cloud.Customer.PutRec(20, Phone: "+1 (650) 123-4567", additionalBinValues: new Dictionary<string, object>() { { "MyBin", "Hello" }});
aerospike_cloud.Customer.Get(20).Dump("Check Phone Number is +1 (650) 123-4567 and the new Bin is listed in the newly display \"Values\" properties");
aerospike_cloud.Customer.Put(20, "Phone", "+1 (650) 644-3358");
aerospike_cloud.Customer.Get(20).Dump("Phone Number restored to orginal value");
aerospike_cloud.Customer.Put(20, "MyBin", null); //Remove the bin I created above...
aerospike_cloud.Customer.Get(20).Dump("\"MyBin\" should have been removed and the \"Values\" property is not visiable");