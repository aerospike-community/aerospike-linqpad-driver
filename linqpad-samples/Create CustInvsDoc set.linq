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

/*
	This program joins the Customer set and Invoice set, grouped by customer id. 
	Onced joined, it creates a new set (e.g., CustInvsDoc) where each customer has a list of their invoices (documents).

Note: this is not meant to be used in a production environment and there can be performance implications using this LinqPad driver! 
*/

void Main()
{
	var setName = "CustInvsDoc";
	//Join Customer and Invoice
	var ciRecs = Demo.Customer.AsEnumerable()
					.GroupJoin(Demo.Invoice.AsEnumerable(),
								c => c.PK,
								i => i.CustomerId,
								(cust, invoices) => new { Customer = cust, Invoices = invoices.ToArray() });

	//Truncate the new set if exists...
	Demo.Truncate(setName);
	
	//Helper function to create invoice with details (lines) by quering the InvoiceLine set using Aerospike Expresions
	IDictionary<string,object> DetermineInvoiceDetails(Demo_NamespaceCls.Invoice_SetCls.RecordCls invoiceRecord)
	{
		var invoiceDict = invoiceRecord.ToDictionary();
		var invoiceLines = Demo.InvoiceLine.Query(Exp.EQ(Exp.IntBin("InvoiceId"), Exp.Val((long) invoiceRecord.PK)));
		
		if(invoiceLines.Any())
			invoiceDict.Add("Lines", invoiceLines);
			
		return invoiceDict;
	}
	
	//Add new records to the set...
	foreach (var ciRecords in ciRecs)
	{
		//Convert customer record to a dictionary so that we can add the invoice records
		//Each key in dict is the bin name and the dict value is the associated bin value.
		var customerDict = ciRecords.Customer.ToDictionary();
							
		//Add a new element to represents the invoices for this customer. 
		customerDict.Add("Invoices", ciRecords.Invoices
										.Select(i => DetermineInvoiceDetails(i)));
																				
		//Put the new customer with invoice record into the DB. The driver will transform the dictionary into bins plus 
		//takes the invoice collection and transforms this into an Aerospike collection (document). 
		Demo.Put(setName, ciRecords.Customer.PK, customerDict, refreshOnNewSet: false);		
	}
	
	Demo.RefreshSet(setName);
}
