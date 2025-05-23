// Ignore Spelling: Pnamespace

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aerospike.Client;
using LINQPad;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LPU = LINQPad.Util;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    /// <summary>
    /// The different Aerospike DB Platforms
    /// </summary>
    public enum DBPlatforms
    {
        None = -1,
        /// <summary>
        /// The non-managed platform
        /// </summary>
        Native = 0
    }

	/// <summary>
	/// A class used to define Aerospike Namespaces.
	/// </summary>
	[DebuggerDisplay("{ToString()}")]
	public class ANamespaceAccess
    {
		private readonly static List<ANamespaceAccess> ANamespacesList = new List<ANamespaceAccess>();

		#region Constructors
		private ANamespaceAccess(string ns,
                                    string[] binNames,
                                    AerospikeConnection dbConnection,
									Policy readPolicy,
									WritePolicy writePolicy,
									QueryPolicy queryPolicy,
									ScanPolicy scanPolicy,
                                    List<SetRecords> sets = null)
        {
			this.AerospikeConnection = dbConnection;
			this.Namespace = ns;
			this.BinNames = binNames is null
								? Array.Empty<string>()
								: Helpers.RemoveDups(binNames);

			this.DefaultWritePolicy = writePolicy ?? new WritePolicy();
			this.DefaultQueryPolicy = queryPolicy ?? new QueryPolicy();
			this.DefaultReadPolicy = readPolicy ?? new QueryPolicy();
			this.DefaultScanPolicy = scanPolicy ?? new ScanPolicy();

            if(sets is not null)
                this._sets = sets;
		}

		/// <summary>
		/// Used for a placeholder.
		/// </summary>
		/// <param name="ns">Namespace</param>
		/// <param name="binNames">A array of bin names associated to this namespace</param>
		public ANamespaceAccess(string ns, string[] binNames = null)
            : this(ns, binNames, null, null, null, null, null)
        {            
            lock(ANamespacesList)
            {
				ANamespacesList.RemoveAll(i => i.Namespace == this.Namespace);
				ANamespacesList.Add(this);
			}
		}

		public ANamespaceAccess(IDbConnection dbConnection, string ns, string[] binNames, bool sc)
			: this(dbConnection as AerospikeConnection,
                    ns,
					binNames,
                    sc)
		{ }

		public ANamespaceAccess(AerospikeConnection dbConnection, string ns, string[] binNames, bool sc)
			: this(ns,
                    binNames,
                    dbConnection,
					new QueryPolicy(dbConnection.AerospikeClient.QueryPolicyDefault),
					new WritePolicy(dbConnection.AerospikeClient.WritePolicyDefault),
					new QueryPolicy(dbConnection.AerospikeClient.QueryPolicyDefault),
					new ScanPolicy(dbConnection.AerospikeClient.ScanPolicyDefault))
		{            
			lock(ANamespacesList)
			{
				ANamespacesList.RemoveAll(i => i.Namespace == this.Namespace);
				ANamespacesList.Add(this);
			}
            this.IsStrongConsistencyMode = sc;
		}

        public ANamespaceAccess(IDbConnection dbConnection,
                                LPNamespace lpNamespace,
                                string ns,
                                string[] binNames,
                                bool sc)
            : this(dbConnection as AerospikeConnection,
					ns,
                    binNames,
                    sc)
		{
			this.LPnamespace = lpNamespace;
        }

        public ANamespaceAccess(ANamespaceAccess clone, Expression expression)
            : this(clone.Namespace,
                    clone.BinNames,
                    clone.AerospikeConnection,
                    new(clone.DefaultReadPolicy)
                    {
                        filterExp = expression
                    },
                    new(clone.DefaultWritePolicy),
                    new(clone.DefaultQueryPolicy)
                    {
                        filterExp = expression
                    },
                    new(clone.DefaultScanPolicy),
                    clone._sets)
        {
            this.LPnamespace = clone.LPnamespace;
			this.IsStrongConsistencyMode = clone.IsStrongConsistencyMode;
		}

		public ANamespaceAccess(ANamespaceAccess clone,
                                    Policy readPolicy = null,
								    WritePolicy writePolicy = null,
								    QueryPolicy queryPolicy = null,
								    ScanPolicy scanPolicy = null)
			: this(clone.Namespace,
					clone.BinNames,
					clone.AerospikeConnection,
					readPolicy ?? new(clone.DefaultReadPolicy),
					writePolicy ?? new(clone.DefaultWritePolicy),
					queryPolicy ?? new(clone.DefaultQueryPolicy),
					scanPolicy ?? new(clone.DefaultScanPolicy),
					clone._sets)
		{
			this.LPnamespace = clone.LPnamespace;
            this.IsStrongConsistencyMode = clone.IsStrongConsistencyMode;
		}

		/// <summary>
		/// Clones the specified instance providing new policies, if provided.
		/// </summary>
		/// <param name="newReadPolicy">The new read policy.</param>
		/// <param name="newWritePolicy">The new write policy.</param>
		/// <param name="newQueryPolicy">The new query policy.</param>
		/// <param name="newScanPolicy">The new scan policy.</param>
		/// <returns>New clone of <see cref="ANamespaceAccess"/> instance.</returns>
		public virtual ANamespaceAccess Clone(Policy newReadPolicy = null,
                                                WritePolicy newWritePolicy = null,
                                                QueryPolicy newQueryPolicy = null,
                                                ScanPolicy newScanPolicy = null)
            => new ANamespaceAccess(this,
                                    newReadPolicy,
                                    newWritePolicy,
                                    newQueryPolicy,
                                    newScanPolicy);
		#endregion

		#region methods and properties

		/// <summary>
		/// The Aerospike Platform this namespace is associated. <see cref="DBPlatforms"/>
		/// </summary>
		public DBPlatforms DBPlatform { get => this.AerospikeConnection?.DBPlatform ?? DBPlatforms.None; }

		/// <summary>
		/// Finds the namespace.
		/// </summary>
		/// <param name="nsName">Name of the namespace.</param>
		/// <returns>ANamespaceAccess or null</returns>
		public static ANamespaceAccess FindNamespace(string nsName)
        {
			lock(ANamespacesList)
			{
				return ANamespacesList.FirstOrDefault(i => i.Namespace == nsName);
			}
		}

        internal static long ForceExplorerRefresh = 0;

        /// <summary>
        /// Refreshes the Connection Explorer
        /// </summary>        
        public static async void RefreshExplorer()
        {
            await DynamicDriver.GetConnection()?.CXInfo?.ForceRefresh();
        }

        internal static void UpdateExplorer()
        {
            Interlocked.Increment(ref ForceExplorerRefresh);
        }

        /// <summary>
        /// This will add a new set that wasn't already created.
        /// </summary>
        /// <param name="setName"></param>
        /// <param name="bins"></param>
        /// <returns></returns>
        private bool AddDynamicSet(string setName, IEnumerable<Bin> bins)
        {
            //System.Diagnostics.Debugger.Launch();
            if (string.IsNullOrEmpty(setName))
                return false;

            var existingBins = bins.Where(b => b.value.Object != null);
            var removedBins = bins.Where(b => b.value.Object is null || b.value.IsNull);
            var result = false;

            if(existingBins.Any())
                result = this.AddDynamicSet(setName,
                                            existingBins.Select(b => new LPSet.BinType(b.name,
                                                                                        b.value.Object.GetType(),
                                                                                        false,
                                                                                        false)));

            return result;
        }

        private bool AddDynamicSet(string setName, IEnumerable<LPSet.BinType> bins)
        {            
            if (string.IsNullOrEmpty(setName))
                return false;

            lock (this)
            {
                var recordSet = this.Sets.FirstOrDefault(s => s.SetName == setName);

                if (recordSet == null)
                {
                    var binNames = bins?.Select(b => b.BinName).ToArray();
                    var lpSet = new LPSet(this.LPnamespace, setName, bins);
                    var accessSet = new SetRecords(lpSet, this, setName, binNames);

                    this.LPnamespace?.TryAddSet(setName, bins);

                    this._sets.Add(accessSet);
                    
                    this.BinNames = this.BinNames.Concat(binNames)
                                            .Distinct().ToArray();

                    if (this.NullSet == null)
                    {
                        this.AddDynamicSet(LPSet.NullSetName, bins);
                    }
                    else
                    {
                        foreach (var b in bins)
                        {
                            this.NullSet.TryAddBin(b.BinName, b.DataType, false);
                        }
                    }

                    this.TryAddBins(accessSet, bins, true);

                    return true;
                }

                return this.TryAddBins(recordSet, bins);
            }
        }

        private bool RemoveBinsFromSet(string setName, IEnumerable<LPSet.BinType> removeBins)
        {
            if (string.IsNullOrEmpty(setName))
                return false;

            lock (this)
            {
                var recordSet = this.Sets.FirstOrDefault(s => s.SetName == setName);

                if (recordSet != null)
                {
                    var binNames = removeBins?.Select(b => b.BinName).ToArray();

                    this.LPnamespace?.TryRemoveSet(setName, removeBins);

                    this.BinNames = this.BinNames
                                    .Where(n => !removeBins.Any(b => b.BinName == n))
                                    .ToArray();

                    this.TryRemoveBins(recordSet, removeBins, true);

                    return true;
                }

                return false;
            }
        }

        private bool TryAddBins(SetRecords set, IEnumerable<LPSet.BinType> bins, bool forceExplorerUpdate = false)
        {
            bool result = false;
            foreach (var b in bins)
            {
                result = set.TryAddBin(b.BinName, b.DataType, false) || result;
                if (this.NullSet != null)
                    result = this.NullSet.TryAddBin(b.BinName, b.DataType, false) || result;
            }

            if (result || forceExplorerUpdate)
                UpdateExplorer();

            return result;
        }

        internal bool TryAddBin(string binName)
        {
            if(this.BinNames.Contains(binName)) return false;
            
            this.BinNames = this.BinNames.Append(binName).ToArray();

            return true;
        }

        private bool TryRemoveBins(SetRecords set, IEnumerable<LPSet.BinType> removeBins, bool forceExplorerUpdate)
        {
            bool result = false;
            foreach (var b in removeBins)
            {
                result = set.TryRemoveBin(b.BinName, false) || result;
                if (this.NullSet != null)
                    result = this.NullSet.TryRemoveBin(b.BinName, false) || result;
            }

            if (result || forceExplorerUpdate)
                UpdateExplorer();

            return result;
        }

        internal bool TryRemoveBin(string binName)
        {
            if (this.BinNames.Contains(binName))
            {
                this.BinNames = this.BinNames
                                    .Where(n => n != binName).ToArray();
                return true;
            }            

            return false;
        }

        public LPNamespace LPnamespace { get; }

        private List<SetRecords> _sets = new List<SetRecords>();

        /// <summary>
        /// Returns the associated set instances for this namespace.
        /// </summary>
        /// <remarks>
        /// The drag and drop set instances from the connection pane in LinqPad are different instances as defined here...
        /// </remarks>
        /// <seealso cref="this[string]"/>
        /// <seealso cref="Exists(string)"/>
        public IEnumerable<SetRecords> Sets
        {
            get
            {
                lock (this)
                {
                    if (this._sets.Any()) return this._sets;
                }

                var setProps = this.GetType().GetProperties()
                                .Where(p => p.PropertyType.IsSubclassOf(typeof(SetRecords)))
                                .Select(p => p.PropertyType);

                var setInstances = new List<SetRecords>();

                foreach (var prop in setProps)
                {
                    setInstances.Add((SetRecords)Activator.CreateInstance(prop, this));
                }

                return this._sets = setInstances.ToList();
            }
        }

		/// <summary>
		/// Gets the names of th sets associate with this namespace.
		/// </summary>
		/// <value>A collection of name of sets.</value>
		public IEnumerable<string> SetNames => this.Sets.Select(s => s.SetName ?? LPSet.NullSetName);

        /// <summary>
        /// Returns the Set instance or null indicating the set doesn't exists in this namespace.
        /// </summary>
        /// <param name="setName">The name of the Aerospike set</param>
        /// <returns>A <see cref="SetRecords"/> instance or null</returns>
        /// <seealso cref="Exists(string)"/>
        public SetRecords this[string setName]
        {
            get => setName == LPSet.NullSetName 
                        ? this.Sets.FirstOrDefault(s => s.SetName is null)
						: this.Sets.FirstOrDefault(s => s.SetName == setName);
        }

		/// <summary>
		/// Gets a value indicating whether this namespace is in strong consistency mode.
		/// </summary>
		/// <value><c>true</c> if this instance is strong consistency mode; otherwise, <c>false</c>.</value>
		/// <seealso href="https://aerospike.com/docs/server/guide/consistency"/>
		public bool IsStrongConsistencyMode { get; }

		/// <summary>
		/// Gets the Aerospike Set Name based on the PK Digest. 
		/// </summary>
		/// <param name="digest">
        /// The PK Digest (<see cref="Aerospike.Client.Key.digest"/>)
        /// </param>
		/// <returns>
		/// Returns the Aerospike Set name based on <paramref name="digest"/> or null to indicate that the PK doesn&apos;t exists.
		/// </returns>
		/// <seealso cref="Aerospike.Client.Key"/>
        /// <seealso cref="Aerospike.Client.Key.digest"/>
		/// <seealso cref="APrimaryKey"/>
        /// <seealso cref="GetSetKeyValue(byte[])"/>
		public string GetSetName(byte[] digest)
        {
			var policy = this.DefaultWritePolicy.Clone();
			policy.sendKey = false;

            var result = this.AerospikeConnection
                                .AerospikeClient
                                .Operate(policy,
                                            new Key(this.Namespace, digest, null, Value.AsNull),
                                            ExpOperation.Read("setname",
                                                                Exp.Build(Exp.SetName()),
                                                                ExpReadFlags.EVAL_NO_FAIL));
            return (string) result?.bins["setname"];
		}

		/// <summary>
		/// Gets the Aerospike Set's name and the key value based on the digest.
		/// </summary>
		/// <param name="digest">
		/// The PK Digest (<see cref="Aerospike.Client.Key.digest"/>)
		/// </param>
		/// <returns>
		/// Returns the Aerospike Set name and Key Value (only if the <see cref="Policy.sendKey"/> is true when the record was create) based on <paramref name="digest"/> or null to indicate that the PK doesn&apos;t exists.
		/// </returns>
		/// <seealso cref="Aerospike.Client.Key"/>
		/// <seealso cref="Aerospike.Client.Key.digest"/>
		/// <seealso cref="APrimaryKey"/>
		/// <seealso cref="GetSetName(byte[])"/>
		public (string setName, object keyValue) GetSetKeyValue(byte[] digest)
		{
			var policy = this.DefaultWritePolicy.Clone();
			policy.sendKey = false;

			var result = this.AerospikeConnection
								.AerospikeClient
								.Operate(policy,
											new Key(this.Namespace, digest, null, Value.AsNull),
											ExpOperation.Read("setname",
																Exp.Build(Exp.SetName()),
																            ExpReadFlags.EVAL_NO_FAIL),
											ExpOperation.Read("keyint", Exp.Build(Exp.Key(Exp.Type.INT)),
																			ExpReadFlags.EVAL_NO_FAIL),
											ExpOperation.Read("keystr", Exp.Build(Exp.Key(Exp.Type.STRING)),
																			ExpReadFlags.EVAL_NO_FAIL),
											ExpOperation.Read("keyblob", Exp.Build(Exp.Key(Exp.Type.BLOB)),
																			ExpReadFlags.EVAL_NO_FAIL));

            return ((string) result?.bins["setname"],
                    result is null
                        ? null
                        : result.bins["keyint"]
                            ?? result.bins["keystr"]
                            ?? result.bins["keyblob"]);
		}

		public override string ToString()
		    => this.BinNames.Length == 0
			    ? $"{this.Namespace}"
                : $"{this.Namespace}{{{string.Join(',', this.BinNames)}}}";

		#endregion

		#region Aerospike API items
		/// <summary>
		/// Determines if a set exists within this namespace.
		/// </summary>
		/// <param name="setName">set name</param>
		/// <returns>
		/// True if the sets exists, otherwise false.
		/// </returns>
		/// <seealso cref="this[string]"/>
		public bool Exists(string setName) => setName == LPSet.NullSetName || this.Sets.Any(s => s.SetName == setName);

        /// <summary>
        /// Returns the Aerospike Null Set for this namespace.
        /// The Null Set will contain all the records with a namespace.
        /// </summary>
        public SetRecords NullSet { get => this[LPSet.NullSetName]; }

        public string Namespace { get; }
        //public string Name { get; }

        /// <summary>
        /// Returns all the bins used within this namespace.
        /// </summary>
        public string[] BinNames { get; private set; }

        public AerospikeConnection AerospikeConnection { get; }

        /// <summary>
        /// <see href="https://docs.aerospike.com/apidocs/csharp/html/t_aerospike_client_querypolicy"/>
        /// </summary>
        public QueryPolicy DefaultQueryPolicy { get; }
        /// <summary>
        /// <see href="https://docs.aerospike.com/apidocs/csharp/html/t_aerospike_client_querypolicy"/>
        /// </summary>
        public WritePolicy DefaultWritePolicy { get; }
        /// <summary>
        /// <see href="https://docs.aerospike.com/apidocs/csharp/html/t_aerospike_client_policy"/>
        /// </summary>
        public Policy DefaultReadPolicy { get; }

		/// <summary>
		/// <see href="https://docs.aerospike.com/apidocs/csharp/html/t_aerospike_client_scanpolicy"/>
		/// </summary>
		public ScanPolicy DefaultScanPolicy { get; }

		/// <summary>
		/// Gets the aerospike <see cref="Aerospike.Client.Txn"/> instance or Null
		/// </summary>
		public virtual Txn GetAerospikeTxn() => this.DefaultWritePolicy.Txn;

		/// <summary>
		/// Gets a value indicating whether this Instance has MRTs enabled.
		/// </summary>
		public bool IsMRT => this.DefaultWritePolicy.Txn is not null;

		/// <summary>
		/// Creates an new Aerospike transaction which is a copy of this namespace.
		/// </summary>
		/// <param name="mrtTimeout">
		/// MRT timeout in seconds. The timer starts when the MRT monitor record is created.
		/// This occurs when the first command in the MRT is executed. If the timeout is reached before
		/// a commit or abort is called, the server will expire and rollback the MRT.
		/// Defaults to 10 seconds.
		/// </param>
		/// <param name="commitRetries">
		/// See <see cref="ATransaction.CommitRetries"/> property.
		/// </param>
		/// <param name="retrySleepMS">
		/// See <see cref="ATransaction.RetrySleepMS"/> property.
		/// </param>
		/// <returns>Transaction Namespace instance</returns>
		/// <seealso cref="ATransaction.Commit"/>
		/// <seealso cref="ATransaction.Abort"/>
		public virtual ATransaction CreateTransaction(int mrtTimeout = 10,
														int commitRetries = 1,
														int retrySleepMS = 1000)
							=> new(this,
									new Txn() { Timeout = mrtTimeout },
									commitRetries,
									retrySleepMS);

		#region Get Methods
		/// <summary>
		/// Gets all records in a set		
		/// </summary>        
		/// <param name="setName">Set name or null for the null set</param>
		/// <param name="bins">bins you wish to get. If not provided all bins for a record</param>
		/// <returns>An array of records in the set</returns>
		/// <seealso cref="AsEnumerable(string, Exp)"/>
		/// <seealso cref="GetRecords(string, string, string[])"/>
		/// <seealso cref="DefaultQueryPolicy"/>
		public ARecord[] GetRecords(string setName, params string[] bins)
                            => GetRecords(this.Namespace, setName, bins);

        /// <summary>
        /// Gets all records in a namespace and/or set        
        /// </summary>
        /// <param name="nsName">namespace</param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="bins">bins you wish to get. If not provided all bins for a record</param>
        /// <returns>An array of records in the set</returns>
        /// <seealso cref="AsEnumerable(string, Exp)"/>
        /// <seealso cref="GetRecords(string, string[])"/>
        /// <seealso cref="DefaultQueryPolicy"/>
        public ARecord[] GetRecords([NotNull] string nsName, string setName, params string[] bins)
        {
			var recordSets = new List<ARecord>();
            
			using(var recordset = this.AerospikeConnection
									.AerospikeClient
									.Query(this.DefaultQueryPolicy,
											string.IsNullOrEmpty(setName) || setName == LPSet.NullSetName
												? new Statement() { Namespace = nsName, BinNames = bins }
												: new Statement() { Namespace = nsName, SetName = setName, BinNames = bins }))
			while(recordset.Next())
			{
				recordSets.Add(new ARecord(this,
											recordset.Key,
											recordset.Record,
											bins,
											dumpType: this.AerospikeConnection.RecordView));
			}

			return recordSets.ToArray();
		}

		/// <summary>
		/// Returns IEnumerable&gt;<see cref="ARecord"/>&lt; for the records of this set based on <see cref="DefaultScanPolicy"/> or <paramref name="filterExpression"/>.
		/// Note: The records&apos; return order may vary between executions. 
		/// </summary>
		/// <param name="setName">Set name or null for the null set</param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <seealso cref="GetRecords(string, string[])"/>
		/// <seealso cref="GetRecords(string, string, string[])"/>
		/// <seealso cref="DefaultScanPolicy"/>
		public IEnumerable<ARecord> AsEnumerable(string setName, Client.Exp filterExpression = null)
		{
			var scanPolicy = filterExpression == null
									? this.DefaultScanPolicy
									: new ScanPolicy(this.DefaultScanPolicy)
									{ filterExp = Exp.Build(filterExpression) };

			var allRecords = new ConcurrentQueue<ARecord>();

			var allTask = Task.Factory.StartNew(() =>
								this.AerospikeConnection
									.AerospikeClient
									.ScanAll(scanPolicy,
												this.Namespace,
												string.IsNullOrEmpty(setName) || setName == LPSet.NullSetName
													? null
													: setName,
											(key, record)
												=> allRecords
													.Enqueue(new ARecord(this,
																			key,
																			record,
                                                                            null,
																			dumpType: this.AerospikeConnection.RecordView))),
								cancellationToken: CancellationToken.None,
								creationOptions: TaskCreationOptions.DenyChildAttach
													| TaskCreationOptions.LongRunning,
								scheduler: TaskScheduler.Current);

			while(!allTask.IsCompleted)
			{
				if(allRecords.TryDequeue(out ARecord value))
					yield return value;
			}

			foreach(var record in allRecords.TakeWhile(rec => rec is not null))
			{
				yield return record;
			}

			if(allTask.IsFaulted && allTask.Exception is not null)
				throw allTask.Exception.InnerExceptions.Count == 1
						? allTask.Exception.InnerExceptions[0]
						: allTask.Exception;
		}

		/// <summary>
		/// Will retrieve a record based on the <paramref name="primaryKey"/>.
		/// </summary>
		/// <param name="setName">The name of the Aerospike set</param>
		/// <param name="primaryKey">
		/// Primary AerospikeKey.
		/// This can be a <see cref="Client.Key"/>, <see cref="Value"/>, or <see cref="Bin"/> object besides a native, collection, etc. value/object.
		/// </param>
		/// <param name="bins">The bins that will be returned</param>
		/// <returns>
		/// The <see cref="ARecord"/>  or null
		/// </returns>
		/// <seealso cref="Put(string, dynamic, IEnumerable{Bin}, WritePolicy, TimeSpan?)"/>
		/// <seealso cref="Put{T}(string, dynamic, string, IEnumerable{T}, WritePolicy, TimeSpan?)"/>
		/// <seealso cref="Put{T}(string, dynamic, string, IList{T}, WritePolicy, TimeSpan?)"/>
		/// <seealso cref="Put{V}(string, dynamic, IDictionary{string, V}, WritePolicy, TimeSpan?)"/>
		/// <seealso cref="Put(string, dynamic, string, object, WritePolicy, TimeSpan?)"/>
		/// <seealso cref="Put(ARecord, string, WritePolicy, TimeSpan?)"/>
		/// <seealso cref="WriteObject{T}(string, dynamic, T, Func{string, string, object, bool, object}, string, WritePolicy, TimeSpan?)"/>
		public ARecord Get(string setName, dynamic primaryKey, params string[] bins)
        {
            Client.Key pk = Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName);
            var policy = this.DefaultReadPolicy;

            if(pk.userKey.IsNull && policy.sendKey)
            {
                policy = policy.Clone();
                policy.sendKey = false;
            }

            var record = this.AerospikeConnection
                                .AerospikeClient
                                .Get(policy, pk, bins);
            var setAccess = this[setName];

            return new ARecord(this,
                                pk,
                                record,
                                setAccess?.BinNames,
                                dumpType: this.AerospikeConnection.RecordView);
        }
		#endregion

		#region Put Methods
		/// <summary>
		/// Puts (Writes) a DB record based on the provided record including Expiration.
		/// Note that if the namespace and/or set is different, this instances&apos;s values are used except 
		/// in the case where the primary key is a digest. In these cases, an <see cref="InvalidOperationException"/> is thrown.
		/// </summary>        
		/// <param name="record">
		/// A <see cref="ARecord"/> object used to add or update the associated record.
		/// </param>
		/// <param name="setName">Set name or null to use the set name defined in the record (default)</param>
		/// <param name="writePolicy">
		/// The write policy. If not provided , the default policy is used.
		/// <seealso cref="WritePolicy"/>
		/// </param>
		/// <param name="ttl">
		/// Time-to-live of the record. 
		/// If null (default), the TTL of <paramref name="record"/> is used.
		/// </param>
		/// <exception cref="InvalidOperationException">
		/// If the record&apos;s primary key is a digest (not an actual value). This exception will be thrown,
		/// since a digest has the namespace and set of where this record was retrieved from. 
		/// </exception>
		/// <seealso cref="Get(string, dynamic, string[])"/>
		/// <seealso cref="BatchWriteRecord{R}(IEnumerable{R}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
		public void Put([NotNull] ARecord record,
                            string setName = null,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
        {
            this.Put(setName ?? record.Aerospike.SetName,
                        record.Aerospike.Key,
                        record.Aerospike.GetValues(),
                        writePolicy,
                        ttl ?? record.Aerospike.TTL);
        }

        /// <summary>
        /// Puts (Writes) a DB record based on the provided key and bin values.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used.
        /// </summary>
        /// <param name="primaryKey">
        /// Primary AerospikeKey.
        /// This can be a <see cref="Client.Key"/>, <see cref="Value"/>, or <see cref="Bin"/> object besides a native, collection, etc. value/object.
        /// </param>
        /// <param name="binValues">
        /// A dictionary where the key is the bin and the value is the bin&apos;s value.
        /// </param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>
        /// <seealso cref="BatchWrite{P,V}(string, IEnumerable{ValueTuple{P, IDictionary{string, V}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        public void Put<V>(string setName,
                            [NotNull] dynamic primaryKey,
                            [NotNull] IDictionary<string, V> binValues,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
        {            
            var writePolicyPut = writePolicy ?? this.DefaultWritePolicy;

            if (ttl.HasValue)
            {
                writePolicyPut = new WritePolicy(writePolicyPut) { expiration = SetRecords.DetermineExpiration(ttl.Value) };
            }

            var bins = Helpers.CreateBinRecord(binValues);
            this.AerospikeConnection
                .AerospikeClient.Put(writePolicyPut,
                                        Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName),
                                        bins);

            this.AddDynamicSet(setName, bins);
        }

        /// <summary>
        /// Puts (writes) a bin to the DB record.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used.
        /// </summary>
        /// <param name="primaryKey">
        /// Primary AerospikeKey.
        /// This can be a <see cref="Client.Key"/>, <see cref="Value"/>, or <see cref="Bin"/> object besides a native, collection, etc. value/object.
        /// </param>
        /// <param name="bin">BinName Name</param>
        /// <param name="binValue">
        /// BinName&apos;s Value.
        /// If null, the bin is removed from the record.
        /// </param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>
        public void Put(string setName,
                            [NotNull] dynamic primaryKey,
                            [NotNull] string bin,
                            [NotNull] object binValue,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
        {
            var writePolicyPut = writePolicy ?? this.DefaultWritePolicy;

            if (ttl.HasValue)
            {
                writePolicyPut = new WritePolicy(writePolicyPut) { expiration = SetRecords.DetermineExpiration(ttl.Value) };
            }

            var bins = Helpers.CreateBinRecord(binValue, bin);
            this.AerospikeConnection
                .AerospikeClient.Put(writePolicyPut,
                                        Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName),
                                        bins);

            this.AddDynamicSet(setName, bins);
        }

        /// <summary>
        /// Puts (writes) a bin to the DB record.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used.
        /// </summary>
        /// <param name="primaryKey">
        /// Primary AerospikeKey.
        /// This can be a <see cref="Client.Key"/>, <see cref="Value"/>, or <see cref="Bin"/> object besides a native, collection, etc. value/object.
        /// </param>
        /// <param name="bin">BinName Name</param>
        /// <param name="binValue">
        /// BinName&apos;s Value.
        /// If null, the bin is removed from the record.
        /// </param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>
        public void Put(string setName,
                            [NotNull] dynamic primaryKey,
                            [NotNull] string bin,
                            [NotNull] string binValue,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
            => this.Put(setName, primaryKey, bin, (object) binValue, writePolicy:  writePolicy, ttl: ttl);

        /// <summary>
        /// Puts (writes) a bin to the DB record.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used.
        /// </summary>
        /// <param name="primaryKey">
        /// Primary AerospikeKey.
        /// This can be a <see cref="Client.Key"/>, <see cref="Value"/>, or <see cref="Bin"/> object besides a native, collection, etc. value/object.
        /// </param>
        /// <param name="bin">BinName Name</param>
        /// <param name="listValue">
        /// BinName&apos;s Value.
        /// If null, the bin is removed from the record.
        /// </param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param> 
        public void Put<T>(string setName,
                            [NotNull] dynamic primaryKey,
                            [NotNull] string bin,
                            [NotNull] IList<T> listValue,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
        {
            var writePolicyPut = writePolicy ?? this.DefaultWritePolicy;

            if (ttl.HasValue)
            {
                writePolicyPut = new WritePolicy(writePolicyPut) { expiration = SetRecords.DetermineExpiration(ttl.Value) };
            }

            var cbin = Helpers.CreateBinRecord(listValue, bin);
            this.AerospikeConnection
                .AerospikeClient.Put(writePolicyPut,
                                        Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName),
                                        cbin);

            this.AddDynamicSet(setName, new Bin[] { cbin });
        }

        /// <summary>
        /// Puts (writes) a bin to the DB record.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used.
        /// </summary>
        /// <param name="primaryKey">
        /// Primary AerospikeKey.
        /// This can be a <see cref="Client.Key"/>, <see cref="Value"/>, or <see cref="Bin"/> object besides a native, collection, etc. value/object.
        /// </param>
        /// <param name="bin">BinName Name</param>
        /// <param name="collectionValue">
        /// BinName&apos;s Value.
        /// If null, the bin is removed from the record.
        /// </param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param> 
        public void Put<K,V>(string setName,
                               [NotNull] dynamic primaryKey,
                                [NotNull] string bin,
                                [NotNull] IDictionary<K,V> collectionValue,
                                WritePolicy writePolicy = null,
                                TimeSpan? ttl = null)
        {
            var writePolicyPut = writePolicy ?? this.DefaultWritePolicy;

            if (ttl.HasValue)
            {
                writePolicyPut = new WritePolicy(writePolicyPut) { expiration = SetRecords.DetermineExpiration(ttl.Value) };
            }

            var cBin = Helpers.CreateBinRecord((IEnumerable<KeyValuePair<K,V>>) collectionValue, bin);
            this.AerospikeConnection
                .AerospikeClient.Put(writePolicyPut,
                                        Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName),
                                        cBin);

            this.AddDynamicSet(setName, new Bin[] { cBin });
        }

        /// <summary>
        /// Puts (writes) a bin to the DB record.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used.
        /// </summary>
        /// <param name="primaryKey">
        /// Primary AerospikeKey.
        /// This can be a <see cref="Client.Key"/>, <see cref="Value"/>, or <see cref="Bin"/> object besides a native, collection, etc. value/object.
        /// </param>
        /// <param name="bin">BinName Name</param>
        /// <param name="collectionValue">
        /// BinName&apos;s Value.
        /// If null, the bin is removed from the record.
        /// </param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param> 
        public void Put<T>(string setName,
                           [NotNull] dynamic primaryKey,
                            [NotNull] string bin,
                            [NotNull] IEnumerable<T> collectionValue,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
        {            
            var writePolicyPut = writePolicy ?? this.DefaultWritePolicy;

            if (ttl.HasValue)
            {
                writePolicyPut = new WritePolicy(writePolicyPut) { expiration = SetRecords.DetermineExpiration(ttl.Value) };
            }

            var cBin = Helpers.CreateBinRecord(collectionValue, bin);
            this.AerospikeConnection
                .AerospikeClient.Put(writePolicyPut,
                                        Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName),
                                        cBin);

            this.AddDynamicSet(setName, new Bin[] {cBin});
        }

        /// <summary>
        /// Put (Writes) a DB record based on the provided key and bin values.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used.
        /// </summary>
        /// <param name="primaryKey">
        /// Primary AerospikeKey.
        /// This can be a <see cref="Client.Key"/>, <see cref="Value"/>, or <see cref="Bin"/> object besides a native, collection, etc. value/object.
        /// </param>
        /// <param name="binsToWrite">
        /// A collection of <see cref="Bin"/> objects used to add/update the associated record.
        /// </param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>
        public void Put(string setName,
                            [NotNull] dynamic primaryKey,
                            [NotNull] IEnumerable<Bin> binsToWrite,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
        {
            var writePolicyPut = writePolicy ?? this.DefaultWritePolicy;

            if (ttl.HasValue)
            {
                writePolicyPut = new WritePolicy(writePolicyPut) { expiration = SetRecords.DetermineExpiration(ttl.Value) };
            }
            
            this.AerospikeConnection
                .AerospikeClient.Put(writePolicyPut,
                                        Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName),
                                        binsToWrite.ToArray());

            this.AddDynamicSet(setName, binsToWrite);
        }

        /// <summary>
        /// Writes the instance where each field/property is a bin name and the associated value the bin's value.
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/> and any of the &quot;Put&quot; methods.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryKey">
        /// Primary AerospikeKey.
        /// This can be a <see cref="Client.Key"/>, <see cref="Value"/>, or <see cref="Bin"/> object besides a native, collection, etc. value/object.
        /// </param>
        /// <param name="instance">
        /// The instance that will be transformed into an Aerospike Record.
        /// </param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="transform">
        /// A action that is called to perform customized transformation. 
        /// First argument -- the name of the property/field within the instance/class
        /// Second argument -- the name of the bin (can be different from property/field name if <see cref="BinNameAttribute"/> is defined)
        /// Third argument -- the <paramref name="instance"/> being transformed
        /// Fourth argument -- if true the instance is within another object.
        /// Returns the new transformed object or null to indicate that this instance should be skipped.
        /// </param>
        /// <param name="documentBinName">
        /// If provided the record is created as a document and this will be the name of the bin. 
        /// </param>
        /// <param name="writePolicy"></param>
        /// <param name="ttl"></param>
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/>
        /// <seealso cref="Aerospike.Client.BinIgnoreAttribute"/>
        /// <seealso cref="Put(string, dynamic, IEnumerable{Bin}, WritePolicy, TimeSpan?)"/>
        /// <seealso cref="Put{T}(string, dynamic, string, IEnumerable{T}, WritePolicy, TimeSpan?)"/>
        /// <seealso cref="Put{T}(string, dynamic, string, IList{T}, WritePolicy, TimeSpan?)"/>
        /// <seealso cref="Put{V}(string, dynamic, IDictionary{string, V}, WritePolicy, TimeSpan?)"/>
        /// <seealso cref="Put(string, dynamic, string, object, WritePolicy, TimeSpan?)"/>
        /// <seealso cref="Put(ARecord, string, WritePolicy, TimeSpan?)"/>
        /// <seealso cref="Get(string, dynamic, string[])"/>
        /// <seealso cref="BatchWriteObject{P,T}(string, IEnumerable{ValueTuple{P, T}}, BatchPolicy, BatchWritePolicy, ParallelOptions, Func{string, string, object, bool, object}, string)"/>
        /// <exception cref="TypeAccessException">Thrown if cannot write <paramref name="instance"/></exception>
        public void WriteObject<T>(string setName,
                                    [NotNull] dynamic primaryKey,
                                    [NotNull] T instance,
                                    Func<string, string, object, bool, object> transform = null,
                                    string documentBinName = null,
                                    WritePolicy writePolicy = null,
                                    TimeSpan? ttl = null)
        {            
            var writePolicyPut = writePolicy ?? this.DefaultWritePolicy;

            if (ttl.HasValue)
            {
                writePolicyPut = new WritePolicy(writePolicyPut) { expiration = SetRecords.DetermineExpiration(ttl.Value) };
            }

            var key = Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName);

            Dictionary<string, object> dictItem;

            if (instance is ARecord)
            {
                throw new TypeAccessException($"Don't know how to Write an ARecord instance. Try using a \"Put\" method.");
            }
            else if (instance is IEnumerable)
            {
                var instanceType = Helpers.GetRealTypeName(instance.GetType());
                throw new TypeAccessException($"Don't know how to Write an IEnumerable Object (\"{instanceType}\"). Try using a \"Put\" method, use BatchWriteObject, or call this method on each item in the collection.");
            }
            else
                dictItem = Helpers.TransForm(instance, transform);

            if(string.IsNullOrEmpty(documentBinName))
            {
                var bins = Helpers.CreateBinRecord(dictItem);

                this.AerospikeConnection
                    .AerospikeClient.Put(writePolicyPut,
                                            key,
                                            bins);
                this.AddDynamicSet(setName, bins);
            }
            else
            {
                var mapPolicy = new MapPolicy(MapOrder.UNORDERED, MapWriteFlags.DEFAULT);

                this.AerospikeConnection
                    .AerospikeClient.Operate(writePolicy,
                                                key,
                                                MapOperation.PutItems(mapPolicy,
                                                                        documentBinName,
                                                                        dictItem));
                this.AddDynamicSet(setName, new LPSet.BinType[] 
                                                    { new LPSet.BinType(documentBinName, typeof(JsonDocument), false, true)});
            }          
        }
        #endregion

        #region Batch Methods

        #region Batch Write
        /// <summary>
        /// Writes a collection of <see cref="ARecord"/> as a <seealso cref="Aerospike.Client.BatchPolicy"/> operation.
        /// </summary>
        /// <typeparam name="R">A <see cref="ARecord"/> instance</typeparam>
        /// <param name="writeRecords">
        /// A collection of <see cref="ARecord"/>.
        /// </param>
        /// <param name="batchPolicy">
        /// <seealso cref="BatchPolicy"/>
        /// </param>
        /// <param name="batchWritePolicy">
        /// <seealso cref="BatchWritePolicy"/>
        /// </param>
        /// <param name="parallelOptions">
        /// <seealso cref="ParallelOptions"/>
        /// </param>
        /// <returns>True if successful</returns>
        /// <seealso cref="BatchWriteObject{P,T}(string, IEnumerable{ValueTuple{P, T}}, BatchPolicy, BatchWritePolicy, ParallelOptions, Func{string, string, object, bool, object}, string)"/>
        /// <seealso cref="BatchWrite{P,V}(string, IEnumerable{ValueTuple{P, IDictionary{string, V}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        /// <seealso cref="Put(ARecord, string, WritePolicy, TimeSpan?)"/>
        public bool BatchWriteRecord<R>([NotNull] IEnumerable<R> writeRecords,
                                        BatchPolicy batchPolicy = null,
                                        BatchWritePolicy batchWritePolicy = null,
                                        ParallelOptions parallelOptions = null)
            where R : ARecord
        {           
            batchPolicy ??= new BatchPolicy(this.DefaultWritePolicy)
                            {
                                maxRetries = 1,
                                sendKey = true,
                                maxConcurrentThreads = 2,
                                sleepBetweenRetries = 5,
                                Txn = this.GetAerospikeTxn()
                            };                
            
            batchWritePolicy ??= new BatchWritePolicy()
                                    {                    
                                        sendKey = true,
                                        recordExistsAction = RecordExistsAction.REPLACE                                        
                                    };

            parallelOptions ??= new ParallelOptions();
            
            var batchArray = new BatchRecord[writeRecords.Count()];

            Parallel.For(0, batchArray.Length, parallelOptions, idx =>
            {
                var aRecord = writeRecords.ElementAt(idx);
                var operations = new Operation[aRecord.Aerospike.Count];

                for(int i = 0; i < operations.Length; ++i)
                {
                    operations[i] = Operation.Put(aRecord.Aerospike.Bins[i]);
                }

                batchArray[idx] = new BatchWrite(batchWritePolicy,
                                                    aRecord.Aerospike.Key,
                                                    operations);       
            });

            return this.AerospikeConnection.AerospikeClient.Operate(batchPolicy,
                                                                    batchArray.ToList());
        }

        /// <summary>
        /// Writes a collection of items to <paramref name="setName"/>.
        /// </summary>
        /// <typeparam name="P">The Primary Key Type</typeparam>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="binRecords">
        /// A collection where each item is the following:
        ///     The Primary Key
        ///     A collection of <see cref="Bin"/>s
        /// </param>
        /// <param name="batchPolicy">
        /// <seealso cref="BatchPolicy"/>
        /// </param>
        /// <param name="batchWritePolicy">
        /// <seealso cref="BatchWritePolicy"/>
        /// </param>
        /// <param name="parallelOptions">
        /// <seealso cref="ParallelOptions"/>
        /// </param>
        /// <returns>True if successful</returns>
        /// <seealso cref="BatchWrite{P,V}(string, IEnumerable{ValueTuple{P, IDictionary{string, V}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        /// <seealso cref="BatchWriteRecord{R}(IEnumerable{R}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        /// <seealso cref="BatchWriteObject{P,T}(string, IEnumerable{ValueTuple{P, T}}, BatchPolicy, BatchWritePolicy, ParallelOptions, Func{string, string, object, bool, object}, string)"/>
        /// <seealso cref="Put{V}(string, dynamic, IDictionary{string, V}, WritePolicy, TimeSpan?)"/>
        public bool BatchWrite<P>([NotNull] string setName,
                                    [NotNull] IEnumerable<(P pk, IEnumerable<Bin> bins)> binRecords,
                                    BatchPolicy batchPolicy = null,
                                    BatchWritePolicy batchWritePolicy = null,
                                    ParallelOptions parallelOptions = null)
        {
            batchPolicy ??= new BatchPolicy(this.DefaultWritePolicy)
            {
                maxRetries = 1,
                sendKey = true,
                maxConcurrentThreads = 2,
                sleepBetweenRetries = 5,
				Txn = this.GetAerospikeTxn()
			};

            batchWritePolicy ??= new BatchWritePolicy()
            {
                sendKey = true,
                recordExistsAction = RecordExistsAction.REPLACE
            };

            parallelOptions ??= new ParallelOptions();

            var batchArray = new BatchRecord[binRecords.Count()];
            var allBins = new ConcurrentQueue<Bin[]>();

            Parallel.For(0, batchArray.Length, parallelOptions, idx =>
            {
                var record = binRecords.ElementAt(idx);
                var bins = record.bins.ToArray();
                var operations = new Operation[bins.Length];
                allBins.Enqueue(bins);

                for (int i = 0; i < operations.Length; ++i)
                {
                    operations[i] = Operation.Put(bins[i]);
                }

                batchArray[idx] = new BatchWrite(batchWritePolicy,
                                                    Helpers.DetermineAerospikeKey(record.pk, this.Namespace, setName),
                                                    operations);
            });

            var result = this.AerospikeConnection.AerospikeClient.Operate(batchPolicy,
                                                                    batchArray.ToList());
            this.AddDynamicSet(setName,
                                allBins
                                    .SelectMany(a => a)
                                    .DistinctBy(a => a.name));
            return result;
        }

        /// <summary>
        /// Writes a collection of items to <paramref name="setName"/>.
        /// </summary>
        /// <typeparam name="P">The Primary Key Type</typeparam>
        /// <typeparam name="V">Bin&apos;s value type</typeparam>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="binRecords">
        /// A collection where each item is the following:
        ///     The Primary Key
        ///     A dictionary where the key is the bin name and the value is the bin&apos;s value.
        /// </param>
        /// <param name="batchPolicy">
        /// <seealso cref="BatchPolicy"/>
        /// </param>
        /// <param name="batchWritePolicy">
        /// <seealso cref="BatchWritePolicy"/>
        /// </param>
        /// <param name="parallelOptions">
        /// <seealso cref="ParallelOptions"/>
        /// </param>
        /// <returns>True if successful</returns>
        /// <seealso cref="BatchWrite{P, V}(string, IEnumerable{ValueTuple{P, IEnumerable{ValueTuple{string, V}}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        /// <seealso cref="BatchWrite{P}(string, IEnumerable{ValueTuple{P, IEnumerable{Bin}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        /// <seealso cref="BatchWriteRecord{R}(IEnumerable{R}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        /// <seealso cref="BatchWriteObject{P,T}(string, IEnumerable{ValueTuple{P, T}}, BatchPolicy, BatchWritePolicy, ParallelOptions, Func{string, string, object, bool, object}, string)"/>
        /// <seealso cref="Put{V}(string, dynamic, IDictionary{string, V}, WritePolicy, TimeSpan?)"/>
        public bool BatchWrite<P,V>([NotNull] string setName,
                                    [NotNull] IEnumerable<(P pk, IDictionary<string, V> bins)> binRecords,
                                    BatchPolicy batchPolicy = null,
                                    BatchWritePolicy batchWritePolicy = null,
                                    ParallelOptions parallelOptions = null)
        {
           batchPolicy ??= new BatchPolicy(this.DefaultWritePolicy)
            {
                maxRetries = 1,
                sendKey = true,
                maxConcurrentThreads = 2,
                sleepBetweenRetries = 5,
			    Txn = this.GetAerospikeTxn()
		   };

            batchWritePolicy ??= new BatchWritePolicy()
                                    {
                                        sendKey = true,
                                        recordExistsAction = RecordExistsAction.REPLACE
                                    };

            parallelOptions ??= new ParallelOptions();            

            var batchArray = new BatchRecord[binRecords.Count()];
            var allBins = new ConcurrentQueue<Bin[]>();

            Parallel.For(0, batchArray.Length, parallelOptions, idx =>
            {
                var record = binRecords.ElementAt(idx);
                var bins = Helpers.CreateBinRecord(record.bins);
                var operations = new Operation[bins.Length];
                allBins.Enqueue(bins);

                for (int i = 0; i < operations.Length; ++i)
                {
                    operations[i] = Operation.Put(bins[i]);
                }

                batchArray[idx] = new BatchWrite(batchWritePolicy,
                                                    Helpers.DetermineAerospikeKey(record.pk, this.Namespace, setName),
                                                    operations);
            });

            var result = this.AerospikeConnection.AerospikeClient.Operate(batchPolicy,
                                                                    batchArray.ToList());
            this.AddDynamicSet(setName,
                                allBins
                                    .SelectMany(a => a)
                                    .DistinctBy(a => a.name));
            return result;
        }

        /// <summary>
        /// Writes a collection of items to <paramref name="setName"/>.
        /// </summary>
        /// <typeparam name="P">The Primary Key Type</typeparam>
        /// <typeparam name="V">Bin&apos;s value type</typeparam>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="binRecords">
        /// A collection where each item is the following:
        ///     The Primary Key
        ///     A dictionary where the key is the bin name and the value is the bin&apos;s value.
        /// </param>
        /// <param name="batchPolicy">
        /// <seealso cref="BatchPolicy"/>
        /// </param>
        /// <param name="batchWritePolicy">
        /// <seealso cref="BatchWritePolicy"/>
        /// </param>
        /// <param name="parallelOptions">
        /// <seealso cref="ParallelOptions"/>
        /// </param>
        /// <returns>True if successful</returns>
        /// <seealso cref="BatchWrite{P}(string, IEnumerable{ValueTuple{P, IEnumerable{Bin}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        /// <seealso cref="BatchWriteRecord{R}(IEnumerable{R}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        /// <seealso cref="BatchWriteObject{P,T}(string, IEnumerable{ValueTuple{P, T}}, BatchPolicy, BatchWritePolicy, ParallelOptions, Func{string, string, object, bool, object}, string)"/>
        /// <seealso cref="BatchWrite{P, V}(string, IEnumerable{ValueTuple{P, IDictionary{string, V}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        /// <seealso cref="Put{V}(string, dynamic, IDictionary{string, V}, WritePolicy, TimeSpan?)"/>
        public bool BatchWrite<P, V>([NotNull] string setName,
                                    [NotNull] IEnumerable<(P pk, IEnumerable<(string binName, V value)> bins)> binRecords,
                                    BatchPolicy batchPolicy = null,
                                    BatchWritePolicy batchWritePolicy = null,
                                    ParallelOptions parallelOptions = null)
        {            
            batchPolicy ??= new BatchPolicy(this.DefaultWritePolicy)
            {
                maxRetries = 1,
                sendKey = true,
                maxConcurrentThreads = 2,
                sleepBetweenRetries = 5,
				Txn = this.GetAerospikeTxn()
			};

            batchWritePolicy ??= new BatchWritePolicy()
            {
                sendKey = true,
                recordExistsAction = RecordExistsAction.REPLACE
            };

            parallelOptions ??= new ParallelOptions();

            var batchArray = new BatchRecord[binRecords.Count()];
            var allBins = new ConcurrentQueue<Bin[]>();

            Parallel.For(0, batchArray.Length, parallelOptions, idx =>
            {
                var record = binRecords.ElementAt(idx);
                var bins = Helpers.CreateBinRecord(record.bins);
                var operations = new Operation[bins.Length];
                allBins.Enqueue(bins);

                for (int i = 0; i < operations.Length; ++i)
                {
                    operations[i] = Operation.Put(bins[i]);                    
                }

                batchArray[idx] = new BatchWrite(batchWritePolicy,
                                                    Helpers.DetermineAerospikeKey(record.pk, this.Namespace, setName),
                                                    operations);
            });

            var result = this.AerospikeConnection.AerospikeClient.Operate(batchPolicy,
                                                                    batchArray.ToList());
            this.AddDynamicSet(setName,
                                allBins
                                    .SelectMany(a => a)
                                    .DistinctBy(a => a.name));
            return result;
        }

        /// <summary>
        /// Writes a collection of <typeparamref name="T"/> objects to <paramref name="setName"/>.
        /// </summary>
        /// <typeparam name="P">The Primary Key Type</typeparam>
        /// <typeparam name="T">instance type</typeparam>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="objRecords"></param>
        /// <param name="batchPolicy">
        /// <seealso cref="BatchPolicy"/>
        /// </param>
        /// <param name="batchWritePolicy">
        /// <seealso cref="BatchWritePolicy"/>
        /// </param>
        /// <param name="parallelOptions">
        /// <seealso cref="ParallelOptions"/>
        /// </param>
        /// <param name="transform">
        /// A action that is called to perform customized transformation. 
        /// First argument -- the name of the property/field within the instance/class
        /// Second argument -- the name of the bin (can be different from property/field name if <see cref="BinNameAttribute"/> is defined)
        /// Third argument -- the instance being transformed
        /// Fourth argument -- if true the instance is within another object.
        /// Returns the new transformed object or null to indicate that this instance should be skipped.
        /// </param>
        /// <param name="documentBinName">
        /// If provided the record is created as a document and this will be the name of the bin. 
        /// </param>
        /// <returns>True if successful</returns>
        /// <exception cref="TypeAccessException">Thrown if cannot write <paramref name="objRecords"/></exception>
        /// <seealso cref="BatchWrite{P,V}(string, IEnumerable{ValueTuple{P, IDictionary{string, V}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        /// <seealso cref="BatchWriteRecord{R}(IEnumerable{R}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        /// <seealso cref="WriteObject{T}(string, dynamic, T, Func{string, string, object, bool, object}, string, WritePolicy, TimeSpan?)"/>
        public bool BatchWriteObject<P,T>([NotNull] string setName,
                                        [NotNull] IEnumerable<(P pk, T instance)> objRecords,
                                        BatchPolicy batchPolicy = null,
                                        BatchWritePolicy batchWritePolicy = null,
                                        ParallelOptions parallelOptions = null,
                                        Func<string, string, object, bool, object> transform = null,
                                        string documentBinName = null)
        {
            batchPolicy ??= new BatchPolicy(this.DefaultWritePolicy)
            {
                maxRetries = 1,
                sendKey = true,
                maxConcurrentThreads = 2,
                sleepBetweenRetries = 5,
				Txn = this.GetAerospikeTxn()
			};

            batchWritePolicy ??= new BatchWritePolicy()
            {
                sendKey = true,
                recordExistsAction = RecordExistsAction.REPLACE
            };

            parallelOptions ??= new ParallelOptions();

            var batchArray = new BatchRecord[objRecords.Count()];
            var allBins = new ConcurrentQueue<Bin[]>();

            Parallel.For(0, batchArray.Length, parallelOptions, idx =>
            {
                var (pk,instance) = objRecords.ElementAt(idx);
                var dictItem = Helpers.TransForm(instance, transform);
                Operation[] operations;

                if (string.IsNullOrEmpty(documentBinName))
                {
                    var bins = Helpers.CreateBinRecord(dictItem);
                    operations = new Operation[bins.Length];
                    allBins.Enqueue(bins);

                    for (int i = 0; i < operations.Length; ++i)
                    {
                        operations[i] = Operation.Put(bins[i]);
                    }
                }
                else
                {                    
                    var mapPolicy = new MapPolicy(MapOrder.UNORDERED, MapWriteFlags.DEFAULT);
                    
                    operations = new Operation[] {
                                        MapOperation.PutItems(mapPolicy,
                                                                documentBinName,
                                                                dictItem) };
                }

                batchArray[idx] = new BatchWrite(batchWritePolicy,
                                                    Helpers.DetermineAerospikeKey(pk, this.Namespace, setName),
                                                    operations);
            });

            var result = this.AerospikeConnection.AerospikeClient.Operate(batchPolicy,
                                                                            batchArray.ToList());

            if(string.IsNullOrEmpty(documentBinName))
            {
                this.AddDynamicSet(setName,
                                    allBins
                                        .SelectMany(a => a)
                                        .DistinctBy(a => a.name));
            }
            else
            {
                this.AddDynamicSet(setName, new LPSet.BinType[]
                                                    { new LPSet.BinType(documentBinName, typeof(JsonDocument), false, true)});
            }
            
            return result;
        }

        /// <summary>
        /// Deletes records defined in <paramref name="primaryKeys"/>.
        /// </summary>
        /// <typeparam name="R">Primary Key Type</typeparam>
        /// <param name="setName">The Set name</param>
        /// <param name="primaryKeys">
        /// A collection of primary keys that will be deleted.
        /// </param>
        /// <param name="batchPolicy">
        /// <see cref="BatchPolicy"/>
        /// </param>
        /// <param name="deletePolicy">
        /// <seealso cref="BatchDeletePolicy"/>
        /// </param>
        /// <param name="filterExpression">The expression that will be applied to the result set. Can be null.</param>
        /// <returns>Returns true if all records deleted or false if one or more wasn't found</returns>
        public bool BatchDelete<R>([NotNull] string setName,
                                    [NotNull] IEnumerable<R> primaryKeys,
                                    BatchPolicy batchPolicy = null,
                                    BatchDeletePolicy deletePolicy = null,
                                    Expression filterExpression = null)
        {
            batchPolicy ??= new BatchPolicy(this.DefaultWritePolicy)
            {
                maxRetries = 1,
                maxConcurrentThreads = 2,
                sleepBetweenRetries = 5,
                filterExp = filterExpression,
				Txn = this.GetAerospikeTxn()
			};

            if (filterExpression is not null && batchPolicy.filterExp is null)
                batchPolicy.filterExp = filterExpression;

            var keys = primaryKeys
                        .Select(k => Helpers.DetermineAerospikeKey(k, this.Namespace, setName))
                        .ToArray();

            var result = this.AerospikeConnection.AerospikeClient.Delete(batchPolicy,
                                                                            deletePolicy,
                                                                            keys);

            return result.status;
        }
        #endregion

        #region Batch Read

        /// <summary>
        /// Return a collection of <see cref="ARecord"/> based on <paramref name="primaryKeys"/>
        /// </summary>
        /// <typeparam name="P">Primary Key Type</typeparam>
        /// <param name="setName">The Set Name</param>
        /// <param name="primaryKeys">A collection of Primarily Keys that will be part of the collection</param>
        /// <param name="batchPolicy">
        /// <seealso cref="BatchPolicy"/>
        /// </param>
        /// <param name="batchReadPolicy">
        /// <seealso cref="BatchReadPolicy"/>
        /// </param>        
        /// <param name="filterExpression">The expression that will be applied to the result set. Can be null.</param>
        /// <param name="returnBins">
        /// Only return these bins
        /// </param>
        /// <param name="dumpType"></param> 
        /// <returns>
        /// A collection of records based on <paramref name="primaryKeys"/> or an empty collection.
        /// If a key is not found, there will be no bins associated with the record.
        /// </returns>
        /// <param name="definedBins">internal use</param>
        /// <seealso cref="BatchRead(IEnumerable{ARecord}, BatchPolicy, BatchReadPolicy, Expression, string[], ARecord.DumpTypes, string[])"/>
        /// <seealso cref="BatchRead(IEnumerable{Key}, BatchPolicy, BatchReadPolicy, Expression, string[], ARecord.DumpTypes, string[])"/>
        public IEnumerable<ARecord> BatchRead<P>([NotNull] string setName,
                                                    [NotNull] IEnumerable<P> primaryKeys,
                                                    BatchPolicy batchPolicy = null,
                                                    BatchReadPolicy batchReadPolicy = null,
                                                    Expression filterExpression = null,
                                                    string[] returnBins = null,
                                                    ARecord.DumpTypes dumpType = ARecord.DumpTypes.Record,
                                                    string[] definedBins = null)
        {
            batchPolicy ??= new BatchPolicy(this.DefaultReadPolicy)
            {
                maxRetries = 2,
                maxConcurrentThreads = 1,
                filterExp = filterExpression,
				Txn = this.GetAerospikeTxn()
			};

            batchReadPolicy ??= new BatchReadPolicy()
            {
                filterExp = filterExpression
            };

            var batchList = new List<BatchRead>(primaryKeys.Count());
            
            foreach(var pk in primaryKeys)
            {                
                if(returnBins is null)
                    batchList.Add(new BatchRead(batchReadPolicy,
                                                    Helpers.DetermineAerospikeKey(pk, this.Namespace, setName),
                                                    true));
                else
                    batchList.Add(new BatchRead(batchReadPolicy,
                                                    Helpers.DetermineAerospikeKey(pk, this.Namespace, setName),
                                                    returnBins));
            };

            this.AerospikeConnection.AerospikeClient.Get(batchPolicy, batchList);
            
            foreach (var batch in batchList)
            {
                yield return new ARecord(this,
                                            batch.key,
                                            batch.record,
                                            binNames: definedBins ?? returnBins,
                                            dumpType: dumpType,
                                            inDoubt: batch.inDoubt);
            }
        }

		/// <summary>
		/// Return a collection of <see cref="ARecord"/> based on <paramref name="primaryKeys"/>
		/// </summary>
		/// <param name="primaryKeys">A collection of <seealso cref="Key"/> that will be part of the collection</param>
		/// <param name="batchPolicy">
		/// <seealso cref="BatchPolicy"/>
		/// </param>
		/// <param name="batchReadPolicy">
		/// <seealso cref="BatchReadPolicy"/>
		/// </param>        
		/// <param name="filterExpression">The expression that will be applied to the result set. Can be null.</param>
		/// <param name="returnBins">
		/// Only return these bins
		/// </param>
		/// <param name="dumpType"></param> 
		/// <returns>
		/// A collection of records based on <paramref name="primaryKeys"/> or an empty collection.
		/// If a key is not found, there will be no bins associated with the record.
		/// </returns>
		/// <param name="definedBins">internal use</param>
        /// <seealso cref="BatchRead(IEnumerable{ARecord}, BatchPolicy, BatchReadPolicy, Expression, string[], ARecord.DumpTypes, string[])"/>
        /// <seealso cref="BatchRead{P}(string, IEnumerable{P}, BatchPolicy, BatchReadPolicy, Expression, string[], ARecord.DumpTypes, string[])"/>
		public IEnumerable<ARecord> BatchRead([NotNull] IEnumerable<Key> primaryKeys,
													BatchPolicy batchPolicy = null,
													BatchReadPolicy batchReadPolicy = null,
													Expression filterExpression = null,
													string[] returnBins = null,
													ARecord.DumpTypes dumpType = ARecord.DumpTypes.Record,
													string[] definedBins = null)
		{
			batchPolicy ??= new BatchPolicy(this.DefaultReadPolicy)
			{
				maxRetries = 2,
				maxConcurrentThreads = 1,
				filterExp = filterExpression,
				Txn = this.GetAerospikeTxn()
			};

			batchReadPolicy ??= new BatchReadPolicy()
			{
				filterExp = filterExpression
			};

			var batchList = new List<BatchRead>(primaryKeys.Count());

			foreach(var pk in primaryKeys)
			{
				if(returnBins is null)
					batchList.Add(new BatchRead(batchReadPolicy,
													pk,
													true));
				else
					batchList.Add(new BatchRead(batchReadPolicy,
													pk,
													returnBins));
			};

			this.AerospikeConnection.AerospikeClient.Get(batchPolicy, batchList);

			foreach(var batch in batchList)
			{
				yield return new ARecord(this,
											batch.key,
											batch.record,
											binNames: definedBins ?? returnBins,
											dumpType: dumpType,
											inDoubt: batch.inDoubt);
			}
		}

		/// <summary>
		/// Return a collection of <see cref="ARecord" /> based on <paramref name="records" />
		/// </summary>
		/// <param name="records">A collection of <seealso cref="ARecord"/> that will be part of the collection</param>
		/// <param name="batchPolicy"><seealso cref="BatchPolicy" /></param>
		/// <param name="batchReadPolicy"><seealso cref="BatchReadPolicy" /></param>
		/// <param name="filterExpression">The expression that will be applied to the result set. Can be null.</param>
		/// <param name="returnBins">Only return these bins</param>
		/// <param name="dumpType">Type of the dump.</param>
		/// <param name="definedBins">internal use</param>
		/// <returns>A collection of records based on <paramref name="records" /> or an empty collection.
		/// If a key is not found, there will be no bins associated with the record.</returns>
		/// <seealso cref="BatchRead{P}(string, IEnumerable{P}, BatchPolicy, BatchReadPolicy, Expression, string[], ARecord.DumpTypes, string[])" />
		/// <seealso cref="BatchRead(IEnumerable{Key}, BatchPolicy, BatchReadPolicy, Expression, string[], ARecord.DumpTypes, string[])"/>
		public IEnumerable<ARecord> BatchRead([NotNull] IEnumerable<ARecord> records,
                                                    BatchPolicy batchPolicy = null,
                                                    BatchReadPolicy batchReadPolicy = null,
                                                    Expression filterExpression = null,
                                                    string[] returnBins = null,
                                                    ARecord.DumpTypes dumpType = ARecord.DumpTypes.Record,
                                                    string[] definedBins = null)
            => this.BatchRead(records.Select(r => r.Aerospike.Key),
                                batchPolicy,
                                batchReadPolicy,
                                filterExpression,
                                returnBins,
                                dumpType,
                                definedBins);

			#endregion

		#endregion

		/// <summary>
		/// Truncates all the Sets in this namespace
		/// </summary>
		/// <param name="infoPolicy">
		/// The <see cref="InfoPolicy"/> used for the truncate. If not provided, the default is used.
		/// </param>
		/// <param name="before">
		/// A Date/time used to truncate the set. Records before this time will be truncated. 
		/// The default is everything up to when this was executed (DateTime.Now).
		/// </param>
		/// <seealso cref="SetRecords.Truncate(InfoPolicy, DateTime?)"/>
		/// <seealso cref="Truncate(string, InfoPolicy, DateTime?)"/>
		/// <exception cref="InvalidOperationException">Thrown if the cluster is a production cluster. Can disable this by going into the connection properties.</exception>
		public void Truncate(InfoPolicy infoPolicy = null, DateTime? before = null)
		{
			if(this.AerospikeConnection.CXInfo.IsProduction)
				throw new InvalidOperationException("Cannot Truncate a Cluster marked \"In Production\"");

			foreach(var set in this.Sets)
			{
				set.Truncate(infoPolicy, before);
			}
		}

		/// <summary>
		/// Truncates the Set
		/// </summary>
		/// <param name="setName">
		/// The name of the set to be truncated. 
		/// </param>
		/// <param name="infoPolicy">
		/// The <see cref="InfoPolicy"/> used for the truncate. If not provided, the default is used.
		/// </param>
		/// <param name="before">
		/// A Date/time used to truncate the set. Records before this time will be truncated. 
		/// The default is everything up to when this was executed (DateTime.Now).
		/// </param>
		/// <returns>
		/// True if the set was truncated or false to indicate the set did not exist in the namespace.
		/// </returns>
		/// <seealso cref="Truncate(InfoPolicy, DateTime?)"/>
		/// <seealso cref="this[string]"/>
		/// <seealso cref="SetRecords.Truncate(InfoPolicy, DateTime?)"/>
		/// <exception cref="InvalidOperationException">Thrown if the cluster is a production cluster. Can disable this by going into the connection properties.</exception>
		public bool Truncate(string setName, InfoPolicy infoPolicy = null, DateTime? before = null)
		{
			var set = this[setName];
			if(set != null)
			{
				set.Truncate(infoPolicy, before);
				return true;
			}

			return false;
		}

		#endregion

		#region Import/Export/Json
		/// <summary>
		/// Imports a <see cref="SetRecords.Export(string, Exp, bool)"/> generated JSON file into a set.
		/// </summary>
		/// <param name="importJSONFile">The JSON file that will be read</param>
		/// <param name="setName">Set name or null for the null set. This can be a new set that will be created.</param>
		/// <param name="writePolicy">
		/// The write policy. If not provided, the default policy is used.
		/// <seealso cref="WritePolicy"/>
		/// </param>
		/// <param name="ttl">
		/// Time-to-live of the records being imported
		/// Note: This is only used, if <paramref name="useImportRecTTL"/> is false.
		/// Also, The <see cref="Aerospike.Client.BatchWritePolicy.expiration"/> property is overwritten  with this value after a copy is made of the policy instance.
		/// <see cref="ARecord.AerospikeAPI.TTL"/>
		/// <see cref="ARecord.AerospikeAPI.Expiration"/>
		/// </param>
		/// <param name="useImportRecTTL">
		/// If true, the TTL of the record at export is used.
		/// Otherwise, <paramref name="ttl"/> is used, if provided.
		/// Note: If true <paramref name="batchPolicy"/> and <paramref name="batchWritePolicy"/> are ignored since batch writes cannot be performed.
		/// </param>
		/// <param name="maxDegreeOfParallelism">
		/// The maximum degree of parallelism.
		/// <see cref="ParallelOptions.MaxDegreeOfParallelism"/>
		/// </param>
		/// <param name="batchPolicy">
		/// <see cref="Aerospike.Client.BatchPolicy"/>
		/// </param>
		/// <param name="batchWritePolicy">
		/// <see cref="Aerospike.Client.BatchWritePolicy"/>
		/// </param>
		/// <param name="useParallelPuts">
		/// If true, Parallel Put actions are used based on <paramref name="maxDegreeOfParallelism"/> is used instead of batch writes.
		/// </param>
		/// <param name="cancellationToken">
		/// The <see cref="System.Threading.CancellationToken">CancellationToken</see>
		/// associated with this <see cref="ParallelOptions"/> instance.
		/// </param>
		/// <returns>The number of records imported</returns>
		/// <seealso cref="Export(string, Exp, bool)"/>
		/// <seealso cref="Import(string, WritePolicy, TimeSpan?, bool, int, BatchPolicy, BatchWritePolicy, bool, CancellationToken)"/>
		/// <seealso cref="SetRecords.Export(string, Exp, bool)"/>
		/// <seealso cref="SetRecords.Import(string, WritePolicy, TimeSpan?, bool, int, BatchPolicy, BatchWritePolicy, bool, CancellationToken)"/>
		/// <seealso cref="AClusterAccess.Import(string, string, string, WritePolicy, int, CancellationToken)"/>
		/// <exception cref="InvalidOperationException">Thrown if the cluster is a production cluster. Can disable this by going into the connection properties.</exception>        
		public int Import([NotNull] string importJSONFile,
                            string setName,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                            bool useImportRecTTL = false,
                            int maxDegreeOfParallelism = -1,
							BatchPolicy batchPolicy = null,
							BatchWritePolicy batchWritePolicy = null,
							bool useParallelPuts = false,
							CancellationToken cancellationToken = default)
        {
			//Debugger.Launch();
			int failedImports = 0;

			if(this.AerospikeConnection.CXInfo.IsProduction)
				throw new InvalidOperationException("Cannot Import into Cluster marked \"In Production\"");

			var jsonStr = System.IO.File.ReadAllText(importJSONFile);
			var jsonSettings = new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.All,
				NullValueHandling = NullValueHandling.Ignore,
				DateParseHandling = DateParseHandling.DateTimeOffset
			};
			var jsonStructs = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonExportStructure[]>(jsonStr, jsonSettings);

			if(maxDegreeOfParallelism == -1
					&& this.AerospikeConnection.DBPlatform == DBPlatforms.Native)
				maxDegreeOfParallelism = Environment.ProcessorCount;

			var parallelOptions = new ParallelOptions()
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = maxDegreeOfParallelism
			};

			if(useImportRecTTL || useParallelPuts)
			{
				Parallel.ForEach(jsonStructs, parallelOptions,
				 item =>
				 {
					 this.Put(setName == string.Empty? item.SetName : setName,
								item.KeyValue ?? item.Digest,
								item.Values,
								writePolicy,
								useImportRecTTL
									? ARecord.AerospikeAPI.CalcTTLTimeSpan(item.TimeToLive)
									: ttl);
				 });
			}
			else
			{
				if(ttl.HasValue)
				{
					if(batchWritePolicy is null)
					{
						batchWritePolicy = new BatchWritePolicy();
					}
					else
					{
						batchWritePolicy = new BatchWritePolicy(batchWritePolicy);
					}

					batchWritePolicy.expiration = SetRecords.DetermineExpiration(ttl.Value);
				}

				batchPolicy ??= new BatchPolicy(this.DefaultWritePolicy)
				{
					maxRetries = this.DefaultWritePolicy.maxRetries,
					sendKey = this.DefaultWritePolicy.sendKey,
					maxConcurrentThreads = 5,
					sleepBetweenRetries = this.DefaultWritePolicy.sleepBetweenRetries,
                    Txn = this.GetAerospikeTxn()
				};

				batchWritePolicy ??= new BatchWritePolicy()
				{
					sendKey = this.DefaultWritePolicy.sendKey,
					recordExistsAction = this.DefaultWritePolicy.recordExistsAction,
                    commitLevel = this.DefaultWritePolicy.commitLevel
				};

				var batchArray = new BatchRecord[jsonStructs.Length];
				var allBins = new ConcurrentQueue<Bin[]>();

				Parallel.For(0, batchArray.Length, parallelOptions, idx =>
				{
					var record = jsonStructs[idx];
					var bins = Helpers.CreateBinRecord(record.Values);
					var operations = new Operation[bins.Length];
					allBins.Enqueue(bins);

					for(int i = 0; i < operations.Length; ++i)
					{
						operations[i] = Operation.Put(bins[i]);
					}

					batchArray[idx] = new BatchWrite(batchWritePolicy,
														Helpers.DetermineAerospikeKey(record.KeyValue,
                                                                                        this.Namespace,
																						setName == string.Empty ? record.SetName : setName),
														operations);
					this.AddDynamicSet(record.SetName, bins);
				});

				if(!this.AerospikeConnection.AerospikeClient.Operate(batchPolicy,
																		batchArray.ToList()))
				{
					failedImports = batchArray.Count(i => i.resultCode != ResultCode.OK);
				}				
			}

			return jsonStructs.Length - failedImports;
		}

        /// <summary>
        /// Imports a <see cref="SetRecords.Export(string, Exp, bool)"/> generated JSON file into the originally exported set.
        /// </summary>
        /// <param name="importJSONFile">The JSON file that will be read</param>
        /// <param name="writePolicy">
        /// The write policy. If not provided, the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">
        /// Time-to-live of the records being imported
        /// Note: This is only used, if <paramref name="useImportRecTTL"/> is false.
        /// Also, The <see cref="Aerospike.Client.BatchWritePolicy.expiration"/> property is overwritten  with this value after a copy is made of the policy instance.
        /// <see cref="ARecord.AerospikeAPI.TTL"/>
        /// <see cref="ARecord.AerospikeAPI.Expiration"/>
        /// </param>
        /// <param name="useImportRecTTL">
        /// If true, the TTL of the record at export is used.
        /// Otherwise, <paramref name="ttl"/> is used, if provided.
        /// Note: If true <paramref name="batchPolicy"/> and <paramref name="batchWritePolicy"/> are ignored since batch writes cannot be performed.
        /// </param>
        /// <param name="maxDegreeOfParallelism">
        /// The maximum degree of parallelism.
        /// <see cref="ParallelOptions.MaxDegreeOfParallelism"/>
        /// </param>
        /// <param name="batchPolicy">
        /// <see cref="Aerospike.Client.BatchPolicy"/>
        /// </param>
        /// <param name="batchWritePolicy">
        /// <see cref="Aerospike.Client.BatchWritePolicy"/>
        /// </param>
        /// <param name="useParallelPuts">
        /// If true, Parallel Put actions are used based on <paramref name="maxDegreeOfParallelism"/> is used instead of batch writes.
        /// </param>
        /// <param name="cancellationToken">
        /// The <see cref="System.Threading.CancellationToken">CancellationToken</see>
        /// associated with this <see cref="ParallelOptions"/> instance.
        /// </param>
        /// <returns>The number of records imported</returns>
        /// <seealso cref="Export(string, Exp, bool)"/>
        /// <seealso cref="Import(string, string, WritePolicy, TimeSpan?, bool, int, BatchPolicy, BatchWritePolicy, bool, CancellationToken)"/>
        /// <seealso cref="SetRecords.Export(string, Exp, bool)"/>
        /// <seealso cref="AClusterAccess.Import(string, string, string, WritePolicy, int, CancellationToken)"/>
        public int Import([NotNull] string importJSONFile,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                            bool useImportRecTTL = false,
                            int maxDegreeOfParallelism = -1,
                            BatchPolicy batchPolicy = null,
                            BatchWritePolicy batchWritePolicy = null,
                            bool useParallelPuts = false,
                            CancellationToken cancellationToken = default)
        => this.Import(importJSONFile,
                        string.Empty,
                        writePolicy: writePolicy,
                        ttl: ttl,
                        useImportRecTTL: useImportRecTTL,
                        maxDegreeOfParallelism: maxDegreeOfParallelism,
                        batchPolicy: batchPolicy,
                        batchWritePolicy: batchWritePolicy,
                        useParallelPuts: useParallelPuts,
                        cancellationToken: cancellationToken);

		/// <summary>
		/// This will import a Json file and convert it into a DB record for update into the DB.
		/// Each line in the file is treated as a new DB record.
		/// 
		/// Each top-level property in the Json is translated into a bin and value. Json Arrays and embedded objects are transformed into an Aerospike List or Map&lt;string,object&gt;.
		/// Note: If the Json string is a Json Object, the following behavior occurs:
		///         If <paramref name="jsonBinName"/> is provided, the Json object is treated as an Aerospike document which will be associated with that bin.
		///         if <paramref name="jsonBinName"/> is null, each json property in that Json object is treated as a separate bin/value.
		/// You can also insert individual records by calling <see cref="FromJson(string, string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>.
		/// </summary>
		/// <param name="importJSONFile">
		/// The file containing Json value where each line is a separate DB record.
		/// </param>
		/// <param name="setName">
		/// Set name or null for the null set. This can be a new set that will be created.
		/// </param>		
		/// <param name="maxDegreeOfParallelism">
		/// The maximum degree of parallelism.
		/// <see cref="ParallelOptions.MaxDegreeOfParallelism"/>
		/// </param>
		/// <param name="batchPolicy">
		/// <see cref="Aerospike.Client.BatchPolicy"/>
		/// </param>
		/// <param name="batchWritePolicy">
		/// <see cref="Aerospike.Client.BatchWritePolicy"/>
		/// </param>
		/// <param name="useParallelPuts">
		/// If true, Parallel Put actions are used based on <paramref name="maxDegreeOfParallelism"/> is used instead of batch writes.
		/// </param>		
		/// <param name="pkPropertyName">
		/// The property name used for the primary key. The default is &apos;_id&apos;.
		/// If the primary key value is not present, the digest is used. In these cases the property value will be a sub property where that name will be &apos;$oid&apos; and the value is a byte string.
		/// </param>
		/// <param name="writePKPropertyName">
		/// If true, the <paramref name="pkPropertyName"/>, is written to the record.
		/// If false (default), it will not be written to the set (only used to define the PK).
		/// </param>
		/// <param name="jsonBinName">
		/// If provided, the Json object is placed into this bin.
		/// If null (default), the each top level Json property will be associated with a bin. Note, if the property name is greater than the bin name limit, an Aerospike exception will occur during the put.
		/// </param>
		/// <param name="writePolicy">
		/// The write policy. If not provided , the default policy is used.
		/// <seealso cref="WritePolicy"/>
		/// </param>
		/// <param name="ttl">Time-to-live of the record</param>		
		/// <param name="treatEmptyStrAsNull">
		/// If true, default, these properties with an empty string value will be considered null (bin not saved).
		/// If false, these properties with an empty string value will have a bin value of empty string.
		/// </param>
		/// <param name="cancellationToken">
		/// The <see cref="System.Threading.CancellationToken">CancellationToken</see>
		/// associated with this <see cref="ParallelOptions"/> instance.
		/// </param>
		/// <returns>
		/// Number of records inserted.
		/// </returns>
		/// <exception cref="InvalidOperationException">Thrown if the cluster is a production cluster. Can disable this by going into the connection properties.</exception>
		public int ImportJsonFile([NotNull] string importJSONFile,
							        string setName,
									string pkPropertyName = "_id",
								    string jsonBinName = null,								
								    bool writePKPropertyName = false,
									WritePolicy writePolicy = null,
							        TimeSpan? ttl = null,
							        int maxDegreeOfParallelism = -1,
							        BatchPolicy batchPolicy = null,
							        BatchWritePolicy batchWritePolicy = null,
							        bool useParallelPuts = false,
									bool treatEmptyStrAsNull = true,
									CancellationToken cancellationToken = default)
		{
			//Debugger.Launch();

			int failedImports = 0;

			if(this.AerospikeConnection.CXInfo.IsProduction)
				throw new InvalidOperationException("Cannot Import into Cluster marked \"In Production\"");

			var jsonRecsTask = System.IO.File.ReadAllLinesAsync(importJSONFile, cancellationToken);
			
			if(maxDegreeOfParallelism == -1
					&& this.AerospikeConnection.DBPlatform == DBPlatforms.Native)
				maxDegreeOfParallelism = Environment.ProcessorCount;

			var parallelOptions = new ParallelOptions()
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = maxDegreeOfParallelism
			};

            var jsonRecs = jsonRecsTask.Result;

			if(useParallelPuts)
			{
				Parallel.ForEach(jsonRecs, parallelOptions,
				 jsonRec =>
				 {
					 this.FromJson(setName,
                                    jsonRec,
                                    pkPropertyName: pkPropertyName,
                                    jsonBinName: jsonBinName,
                                    writePolicy: writePolicy,
                                    ttl: ttl,
                                    treatEmptyStrAsNull: treatEmptyStrAsNull,
                                    writePKPropertyName: writePKPropertyName);
				 });
			}
			else
			{
				if(ttl.HasValue)
				{
					if(batchWritePolicy is null)
					{
						batchWritePolicy = new BatchWritePolicy();
					}
					else
					{
						batchWritePolicy = new BatchWritePolicy(batchWritePolicy);
					}

					batchWritePolicy.expiration = SetRecords.DetermineExpiration(ttl.Value);
				}

				batchPolicy ??= new BatchPolicy(this.DefaultWritePolicy)
				{
					maxRetries = this.DefaultWritePolicy.maxRetries,
					sendKey = this.DefaultWritePolicy.sendKey,
					maxConcurrentThreads = 5,
					sleepBetweenRetries = this.DefaultWritePolicy.sleepBetweenRetries,
                    Txn = this.GetAerospikeTxn()
				};

				batchWritePolicy ??= new BatchWritePolicy()
				{
					sendKey = this.DefaultWritePolicy.sendKey,
					recordExistsAction = this.DefaultWritePolicy.recordExistsAction
				};

				var batchArray = new BatchRecord[jsonRecs.Length];
				var allBins = new ConcurrentQueue<Bin[]>();

				Parallel.For(0, batchArray.Length, parallelOptions, idx =>
				{
                    var jsonRec = jsonRecs[idx];
                    var record = ARecord.FromJson(this.Namespace,
                                                    setName,
                                                    jsonRec,
                                                    pkPropertyName,
                                                    jsonBinName,
                                                    this,
                                                    writePKPropertyName);
					var bins = record.Aerospike.Bins;
					var operations = new Operation[bins.Length];
					allBins.Enqueue(bins);

					for(int i = 0; i < operations.Length; ++i)
					{
						operations[i] = Operation.Put(bins[i]);
					}

					batchArray[idx] = new BatchWrite(batchWritePolicy,
														record.Aerospike.Key,
														operations);
					this.AddDynamicSet(setName, bins);
				});

				if(!this.AerospikeConnection.AerospikeClient.Operate(batchPolicy,
																		batchArray.ToList()))
				{
					failedImports = batchArray.Count(i => i.resultCode != ResultCode.OK);
				}
			}

			return jsonRecs.Length - failedImports;
		}

		/// <summary>
		/// Exports all the records in this namespace to a JSON file.
		/// </summary>
		/// <remarks>This just exports the Aerospike <see cref="NullSet"/></remarks>
		/// <param name="exportJSONFile">
		/// The JSON file where the JSON will be written.
		/// If this is an existing directory, the file name will be generated where the name is this namespace name with the JSON extension.
		/// If the file exists it will be overwritten or created.
		/// </param>
		/// <param name="filterExpression">A filter expression that will be applied that will determine the result set.</param>
		/// <param name="indented">If true the JSON string is formatted for readability</param>
		/// <returns>Number of records written</returns>
		/// <seealso cref="SetRecords.Import(string, WritePolicy, TimeSpan?, bool, int, BatchPolicy, BatchWritePolicy, bool, CancellationToken)"/>
		/// <seealso cref="ANamespaceAccess.Import(string, string, WritePolicy, TimeSpan?, bool, int, BatchPolicy, BatchWritePolicy, bool, CancellationToken)"/>
		/// <seealso cref="ANamespaceAccess.Import(string, WritePolicy, TimeSpan?, bool, int, BatchPolicy, BatchWritePolicy, bool, CancellationToken)"/>
		/// <seealso cref="ARecord.Export(bool, JsonSerializerSettings)"/>
		public int Export([NotNull] string exportJSONFile, Client.Exp filterExpression = null, bool indented = true)
        {
            if (Directory.Exists(exportJSONFile))
            {
                exportJSONFile = Path.Combine(exportJSONFile, $"{this.Namespace}.json");
            }

            return NullSet.Export(exportJSONFile, filterExpression, indented);
        }

        /// <summary>
        /// Creates a Json Array of all records in the set.
        /// </summary>
        /// <param name="setName">Set name or null for the null set.</param>
        /// <param name="pkPropertyName">
        /// The property name used for the primary key. The default is &apos;_id&apos;.
        /// If the primary key value is not present, the digest is used. In these cases the property value will be a sub property where that name will be &apos;$oid&apos; and the value is a byte string.
        /// If this is null, no PK property is written. 
        /// </param>
        /// <param name="useDigest">
        /// If true, always use the PK digest as the primary key.
        /// If false, use the PK value is present, otherwise use the digest. 
        /// Default is false.
        /// </param>
        /// <returns>Json Array of the records in the set.</returns>
        /// <seealso cref="FromJson(string, string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="FromJson(string, string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
        /// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
        /// <seealso cref="ARecord.ToJson(string, bool)"/>
        /// <seealso cref="SetRecords.ToJson(Exp, string, bool)"/>
        /// <seealso cref="Aerospike.Client.Exp"/>
        public JArray ToJson(string setName, [AllowNull] string pkPropertyName = "_id", bool useDigest = false)
        {
            var jsonArray = new JArray();

            foreach (var rec in this.AsEnumerable(setName))
            {
                jsonArray.Add(rec.ToJson(pkPropertyName, useDigest));
            }

            return jsonArray;
        }

		/// <summary>
		/// Converts a Json string into an <see cref="ARecord"/> which is than put into <paramref name="setName"/>.
		/// Each top-level property in the Json is translated into a bin and value. Json Arrays and embedded objects are transformed into an Aerospike List or Map&lt;string,object&gt;.
		/// Note: If the Json string is an Json Array, each element is treated as a separate record. 
		///         If the Json string is a Json Object, the following behavior occurs:
		///             If <paramref name="jsonBinName"/> is provided, the Json object is treated as an Aerospike document which will be associated with that bin.
		///             if <paramref name="jsonBinName"/> is null, each json property in that Json object is treated as a separate bin/value.
		///         You can also insert individual records by calling <see cref="FromJson(string, string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>.
		/// </summary>
		/// <param name="setName">Set name or null for the null set. This can be a new set that will be created.</param>
		/// <param name="json">
		/// The Json string. 
		/// note: in-line json types are supported.
		///     Example:
		///         <code>&quot;bucket_start_date&quot;: &quot;$date&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
		/// </param>
		/// <param name="pkPropertyName">
		/// The property name used for the primary key. The default is &apos;_id&apos;.
		/// If the primary key value is not present, the digest is used. In these cases the property value will be a sub property where that name will be &apos;$oid&apos; and the value is a byte string.
		/// </param>
		/// <param name="writePKPropertyName">
		/// If true, the <paramref name="pkPropertyName"/>, is written to the record.
		/// If false (default), it will not be written to the set (only used to define the PK).
		/// </param>
		/// <param name="jsonBinName">
		/// If provided, the Json object is placed into this bin.
		/// If null (default), the each top level Json property will be associated with a bin. Note, if the property name is greater than the bin name limit, an Aerospike exception will occur during the put.
		/// </param>
		/// <param name="writePolicy">
		/// The write policy. If not provided , the default policy is used.
		/// <seealso cref="WritePolicy"/>
		/// </param>
		/// <param name="ttl">Time-to-live of the record</param>
		/// <param name="insertIntoList">
		/// If provided, records are inserted into this collection instead of inserting into the database.
		/// </param>
		/// <param name="treatEmptyStrAsNull">
		/// If true, default, these properties with an empty string value will be considered null (bin not saved).
		/// If false, these properties with an empty string value will have a bin value of empty string.
		/// </param>
		/// <returns>The number of items put.</returns>
		/// <seealso cref="ToJson(string, string, bool)"/>
		/// <seealso cref="ARecord.ToJson(string, bool)"/>
		/// <seealso cref="FromJson(string, string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
		/// <seealso cref="SetRecords.FromJson(string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
		/// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
		/// <seealso cref="ARecord.FromJson(string, string, string, string, string, ANamespaceAccess, bool, bool)"/>
		/// <seealso cref="Put(ARecord, string, WritePolicy, TimeSpan?)"/>
		/// <exception cref="KeyNotFoundException">
		/// Thrown if the <paramref name="pkPropertyName"/> is not found as a top-level field. 
		/// </exception>
		/// <exception cref="InvalidDataException">
		/// Thrown if an unexpected data type is encountered.
		/// </exception>
		/// <remarks>
		/// The Json string can include Json in-line types. Below are the supported types:
		///     <code>$date</code> or <code>$datetime</code>,
		///         This can include an optional sub Json Type.Example:
		///             <code>&quot;bucket_start_date&quot;: &quot;$date&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
		///     <code>$datetimeoffset</code>,
		///         This can include an optional sub Json Type. Example:
		///             <code>&quot;bucket_start_datetimeoffset&quot;: &quot;$datetimeoffset&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
		///     <code>$timespan</code>,
		///         This can include an optional sub Json Type. Example:
		///             <code>&quot;bucket_start_time&quot;: &quot;$timespan&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
		///     <code>$timestamp</code>,
		///     <code>$guid</code> or <code>$uuid</code>,
		///     <code>$oid</code>,
		///         If the Json string value equals 40 in length it will be treated as a digest and converted into a byte array.
		///         Example:
		///             <code>&quot;_id&quot;: { &quot;$oid &quot;: &quot;0080a245fabe57999707dc41ced60edc4ac7ac40&quot; }</code> ==&gt; <code>&quot;_id&quot;:[00 80 A2 45 FA BE 57 99 97 07 DC 41 CE D6 0E DC 4A C7 AC 40]</code>
		///     <code>$numberint64</code> or <code>$numberlong</code>,
		///     <code>$numberint32</code>, or <code>$numberint</code>,
		///     <code>$numberdecimal</code>,
		///     <code>$numberdouble</code>,
		///     <code>$numberfloat</code> or <code>$single</code>,
		///     <code>$numberint16</code> or <code>$numbershort</code>,
		///     <code>$numberuint32</code> or <code>$numberuint</code>,
		///     <code>$numberuint64</code> or <code>$numberulong</code>,
		///     <code>$numberuint16</code> or <code>$numberushort</code>,
		///     <code>$bool</code> or <code>$boolean</code>;
		/// </remarks>
		public int FromJson(string setName,
                                string json,
                                string pkPropertyName = "_id",
                                string jsonBinName = null,
                                WritePolicy writePolicy = null,
                                TimeSpan? ttl = null,
                                bool writePKPropertyName = false,
                                IList<ARecord> insertIntoList = null,
                                bool treatEmptyStrAsNull = true)
        {            
            var converter = new CDTConverter(treatEmptyStrAsNull);
            var bins = JsonConvert.DeserializeObject<object>(json, converter);
            int cnt = 0;

            (object pk, Dictionary<string, object> recBins) GetRecord(Dictionary<string, object> binDict)
            {
                var primaryKeyValue = binDict[pkPropertyName];
                if (!writePKPropertyName)
                    binDict.Remove(pkPropertyName);

                return (primaryKeyValue,
                        string.IsNullOrEmpty(jsonBinName)
                            ? binDict
                            : new Dictionary<string, object>() { { jsonBinName, binDict } });
            }

            if (bins is Dictionary<string, object> binDictionary)
            {
                var (pk, recBins) = GetRecord(binDictionary);

                if (insertIntoList is null)
                {
                    this.Put(setName,
                                pk,
                                recBins,
                                writePolicy: writePolicy,
                                ttl: ttl);
                }
                else
                {
                    insertIntoList.Add(new ARecord(this.Namespace,
                                                    setName,
                                                    pk,
                                                    recBins,
                                                    expirationDate: ttl.HasValue
                                                                        ? DateTimeOffset.Now + ttl.Value
                                                                        : null));
                }
                cnt++;
            }
            else if (bins is List<object> binList)
            {
                foreach (var item in binList)
                {
                    if (item is Dictionary<string, object> binDict)
                    {
                        var (pk, recBins) = GetRecord(binDict);

                        if (insertIntoList is null)
                        {
                            this.Put(setName,
                                        pk,
                                        recBins,
                                        writePolicy: writePolicy,
                                        ttl: ttl);
                        }
                        else
                        {
                            insertIntoList.Add(new ARecord(this.Namespace,
                                                            setName,
                                                            pk,
                                                            recBins,
                                                            expirationDate: ttl.HasValue
                                                                                ? DateTimeOffset.Now + ttl.Value
                                                                                : null));
                        }
                        cnt++;
                    }
                    else
                        throw new InvalidDataException($"An unexpected data type was encounter. Except a Dictionary<string, object> but received a {item.GetType()}.");
                }
            }
            else
                throw new InvalidDataException($"An unexpected data type was encounter. Except a Dictionary<string, object> or List<object> but received a {bins.GetType()}.");

            return cnt;
        }

        /// <summary>
        /// Converts a Json string into an <see cref="ARecord"/> which is than put into <paramref name="setName"/>.
        /// Each top-level property in the Json is translated into a bin and value. Json Arrays and embedded objects are transformed into an Aerospike List or Map&lt;string,object&gt;.
        /// 
        /// Note: If <paramref name="jsonBinName"/> is provided the Json item will completely be placed into this bin as its' value.
        /// </summary>
        /// <param name="setName">Set name or null for the null set. This can be a new set that will be created.</param>
        /// <param name="json">
        /// The Json string. 
        /// note: in-line json types are supported.
        ///     Example:
        ///         <code>&quot;bucket_start_date&quot;: &quot;$date&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
        /// </param>
        /// <param name="primaryKey">
        /// Primary AerospikeKey, if provided. If null, the Json object will have to provide a PK based on <paramref name="jsonBinName"/>.
        /// This can be a <see cref="Client.Key"/>, <see cref="Value"/>, or <see cref="Bin"/> object besides a native, collection, etc. value/object.
        /// </param>
        /// <param name="pkPropertyName">
        /// The property name used for the primary key only if <paramref name="primaryKey"/> is null. The default is &apos;_id&apos;.
        /// </param> 
        /// <param name="writePKPropertyName">
        /// If true, the <paramref name="pkPropertyName"/>, is written to the record.
        /// If false (default), it will not be written to the set (only used to define the PK).
        /// </param>
        /// <param name="jsonBinName">
        /// If provided, the Json object is placed into this bin.
        /// If null (default), the each top level Json property will be associated with a bin. Note, if the property name is greater than the bin name limit, an Aerospike exception will occur during the put.
        /// </param>
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>
        /// <returns>The number of items put.</returns>
        /// <seealso cref="ToJson(string, string, bool)"/>
        /// <seealso cref="ARecord.ToJson(string, bool)"/>
        /// <seealso cref="SetRecords.FromJson(string, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
        /// <seealso cref="ARecord.FromJson(string, string, string, string, string, ANamespaceAccess, bool, bool)"/>
        /// <seealso cref="Put(ARecord, string, WritePolicy, TimeSpan?)"/>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the <paramref name="pkPropertyName"/> is not found as a top-level field. 
        /// </exception>
        /// <exception cref="InvalidDataException">
        /// Thrown if an unexpected data type is encountered.
        /// </exception>
        /// <remarks>
        /// The Json string can include Json in-line types. Below are the supported types:
        ///     <code>$date</code> or <code>$datetime</code>,
        ///         This can include an optional sub Json Type.Example:
        ///             <code>&quot;bucket_start_date&quot;: &quot;$date&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
        ///     <code>$datetimeoffset</code>,
        ///         This can include an optional sub Json Type. Example:
        ///             <code>&quot;bucket_start_datetimeoffset&quot;: &quot;$datetimeoffset&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
        ///     <code>$timespan</code>,
        ///         This can include an optional sub Json Type. Example:
        ///             <code>&quot;bucket_start_time&quot;: &quot;$timespan&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
        ///     <code>$timestamp</code>,
        ///     <code>$guid</code> or <code>$uuid</code>,
        ///     <code>$oid</code>,
        ///         If the Json string value equals 40 in length it will be treated as a digest and converted into a byte array.
        ///         Example:
        ///             <code>&quot;_id&quot;: { &quot;$oid &quot;: &quot;0080a245fabe57999707dc41ced60edc4ac7ac40&quot; }</code> ==&gt; <code>&quot;_id&quot;:[00 80 A2 45 FA BE 57 99 97 07 DC 41 CE D6 0E DC 4A C7 AC 40]</code>
        ///     <code>$numberint64</code> or <code>$numberlong</code>,
        ///     <code>$numberint32</code>, or <code>$numberint</code>,
        ///     <code>$numberdecimal</code>,
        ///     <code>$numberdouble</code>,
        ///     <code>$numberfloat</code> or <code>$single</code>,
        ///     <code>$numberint16</code> or <code>$numbershort</code>,
        ///     <code>$numberuint32</code> or <code>$numberuint</code>,
        ///     <code>$numberuint64</code> or <code>$numberulong</code>,
        ///     <code>$numberuint16</code> or <code>$numberushort</code>,
        ///     <code>$bool</code> or <code>$boolean</code>;
        /// </remarks>
        public int FromJson(string setName,
                                string json,
                                [AllowNull]
                                dynamic primaryKey,
                                string pkPropertyName = "_id",
                                string jsonBinName = null,
                                WritePolicy writePolicy = null,
                                TimeSpan? ttl = null,
                                bool writePKPropertyName = false)
        {
            var converter = new CDTConverter();
            var bins = JsonConvert.DeserializeObject<object>(json, converter);
            int cnt = 0;
            Client.Key PKValue = null;

            if (primaryKey != null)
            {
                PKValue = Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName);
            }

            (object pk, Dictionary<string, object> recBins) GetRecord(Dictionary<string, object> binDict)
            {
                var primaryKeyValue = PKValue ?? binDict[pkPropertyName];
                if (!writePKPropertyName)
                    binDict.Remove(pkPropertyName);

                return (primaryKeyValue,
                        string.IsNullOrEmpty(jsonBinName)
                            ? binDict
                            : new Dictionary<string, object>() { { jsonBinName, binDict } });
            }

            (object pk, Dictionary<string, object> recBins) GetRecordLst(List<object> binList)
            {
                var primaryKeyValue = PKValue;

                return(primaryKeyValue,
                        new Dictionary<string, object>() { { jsonBinName, binList } });
            }

            if (bins is Dictionary<string, object> binDictionary)
            {
                var (pk, recBins) = GetRecord(binDictionary);

                this.Put(setName,
                            pk,
                            recBins,
                            writePolicy: writePolicy,
                            ttl: ttl);
                cnt++;
            }
            else if (bins is List<object> binList)
            {
                if (string.IsNullOrEmpty(jsonBinName) || PKValue is null)
                {
                    throw new NullReferenceException("A jsonBinName and/or primaryKey parameter(s) are required for a Json Array on an individual record.");
                }

                var (pk, recBins) = GetRecordLst(binList);

                this.Put(setName,
                            pk,
                            recBins,
                            writePolicy: writePolicy,
                            ttl: ttl);
                cnt++;
            }
            else
                throw new InvalidDataException($"An unexpected data type was encounter. Except a Dictionary<string, object> or List<object> but received a {bins.GetType()}.");

            return cnt;
        }

		#endregion


		public virtual object ToDump()
            => LPU.ToExpando(this, include: "Namespace, DBPlatform, SetNames, BinNames, AerospikeConnection, DefaultReadPolicy, DefaultQueryPolicy, DefaultScanPolicy, DefaultWritePolicy");

	}
}
