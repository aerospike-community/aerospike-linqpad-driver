<Query Kind="Expression">
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
    </DriverData>
  </Connection>
</Query>

/*
This performs a join between the customer and invoice sets.

Note: this is not meant to be used in a production environment and there can be performance implications using either this LinqPad driver and expresions! 
*/
aerospike_cloud.Customer.AsEnumerable()
	.GroupJoin(aerospike_cloud.Invoice.AsEnumerable(),
				c => c.PK,
				i => i.CustomerId,
				(cust, invoices) => new {Customer = cust, Invoices=invoices})
	