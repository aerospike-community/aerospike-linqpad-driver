using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aerospike.Client;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
	public sealed class TransRecords //: SetRecords
	{

		public TransRecords(SetRecords set)
		{

		}

		public Txn Txn { get; }
		public Policy Policy { get; }
		public WritePolicy WritePolicy { get; }
		public QueryPolicy QueryPolicy { get; }
		public long Id => Txn.Id;
	}
}
