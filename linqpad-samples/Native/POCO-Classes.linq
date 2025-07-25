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
</Query>

/*
Classes used in the POCO sample
     
Note: this is not meant to be used in a production environment and there can be performance implications using this LinqPad driver!  
*/

//Class definations for Customer and Invoice
public class Customer
{	
	/// <summary>
	/// The constructor used to create the object. 
	/// Note that the property "Invoices" will be set using the accessor. 
	/// </summary>
	[Aerospike.Client.Constructor]
	public Customer(long id,
					string address,
	                string city,
					string country,
					string email,
					string firstName,
					string lastName,
					string phone,
					string postalCode,
					string state, 
					int supportRepId)
	{
		this.Id = id;
		this.Address = address;
		this.City = city;
		this.Country = country;
		this.Email = email;
		this.FirstName = firstName;
		this.LastName = lastName;
		this.Phone = phone;
		this.PostalCode = postalCode;
		this.State = state;
		this.SupportRepId = supportRepId;
	}
	
	/// <summary>
	/// This property will contain the primary key value but will not be written in the set as a bin. 
	/// </summary>
	[Aerospike.Client.PrimaryKey]
	[Aerospike.Client.BinIgnore]
	public long Id { get; }
	public string Address{ get; }	
	public string City { get; }	
	public string Country { get; }	
	public string Email { get; }
	public string FirstName { get; }
	public string LastName	{ get; }
	public string Phone { get; }
	public string PostalCode { get; }
	public string State { get; }
	public int SupportRepId { get; }
	public List<Invoice> Invoices { get; set; }
}

public class Invoice
{	
	[Aerospike.Client.Constructor]
	public Invoice(string billingAddress,
					string billingCity,
					string billingCountry,
					string billingCode,
					string billingState,
					DateTime invoiceDate,
					decimal total,
					List<InvoiceLine> lines)
	{
		this.BillingAddress = billingAddress;
		this.BillingCity = billingCity;
		this.BillingCode = billingCode;
		this.BillingState = billingState;
		this.BillingCountry = billingCountry;
		this.InvoiceDate = invoiceDate;
		this.Total = total;
		this.Lines = lines;
	}
	
	/// <summary>
	/// Uses the bin name BillingAddr instead of the property name.
	/// </summary>
	[Aerospike.Client.BinName("BillingAddr")]
	public string BillingAddress { get;}
	public string BillingCity { get; }		
	[Aerospike.Client.BinName("BillingCtry")]
	public string BillingCountry { get; }
	public string BillingCode { get; }
	public string BillingState { get; }
	/// <summary>
	/// Notice that the driver will convert the DB value into the targed value automatically.
	/// The value is stored as a sting in the DB but converted to a date/time. Upon write it will be converted from back to a native DB type (e.g., string or long depending on configuration).
	/// </summary>
	public DateTime InvoiceDate { get; }
	/// <summary>
	/// This is stored as a double in the DB but is automatically converted to a decimal.
	/// </summary>
	public Decimal Total { get; }
	public IList<InvoiceLine> Lines { get; }
}

public class InvoiceLine
{
	[Aerospike.Client.Constructor]
	public InvoiceLine(long invoiceId,
						long quantity,
						long trackId,
						decimal unitPrice)
	{
		this.InvoiceId = invoiceId;
		this.Quantity = quantity;
		this.TrackId = trackId;
		this.UnitPrice = unitPrice;
	}

	public long InvoiceId { get; }
	public long Quantity { get; }
	public long TrackId { get; }
	public decimal UnitPrice { get; }
}