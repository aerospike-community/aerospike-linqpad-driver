using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aerospike.Database.LINQPadDriver;
using System;
using System.Collections.Generic;
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
    }
}