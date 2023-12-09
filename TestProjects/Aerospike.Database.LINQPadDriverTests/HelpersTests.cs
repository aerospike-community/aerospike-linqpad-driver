using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aerospike.Database.LINQPadDriver;
using System;
using System.Collections.Generic;
using System.Text;
using static Aerospike.Database.LINQPadDriver.Tests.HelpersTests;

namespace Aerospike.Database.LINQPadDriver.Tests
{
    [TestClass()]
    public class HelpersTests
    {
        [TestMethod()]
        public void EqualsTest()
        {

            Assert.IsTrue(Helpers.Equals(1, 1));
            Assert.IsTrue(Helpers.Equals(1l, (short) 1));
            Assert.IsTrue(Helpers.Equals((short) 1, 1l));
            Assert.IsFalse(Helpers.Equals(1l, (short)2));
            Assert.IsFalse(Helpers.Equals((short)1, 2l));

            Assert.IsTrue(Helpers.Equals(1l, 1d));
            Assert.IsTrue(Helpers.Equals(1d, 1l));
            Assert.IsTrue(Helpers.Equals(1d, (short)1));
            Assert.IsTrue(Helpers.Equals((short)1, 1d));
            Assert.IsTrue(Helpers.Equals(1l, 1m));
            Assert.IsTrue(Helpers.Equals(1m, 1l));
            Assert.IsTrue(Helpers.Equals(1m, (short)1));
            Assert.IsTrue(Helpers.Equals((short)1, 1m));
            Assert.IsTrue(Helpers.Equals(1m, 1d));
            Assert.IsTrue(Helpers.Equals(1d, 1m));

            Assert.IsFalse(Helpers.Equals(2l, 1d));
            Assert.IsFalse(Helpers.Equals(2d, 1l));
            Assert.IsFalse(Helpers.Equals(2d, (short)1));
            Assert.IsFalse(Helpers.Equals((short)2, 1d));
            Assert.IsFalse(Helpers.Equals(2l, 1m));
            Assert.IsFalse(Helpers.Equals(2m, 1l));
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

            Assert.IsFalse(Helpers.Equals(1l, dtNow));
            Assert.IsFalse(Helpers.Equals(dtNow, 1l));

            Assert.IsFalse(Helpers.Equals(1, dtNow));
            Assert.IsFalse(Helpers.Equals(dtNow, 1));

            Assert.IsTrue(Helpers.Equals("10", "10"));
            Assert.IsFalse(Helpers.Equals("10", "ab"));
            Assert.IsFalse(Helpers.Equals(1, "1"));
            Assert.IsFalse(Helpers.Equals("1", 1));

        }

        interface TestInterface
        {

        }

        public class TestClass : TestInterface
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

            expected = "HelpersTests.TestInterface";
            result = Helpers.GetRealTypeName(typeof(TestInterface));

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
    }
}