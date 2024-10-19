using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aerospike.Database.LINQPadDriver.Extensions.Tests
{
	[TestClass()]
	public class NamespaceSetTests
	{
		[TestMethod]
		public void CloneCopyTestNS()
		{
			var ns = new ANamespaceAccess("myNamespace", new [] { "Bina", "Binb", "Binc", "Bina", "Binc", "Bine", "Bind", "Bind" });

			Assert.IsNotNull(ns);
			Assert.AreEqual("myNamespace", ns.Namespace);
			Assert.IsNotNull(ns.Sets);
			Assert.AreEqual(0, ns.Sets.Count());
			Assert.IsNull(ns["none"]);
			CollectionAssert.AreEqual(new []{ "Bina", "Binb", "Binc", "Bine", "Bind" },
										ns.BinNames);
			
			Assert.IsNotNull(ns.DefaultQueryPolicy);
			Assert.IsNotNull(ns.DefaultWritePolicy);
			Assert.IsNotNull(ns.DefaultReadPolicy);
			Assert.IsNotNull(ns.DefaultScanPolicy);

			ns.DefaultScanPolicy.maxRetries = 1;
			ns.DefaultQueryPolicy.maxRetries = 2;
			ns.DefaultReadPolicy.maxRetries = 3;
			ns.DefaultWritePolicy.maxRetries = 4;

			Assert.AreEqual(2, ns.DefaultQueryPolicy.maxRetries);
			Assert.AreEqual(4, ns.DefaultWritePolicy.maxRetries);
			Assert.AreEqual(3, ns.DefaultReadPolicy.maxRetries);
			Assert.AreEqual(1, ns.DefaultScanPolicy.maxRetries);

			Assert.IsNull(ns.DefaultQueryPolicy.Txn);
			Assert.IsNull(ns.DefaultWritePolicy.Txn);
			Assert.IsNull(ns.DefaultReadPolicy.Txn);
			Assert.IsNull(ns.DefaultScanPolicy.Txn);

			Assert.AreEqual(DBPlatforms.None, ns.DBPlatform);

			var nsSet = new SetRecords(ns, "mySet1", "Bina", "Binb", "Binc", "Bine", "Bind");

			Assert.AreEqual("mySet1", nsSet.SetName);
			Assert.AreEqual(5, nsSet.BinNames.Length);
			CollectionAssert.AreEqual(new[] { "Bina", "Binb", "Binc", "Bine", "Bind" },
										nsSet.BinNames);

			Assert.AreEqual(0, ns.Sets.Count());
			Assert.IsNull(ns["mySet1"]);

			var clonens = ns.Clone();

			Assert.IsInstanceOfType(clonens, typeof(ANamespaceAccess));
			Assert.AreNotEqual(ns, clonens);

			Assert.AreEqual(ns.Namespace, clonens.Namespace);
			Assert.IsNotNull(clonens.Sets);
			Assert.AreEqual(ns.Sets.Count(), clonens.Sets.Count());
			CollectionAssert.AreEqual(ns.BinNames,
										clonens.BinNames);

			Assert.IsNotNull(clonens.DefaultQueryPolicy);
			Assert.IsNotNull(clonens.DefaultWritePolicy);
			Assert.IsNotNull(clonens.DefaultReadPolicy);
			Assert.IsNotNull(clonens.DefaultScanPolicy);

			Assert.AreEqual(ns.DefaultQueryPolicy.maxRetries, clonens.DefaultQueryPolicy.maxRetries);
			Assert.AreEqual(ns.DefaultWritePolicy.maxRetries, clonens.DefaultWritePolicy.maxRetries);
			Assert.AreEqual(ns.DefaultReadPolicy.maxRetries, clonens.DefaultReadPolicy.maxRetries);
			Assert.AreEqual(ns.DefaultScanPolicy.maxRetries, clonens.DefaultScanPolicy.maxRetries);

			Assert.AreEqual(ns.DefaultQueryPolicy.Txn, clonens.DefaultQueryPolicy.Txn);
			Assert.AreEqual(ns.DefaultWritePolicy.Txn, clonens.DefaultWritePolicy.Txn);
			Assert.AreEqual(ns.DefaultReadPolicy.Txn, clonens.DefaultReadPolicy.Txn);
			Assert.AreEqual(ns.DefaultScanPolicy.Txn, clonens.DefaultScanPolicy.Txn);

			Assert.AreEqual(ns.DBPlatform, clonens.DBPlatform);

			clonens.DefaultQueryPolicy.maxRetries = 5;
			clonens.DefaultWritePolicy.maxRetries = 6;
			clonens.DefaultReadPolicy.maxRetries = 7;
			clonens.DefaultScanPolicy.maxRetries = 8;

			Assert.AreEqual(5, clonens.DefaultQueryPolicy.maxRetries);
			Assert.AreEqual(6, clonens.DefaultWritePolicy.maxRetries);
			Assert.AreEqual(7, clonens.DefaultReadPolicy.maxRetries);
			Assert.AreEqual(8, clonens.DefaultScanPolicy.maxRetries);

			Assert.AreNotEqual(ns.DefaultQueryPolicy.maxRetries, clonens.DefaultQueryPolicy.maxRetries);
			Assert.AreNotEqual(ns.DefaultWritePolicy.maxRetries, clonens.DefaultWritePolicy.maxRetries);
			Assert.AreNotEqual(ns.DefaultReadPolicy.maxRetries, clonens.DefaultReadPolicy.maxRetries);
			Assert.AreNotEqual(ns.DefaultScanPolicy.maxRetries, clonens.DefaultScanPolicy.maxRetries);

		}

		[TestMethod]
		public void CloneCopyTestSet()
		{
			var ns = new ANamespaceAccess("myNamespace", new[] { "Bina", "Binb", "Binc", "Bina", "Binc", "Bine", "Bind", "Bind" });

			ns.DefaultScanPolicy.maxRetries = 1;
			ns.DefaultQueryPolicy.maxRetries = 2;
			ns.DefaultReadPolicy.maxRetries = 3;
			ns.DefaultWritePolicy.maxRetries = 4;

			var nsSet = new SetRecords(ns, "mySet1", "Bina", "Binb", "Binc", "Bine", "Bind", "Bina");

			Assert.AreEqual("mySet1", nsSet.SetName);
			Assert.AreEqual(5, nsSet.BinNames.Length);
			CollectionAssert.AreEqual(new[] { "Bina", "Binb", "Binc", "Bine", "Bind" },
										nsSet.BinNames);

			Assert.AreEqual("myNamespace", nsSet.Namespace);
			Assert.AreEqual("myNamespace.mySet1", nsSet.SetFullName);
			Assert.AreEqual(ns, nsSet.SetAccess);

			Assert.IsNotNull(nsSet.DefaultQueryPolicy);
			Assert.IsNotNull(nsSet.DefaultWritePolicy);
			Assert.IsNotNull(nsSet.DefaultReadPolicy);
			Assert.IsNotNull(nsSet.DefaultScanPolicy);

			Assert.AreEqual(2, nsSet.DefaultQueryPolicy.maxRetries);
			Assert.AreEqual(4, nsSet.DefaultWritePolicy.maxRetries);
			Assert.AreEqual(3, nsSet.DefaultReadPolicy.maxRetries);
			Assert.AreEqual(1, nsSet.DefaultScanPolicy.maxRetries);

			Assert.IsNull(nsSet.DefaultQueryPolicy.Txn);
			Assert.IsNull(nsSet.DefaultWritePolicy.Txn);
			Assert.IsNull(nsSet.DefaultReadPolicy.Txn);
			Assert.IsNull(nsSet.DefaultScanPolicy.Txn);

			Assert.AreEqual(0, nsSet.AsEnumerable().Count());

			Assert.AreNotEqual(0, nsSet.BinsHashCode);

			var cloneNSSet = nsSet.Clone();

			Assert.AreEqual(nsSet, cloneNSSet);
			Assert.IsInstanceOfType<SetRecords>(cloneNSSet);
			Assert.AreEqual(nsSet.SetName, cloneNSSet.SetName);
			Assert.AreEqual(nsSet.BinNames.Length, cloneNSSet.BinNames.Length);
			CollectionAssert.AreEqual(nsSet.BinNames, cloneNSSet.BinNames);

			Assert.AreEqual(nsSet.Namespace, cloneNSSet.Namespace);
			Assert.AreEqual(nsSet.SetFullName, cloneNSSet.SetFullName);
			Assert.AreEqual(nsSet.SetAccess, cloneNSSet.SetAccess);

			Assert.IsNotNull(cloneNSSet.DefaultQueryPolicy);
			Assert.IsNotNull(cloneNSSet.DefaultWritePolicy);
			Assert.IsNotNull(cloneNSSet.DefaultReadPolicy);
			Assert.IsNotNull(cloneNSSet.DefaultScanPolicy);

			Assert.AreEqual(nsSet.DefaultQueryPolicy.maxRetries, cloneNSSet.DefaultQueryPolicy.maxRetries);
			Assert.AreEqual(nsSet.DefaultWritePolicy.maxRetries, cloneNSSet.DefaultWritePolicy.maxRetries);
			Assert.AreEqual(nsSet.DefaultReadPolicy.maxRetries, cloneNSSet.DefaultReadPolicy.maxRetries);
			Assert.AreEqual(nsSet.DefaultScanPolicy.maxRetries, cloneNSSet.DefaultScanPolicy.maxRetries);

			Assert.AreEqual(nsSet.DefaultQueryPolicy.Txn, cloneNSSet.DefaultQueryPolicy.Txn);
			Assert.AreEqual(nsSet.DefaultWritePolicy.Txn, cloneNSSet.DefaultWritePolicy.Txn);
			Assert.AreEqual(nsSet.DefaultReadPolicy.Txn, cloneNSSet.DefaultReadPolicy.Txn);
			Assert.AreEqual(nsSet.DefaultScanPolicy.Txn, cloneNSSet.DefaultScanPolicy.Txn);

			Assert.AreEqual(nsSet.AsEnumerable().Count(), cloneNSSet.AsEnumerable().Count());

			Assert.AreEqual(nsSet.BinsHashCode, cloneNSSet.BinsHashCode);

			cloneNSSet.DefaultQueryPolicy.maxRetries = 5;
			cloneNSSet.DefaultWritePolicy.maxRetries = 6;
			cloneNSSet.DefaultReadPolicy.maxRetries = 7;
			cloneNSSet.DefaultScanPolicy.maxRetries = 8;

			Assert.AreEqual(5, cloneNSSet.DefaultQueryPolicy.maxRetries);
			Assert.AreEqual(6, cloneNSSet.DefaultWritePolicy.maxRetries);
			Assert.AreEqual(7, cloneNSSet.DefaultReadPolicy.maxRetries);
			Assert.AreEqual(8, cloneNSSet.DefaultScanPolicy.maxRetries);

			Assert.AreNotEqual(nsSet.DefaultQueryPolicy.maxRetries, cloneNSSet.DefaultQueryPolicy.maxRetries);
			Assert.AreNotEqual(nsSet.DefaultWritePolicy.maxRetries, cloneNSSet.DefaultWritePolicy.maxRetries);
			Assert.AreNotEqual(nsSet.DefaultReadPolicy.maxRetries, cloneNSSet.DefaultReadPolicy.maxRetries);
			Assert.AreNotEqual(nsSet.DefaultScanPolicy.maxRetries, cloneNSSet.DefaultScanPolicy.maxRetries);

		}

		[TestMethod]
		public void MRTTestNS()
		{
			var ns = new ANamespaceAccess("myNamespace", new[] { "Bina", "Binb", "Binc", "Bina", "Binc", "Bine", "Bind", "Bind" });
			
			Assert.IsNull(ns.DefaultQueryPolicy.Txn);
			Assert.IsNull(ns.DefaultWritePolicy.Txn);
			Assert.IsNull(ns.DefaultReadPolicy.Txn);
			Assert.IsNull(ns.DefaultScanPolicy.Txn);

			Assert.IsFalse(ns.TransactionId.HasValue);
			Assert.IsNull(ns.AerospikeTxn);

			var txnNS = ns.CreateTransaction();

			Assert.IsNull(ns.DefaultQueryPolicy.Txn);
			Assert.IsNull(ns.DefaultWritePolicy.Txn);
			Assert.IsNull(ns.DefaultReadPolicy.Txn);
			Assert.IsNull(ns.DefaultScanPolicy.Txn);

			Assert.IsFalse(ns.TransactionId.HasValue);
			Assert.IsNull(ns.AerospikeTxn);

			Assert.IsNotNull(txnNS.DefaultQueryPolicy.Txn);
			Assert.IsNotNull(txnNS.DefaultWritePolicy.Txn);
			Assert.IsNotNull(txnNS.DefaultReadPolicy.Txn);
			Assert.IsNotNull(txnNS.DefaultScanPolicy.Txn);

			Assert.IsTrue(txnNS.TransactionId.HasValue);
			Assert.IsNotNull(txnNS.AerospikeTxn);

		}

		[TestMethod]
		public void MRTTestSet()
		{
			var ns = new ANamespaceAccess("myNamespace", new[] { "Bina", "Binb", "Binc", "Bina", "Binc", "Bine", "Bind", "Bind" });

			Assert.IsNull(ns.DefaultQueryPolicy.Txn);
			Assert.IsNull(ns.DefaultWritePolicy.Txn);
			Assert.IsNull(ns.DefaultReadPolicy.Txn);
			Assert.IsNull(ns.DefaultScanPolicy.Txn);

			Assert.IsFalse(ns.TransactionId.HasValue);
			Assert.IsNull(ns.AerospikeTxn);

			var nsSet = new SetRecords(ns, "mySet1", "Bina", "Binb", "Binc", "Bine", "Bind", "Bina");

			Assert.IsNull(nsSet.DefaultQueryPolicy.Txn);
			Assert.IsNull(nsSet.DefaultWritePolicy.Txn);
			Assert.IsNull(nsSet.DefaultReadPolicy.Txn);
			Assert.IsNull(nsSet.DefaultScanPolicy.Txn);

			Assert.IsFalse(nsSet.TransactionId.HasValue);
			Assert.IsNull(nsSet.AerospikeTxn);

			var mrtSet = nsSet.CreateTransaction();

			Assert.IsNull(ns.DefaultQueryPolicy.Txn);
			Assert.IsNull(ns.DefaultWritePolicy.Txn);
			Assert.IsNull(ns.DefaultReadPolicy.Txn);
			Assert.IsNull(ns.DefaultScanPolicy.Txn);

			Assert.IsFalse(ns.TransactionId.HasValue);
			Assert.IsNull(ns.AerospikeTxn);

			Assert.IsNull(nsSet.DefaultQueryPolicy.Txn);
			Assert.IsNull(nsSet.DefaultWritePolicy.Txn);
			Assert.IsNull(nsSet.DefaultReadPolicy.Txn);
			Assert.IsNull(nsSet.DefaultScanPolicy.Txn);

			Assert.IsFalse(nsSet.TransactionId.HasValue);
			Assert.IsNull(nsSet.AerospikeTxn);

			Assert.IsNotNull(mrtSet.DefaultQueryPolicy.Txn);
			Assert.IsNotNull(mrtSet.DefaultWritePolicy.Txn);
			Assert.IsNotNull(mrtSet.DefaultReadPolicy.Txn);
			Assert.IsNotNull(mrtSet.DefaultScanPolicy.Txn);

			Assert.IsTrue(mrtSet.TransactionId.HasValue);
			Assert.IsNotNull(mrtSet.AerospikeTxn);

			var mrtSet2 = mrtSet.CreateTransaction();

			Assert.IsTrue(mrtSet2.TransactionId.HasValue);
			Assert.IsNotNull(mrtSet2.AerospikeTxn);
			Assert.AreNotEqual(mrtSet.TransactionId, mrtSet2.TransactionId);

		}

		[TestMethod]
		public void MRTTestNSSet()
		{
			var ns = new ANamespaceAccess("myNamespace", new[] { "Bina", "Binb", "Binc", "Bina", "Binc", "Bine", "Bind", "Bind" });

			Assert.IsFalse(ns.TransactionId.HasValue);
			Assert.IsNull(ns.AerospikeTxn);

			var mrtNS = ns.CreateTransaction();

			Assert.IsTrue(mrtNS.TransactionId.HasValue);
			Assert.IsNotNull(mrtNS.AerospikeTxn);

			var mrtSet1 = new SetRecords(mrtNS, "mySet1", "Bina", "Binb", "Binc", "Bine", "Bind", "Bina");

			Assert.IsTrue(mrtSet1.TransactionId.HasValue);
			Assert.IsNotNull(mrtSet1.AerospikeTxn);
		}
	}
}