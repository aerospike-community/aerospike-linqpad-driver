using Aerospike.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aerospike.Database.LINQPadDriver;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Aerospike.Database.LINQPadDriver.Tests
{
    [TestClass()]
    public class HelpersTests
    {
        [TestMethod()]
        public void EqualsTest()
        {

            Assert.IsTrue(Helpers.Equals(1, 1));
            Assert.IsTrue(Helpers.Equals(1L, (short) 1));
            Assert.IsTrue(Helpers.Equals((short) 1, 1L));
            Assert.IsFalse(Helpers.Equals(1L, (short)2));
            Assert.IsFalse(Helpers.Equals((short)1, 2L));

            Assert.IsTrue(Helpers.Equals(1L, 1d));
            Assert.IsTrue(Helpers.Equals(1d, 1L));
            Assert.IsTrue(Helpers.Equals(1d, (short)1));
            Assert.IsTrue(Helpers.Equals((short)1, 1d));
            Assert.IsTrue(Helpers.Equals(1L, 1m));
            Assert.IsTrue(Helpers.Equals(1m, 1L));
            Assert.IsTrue(Helpers.Equals(1m, (short)1));
            Assert.IsTrue(Helpers.Equals((short)1, 1m));
            Assert.IsTrue(Helpers.Equals(1m, 1d));
            Assert.IsTrue(Helpers.Equals(1d, 1m));

            Assert.IsFalse(Helpers.Equals(2L, 1d));
            Assert.IsFalse(Helpers.Equals(2d, 1L));
            Assert.IsFalse(Helpers.Equals(2d, (short)1));
            Assert.IsFalse(Helpers.Equals((short)2, 1d));
            Assert.IsFalse(Helpers.Equals(2L, 1m));
            Assert.IsFalse(Helpers.Equals(2m, 1L));
            Assert.IsFalse(Helpers.Equals(2m, (short)1));
            Assert.IsFalse(Helpers.Equals((short)2, 1m));
            Assert.IsFalse(Helpers.Equals(2m, 1d));
            Assert.IsFalse(Helpers.Equals(2d, 1m));

            var dtNow = DateTime.Now;

            Assert.IsTrue(Helpers.Equals(dtNow, dtNow));
            Assert.IsTrue(Helpers.Equals(dtNow, new DateTime(dtNow.Ticks)));
            Assert.IsTrue(Helpers.Equals(new DateTime(dtNow.Ticks), dtNow));

            Assert.IsFalse(Helpers.Equals(dtNow, dtNow.AddHours(1)));
            Assert.IsFalse(Helpers.Equals(dtNow.AddHours(1), dtNow));

            Assert.IsFalse(Helpers.Equals(1d, dtNow));
            Assert.IsFalse(Helpers.Equals(dtNow, 1d));

            Assert.IsFalse(Helpers.Equals(1L, dtNow));
            Assert.IsFalse(Helpers.Equals(dtNow, 1L));

            Assert.IsFalse(Helpers.Equals(1, dtNow));
            Assert.IsFalse(Helpers.Equals(dtNow, 1));

            Assert.IsTrue(Helpers.Equals("10", "10"));
            Assert.IsFalse(Helpers.Equals("10", "ab"));
            Assert.IsFalse(Helpers.Equals(1, "1"));
            Assert.IsFalse(Helpers.Equals("1", 1));

        }

        interface ITestInterface
        {

        }

        public class TestClass : ITestInterface
        {
            public class TestClassInner
            {

            }

            public class TestClassInnerG1<T>
            {

            }

            public class TestClassInnerG2<T,U>
            {

            }
        }

        public class TestClassG1<T>
        {
            public class TestClassG1InnerG1<U>
            {

            }
        }

        public class TestClassG2<T, U>
        {
            public class TestClassG2Inner
            {

            }

            public class TestClassG2InnerG1<Z>
            {

            }

            public class TestClassG2InnerG2<Z,X>
            {

            }
        }


        [TestMethod]
        public void GetRealTypeName()
        {
            var expected = "Int32";
            var result = Helpers.GetRealTypeName(typeof(int));

            Assert.AreEqual(expected, result);

            expected = "Nullable<Int32>";
            result = Helpers.GetRealTypeName(typeof(int), true);

            Assert.AreEqual(expected, result);

            expected = "Nullable<Int32>";
            result = Helpers.GetRealTypeName(typeof(int?));

            Assert.AreEqual(expected, result);

            expected = "HelpersTests.ITestInterface";
            result = Helpers.GetRealTypeName(typeof(ITestInterface));

            Assert.AreEqual(expected, result);            

            expected = "HelpersTests.TestClass";
            result = Helpers.GetRealTypeName(typeof(TestClass));

            Assert.AreEqual(expected, result);

            expected = "HelpersTests.TestClassG1<Int32>";
            result = Helpers.GetRealTypeName(typeof(TestClassG1<int>));

            Assert.AreEqual(expected, result);

            expected = "HelpersTests.TestClassG1<String>";
            result = Helpers.GetRealTypeName(typeof(TestClassG1<string>));

            Assert.AreEqual(expected, result);

            expected = "HelpersTests.TestClassG1<Nullable<Int32>>";
            result = Helpers.GetRealTypeName(typeof(TestClassG1<int?>));

            Assert.AreEqual(expected, result);

            expected = "HelpersTests.TestClassG2<Int32,Int64>";
            result = Helpers.GetRealTypeName(typeof(TestClassG2<int,long>));

            Assert.AreEqual(expected, result);

            expected = "HelpersTests.TestClassG2<String,Int64>";
            result = Helpers.GetRealTypeName(typeof(TestClassG2<string, long>));

            Assert.AreEqual(expected, result);

            expected = "HelpersTests.TestClassG2<Int64,String>";
            result = Helpers.GetRealTypeName(typeof(TestClassG2<long, string>));

            Assert.AreEqual(expected, result);

            expected = "HelpersTests.TestClassG2<String,String>";
            result = Helpers.GetRealTypeName(typeof(TestClassG2<string, string>));

            Assert.AreEqual(expected, result);

            expected = "HelpersTests.TestClass.TestClassInner";
            result = Helpers.GetRealTypeName(typeof(TestClass.TestClassInner));

            Assert.AreEqual(expected, result, true);

            expected = "HelpersTests.TestClass.TestClassInnerG1<Int32>";
            result = Helpers.GetRealTypeName(typeof(TestClass.TestClassInnerG1<Int32>));

            Assert.AreEqual(expected, result, true);

            expected = "HelpersTests.TestClass.TestClassInnerG1<Nullable<Int32>>";
            result = Helpers.GetRealTypeName(typeof(TestClass.TestClassInnerG1<Int32?>));

            Assert.AreEqual(expected, result, true);

            expected = "HelpersTests.TestClass.TestClassInnerG2<Int32,HelpersTests.TestClass.TestClassInner>";
            result = Helpers.GetRealTypeName(typeof(TestClass.TestClassInnerG2<Int32,TestClass.TestClassInner>));

            Assert.AreEqual(expected, result, true);

            expected = "HelpersTests.TestClass.TestClassInnerG2<Int32,HelpersTests.TestClass.TestClassInnerG1<Nullable<Int32>>>";
            result = Helpers.GetRealTypeName(typeof(TestClass.TestClassInnerG2<Int32, TestClass.TestClassInnerG1<Int32?>>));

            Assert.AreEqual(expected, result, true);

            expected = "HelpersTests.TestClassG1<String>.TestClassG1InnerG1<Int32>";
            result = Helpers.GetRealTypeName(typeof(TestClassG1<String>.TestClassG1InnerG1<Int32>));

            Assert.AreEqual(expected, result);

            expected = "HelpersTests.TestClassG2<Int32,Int64>.TestClassG2Inner";
            result = Helpers.GetRealTypeName(typeof(TestClassG2<Int32,Int64>.TestClassG2Inner));

            Assert.AreEqual(expected, result);

            expected = "HelpersTests.TestClassG2<Int32,Int64>.TestClassG2InnerG1<String>";
            result = Helpers.GetRealTypeName(typeof(TestClassG2<Int32, Int64>.TestClassG2InnerG1<string>));

            Assert.AreEqual(expected, result);

            expected = "HelpersTests.TestClassG2<Int32,Int64>.TestClassG2InnerG2<String,Boolean>";
            result = Helpers.GetRealTypeName(typeof(TestClassG2<Int32, Int64>.TestClassG2InnerG2<string,Boolean>));

            Assert.AreEqual(expected, result);
        }


        [TestMethod()]
        public void ToAerospikeValueTest()
        {
            object? frmValue = null;
            var asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.AreEqual(asValue, Client.Value.AsNull);

            frmValue = "abc";
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.AreEqual(asValue.Object, frmValue);

            frmValue = 10;
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.AreEqual(asValue.Object, frmValue);

            frmValue = 10L;
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.AreEqual(asValue.Object, frmValue);

            frmValue = (short)10;
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.AreEqual(asValue.Object, frmValue);

            frmValue = (ushort)10;
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.AreEqual(asValue.Object, frmValue);

            frmValue = (ulong)10;
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.AreEqual(asValue.Object, frmValue);

            frmValue = (uint)10;
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.AreEqual(asValue.Object, frmValue);

            frmValue = (decimal)10.123;
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.AreEqual(asValue.Object, (double)(decimal)frmValue);

            frmValue = (double)10.123;
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.AreEqual(asValue.Object, frmValue);

            frmValue = Client.MapOrder.KEY_ORDERED;
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.AreEqual("KEY_ORDERED", asValue.Object);

            frmValue = new List<string>() { "a", "b", "c" };
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.IsInstanceOfType(asValue.Object, frmValue.GetType());
            CollectionAssert.AreEqual((System.Collections.ICollection)asValue.Object, (System.Collections.ICollection)frmValue);

            frmValue = new List<int>() { 1, 2, 3 };
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.IsInstanceOfType(asValue.Object, frmValue.GetType());
            CollectionAssert.AreEqual((System.Collections.ICollection)asValue.Object, (System.Collections.ICollection)frmValue);

            frmValue = new List<ushort>() { 1, 2, 3 };
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.IsInstanceOfType(asValue.Object, frmValue.GetType());
            CollectionAssert.AreEqual((System.Collections.ICollection)asValue.Object, (System.Collections.ICollection)frmValue);

            frmValue = new Dictionary<int, string>() { { 1, "a" }, { 2, "b" }, { 3, "c" } };
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.IsInstanceOfType(asValue.Object, frmValue.GetType());
            CollectionAssert.AreEqual((System.Collections.ICollection)asValue.Object, (System.Collections.ICollection)frmValue);

            frmValue = new Dictionary<ushort, string>() { { 1, "a" }, { 2, "b" }, { 3, "c" } };
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.IsInstanceOfType(asValue.Object, frmValue.GetType());
            CollectionAssert.AreEqual((System.Collections.ICollection)asValue.Object, (System.Collections.ICollection)frmValue);

            frmValue = new Dictionary<ushort, Client.MapOrder>() { { 1, Client.MapOrder.KEY_ORDERED }, { 2, Client.MapOrder.KEY_VALUE_ORDERED }, { 3, Client.MapOrder.UNORDERED } };
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.IsInstanceOfType<IDictionary<object, object>>(asValue.Object);
            CollectionAssert.AreEqual(((Dictionary<object, object>)asValue.Object).Keys,
                                            ((Dictionary<ushort, Client.MapOrder>)frmValue).Keys);
            CollectionAssert.AreEqual(((Dictionary<object, object>)asValue.Object).Values,
                                            ((Dictionary<ushort, Client.MapOrder>)frmValue)
                                                .Values
                                                .Select(i => i.ToString())
                                                .ToList());

            frmValue = new Dictionary<Client.MapOrder, decimal>() { { Client.MapOrder.KEY_ORDERED, 1.1M }, { Client.MapOrder.KEY_VALUE_ORDERED, 2.2M }, { Client.MapOrder.UNORDERED, 3.3M } };
            asValue = frmValue.ToAerospikeValue();

            Assert.IsNotNull(asValue);
            Assert.IsInstanceOfType<IDictionary<object, object>>(asValue.Object);
            CollectionAssert.AreEqual(((Dictionary<object, object>)asValue.Object).Keys,
                                        ((Dictionary<Client.MapOrder, decimal>)frmValue)
                                                    .Keys
                                                    .Select(i => i.ToString())
                                                    .ToList());
            CollectionAssert.AreEqual(((Dictionary<object, object>)asValue.Object).Values,
                                        ((Dictionary<Client.MapOrder, decimal>)frmValue)
                                            .Values
                                            .Select(i => (double) i)
                                            .ToList());

            var AllDateTimeUseUnixEpochNanoRestore = Helpers.AllDateTimeUseUnixEpochNano;
            try
            {
                var dtatetimeOffset = DateTimeOffset.Now;
                
                Helpers.AllDateTimeUseUnixEpochNano = false;

                frmValue = dtatetimeOffset;
                asValue = frmValue.ToAerospikeValue();

                Assert.IsNotNull(asValue);
                Assert.AreEqual(dtatetimeOffset.ToString(Helpers.DateTimeOffsetFormat), asValue.Object);

                frmValue = dtatetimeOffset.DateTime;
                asValue = frmValue.ToAerospikeValue();

                Assert.IsNotNull(asValue);
                Assert.AreEqual(dtatetimeOffset.DateTime.ToString(Helpers.DateTimeFormat), asValue.Object);

                frmValue = dtatetimeOffset.Date;
                asValue = frmValue.ToAerospikeValue();

                Assert.IsNotNull(asValue);
                Assert.AreEqual(dtatetimeOffset.Date.ToString(Helpers.DateTimeFormat), asValue.Object);

                frmValue = dtatetimeOffset.TimeOfDay;
                asValue = frmValue.ToAerospikeValue();

                Assert.IsNotNull(asValue);
                Assert.AreEqual(dtatetimeOffset.TimeOfDay.ToString(Helpers.TimeSpanFormat), asValue.Object);

                {
                    Helpers.AllDateTimeUseUnixEpochNano = true;

                    frmValue = dtatetimeOffset;
                    asValue = frmValue.ToAerospikeValue();

                    Assert.IsNotNull(asValue);
                    Assert.AreEqual(Helpers.NanosFromEpoch(dtatetimeOffset.UtcDateTime), asValue.Object);

                    frmValue = dtatetimeOffset.DateTime;
                    asValue = frmValue.ToAerospikeValue();

                    Assert.IsNotNull(asValue);
                    Assert.AreEqual(Helpers.NanosFromEpoch(dtatetimeOffset.DateTime), asValue.Object);

                    frmValue = dtatetimeOffset.Date;
                    asValue = frmValue.ToAerospikeValue();

                    Assert.IsNotNull(asValue);
                    Assert.AreEqual(Helpers.NanosFromEpoch(dtatetimeOffset.Date), asValue.Object);

                    frmValue = dtatetimeOffset.TimeOfDay;
                    asValue = frmValue.ToAerospikeValue();

                    Assert.IsNotNull(asValue);
                    Assert.AreEqual((long)dtatetimeOffset.TimeOfDay.TotalMilliseconds * 1000000L, asValue.Object);
                }
            }
            finally
            {
                Helpers.AllDateTimeUseUnixEpochNano = AllDateTimeUseUnixEpochNanoRestore;
            }

            {
                var invLines = new List<InvoiceLine>()
                {
                    new InvoiceLine(11, 101, 1021, 1031.11m),
                    new InvoiceLine(12, 102, 1022, 1032.11m),
                    new InvoiceLine(13, 103, 1023, 1033.11m)
                };
                var inv = new Invoice("123 address",
                                        "city",
                                        "country",
                                        "code",
                                        "state",
                                        DateTime.Now,
                                        invLines.Sum(i => i.UnitPrice),
                                        invLines);
                var cust = new Customer(1,
                                        inv.BillingAddress,
                                        inv.BillingCity,
                                        inv.BillingCountry,
                                        "email",
                                        "firstname",
                                        "lastname",
                                        "phone",
                                        inv.BillingCode,
                                        inv.BillingState,
                                        101,
                                        new GeoJSON.Net.Geometry.Point(new GeoJSON.Net.Geometry.Position(51.899523, -2.124156)));

                cust.Invoices = new List<Invoice>() { inv };

                frmValue = cust;
                asValue = frmValue.ToAerospikeValue();

                Assert.IsNotNull(asValue);
                Assert.IsInstanceOfType<Dictionary<string, object>>(asValue.Object);

                var expectedValue = Helpers.Transform<Customer>((Dictionary<string, object>) asValue.Object);

                Assert.IsNotNull(expectedValue);
                Assert.IsInstanceOfType<Customer>(expectedValue);

                var expectedCust = (Customer)expectedValue;

                cust.AssertCheck(expectedCust);
                
            }


        }



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
                            int supportRepId,
                            GeoJSON.Net.Geometry.Point position)
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
                this.Position = position;
            }

            /// <summary>
            /// This property will contain the primary key value but will not be written in the set as a bin. 
            /// </summary>
            [Aerospike.Client.PrimaryKey]
            public long Id { get; }
            public string Address { get; }
            public string City { get; }
            public string Country { get; }
            public string Email { get; }
            public string FirstName { get; }
            public string LastName { get; }
            public string Phone { get; }
            public string PostalCode { get; }
            public string State { get; }
            public GeoJSON.Net.Geometry.Point Position { get; }
            public int SupportRepId { get; }
            public List<Invoice>? Invoices { get; set; }

            public void AssertCheck(Customer customer)
            {
                Assert.IsNotNull(customer);

                Assert.AreEqual(State, customer.State);
                Assert.AreEqual(Phone, customer.Phone);
                Assert.AreEqual(Email, customer.Email);
                Assert.AreEqual(FirstName, customer.FirstName);
                Assert.AreEqual(LastName, customer.LastName);
                Assert.AreEqual(Phone, customer.Phone);
                Assert.AreEqual(Country, customer.Country);
                Assert.AreEqual(City, customer.City);
                Assert.AreEqual(Id, customer.Id);
                Assert.AreEqual(SupportRepId, customer.SupportRepId);
                Assert.AreEqual(PostalCode, customer.PostalCode);
                Assert.AreEqual(Position, customer.Position);

                if (Invoices is null)
                {
                    Assert.IsNull(customer.Invoices);
                }
                else
                {
                    if (customer.Invoices is null)
                    {
                        Assert.Fail("Expected Customer Invoice was NULL and it should not be NULL...");
                    }
                    else
                    {
                        Assert.AreEqual(Invoices.Count, customer.Invoices.Count);
                        for (int i = 0; i < Invoices.Count; i++)
                        {
                            Invoices[i].AssertCheck(customer.Invoices[i]);
                        }
                    }
                }
            }
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
            public string BillingAddress { get; }
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

            public void AssertCheck(Invoice invoice)
            {
                Assert.IsNotNull(invoice);

                Assert.AreEqual(BillingCode, invoice.BillingCode);
                Assert.AreEqual(BillingCity, invoice.BillingCity);
                Assert.AreEqual(BillingState, invoice.BillingState);
                Assert.AreEqual(BillingCountry, invoice.BillingCountry);
                Assert.AreEqual(BillingAddress, invoice.BillingAddress);
                Assert.IsTrue(Math.Abs((InvoiceDate - invoice.InvoiceDate).TotalMilliseconds) < 1,
                                "Customer InvoiceDate do not match");
                if (Lines is null)
                    Assert.IsNull(invoice.Lines);
                else
                {
                    Assert.AreEqual(Lines.Count, invoice.Lines.Count);
                    for(int i = 0; i < Lines.Count; i++)
                    {
                        Lines[i].AssertCheck(invoice.Lines[i]);
                    }
                }
            }
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

            public void AssertCheck(InvoiceLine invoiceLine)
            {
                Assert.IsNotNull(invoiceLine);
                Assert.AreEqual(TrackId, invoiceLine.TrackId);
                Assert.AreEqual(InvoiceId, invoiceLine.InvoiceId);
                Assert.AreEqual(Quantity, invoiceLine.Quantity);
                Assert.AreEqual(UnitPrice, invoiceLine.UnitPrice);
            }
        }
    }
}