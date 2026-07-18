using Aerospike.Client;
using Aerospike.Database.LINQPadDriver;
using Aerospike.Database.LINQPadDriver.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Text;

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

            {
				var key = 1007466L;
				var pk = new Key("ns", "set", key);
				var pkDigest = pk.digest;
                var hexStr = "0x" + Helpers.ByteArrayToString(pkDigest);

                Assert.IsTrue(Helpers.Equals(pkDigest, hexStr));
				Assert.IsTrue(Helpers.Equals(hexStr, pkDigest));
				Assert.IsTrue(Helpers.Equals(hexStr, hexStr));
				Assert.IsTrue(Helpers.Equals(pkDigest, pkDigest));
				Assert.IsFalse(Helpers.Equals(pkDigest, hexStr.Substring(2)));
				Assert.IsFalse(Helpers.Equals(pkDigest, hexStr.Substring(0,10)));
				Assert.IsFalse(Helpers.Equals(pkDigest, "0x" + hexStr));
				Assert.IsFalse(Helpers.Equals(pkDigest, null));
				Assert.IsFalse(Helpers.Equals(pkDigest, ""));
				Assert.IsFalse(Helpers.Equals(null, pkDigest));
				Assert.IsFalse(Helpers.Equals("", pkDigest));
				Assert.IsFalse(Helpers.Equals(hexStr, null));
				Assert.IsFalse(Helpers.Equals(hexStr, ""));
			}


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

		[TestMethod()]
		public void CanCastToNativeTypeTest()
		{
			// Null handling tests
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), null));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int?), null));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(int), null));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DateTime?), null));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(DateTime), null));

			// Byte value tests
			byte byteValue = 10;
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(byte), byteValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(short), byteValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int), byteValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(long), byteValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(uint), byteValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(ulong), byteValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(ushort), byteValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(decimal), byteValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(float), byteValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(double), byteValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(bool), byteValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), byteValue));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(DateTime), byteValue));

			// Long/Int64 value tests
			long longValue = 12345L;
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(byte), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(short), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(long), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(uint), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(ulong), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(ushort), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(decimal), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(float), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(double), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(bool), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DateTime), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DateTimeOffset), longValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(TimeSpan), longValue));

			// Double value tests
			double doubleValue = 123.45;
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(short), doubleValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int), doubleValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(uint), doubleValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(ulong), doubleValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(ushort), doubleValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(decimal), doubleValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(float), doubleValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(double), doubleValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(bool), doubleValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), doubleValue));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(DateTime), doubleValue));

			// String value tests - valid conversions
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), "test"));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int), "123"));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(long), "123"));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(double), "123.45"));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(decimal), "123.45"));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(bool), "true"));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(Guid), Guid.NewGuid().ToString()));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DateTime), DateTime.Now.ToString()));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DateTimeOffset), DateTimeOffset.Now.ToString()));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(TimeSpan), TimeSpan.FromHours(1).ToString()));

			// String value tests - invalid conversions
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(int), "not a number"));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(double), "not a number"));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(bool), "not a bool"));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(Guid), "not a guid"));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(DateTime), "not a date"));

			// Empty string tests
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), ""));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), string.Empty));

			// DateTime tests
			DateTime dtNow = DateTime.Now;
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DateTime), dtNow));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DateTimeOffset), dtNow));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), dtNow));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(long), dtNow));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JObject), dtNow));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JToken), dtNow));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(int), dtNow));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(bool), dtNow));

			// DateTimeOffset tests
			DateTimeOffset dtoNow = DateTimeOffset.Now;
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DateTime), dtoNow));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DateTimeOffset), dtoNow));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), dtoNow));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(long), dtoNow));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JObject), dtoNow));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JToken), dtoNow));

			// TimeSpan tests
			TimeSpan ts = TimeSpan.FromHours(1);
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(TimeSpan), ts));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), ts));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JObject), ts));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JToken), ts));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(int), ts));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(DateTime), ts));

			// List/Collection tests
			var intList = new List<int> { 1, 2, 3 };
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int[]), intList));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(List<int>), intList));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(IList<int>), intList));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(IEnumerable<int>), intList));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(string), intList));

			var objectList = new List<object> { 1, "test", 3.14 };
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(object[]), objectList));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(List<object>), objectList));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(IList<object>), objectList));

			// Dictionary tests
			var stringDict = new Dictionary<string, object> { { "key1", "value1" }, { "key2", 123 } };
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(Dictionary<string, object>), stringDict));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(IDictionary<string, object>), stringDict));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JsonDocument), stringDict));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JObject), stringDict));

			var objectDict = new Dictionary<object, object> { { "key1", "value1" }, { 1, 123 } };
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(Dictionary<object, object>), objectDict));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(IDictionary<object, object>), objectDict));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JsonDocument), objectDict));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JObject), objectDict));

			// JToken types - should always be true
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JToken), "test"));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JToken), 123));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JToken), null));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JObject), "test"));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JArray), "test"));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(JsonDocument), "test"));

			// JSON string tests
			string jsonObject = "{\"name\":\"test\",\"value\":123}";
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JObject), jsonObject));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JToken), jsonObject));

			string jsonArray = "[1,2,3]";
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JArray), jsonArray));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(JToken), jsonArray));

			string invalidJson = "{invalid json";
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(JObject), invalidJson));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(JArray), invalidJson));

			// Enum tests
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DayOfWeek), "Monday"));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DayOfWeek), "MONDAY"));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DayOfWeek), 1L));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(DayOfWeek), "InvalidDay"));

			// Nullable type tests
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int?), 123));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int?), "123"));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int?), null));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DateTime?), dtNow));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DateTime?), null));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(int?), "not a number"));

			// AValue tests
			var aValue = new Extensions.AValue(123, "testBin", "testField");
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int), aValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(long), aValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), aValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(double), aValue));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(decimal), aValue));

			var aValueNull = new Extensions.AValue(null, "testBin", "testField");
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), aValueNull));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int?), aValueNull));

			// Array tests
			int[] intArray = new int[] { 1, 2, 3 };
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int[]), intArray));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(List<int>), intArray));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(IEnumerable<int>), intArray));

			byte[] byteArray = new byte[] { 1, 2, 3 };
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(byte[]), byteArray));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), byteArray));

			// Invalid type argument
			Assert.IsFalse(Helpers.CanCastToNativeType(null, 123));

			// Edge cases - incompatible types
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(int), new List<string>()));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(DateTime), "invalid date format"));
			Assert.IsFalse(Helpers.CanCastToNativeType(typeof(Guid), "not-a-guid"));
		}

		[TestMethod()]
		public void CanCastToNativeType_ComplexScenarios()
		{
			// Test nested generic types
			var nestedList = new List<List<int>> { new List<int> { 1, 2 }, new List<int> { 3, 4 } };
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(List<List<int>>), nestedList));

			// Test dictionary with different key/value types
			var mixedDict = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } };
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(Dictionary<int, string>), mixedDict));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(IDictionary<int, string>), mixedDict));

			// Test IConvertible types
			IConvertible convertible = 123;
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int), convertible));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(string), convertible));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(double), convertible));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(DateTime), convertible));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(decimal), convertible));

			// Test with very large numbers
			long largeLong = long.MaxValue;
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(long), largeLong));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(ulong), largeLong));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(decimal), largeLong));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(double), largeLong));

			// Test with zero
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(bool), 0L));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(bool), 0.0));
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int), 0L));

			// Test string parsing edge cases
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(int), "  123  ")); // whitespace should be handled
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(double), "123.45e2")); // scientific notation
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(bool), "TRUE")); // case insensitive
			Assert.IsTrue(Helpers.CanCastToNativeType(typeof(bool), "FALSE"));
		}

		[TestMethod()]
		public void CastToNativeTypeTest()
		{

			// Null handling tests
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(string), "bin", null));
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(int?), "bin", null));
			try
			{
				Helpers.CastToNativeType("field", typeof(int), "bin", null);
				Assert.Fail("Expected ArgumentException was not thrown.");
			}
			catch(ArgumentException)
			{				
			}

			// Byte value conversions
			byte byteValue = 10;
			Assert.AreEqual((byte) 10, Helpers.CastToNativeType("field", typeof(byte), "bin", byteValue));
			Assert.AreEqual((short) 10, Helpers.CastToNativeType("field", typeof(short), "bin", byteValue));
			Assert.AreEqual(10, Helpers.CastToNativeType("field", typeof(int), "bin", byteValue));
			Assert.AreEqual(10L, Helpers.CastToNativeType("field", typeof(long), "bin", byteValue));
			Assert.AreEqual(10.0f, Helpers.CastToNativeType("field", typeof(float), "bin", byteValue));
			Assert.AreEqual(10.0, Helpers.CastToNativeType("field", typeof(double), "bin", byteValue));
			Assert.AreEqual(10m, Helpers.CastToNativeType("field", typeof(decimal), "bin", byteValue));
			Assert.AreEqual(true, Helpers.CastToNativeType("field", typeof(bool), "bin", byteValue));
			Assert.AreEqual("10", Helpers.CastToNativeType("field", typeof(string), "bin", byteValue));

			// Long value conversions
			long longValue = 12345L;
			Assert.AreEqual((short) 12345, Helpers.CastToNativeType("field", typeof(short), "bin", longValue));
			Assert.AreEqual(12345, Helpers.CastToNativeType("field", typeof(int), "bin", longValue));
			Assert.AreEqual(12345L, Helpers.CastToNativeType("field", typeof(long), "bin", longValue));
			Assert.AreEqual(12345.0, Helpers.CastToNativeType("field", typeof(double), "bin", longValue));
			Assert.AreEqual(12345m, Helpers.CastToNativeType("field", typeof(decimal), "bin", longValue));
			Assert.AreEqual("12345", Helpers.CastToNativeType("field", typeof(string), "bin", longValue));
			Assert.AreEqual(true, Helpers.CastToNativeType("field", typeof(bool), "bin", longValue));

			// Long to DateTime conversion (based on configuration)
			var previousSetting = Helpers.AllDateTimeUseUnixEpochNano;
			var previousUnixEpochNanoSetting = Helpers.UseUnixEpochNanoForNumericDateTime;
			try
			{
				Helpers.UseUnixEpochNanoForNumericDateTime = false;
				var ticksValue = DateTime.Now.Ticks;
				var dtFromTicks = (DateTime) Helpers.CastToNativeType("field", typeof(DateTime), "bin", ticksValue);
				Assert.AreEqual(new DateTime(ticksValue), dtFromTicks);

				Helpers.UseUnixEpochNanoForNumericDateTime = true;
				var nanoValue = Helpers.NanosFromEpoch(DateTime.UtcNow);
				var dtFromNano = (DateTime) Helpers.CastToNativeType("field", typeof(DateTime), "bin", nanoValue);
				var rts = (DateTime.UtcNow - dtFromNano).TotalSeconds;
				Assert.IsTrue(rts < 1);
			}
			finally
			{
				Helpers.AllDateTimeUseUnixEpochNano = previousSetting;
				Helpers.UseUnixEpochNanoForNumericDateTime = previousUnixEpochNanoSetting;
			}

			// Double value conversions
			double doubleValue = 123.45;
			Assert.AreEqual(123, Helpers.CastToNativeType("field", typeof(int), "bin", doubleValue));
			Assert.AreEqual(123L, Helpers.CastToNativeType("field", typeof(long), "bin", doubleValue));
			Assert.AreEqual(123.45f, (float) Helpers.CastToNativeType("field", typeof(float), "bin", doubleValue), 0.01f);
			Assert.AreEqual(123.45, Helpers.CastToNativeType("field", typeof(double), "bin", doubleValue));
			Assert.AreEqual(123.45m, Helpers.CastToNativeType("field", typeof(decimal), "bin", doubleValue));
			Assert.AreEqual("123.45", Helpers.CastToNativeType("field", typeof(string), "bin", doubleValue));
			Assert.AreEqual(true, Helpers.CastToNativeType("field", typeof(bool), "bin", doubleValue));
			Assert.AreEqual(false, Helpers.CastToNativeType("field", typeof(bool), "bin", 0.0));

			// String to numeric conversions
			Assert.AreEqual(123, Helpers.CastToNativeType("field", typeof(int), "bin", "123"));
			Assert.AreEqual(123L, Helpers.CastToNativeType("field", typeof(long), "bin", "123"));
			Assert.AreEqual((short) 123, Helpers.CastToNativeType("field", typeof(short), "bin", "123"));
			Assert.AreEqual(123.45, Helpers.CastToNativeType("field", typeof(double), "bin", "123.45"));
			Assert.AreEqual(123.45f, Helpers.CastToNativeType("field", typeof(float), "bin", "123.45"));
			Assert.AreEqual(123.45m, Helpers.CastToNativeType("field", typeof(decimal), "bin", "123.45"));

			// String to bool conversions
			Assert.AreEqual(true, Helpers.CastToNativeType("field", typeof(bool), "bin", "true"));
			Assert.AreEqual(true, Helpers.CastToNativeType("field", typeof(bool), "bin", "True"));
			Assert.AreEqual(false, Helpers.CastToNativeType("field", typeof(bool), "bin", "false"));
			Assert.AreEqual(false, Helpers.CastToNativeType("field", typeof(bool), "bin", "False"));

			// String to Guid conversion
			var guid = Guid.NewGuid();
			var guidStr = guid.ToString();
			Assert.AreEqual(guid, Helpers.CastToNativeType("field", typeof(Guid), "bin", guidStr));

			// String to DateTime conversions
			var dtNow = DateTime.Now;
			var dtStr = dtNow.ToString(Helpers.DateTimeFormat);
			var convertedDt = (DateTime) Helpers.CastToNativeType("field", typeof(DateTime), "bin", dtStr);
			Assert.IsTrue((dtNow - convertedDt).TotalSeconds < 1);

			// String to DateTimeOffset conversions
			var dtoNow = DateTimeOffset.Now;
			var dtoStr = dtoNow.ToString(Helpers.DateTimeOffsetFormat);
			var convertedDto = (DateTimeOffset) Helpers.CastToNativeType("field", typeof(DateTimeOffset), "bin", dtoStr);
			Assert.IsTrue((dtoNow - convertedDto).TotalSeconds < 1);

			// String to TimeSpan conversions
			var ts = TimeSpan.FromHours(2.5);
			var tsStr = ts.ToString(Helpers.TimeSpanFormat);
			var convertedTs = (TimeSpan) Helpers.CastToNativeType("field", typeof(TimeSpan), "bin", tsStr);
			Assert.AreEqual(ts, convertedTs);

			// Empty string handling
			Assert.AreEqual(string.Empty, Helpers.CastToNativeType("field", typeof(string), "bin", string.Empty));

			// DateTime to various types
			var dt = DateTime.Now;
			Assert.AreEqual(dt, Helpers.CastToNativeType("field", typeof(DateTime), "bin", dt));
			Assert.IsInstanceOfType(Helpers.CastToNativeType("field", typeof(DateTimeOffset), "bin", dt), typeof(DateTimeOffset));
			Assert.IsInstanceOfType(Helpers.CastToNativeType("field", typeof(string), "bin", dt), typeof(string));
			Assert.IsInstanceOfType(Helpers.CastToNativeType("field", typeof(long), "bin", dt), typeof(long));
			Assert.IsInstanceOfType(Helpers.CastToNativeType("field", typeof(JObject), "bin", dt), typeof(JObject));

			// DateTimeOffset to various types
			var dto = DateTimeOffset.Now;
			Assert.IsInstanceOfType(Helpers.CastToNativeType("field", typeof(DateTime), "bin", dto), typeof(DateTime));
			Assert.AreEqual(dto, Helpers.CastToNativeType("field", typeof(DateTimeOffset), "bin", dto));
			Assert.IsInstanceOfType(Helpers.CastToNativeType("field", typeof(string), "bin", dto), typeof(string));
			Assert.IsInstanceOfType(Helpers.CastToNativeType("field", typeof(long), "bin", dto), typeof(long));

			// TimeSpan to various types
			var timespan = TimeSpan.FromHours(1);
			Assert.AreEqual(timespan, Helpers.CastToNativeType("field", typeof(TimeSpan), "bin", timespan));
			Assert.IsInstanceOfType(Helpers.CastToNativeType("field", typeof(string), "bin", timespan), typeof(string));
			Assert.IsInstanceOfType(Helpers.CastToNativeType("field", typeof(JObject), "bin", timespan), typeof(JObject));

			// List conversions
			var intList = new List<object> { 1L, 2L, 3L };
			var convertedArray = (int[]) Helpers.CastToNativeType("field", typeof(int[]), "bin", intList);
			Assert.AreEqual(3, convertedArray.Length);
			Assert.AreEqual(1, convertedArray[0]);
			Assert.AreEqual(2, convertedArray[1]);
			Assert.AreEqual(3, convertedArray[2]);

			var convertedList = (List<int>) Helpers.CastToNativeType("field", typeof(List<int>), "bin", intList);
			Assert.AreEqual(3, convertedList.Count);
			Assert.AreEqual(1, convertedList[0]);

			// Dictionary conversions
			var dict = new Dictionary<object, object>
	{
		{ "key1", "value1" },
		{ "key2", 123L }
	};

			var convertedDict = (Dictionary<string, object>) Helpers.CastToNativeType(
				"field", typeof(Dictionary<string, object>), "bin", dict);
			Assert.AreEqual(2, convertedDict.Count);
			Assert.AreEqual("value1", convertedDict["key1"]);
			Assert.AreEqual(123L, convertedDict["key2"]);

			// Dictionary to JObject
			var jobj = (JObject) Helpers.CastToNativeType("field", typeof(JObject), "bin", dict);
			Assert.IsNotNull(jobj);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
			Assert.AreEqual("value1", jobj["key1"].ToString());
#pragma warning restore CS8602 // Dereference of a possibly null reference.

			// Dictionary to JsonDocument
			var jsonDoc = (JsonDocument) Helpers.CastToNativeType("field", typeof(JsonDocument), "bin", dict);
			Assert.IsNotNull(jsonDoc);

			// String to JSON conversions
			string jsonStr = "{\"name\":\"test\",\"value\":123}";
			var jobjFromStr = (JObject) Helpers.CastToNativeType("field", typeof(JObject), "bin", jsonStr);
			Assert.IsNotNull(jobjFromStr);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
			Assert.AreEqual("test", jobjFromStr["name"].ToString());
#pragma warning restore CS8602 // Dereference of a possibly null reference.

			string jsonArrayStr = "[1,2,3]";
			var jarr = (JArray) Helpers.CastToNativeType("field", typeof(JArray), "bin", jsonArrayStr);
			Assert.IsNotNull(jarr);
			Assert.AreEqual(3, jarr.Count);

			// Enum conversions
			Assert.AreEqual(DayOfWeek.Monday, Helpers.CastToNativeType("field", typeof(DayOfWeek), "bin", "Monday"));
			Assert.AreEqual(DayOfWeek.Monday, Helpers.CastToNativeType("field", typeof(DayOfWeek), "bin", "MONDAY"));
			Assert.AreEqual(DayOfWeek.Monday, Helpers.CastToNativeType("field", typeof(DayOfWeek), "bin", 1L));

			// Nullable type conversions
			Assert.AreEqual(123, Helpers.CastToNativeType("field", typeof(int?), "bin", 123L));
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(int?), "bin", null));
			Assert.AreEqual(123, Helpers.CastToNativeType("field", typeof(int?), "bin", "123"));

			// Add these nullable primitive tests after line 928 (after the existing int? tests)

			// Nullable long conversions
			Assert.AreEqual(123L, Helpers.CastToNativeType("field", typeof(long?), "bin", 123L));
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(long?), "bin", null));
			Assert.AreEqual(123L, Helpers.CastToNativeType("field", typeof(long?), "bin", "123"));
			Assert.AreEqual(123L, Helpers.CastToNativeType("field", typeof(long?), "bin", 123));

			// Nullable double conversions
			Assert.AreEqual(123.45, Helpers.CastToNativeType("field", typeof(double?), "bin", 123.45));
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(double?), "bin", null));
			Assert.AreEqual(123.45, Helpers.CastToNativeType("field", typeof(double?), "bin", "123.45"));
			Assert.AreEqual(123.0, Helpers.CastToNativeType("field", typeof(double?), "bin", 123L));

			// Nullable float conversions
			Assert.AreEqual(123.45f, (float) Helpers.CastToNativeType("field", typeof(float?), "bin", 123.45f), 0.01f);
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(float?), "bin", null));
			Assert.AreEqual(123.45f, (float) Helpers.CastToNativeType("field", typeof(float?), "bin", "123.45"), 0.01f);
			Assert.AreEqual(123.0f, (float) Helpers.CastToNativeType("field", typeof(float?), "bin", 123L), 0.01f);

			// Nullable decimal conversions
			Assert.AreEqual(123.45m, Helpers.CastToNativeType("field", typeof(decimal?), "bin", 123.45m));
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(decimal?), "bin", null));
			Assert.AreEqual(123.45m, Helpers.CastToNativeType("field", typeof(decimal?), "bin", "123.45"));
			Assert.AreEqual(123m, Helpers.CastToNativeType("field", typeof(decimal?), "bin", 123L));

			// Nullable bool conversions
			Assert.AreEqual(true, Helpers.CastToNativeType("field", typeof(bool?), "bin", true));
			Assert.AreEqual(false, Helpers.CastToNativeType("field", typeof(bool?), "bin", false));
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(bool?), "bin", null));
			Assert.AreEqual(true, Helpers.CastToNativeType("field", typeof(bool?), "bin", "true"));
			Assert.AreEqual(false, Helpers.CastToNativeType("field", typeof(bool?), "bin", "false"));
			Assert.AreEqual(true, Helpers.CastToNativeType("field", typeof(bool?), "bin", 1L));
			Assert.AreEqual(false, Helpers.CastToNativeType("field", typeof(bool?), "bin", 0L));

			// Nullable byte conversions
			Assert.AreEqual((byte) 10, Helpers.CastToNativeType("field", typeof(byte?), "bin", (byte) 10));
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(byte?), "bin", null));
			Assert.AreEqual((byte) 10, Helpers.CastToNativeType("field", typeof(byte?), "bin", "10"));
			Assert.AreEqual((byte) 10, Helpers.CastToNativeType("field", typeof(byte?), "bin", 10L));

			// Nullable short conversions
			Assert.AreEqual((short) 123, Helpers.CastToNativeType("field", typeof(short?), "bin", (short) 123));
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(short?), "bin", null));
			Assert.AreEqual((short) 123, Helpers.CastToNativeType("field", typeof(short?), "bin", "123"));
			Assert.AreEqual((short) 123, Helpers.CastToNativeType("field", typeof(short?), "bin", 123L));

			// Nullable Guid conversions
			var testGuid = Guid.NewGuid();
			Assert.AreEqual(testGuid, Helpers.CastToNativeType("field", typeof(Guid?), "bin", testGuid));
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(Guid?), "bin", null));
			Assert.AreEqual(testGuid, Helpers.CastToNativeType("field", typeof(Guid?), "bin", testGuid.ToString()));

			// Nullable DateTime conversions
			var testDateTime = DateTime.Now;
			var testDateTimeResult = (DateTime?) Helpers.CastToNativeType("field", typeof(DateTime?), "bin", testDateTime);
			Assert.IsNotNull(testDateTimeResult);
			Assert.IsTrue((testDateTime - testDateTimeResult.Value).TotalSeconds < 1);
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(DateTime?), "bin", null));

			var dtStr2 = testDateTime.ToString(Helpers.DateTimeFormat);
			var testDateTimeFromStr = (DateTime?) Helpers.CastToNativeType("field", typeof(DateTime?), "bin", dtStr2);
			Assert.IsNotNull(testDateTimeFromStr);
			Assert.IsTrue((testDateTime - testDateTimeFromStr.Value).TotalSeconds < 1);

			// Nullable DateTimeOffset conversions
			var testDateTimeOffset = DateTimeOffset.Now;
			var testDtoResult = (DateTimeOffset?) Helpers.CastToNativeType("field", typeof(DateTimeOffset?), "bin", testDateTimeOffset);
			Assert.IsNotNull(testDtoResult);
			Assert.IsTrue((testDateTimeOffset - testDtoResult.Value).TotalSeconds < 1);
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(DateTimeOffset?), "bin", null));

			var dtoStr2 = testDateTimeOffset.ToString(Helpers.DateTimeOffsetFormat);
			var testDtoFromStr = (DateTimeOffset?) Helpers.CastToNativeType("field", typeof(DateTimeOffset?), "bin", dtoStr2);
			Assert.IsNotNull(testDtoFromStr);
			Assert.IsTrue((testDateTimeOffset - testDtoFromStr.Value).TotalSeconds < 1);

			// Nullable TimeSpan conversions
			var testTimeSpan = TimeSpan.FromHours(3.5);
			Assert.AreEqual(testTimeSpan, Helpers.CastToNativeType("field", typeof(TimeSpan?), "bin", testTimeSpan));
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(TimeSpan?), "bin", null));

			var tsStr2 = testTimeSpan.ToString(Helpers.TimeSpanFormat);
			var testTsFromStr = (TimeSpan?) Helpers.CastToNativeType("field", typeof(TimeSpan?), "bin", tsStr2);
			Assert.AreEqual(testTimeSpan, testTsFromStr);

			// Nullable enum conversions
			Assert.AreEqual(DayOfWeek.Friday, Helpers.CastToNativeType("field", typeof(DayOfWeek?), "bin", "Friday"));
			Assert.AreEqual(DayOfWeek.Friday, Helpers.CastToNativeType("field", typeof(DayOfWeek?), "bin", "FRIDAY"));
			Assert.AreEqual(DayOfWeek.Friday, Helpers.CastToNativeType("field", typeof(DayOfWeek?), "bin", 5L));
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(DayOfWeek?), "bin", null));

			// AValue conversions
			var aValue = new Extensions.AValue(123L, "testBin", "testField");
			Assert.AreEqual(123, Helpers.CastToNativeType("field", typeof(int), "bin", aValue));
			Assert.AreEqual(123L, Helpers.CastToNativeType("field", typeof(long), "bin", aValue));
			Assert.AreEqual("123", Helpers.CastToNativeType("field", typeof(string), "bin", aValue));
			Assert.AreEqual(123.0, Helpers.CastToNativeType("field", typeof(double), "bin", aValue));

			// Array to List conversion
			int[] intArray = new int[] { 1, 2, 3 };
			var listFromArray = (List<int>) Helpers.CastToNativeType("field", typeof(List<int>), "bin", intArray);
			Assert.AreEqual(3, listFromArray.Count);
			Assert.AreEqual(1, listFromArray[0]);

			// byte[] to string conversion
			byte[] byteArray = Encoding.Default.GetBytes("test");
			var strFromBytes = (string) Helpers.CastToNativeType("field", typeof(string), "bin", byteArray);
			Assert.AreEqual("test", strFromBytes);

			// IConvertible conversions
			IConvertible convertible = 123;
			Assert.AreEqual(123, Helpers.CastToNativeType("field", typeof(int), "bin", convertible));
			Assert.AreEqual("123", Helpers.CastToNativeType("field", typeof(string), "bin", convertible));
			Assert.AreEqual(123.0, Helpers.CastToNativeType("field", typeof(double), "bin", convertible));
		}

		[TestMethod()]
		public void CastToNativeType_ExceptionTests()
		{
			// Invalid string to int conversion
			try
			{
				Helpers.CastToNativeType("field", typeof(int), "bin", "not a number");
				Assert.Fail("Expected ArgumentException was not thrown.");
			}
			catch { }

			// Invalid string to double conversion
			try
			{
				Helpers.CastToNativeType("field", typeof(double), "bin", "not a number");
				Assert.Fail("Expected ArgumentException was not thrown.");
			}
			catch { }

			// Invalid string to bool conversion
			try
			{
				Helpers.CastToNativeType("field", typeof(bool), "bin", "not a bool");
				Assert.Fail("Expected ArgumentException was not thrown.");
			}
			catch { }

			// Invalid string to Guid conversion
			try
			{
				Helpers.CastToNativeType("field", typeof(Guid), "bin", "not-a-guid");
				Assert.Fail("Expected ArgumentException was not thrown.");
			}
			catch { }

			// Invalid string to DateTime conversion
			try
			{
				Helpers.CastToNativeType("field", typeof(DateTime), "bin", "invalid date");
				Assert.Fail("Expected ArgumentException was not thrown.");
			}
			catch { }

			// Invalid JSON string
			try
			{
				Helpers.CastToNativeType("field", typeof(JObject), "bin", "{invalid json");
				Assert.Fail("Expected ArgumentException was not thrown.");
			}
			catch { }

			// Incompatible type conversion
			try
			{
				Helpers.CastToNativeType("field", typeof(int), "bin", new List<string>());
				Assert.Fail("Expected ArgumentException was not thrown.");
			}
			catch { }

			// Null to non-nullable value type
			try
			{
				Helpers.CastToNativeType("field", typeof(int), "bin", null);
				Assert.Fail("Expected ArgumentException was not thrown.");
			}
			catch { }

			// Invalid enum value
			try
			{
				Helpers.CastToNativeType("field", typeof(DayOfWeek), "bin", "InvalidDay");
				Assert.Fail("Expected ArgumentException was not thrown.");
			} catch { }

			// Unsupported conversion from DateTime
			try
			{
				Helpers.CastToNativeType("field", typeof(int), "bin", DateTime.Now);
				Assert.Fail("Expected ArgumentException was not thrown.");
			}
			catch { }

			// Unsupported conversion from TimeSpan
			try
			{
				Helpers.CastToNativeType("field", typeof(int), "bin", TimeSpan.FromHours(1));
				Assert.Fail("Expected ArgumentException was not thrown.");
			}
			catch { }
		}

		[TestMethod()]
		public void CastToNativeType_EdgeCases()
		{
			// Zero values
			Assert.AreEqual(0, Helpers.CastToNativeType("field", typeof(int), "bin", 0L));
			Assert.AreEqual(0.0, Helpers.CastToNativeType("field", typeof(double), "bin", 0L));
			Assert.AreEqual(false, Helpers.CastToNativeType("field", typeof(bool), "bin", 0L));

			// Very large numbers
			long maxLong = long.MaxValue;
			Assert.AreEqual(maxLong, Helpers.CastToNativeType("field", typeof(long), "bin", maxLong));
			Assert.AreEqual((ulong) maxLong, Helpers.CastToNativeType("field", typeof(ulong), "bin", maxLong));

			// Negative numbers
			long negativeLong = -12345L;
			Assert.AreEqual(-12345, Helpers.CastToNativeType("field", typeof(int), "bin", negativeLong));
			Assert.AreEqual(-12345.0, Helpers.CastToNativeType("field", typeof(double), "bin", negativeLong));

			// Empty collections
			var emptyList = new List<object>();
			var emptyArray = (int[]) Helpers.CastToNativeType("field", typeof(int[]), "bin", emptyList);
			Assert.AreEqual(0, emptyArray.Length);

			var emptyDict = new Dictionary<object, object>();
			var emptyDictResult = (Dictionary<string, object>) Helpers.CastToNativeType(
				"field", typeof(Dictionary<string, object>), "bin", emptyDict);
			Assert.AreEqual(0, emptyDictResult.Count);

			// Whitespace in strings
			Assert.AreEqual(123, Helpers.CastToNativeType("field", typeof(int), "bin", "  123  "));
			Assert.AreEqual(true, Helpers.CastToNativeType("field", typeof(bool), "bin", "  true  "));

			// Case insensitive conversions
			Assert.AreEqual(true, Helpers.CastToNativeType("field", typeof(bool), "bin", "TRUE"));
			Assert.AreEqual(false, Helpers.CastToNativeType("field", typeof(bool), "bin", "FALSE"));

			// Nested collections
			var nestedList = new List<object>
	{
		new List<object> { 1L, 2L },
		new List<object> { 3L, 4L }
	};
			var convertedNested = (List<List<int>>) Helpers.CastToNativeType(
				"field", typeof(List<List<int>>), "bin", nestedList);
			Assert.AreEqual(2, convertedNested.Count);
			Assert.AreEqual(2, convertedNested[0].Count);
			Assert.AreEqual(1, convertedNested[0][0]);
		}

		[TestMethod()]
		public void CastToNativeType_ComplexScenarios()
		{
			// Mixed-type collections with conversion
			var mixedList = new List<object> { 1L, "2", 3.0 };
			var intArrayFromMixed = (int[]) Helpers.CastToNativeType("field", typeof(int[]), "bin", mixedList);
			Assert.AreEqual(3, intArrayFromMixed.Length);
			Assert.AreEqual(1, intArrayFromMixed[0]);
			Assert.AreEqual(2, intArrayFromMixed[1]);
			Assert.AreEqual(3, intArrayFromMixed[2]);

			// Dictionary with type conversions
			var mixedDict = new Dictionary<object, object>
	{
		{ 1L, "one" },
		{ "2", 2L },
		{ 3.0, "three" }
	};
			var stringIntDict = (Dictionary<string, int>) Helpers.CastToNativeType(
				"field", typeof(Dictionary<string, int>), "bin",
				new Dictionary<object, object> { { "a", 1L }, { "b", 2L } });
			Assert.AreEqual(2, stringIntDict.Count);
			Assert.AreEqual(1, stringIntDict["a"]);
			Assert.AreEqual(2, stringIntDict["b"]);

			// JToken conversions
			var jtokenValue = JToken.FromObject(123);
			var jobj = (JObject) Helpers.CastToNativeType("field", typeof(JObject), "bin", new { name = "test", value = 123 });
			Assert.IsNotNull(jobj);

			// AValue with null value
			var nullAValue = new Extensions.AValue(null, "bin", "field");
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(string), "bin", nullAValue));
			Assert.IsNull(Helpers.CastToNativeType("field", typeof(int?), "bin", nullAValue));

			// AValue with nested conversion
			var nestedAValue = new Extensions.AValue(123L, "bin", "field");
			Assert.AreEqual(123.0f, (float) Helpers.CastToNativeType("field", typeof(float), "bin", nestedAValue), 0.01f);
		}

		[TestMethod()]
		public void CastToNativeType_GenericCollections()
		{
			// List<int> to int[]
			var intListGeneric = new List<int> { 1, 2, 3 };
			var arrayFromGenericList = (int[]) Helpers.CastToNativeType("field", typeof(int[]), "bin", intListGeneric);
			Assert.AreEqual(3, arrayFromGenericList.Length);
			Assert.AreEqual(1, arrayFromGenericList[0]);
			Assert.AreEqual(2, arrayFromGenericList[1]);
			Assert.AreEqual(3, arrayFromGenericList[2]);

			// List<int> to List<int> (direct assignment)
			var listFromGenericList = (List<int>) Helpers.CastToNativeType("field", typeof(List<int>), "bin", intListGeneric);
			Assert.AreEqual(3, listFromGenericList.Count);
			Assert.AreEqual(1, listFromGenericList[0]);

			// List<int> to IList<int>
			var ilistFromGenericList = (IList<int>) Helpers.CastToNativeType("field", typeof(IList<int>), "bin", intListGeneric);
			Assert.AreEqual(3, ilistFromGenericList.Count);

			// List<int> to IEnumerable<int>
			var enumerableFromGenericList = (IEnumerable<int>) Helpers.CastToNativeType("field", typeof(IEnumerable<int>), "bin", intListGeneric);
			Assert.AreEqual(3, enumerableFromGenericList.Count());

			// Nested generic collections
			var nestedGenericList = new List<List<int>> { new List<int> { 1, 2 }, new List<int> { 3, 4 } };
			var convertedNestedGeneric = (List<List<int>>) Helpers.CastToNativeType(
				"field", typeof(List<List<int>>), "bin", nestedGenericList);
			Assert.AreEqual(2, convertedNestedGeneric.Count);
			Assert.AreEqual(2, convertedNestedGeneric[0].Count);
			Assert.AreEqual(1, convertedNestedGeneric[0][0]);

			// Dictionary with non-object generic types
			var intStringDict = new Dictionary<int, string> { { 1, "one" }, { 2, "two" } };
			var convertedIntStringDict = (Dictionary<int, string>) Helpers.CastToNativeType(
				"field", typeof(Dictionary<int, string>), "bin", intStringDict);
			Assert.AreEqual(2, convertedIntStringDict.Count);
			Assert.AreEqual("one", convertedIntStringDict[1]);

			// Dictionary to IDictionary<K,V>
			var idictFromDict = (IDictionary<int, string>) Helpers.CastToNativeType(
				"field", typeof(IDictionary<int, string>), "bin", intStringDict);
			Assert.AreEqual(2, idictFromDict.Count);
			Assert.AreEqual("two", idictFromDict[2]);
		}

		[TestMethod()]
		public void CastToNativeType_AdvancedStringParsing()
		{
			// Scientific notation
			Assert.AreEqual(12345.0, Helpers.CastToNativeType("field", typeof(double), "bin", "123.45e2"));

			// Case-insensitive bool (already partially covered, but add explicit tests)
			Assert.AreEqual(true, Helpers.CastToNativeType("field", typeof(bool), "bin", "TRUE"));
			Assert.AreEqual(false, Helpers.CastToNativeType("field", typeof(bool), "bin", "FALSE"));
		}

		[TestMethod()]
		public void CastToNativeType_IConvertibleAdvanced()
		{
			IConvertible convertible = 123;

			var intFromConvertible = (int) Helpers.CastToNativeType("field", typeof(int), "bin", convertible);
			Assert.IsInstanceOfType(intFromConvertible, typeof(int));

			var dFromConvertible = (decimal) Helpers.CastToNativeType("field", typeof(decimal), "bin", convertible);
			Assert.IsInstanceOfType(dFromConvertible, typeof(decimal));

			// IConvertible to Guid
			IConvertible guidConvertible = Guid.NewGuid().ToString();
			var guidFromConvertible = (Guid) Helpers.CastToNativeType("field", typeof(Guid), "bin", guidConvertible);
			Assert.IsInstanceOfType(guidFromConvertible, typeof(Guid));
		}

		[TestMethod()]
		public void ConvertToAerospikeTypeTest()
		{
			// Null handling
			Assert.IsNull(Helpers.ConvertToAerospikeType(null));

			// Primitive types - should pass through unchanged
			Assert.AreEqual(123, Helpers.ConvertToAerospikeType(123));
			Assert.AreEqual(123L, Helpers.ConvertToAerospikeType(123L));
			Assert.AreEqual(123.45, Helpers.ConvertToAerospikeType(123.45));
			Assert.AreEqual(123.45f, Helpers.ConvertToAerospikeType(123.45f));
			Assert.AreEqual(true, Helpers.ConvertToAerospikeType(true));
			Assert.AreEqual((byte) 10, Helpers.ConvertToAerospikeType((byte) 10));
			Assert.AreEqual((short) 10, Helpers.ConvertToAerospikeType((short) 10));

			// String - should pass through unchanged
			Assert.AreEqual("test", Helpers.ConvertToAerospikeType("test"));
			Assert.AreEqual(string.Empty, Helpers.ConvertToAerospikeType(string.Empty));

			// Decimal - should convert to double
			decimal decValue = 123.45m;
			var convertedDec = Helpers.ConvertToAerospikeType(decValue);
			Assert.IsInstanceOfType(convertedDec, typeof(double));
			Assert.AreEqual(123.45, (double) convertedDec, 0.001);

			// Enum - should convert to string
			DayOfWeek day = DayOfWeek.Monday;
			var convertedEnum = Helpers.ConvertToAerospikeType(day);
			Assert.IsInstanceOfType(convertedEnum, typeof(string));
			Assert.AreEqual("Monday", convertedEnum);

			// Guid - should convert to string
			var guid = Guid.NewGuid();
			var convertedGuid = Helpers.ConvertToAerospikeType(guid);
			Assert.IsInstanceOfType(convertedGuid, typeof(string));
			Assert.AreEqual(guid.ToString(), convertedGuid);

			// byte[] - should pass through unchanged
			byte[] byteArray = new byte[] { 1, 2, 3 };
			var convertedBytes = Helpers.ConvertToAerospikeType(byteArray);
			Assert.AreSame(byteArray, convertedBytes);

			// Aerospike Value - should extract Object
			var asValue = Value.Get(123);
			var convertedValue = Helpers.ConvertToAerospikeType(asValue);
			Assert.IsInstanceOfType(convertedValue, typeof(Value<int>));
			Assert.AreEqual(123L, ((Value<int>)convertedValue).value);

			// Aerospike Bin - should extract value's Object
			var bin = new Bin("testBin", 456);
			var convertedBin = Helpers.ConvertToAerospikeType(bin);
			Assert.AreEqual(456, convertedBin);
		}

		[TestMethod()]
		public void ConvertToAerospikeType_DateTimeConversions()
		{
			var dt = new DateTime(2024, 1, 15, 10, 30, 0);
			var dto = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.FromHours(-5));
			var ts = TimeSpan.FromHours(2.5);

			// Test with AllDateTimeUseUnixEpochNano = false (default)
			var previousSetting = Helpers.AllDateTimeUseUnixEpochNano;
			try
			{
				Helpers.AllDateTimeUseUnixEpochNano = false;

				// DateTime should convert to string
				var convertedDt = Helpers.ConvertToAerospikeType(dt);
				Assert.IsInstanceOfType(convertedDt, typeof(string));
				Assert.AreEqual(dt.ToString(Helpers.DateTimeFormat), convertedDt);

				// DateTimeOffset should convert to string
				var convertedDto = Helpers.ConvertToAerospikeType(dto);
				Assert.IsInstanceOfType(convertedDto, typeof(string));
				Assert.AreEqual(dto.ToString(Helpers.DateTimeOffsetFormat), convertedDto);

				// TimeSpan should convert to string
				var convertedTs = Helpers.ConvertToAerospikeType(ts);
				Assert.IsInstanceOfType(convertedTs, typeof(string));
				Assert.AreEqual(ts.ToString(Helpers.TimeSpanFormat), convertedTs);

				// Test with AllDateTimeUseUnixEpochNano = true
				Helpers.AllDateTimeUseUnixEpochNano = true;

				// DateTime should convert to long (nanos from epoch)
				var convertedDtNano = Helpers.ConvertToAerospikeType(dt);
				Assert.IsInstanceOfType(convertedDtNano, typeof(long));
				Assert.AreEqual(Helpers.NanosFromEpoch(dt), convertedDtNano);

				// DateTimeOffset should convert to long (nanos from epoch)
				var convertedDtoNano = Helpers.ConvertToAerospikeType(dto);
				Assert.IsInstanceOfType(convertedDtoNano, typeof(long));
				Assert.AreEqual(Helpers.NanosFromEpoch(dto.UtcDateTime), convertedDtoNano);

				// TimeSpan should convert to long (total milliseconds * 1000000)
				var convertedTsNano = Helpers.ConvertToAerospikeType(ts);
				Assert.IsInstanceOfType(convertedTsNano, typeof(long));
				Assert.AreEqual((long) ts.TotalMilliseconds * 1000000L, convertedTsNano);
			}
			finally
			{
				Helpers.AllDateTimeUseUnixEpochNano = previousSetting;
			}
		}

		[TestMethod()]
		public void ConvertToAerospikeType_Collections()
		{
			// List with primitive types - should pass through
			var primitiveList = new List<int> { 1, 2, 3 };
			var convertedPrimitiveList = Helpers.ConvertToAerospikeType(primitiveList);
			Assert.AreSame(primitiveList, convertedPrimitiveList);

			// List with non-Aerospike types - should convert elements
			var dateList = new List<DateTime> { DateTime.Now, DateTime.Now.AddDays(1) };
			var convertedDateList = Helpers.ConvertToAerospikeType(dateList);
			Assert.IsInstanceOfType(convertedDateList, typeof(List<object>));
			var resultList = (List<object>) convertedDateList;
			Assert.AreEqual(2, resultList.Count);
			Assert.IsInstanceOfType(resultList[0], typeof(string));

			// List with mixed types
			var mixedList = new List<object> { 1, "test", DateTime.Now };
			var convertedMixedList = Helpers.ConvertToAerospikeType(mixedList);
			Assert.IsInstanceOfType(convertedMixedList, typeof(List<object>));

			// Empty list
			var emptyList = new List<Guid>();
			var convertedEmpty = Helpers.ConvertToAerospikeType(emptyList);
			Assert.IsInstanceOfType(convertedEmpty, typeof(List<object>));
			Assert.AreEqual(0, ((List<object>) convertedEmpty).Count);

			// Array with primitives - should pass through
			int[] intArray = new int[] { 1, 2, 3 };
			var convertedArray = Helpers.ConvertToAerospikeType(intArray);
			CollectionAssert.AreEquivalent(intArray, (List<object>)convertedArray);
		}

		[TestMethod()]
		public void ConvertToAerospikeType_Dictionaries()
		{
			// Dictionary<string, primitive> - should pass through
			var primitiveDict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
			var convertedPrimitiveDict = Helpers.ConvertToAerospikeType(primitiveDict);
			Assert.AreSame(primitiveDict, convertedPrimitiveDict);

			// Dictionary<string, non-Aerospike> - should convert values
			var guidDict = new Dictionary<string, Guid>
	{
		{ "id1", Guid.NewGuid() },
		{ "id2", Guid.NewGuid() }
	};
			var convertedGuidDict = Helpers.ConvertToAerospikeType(guidDict);
			Assert.IsInstanceOfType(convertedGuidDict, typeof(Dictionary<string, object>));
			var resultDict = (Dictionary<string, object>) convertedGuidDict;
			Assert.AreEqual(2, resultDict.Count);
			Assert.IsInstanceOfType(resultDict["id1"], typeof(string));

			// Dictionary<object, object> with non-Aerospike keys/values
			var objDict = new Dictionary<object, object>
	{
		{ Guid.NewGuid(), DateTime.Now },
		{ 1, "test" }
	};
			var convertedObjDict = Helpers.ConvertToAerospikeType(objDict);
			Assert.IsInstanceOfType(convertedObjDict, typeof(Dictionary<object, object>));
			var resultObjDict = (Dictionary<object, object>) convertedObjDict;
			Assert.AreEqual(2, resultObjDict.Count);

			// Empty dictionary
			var emptyDict = new Dictionary<string, Guid>();
			var convertedEmptyDict = Helpers.ConvertToAerospikeType(emptyDict);
			Assert.IsInstanceOfType(convertedEmptyDict, typeof(Dictionary<string, object>));
			Assert.AreEqual(0, ((Dictionary<string, object>) convertedEmptyDict).Count);
		}

		[TestMethod()]
		public void ConvertToAerospikeType_JsonTypes()
		{
			// JObject - should convert to dictionary
			var jobj = new JObject
	{
		{ "name", "test" },
		{ "value", 123 }
	};
			var convertedJObj = Helpers.ConvertToAerospikeType(jobj);
			Assert.IsInstanceOfType(convertedJObj, typeof(Dictionary<string, object>));
			var dictFromJObj = (Dictionary<string, object>) convertedJObj;
			Assert.AreEqual(2, dictFromJObj.Count);
			Assert.AreEqual("test", dictFromJObj["name"]);

			// JArray - should convert to list
			var jarr = new JArray { 1, 2, 3 };
			var convertedJArr = Helpers.ConvertToAerospikeType(jarr);
			Assert.IsInstanceOfType(convertedJArr, typeof(List<object>));
			var listFromJArr = (List<object>) convertedJArr;
			Assert.AreEqual(3, listFromJArr.Count);

			// JValue - should extract underlying value
			var jval = new JValue(123);
			var convertedJVal = Helpers.ConvertToAerospikeType(jval);
			Assert.AreEqual(123L, convertedJVal);

			// JProperty - should convert to dictionary entry
			var jprop = new JProperty("key", "value");
			var convertedJProp = Helpers.ConvertToAerospikeType(jprop);
			Assert.IsInstanceOfType(convertedJProp, typeof(Dictionary<string, object>));
		}

		[TestMethod()]
		public void ConvertToAerospikeType_AValueAndARecord()
		{
			// AValue wrapping primitive - should extract and pass through
			var aValuePrimitive = new Extensions.AValue(123, "bin", "field");
			var convertedAValuePrim = Helpers.ConvertToAerospikeType(aValuePrimitive);
			Assert.AreEqual(123, convertedAValuePrim);

			// AValue wrapping non-Aerospike type - should convert
			var aValueGuid = new Extensions.AValue(Guid.NewGuid(), "bin", "field");
			var convertedAValueGuid = Helpers.ConvertToAerospikeType(aValueGuid);
			Assert.IsInstanceOfType(convertedAValueGuid, typeof(string));

			// AValue wrapping null
			var aValueNull = new Extensions.AValue(null, "bin", "field");
			var convertedAValueNull = Helpers.ConvertToAerospikeType(aValueNull);
			Assert.IsNull(convertedAValueNull);
		}

		[TestMethod()]
		public void ConvertToAerospikeType_NestedCollections()
		{
			// Nested list with non-Aerospike types
			var nestedList = new List<List<Guid>>
	{
		new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
		new List<Guid> { Guid.NewGuid() }
	};
			var convertedNested = Helpers.ConvertToAerospikeType(nestedList);
			Assert.IsInstanceOfType(convertedNested, typeof(List<object>));
			var resultNested = (List<object>) convertedNested;
			Assert.AreEqual(2, resultNested.Count);
			Assert.IsInstanceOfType(resultNested[0], typeof(List<object>));

			var innerList = (List<object>) resultNested[0];
			Assert.AreEqual(2, innerList.Count);
			Assert.IsInstanceOfType(innerList[0], typeof(string));

			// Nested dictionary
			var nestedDict = new Dictionary<string, Dictionary<string, DateTime>>
	{
		{ "dates", new Dictionary<string, DateTime> { { "start", DateTime.Now } } }
	};
			var convertedNestedDict = Helpers.ConvertToAerospikeType(nestedDict);
			Assert.IsInstanceOfType(convertedNestedDict, typeof(Dictionary<string, object>));
			var resultNestedDict = (Dictionary<string, object>) convertedNestedDict;
			Assert.IsInstanceOfType(resultNestedDict["dates"], typeof(Dictionary<string, object>));
		}

		[TestMethod()]
		public void ConvertToAerospikeType_ComplexScenarios()
		{
			// Mixed collection with various types
			var complexList = new List<object>
	{
		123,
		"test",
		DateTime.Now,
		Guid.NewGuid(),
		DayOfWeek.Friday,
		new List<int> { 1, 2, 3 },
		new Dictionary<string, string> { { "key", "value" } }
	};
			var convertedComplex = Helpers.ConvertToAerospikeType(complexList);
			Assert.IsInstanceOfType(convertedComplex, typeof(List<object>));
			var resultComplex = (List<object>) convertedComplex;
			Assert.AreEqual(7, resultComplex.Count);
			Assert.IsInstanceOfType(resultComplex[2], typeof(string)); // DateTime
			Assert.IsInstanceOfType(resultComplex[3], typeof(string)); // Guid
			Assert.IsInstanceOfType(resultComplex[4], typeof(string)); // Enum

			// Dictionary with mixed value types
			var complexDict = new Dictionary<string, object>
	{
		{ "int", 123 },
		{ "string", "test" },
		{ "date", DateTime.Now },
		{ "guid", Guid.NewGuid() },
		{ "list", new List<int> { 1, 2 } }
	};
			var convertedComplexDict = Helpers.ConvertToAerospikeType(complexDict);
			Assert.IsInstanceOfType(convertedComplexDict, typeof(Dictionary<string, object>));
			var resultComplexDict = (Dictionary<string, object>) convertedComplexDict;
			Assert.IsInstanceOfType(resultComplexDict["date"], typeof(string));
			Assert.IsInstanceOfType(resultComplexDict["guid"], typeof(string));
		}

		[TestMethod()]
		public void ConvertToAerospikeType_EdgeCases()
		{
			// Zero values
			Assert.AreEqual(0, Helpers.ConvertToAerospikeType(0));
			Assert.AreEqual(0L, Helpers.ConvertToAerospikeType(0L));
			Assert.AreEqual(0.0, Helpers.ConvertToAerospikeType(0.0));

			// Negative numbers
			Assert.AreEqual(-123, Helpers.ConvertToAerospikeType(-123));
			Assert.AreEqual(-123.45, Helpers.ConvertToAerospikeType(-123.45));

			// Max/Min values
			Assert.AreEqual(int.MaxValue, Helpers.ConvertToAerospikeType(int.MaxValue));
			Assert.AreEqual(long.MaxValue, Helpers.ConvertToAerospikeType(long.MaxValue));
			Assert.AreEqual(double.MaxValue, Helpers.ConvertToAerospikeType(double.MaxValue));

			// Empty string
			Assert.AreEqual(string.Empty, Helpers.ConvertToAerospikeType(string.Empty));

			// Whitespace string
			Assert.AreEqual("  ", Helpers.ConvertToAerospikeType("  "));

			// Special double values
			Assert.AreEqual(double.NaN, Helpers.ConvertToAerospikeType(double.NaN));
			Assert.AreEqual(double.PositiveInfinity, Helpers.ConvertToAerospikeType(double.PositiveInfinity));
			Assert.AreEqual(double.NegativeInfinity, Helpers.ConvertToAerospikeType(double.NegativeInfinity));

			// Empty Guid
			var emptyGuid = Guid.Empty;
			var convertedEmptyGuid = Helpers.ConvertToAerospikeType(emptyGuid);
			Assert.AreEqual(emptyGuid.ToString(), convertedEmptyGuid);
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