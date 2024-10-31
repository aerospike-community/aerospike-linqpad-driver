// Ignore Spelling: Pset

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.IO;
using Aerospike.Client;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    
    [DebuggerDisplay("{ToString()}")]
    public abstract class SetRecords<T> : SetRecords, IEnumerable<T>
        where T : ARecord
    {
        #region Constructors
        public SetRecords([NotNull] LPSet lpSet,
                            [NotNull] ANamespaceAccess setAccess,
                            [NotNull] string setName,
                            params string[] bins)
            : base(lpSet, setAccess, setName, bins)
        { }

        public SetRecords([NotNull] ANamespaceAccess setAccess,
                           [NotNull] string setName,
                           params string[] bins)
           : base(setAccess, setName, bins)
        { }

        public SetRecords([NotNull] SetRecords<T> clone,
							Policy readPolicy = null,
							WritePolicy writePolicy = null,
							QueryPolicy queryPolicy = null,
							ScanPolicy scanPolicy = null)
           : base(clone, readPolicy, writePolicy, queryPolicy, scanPolicy)
        { }

		#endregion
		/// <summary>
		/// Initializes a new instance of <see cref="SetRecords{T}"/> as an Aerospike transactional unit.
		/// If <see cref="SetRecords.Commit"/> method is not called the server will abort (rollback) this transaction.
		/// </summary>
		/// <param name="baseSet">Base Aerospike Set instance</param>
		/// <param name="txn">
		/// The Aerospike <see cref="Txn"/> instance or null to create a new transactional unit.
		/// </param>
		/// <param name="newNSAccess">
		/// An new <see cref="ANamespaceAccess"/> instance to use with the transaction. 
		/// </param>
		/// <seealso cref="SetRecords.CreateTransaction"/>
		/// <seealso cref="SetRecords.Commit"/>
		/// <seealso cref="SetRecords.Abort"/>
		public SetRecords([NotNull] SetRecords baseSet,
                            [AllowNull] Txn txn,
							[AllowNull] ANamespaceAccess newNSAccess = null)
            : base(baseSet, txn, newNSAccess)
        { }

		/// <summary>
		/// Changes how records are displayed using the LinqPad <see cref="LINQPad.Extensions.Dump{T}(T)"/> method.        
		/// </summary>
		/// <param name="newRecordView">See <see cref="ARecord.DumpTypes"/> for more information.</param>
		/// <returns>This instance</returns>
		/// <seealso cref="ARecord.DumpTypes"/>
		/// <seealso cref="SetRecords.DefaultRecordView"/>
		public new SetRecords<T> ChangeRecordView(ARecord.DumpTypes newRecordView)
        {
            this.DefaultRecordView = newRecordView;
            return this;
        }

		#region Get Methods
		/// <summary>
		/// Returns the record based on the primary key
		/// </summary>
		/// <param name="primaryKey">
		/// The primary key can be a <see cref="Aerospike.Client.Key"/>, <see cref="Aerospike.Client.Value"/>, digest (byte[]), or a .net type.
		/// </param>
		/// <param name="bins">
		/// An optional arguments, if provided only those bins are returned.
		/// </param>
		/// <returns>
		/// A record if the primary key is found otherwise null.
		/// </returns>
		/// <seealso cref="Get(dynamic, Expression, string[])"/>
		public new T Get([NotNull] dynamic primaryKey, params string[] bins)
        {            
            Client.Key key = Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, this.SetName);

            var record = this.SetAccess
                                .AerospikeConnection
                                .AerospikeClient
                                .Get(this.DefaultReadPolicy, key, bins.Length == 0 ? null : bins);

            if (record == null) return null;

            return (T) CreateRecord(this.SetAccess,
                                        key,
                                        record,
                                        this._bins,
                                        this.BinsHashCode,
                                        recordView: this.DefaultRecordView,
										fkBins: this.DetermineFKBins(record));
        }

        /// <summary>
        /// Returns the record based on the primary key
        /// </summary>
        /// <param name="primaryKey">
        /// The primary key can be a <see cref="Aerospike.Client.Key"/>, <see cref="Aerospike.Client.Value"/>, digest (byte[]), or a .net type.
        /// </param>
        /// <param name="filterExpression">
        /// A filter expression that is applied after obtaining the record via the primary key.
        /// </param>
        /// <param name="bins">
        /// An optional arguments, if provided only those bins are returned.
        /// </param>
        /// <returns>
        /// A record if the primary key is found otherwise null.
        /// </returns>
        /// <seealso cref="Get(dynamic, string[])"/>
        public new T Get([NotNull] dynamic primaryKey, Expression filterExpression, params string[] bins)
        {
            Client.Key key = Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, this.SetName);

            var policy = new Client.Policy(this.DefaultReadPolicy) { filterExp = filterExpression };

            var record = this.SetAccess
                                .AerospikeConnection
                                .AerospikeClient
                                .Get(policy, key, bins.Length == 0 ? null : bins);

            if (record == null) return null;

            return (T)CreateRecord(this.SetAccess,
                                        key,
                                        record,
                                        this._bins,
                                        this.BinsHashCode,
                                        recordView: this.DefaultRecordView,
										fkBins: this.DetermineFKBins(record));
        }
        #endregion

        #region Query Methods

        /// <summary>
        /// Returns all the records based on the associated bins.
        /// </summary>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of all records
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(Exp, string[])"/>               
        new public IEnumerable<T> Query(params string[] bins)
        {
            var queryPolicy = new QueryPolicy(this.DefaultQueryPolicy);
            var stmt = new Statement();

            stmt.SetNamespace(this.Namespace);

            if (!string.IsNullOrEmpty(this.SetName) && this.SetName != LPSet.NullSetName)
                stmt.SetSetName(this.SetName);

            stmt.SetBinNames(bins);

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy, stmt);

            while (recordset.Next())
            {
                yield return (T)CreateRecord(this.SetAccess,
                                                recordset.Key,
                                                recordset.Record,
                                                this._bins,
                                                this.BinsHashCode,
                                                recordView: this.DefaultRecordView,
                                                fkBins: this.DetermineFKBins(recordset.Record));
            }
        }

        /// <summary>
        /// Performs a <see cref="Client.AerospikeClient.Query(QueryPolicy, Statement)"/> applying the expression filter.
        /// </summary>
        /// <param name="filterExpression">
        /// The Aerospike filter <see cref="Client.Exp"/> that will be applied.
        /// <seealso cref="Aerospike.Client.ListExp"/>
        /// <seealso cref="Aerospike.Client.MapExp"/>
        /// <seealso cref="Aerospike.Client.BitExp"/>
        /// <seealso cref="Aerospike.Client.HLLExp"/>
        /// </param>
        /// <param name="bins">Return only the bins provided in the result set</param>
        /// <returns>
        /// The result set based on the expression filter.
        /// </returns>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(string[])"/>
        /// <seealso cref="Operation"/>
        new public IEnumerable<T> Query([NotNull] Client.Exp filterExpression, params string[] bins)
        {
            var queryPolicy = new QueryPolicy(this.DefaultQueryPolicy) { filterExp = Exp.Build(filterExpression) };

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy,
                                           string.IsNullOrEmpty(this.SetName) || this.SetName == LPSet.NullSetName
                                                   ? new Statement() { Namespace = this.Namespace, BinNames = bins }
                                                   : new Statement() { Namespace = this.Namespace, SetName = this.SetName, BinNames = bins });

            while (recordset.Next())
            {
                yield return (T)CreateRecord(this.SetAccess,
                                                recordset.Key,
                                                recordset.Record,
                                                this._bins,
                                                this.BinsHashCode,
                                                recordView: this.DefaultRecordView,
                                                fkBins: this.DetermineFKBins(recordset.Record));
            }
        }

        /// <summary>
        /// Performs a secondary index query using the provided <see cref="Client.Filter"/>.
        /// </summary>
        /// <param name="secondaryIdxFilter">The filter used against the secondary index</param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filter.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Exp, string[])"/>
        /// <seealso cref="Query(string[])"/>
        /// <seealso cref="CreateIndex(string, string, IndexType)"/>
        /// <seealso cref="CreateIndex(string, string, IndexType, IndexCollectionType, CTX[])"/>
        new public IEnumerable<T> Query([NotNull] Client.Filter secondaryIdxFilter, params string[] bins)
        {
            var queryPolicy = new QueryPolicy(this.DefaultQueryPolicy);
            var stmt = new Statement();

            stmt.SetNamespace(this.Namespace);

            if (!string.IsNullOrEmpty(this.SetName) && this.SetName != LPSet.NullSetName)
                stmt.SetSetName(this.SetName);

            stmt.SetFilter(secondaryIdxFilter);
            stmt.SetBinNames(bins);

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy, stmt);

            while (recordset.Next())
            {
                yield return (T)CreateRecord(this.SetAccess,
                                                recordset.Key,
                                                recordset.Record,
                                                this._bins,
                                                this.BinsHashCode,
                                                recordView: this.DefaultRecordView,
                                                fkBins: this.DetermineFKBins(recordset.Record));
            }
        }

        /// <summary>
        /// Performs a secondary index query using the provided <see cref="Client.Filter"/> and than apply the filter expression.
        /// </summary>
        /// <param name="secondaryIdxFilter">The filter used against the secondary index</param>
        /// <param name="filterExpression">
        /// The Aerospike filter <see cref="Client.Exp"/> that will be applied after the index filter is applied.
        /// <seealso cref="Aerospike.Client.ListExp"/>
        /// <seealso cref="Aerospike.Client.MapExp"/>
        /// <seealso cref="Aerospike.Client.BitExp"/>
        /// <seealso cref="Aerospike.Client.HLLExp"/>
        /// </param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the <paramref name="filterExpression"/>.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(string[])"/>
        /// <seealso cref="Query(Exp, string[])"/>
        /// <seealso cref="CreateIndex(string, string, IndexType)"/>
        /// <seealso cref="CreateIndex(string, string, IndexType, IndexCollectionType, CTX[])"/>
        new public IEnumerable<T> Query([NotNull] Client.Filter secondaryIdxFilter, Client.Exp filterExpression, params string[] bins)
        {
            var queryPolicy = filterExpression == null
                                ? this.DefaultQueryPolicy
                                : new QueryPolicy(this.DefaultQueryPolicy) { filterExp = Exp.Build(filterExpression) };

            var stmt = new Statement();

            stmt.SetNamespace(this.Namespace);

            if (!string.IsNullOrEmpty(this.SetName) && this.SetName != LPSet.NullSetName)
                stmt.SetSetName(this.SetName);

            stmt.SetFilter(secondaryIdxFilter);
            stmt.SetBinNames(bins);

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy, stmt);

            while (recordset.Next())
            {
                yield return (T)CreateRecord(this.SetAccess,
                                                recordset.Key,
                                                recordset.Record,
                                                this._bins,
                                                this.BinsHashCode,
                                                recordView: this.DefaultRecordView,
                                                fkBins: this.DetermineFKBins(recordset.Record));
            }
        }

        #endregion

        #region Batch Methods

        /// <summary>
        /// Writes a collection of <see cref="ARecord"/> as a <seealso cref="Aerospike.Client.BatchPolicy"/> operation.
        /// </summary>
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
        /// <seealso cref="ANamespaceAccess.BatchWriteRecord{R}(IEnumerable{R}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
        public bool BatchWrite([NotNull] IEnumerable<T> writeRecords,
                                    BatchPolicy batchPolicy = null,
                                    BatchWritePolicy batchWritePolicy = null,
                                    ParallelOptions parallelOptions = null)
            => this.SetAccess.BatchWriteRecord(writeRecords, batchPolicy, batchWritePolicy, parallelOptions);

        /// <summary>
        /// Return a collection of <see cref="ARecord"/> based on <paramref name="primaryKeys"/>
        /// </summary>
        /// <typeparam name="P">Primary Key Type</typeparam>
        /// <param name="primaryKeys">A collection of Primarily Keys that will be part of the collection</param>
        /// <param name="batchPolicy">
        /// <seealso cref="BatchPolicy"/>
        /// </param>
        /// <param name="batchReadPolicy">
        /// <seealso cref="BatchReadPolicy"/>
        /// </param>
        /// <param name="filterExpression">The expression that will be applied to the result set. Can be null.</param>
        /// <param name="returnBins">A collection of bins that are returned</param>
        /// <returns>A collection of records based on <paramref name="primaryKeys"/> or an empty collection</returns>
        public new IEnumerable<T> BatchRead<P>([NotNull] IEnumerable<P> primaryKeys,
                                                BatchPolicy batchPolicy = null,
                                                BatchReadPolicy batchReadPolicy = null,
                                                Expression filterExpression = null,
                                                string[] returnBins = null)
        {
            batchPolicy ??= new BatchPolicy(this.DefaultWritePolicy)
            {
                maxRetries = 2,
                maxConcurrentThreads = 1,
                filterExp = filterExpression,
				Txn = this.AerospikeTxn
			};

            batchReadPolicy ??= new BatchReadPolicy()
            {
                filterExp = filterExpression
            };

            var batchList = new List<BatchRead>(primaryKeys.Count());

            foreach (var pk in primaryKeys)
            {
                if (returnBins is null)
                    batchList.Add(new BatchRead(batchReadPolicy,
                                                    Helpers.DetermineAerospikeKey(pk, this.Namespace, this.SetName),
                                                    true));
                else
                    batchList.Add(new BatchRead(batchReadPolicy,
                                                    Helpers.DetermineAerospikeKey(pk, this.Namespace, this.SetName),
                                                    returnBins));
            };

            this.SetAccess
                .AerospikeConnection
                .AerospikeClient
                .Get(batchPolicy, batchList);

            foreach (var batch in batchList)
            {
                yield return (T)CreateRecord(this.SetAccess,
                                                batch.key,
                                                batch.record,
                                                this._bins,
                                                this.BinsHashCode,
                                                recordView: this.DefaultRecordView);
            }
        }

        #endregion

        #region Linq Type Methods

        /// <summary>
        /// Returns the top number of records from the set based on <see cref="SetRecords.DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
        /// </summary>
        /// <param name="numberRecords">Number of records to return</param>
        /// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
        /// <returns>A collection of records or empty set</returns>
        /// <seealso cref="First(Exp)"/>
        /// <seealso cref="FirstOrDefault(Client.Exp)"/>
        /// <seealso cref="AsEnumerable(Client.Exp)"/>
        /// <seealso cref="Get(dynamic, string[])"/>
        /// <seealso cref="SetRecords.Operate(dynamic, Operation[])"/>
        /// <seealso cref="SetRecords.DefaultQueryPolicy"/>
        public new IEnumerable<T> Take(int numberRecords, Client.Exp filterExpression = null)
        {
            if (numberRecords <= 0) yield break;

            var queryPolicy = filterExpression == null
                                    ? this.DefaultQueryPolicy
                                    : new QueryPolicy(this.DefaultQueryPolicy) { filterExp = Exp.Build(filterExpression) };

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy,
                                           string.IsNullOrEmpty(this.SetName) || this.SetName == LPSet.NullSetName
                                           ? new Statement() { Namespace = this.Namespace, MaxRecords = numberRecords }
                                           : new Statement() { Namespace = this.Namespace, SetName = this.SetName, MaxRecords = numberRecords });

            while (recordset.Next())
            {
                yield return (T) CreateRecord(this.SetAccess,
                                                recordset.Key,
                                                recordset.Record,
                                                this._bins,
                                                this.BinsHashCode,
                                                recordView: this.DefaultRecordView,
												fkBins: this.DetermineFKBins(recordset.Record));
            }
        }

        /// <summary>
        /// Returns the first record from the set based on <see cref="SetRecords.DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
        /// </summary>
        /// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
        /// <returns></returns>
        /// <seealso cref="Take(int, Client.Exp)"/>
        /// <see cref="First(Func{T, bool}, Exp)"/>
        /// <seealso cref="FirstOrDefault(Client.Exp)"/>
        /// <seealso cref="FirstOrDefault(Func{T, bool}, Exp)"/>
        /// <seealso cref="AsEnumerable(Client.Exp)"/>
        /// <seealso cref="Get(dynamic, string[])"/>
        /// <seealso cref="SetRecords.Operate(dynamic, Operation[])"/>
        /// <seealso cref="SetRecords.DefaultQueryPolicy"/>
        public new T First(Client.Exp filterExpression = null)
                        => this.Take(1, filterExpression).First();        

        /// <summary>
        /// Returns the first record or null from the set based on <see cref="SetRecords.DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
        /// </summary>
        /// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
        /// <returns></returns>
        /// <seealso cref="Take(int, Client.Exp)"/>
        /// <seealso cref="First(Client.Exp)"/>
        /// <seealso cref="First(Func{T, bool}, Exp)"/>
        /// <seealso cref="FirstOrDefault(Func{T, bool}, Exp)"/>
        /// <seealso cref="AsEnumerable(Client.Exp)"/>
        /// <seealso cref="Get(dynamic, string[])"/>
        /// <seealso cref="SetRecords.Operate(dynamic, Operation[])"/>
        /// <seealso cref="SetRecords.DefaultQueryPolicy"/>
        public new T FirstOrDefault(Client.Exp filterExpression = null)
                        => this.Take(1, filterExpression).FirstOrDefault();

		/// <summary>
		/// Returns the first record from the set based on <see cref="SetRecords.DefaultScanPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="predicate">
		/// Predicate used to find the first occurrence.
		/// </param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp)"/>
		/// <seealso cref="First(Exp)"/>
		/// <seealso cref="First(Func{T, bool}, Exp)"/>
		/// <seealso cref="AsEnumerable(Client.Exp)"/>
		/// <seealso cref="Get(dynamic, string[])"/>
		/// <seealso cref="SetRecords.Operate(dynamic, Operation[])"/>
		/// <seealso cref="SetRecords.DefaultScanPolicy"/>
		public T First(Func<T, bool> predicate, Client.Exp filterExpression = null)
						=> this.AsEnumerable(filterExpression).First(predicate);

		/// <summary>
		/// Returns the first record or null from the set based on <see cref="SetRecords.DefaultScanPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="predicate">
		/// Predicate used to find the first occurrence.
		/// </param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp)"/>
		/// <seealso cref="First(Client.Exp)"/>
		/// <seealso cref="First(Func{T, bool}, Exp)"/>
		/// <seealso cref="FirstOrDefault(Exp)"/>
		/// <seealso cref="AsEnumerable(Client.Exp)"/>
		/// <seealso cref="Get(dynamic, string[])"/>
		/// <seealso cref="SetRecords.Operate(dynamic, Operation[])"/>
		/// <seealso cref="SetRecords.DefaultScanPolicy"/>
		public T FirstOrDefault(Func<T, bool> predicate, Client.Exp filterExpression = null)
						=> this.AsEnumerable(filterExpression).FirstOrDefault(predicate);

		/// <summary>
		/// Skips the number of records from the set based on <see cref="SetRecords.DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="numberRecords">Number of records to skip</param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp)"/>
		/// <seealso cref="First(Client.Exp)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp)"/>
		/// <seealso cref="AsEnumerable(Client.Exp)"/>
		/// <seealso cref="Get(dynamic, string[])"/>
		/// <seealso cref="SetRecords.Operate(dynamic, Operation[])"/>
		/// <seealso cref="SetRecords.DefaultQueryPolicy"/>
		public new IEnumerable<T> Skip(int numberRecords, Client.Exp filterExpression = null)
        {
            int currentIdx = 0;

            var queryPolicy = filterExpression == null
                                    ? this.DefaultQueryPolicy
                                    : new QueryPolicy(this.DefaultQueryPolicy) { filterExp = Exp.Build(filterExpression) };

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy,
                                           string.IsNullOrEmpty(this.SetName) || this.SetName == LPSet.NullSetName
                                           ? new Statement() { Namespace = this.Namespace }
                                           : new Statement() { Namespace = this.Namespace, SetName = this.SetName });

            while (recordset.Next())
            {
                if (++currentIdx > numberRecords)
                    yield return (T) CreateRecord(this.SetAccess,
                                                    recordset.Key,
                                                    recordset.Record,
                                                    this._bins,
                                                    this.BinsHashCode,
                                                    recordView: this.DefaultRecordView,
                                                    fkBins: this.DetermineFKBins(recordset.Record));
            }
        }

        /// <summary>
        /// Filters a collection based on <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">A function that is used to determine if the item should be returned</param>
        /// <returns>
        /// A collection of filtered items.
        /// </returns>
        public IEnumerable<T> Where(Func<T, bool> predicate)
                    => this.AsEnumerable().Where(predicate);

        /// <summary>
        /// Projects each element of an <see cref="ARecord"/> into a new form.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the value returned by <paramref name="selector"/>.
        /// </typeparam>
        /// <param name="selector">
        /// A transform function to apply to each element.
        /// </param>
        /// <returns>
        /// An IEnumerable&lt;T&gt; whose elements are the result of invoking the transform function on each element of <paramref name="selector"/>.
        /// </returns>
        public IEnumerable<TResult> Select<TResult>(Func<T, TResult> selector)
            => this.AsEnumerable().Select(selector);

		/// <summary>
		/// Returns IEnumerable&gt;<see cref="ARecord"/>&lt; for the records of this set based on <see cref="SetRecords.DefaultScanPolicy"/> or <paramref name="filterExpression"/>.
		/// Note: The records&apos; return order may vary between executions.
		/// </summary>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <seealso cref="Take(int, Client.Exp)"/>
		/// <seealso cref="First(Client.Exp)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp)"/>
		/// <seealso cref="Get(dynamic, string[])"/>
		/// <seealso cref="SetRecords.Operate(dynamic, Operation[])"/>
		/// <seealso cref="SetRecords.DefaultScanPolicy"/>
		public new IEnumerable<T> AsEnumerable(Client.Exp filterExpression = null)
        {
			var scanPolicy = filterExpression == null
									? this.DefaultScanPolicy
									: new ScanPolicy(this.DefaultScanPolicy)
									{ filterExp = Exp.Build(filterExpression) };


			var allRecords = new ConcurrentQueue<T>();

			var allTask = Task.Factory.StartNew(() =>
							this.SetAccess.AerospikeConnection
								.AerospikeClient
								.ScanAll(scanPolicy,
											this.Namespace,
											string.IsNullOrEmpty(this.SetName) || this.SetName == LPSet.NullSetName
												? null
												: this.SetName,
										(key, record)
											=> allRecords
												.Enqueue((T) CreateRecord(this.SetAccess,
																			key,
																			record,
																			this._bins,
																			this.BinsHashCode,
																			recordView: this.DefaultRecordView,
                                                                            fkBins: this.DetermineFKBins(record)))),
							cancellationToken: CancellationToken.None,
							creationOptions: TaskCreationOptions.DenyChildAttach
												| TaskCreationOptions.LongRunning,
							scheduler: TaskScheduler.Current);

			while(!allTask.IsCompleted)
			{
				if(allRecords.TryDequeue(out T value))
					yield return value;
			}

			foreach(var record in allRecords.TakeWhile(record => record is not null))
			{
				yield return record;
			}

			if(allTask.IsFaulted && allTask.Exception is not null)
				throw allTask.Exception.InnerExceptions.Count == 1
						? allTask.Exception.InnerExceptions[0]
						: allTask.Exception;
		}

        #endregion

        #region Idx Methods
        /// <summary>
        /// Creates a secondary index on this set for a bin <see href="https://docs.aerospike.com/server/guide/query"/>
        /// </summary>
        /// <param name="idxName">The name of the index</param>
        /// <param name="idxOnBin">The bin&apos;s values that will be used to build the index</param>
        /// <param name="indexType">The type of index to be built</param>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="CreateIndex(string, string, IndexType, IndexCollectionType, CTX[])"/>
        /// <seealso cref="DropIndex(string)"/>
        new public SetRecords<T> CreateIndex(string idxName, string idxOnBin, Client.IndexType indexType)
        {
            base.CreateIndex(idxName, idxOnBin, indexType);
            return this;
        }

        /// <summary>
        /// Creates a secondary index on this set for a bin <see href="https://docs.aerospike.com/server/guide/query"/>
        /// </summary>
        /// <param name="idxName">The name of the index</param>
        /// <param name="idxOnBin">The bin&apos;s values that will be used to build the index</param>
        /// <param name="indexType">The type of index to be built</param>
        /// <param name="indexCollectionType">The bin must be a collection and this determines on to build the index on the collection.</param>
        /// <param name="ctx">Provides additional processing of the collection</param>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="CreateIndex(string, string, IndexType)"/>
        /// <seealso cref="DropIndex(string)"/>
        new public SetRecords<T> CreateIndex(string idxName, string idxOnBin,
                                            Client.IndexType indexType,
                                            Client.IndexCollectionType indexCollectionType, params Client.CTX[] ctx)
        {
            base.CreateIndex(idxName, idxOnBin, indexType, indexCollectionType, ctx);
            return this;
        }

        /// <summary>
        /// Drops a secondary index.
        /// </summary>
        /// <param name="idxName">The name of the index</param>
        /// <returns></returns>
        /// <seealso cref="CreateIndex(string, string, IndexType)"/>
        /// <seealso cref="CreateIndex(string, string, IndexType, IndexCollectionType, CTX[])"/>
        new public SetRecords<T> DropIndex(string idxName)
        {
            base.DropIndex(idxName);
            return this;
        }
        #endregion
       
        #region IEnumerable

        abstract protected ARecord CreateRecord([NotNull] ANamespaceAccess setAccess,
                                                    [NotNull] Client.Key key,
                                                    [NotNull] Record record,
                                                    string[] binNames,
                                                    int binsHashCode,
                                                    ARecord.DumpTypes recordView = ARecord.DumpTypes.Record,
													IEnumerable<LPSet.BinType> fkBins = null);

		public new IEnumerator<T> GetEnumerator()
		{
			var allRecords = new ConcurrentQueue<T>();

			var allTask = Task.Factory.StartNew(() =>
							this.SetAccess.AerospikeConnection
								.AerospikeClient
								.ScanAll(this.DefaultScanPolicy,
											this.Namespace,
											string.IsNullOrEmpty(this.SetName) || this.SetName == LPSet.NullSetName
												? null
												: this.SetName,
										(key, record)
											=> allRecords
												.Enqueue((T) CreateRecord(this.SetAccess,
												                            key,
												                            record,
												                            this._bins,
												                            this.BinsHashCode,
												                            recordView: this.DefaultRecordView,
																			fkBins: this.DetermineFKBins(record)))),
							cancellationToken: CancellationToken.None,
							creationOptions: TaskCreationOptions.DenyChildAttach
												| TaskCreationOptions.LongRunning,
							scheduler: TaskScheduler.Current);

			while(!allTask.IsCompleted)
			{
				if(allRecords.TryDequeue(out T value))
					yield return value;
			}

			foreach(var record in allRecords.TakeWhile(record => record is not null))
			{
				yield return record;
			}

			if(allTask.IsFaulted && allTask.Exception is not null)
				throw allTask.Exception.InnerExceptions.Count == 1
						? allTask.Exception.InnerExceptions[0]
						: allTask.Exception;
		}
		
        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();

        public new T[] ToArray() => this.AsEnumerable().ToArray();
		public new List<T> ToList() => this.AsEnumerable().ToList();

		#endregion

		#region Copy Methods

		/// <inheritdoc cref="LPDHelpers.CopyRecords(IEnumerable{ARecord}, SetRecords, Func{ARecord, dynamic}, WritePolicy, ParallelOptions)"/>
		public SetRecords<C> CopyRecords<C>([NotNull] SetRecords<C> targetSet,
										    Func<T, dynamic> newPrimaryKeyValue,
										    WritePolicy writePolity = null,
										    ParallelOptions parallelOptions = null)
            where C : ARecord
			=> (SetRecords<C>) LPDHelpers.CopyRecords<T>(this.AsEnumerable(),
                                                            targetSet,
                                                            newPrimaryKeyValue,
                                                            writePolity,
                                                            parallelOptions);

        /// <inheritdoc cref="CopyRecords(ANamespaceAccess, string, Func{T, dynamic}, WritePolicy, ParallelOptions)"/>
		public SetRecords CopyRecords([NotNull] SetRecords targetSet,
											Func<T, dynamic> newPrimaryKeyValue,
											WritePolicy writePolity = null,
											ParallelOptions parallelOptions = null)			
			=> LPDHelpers.CopyRecords<T>(this.AsEnumerable(),
											targetSet,
											newPrimaryKeyValue,
											writePolity,
											parallelOptions);

		/// <inheritdoc cref="LPDHelpers.CopyRecords(IEnumerable{ARecord}, ANamespaceAccess, string, Func{ARecord, dynamic}, WritePolicy, ParallelOptions)"/>
		public SetRecords CopyRecords([NotNull] ANamespaceAccess targetNamespace,
												string targetSetName,
												Func<T, dynamic> newPrimaryKeyValue,
												WritePolicy writePolity = null,
												ParallelOptions parallelOptions = null)
			=> LPDHelpers.CopyRecords<T>(this.AsEnumerable(),
                                            targetNamespace,
                                            targetSetName, 
                                            newPrimaryKeyValue,
                                            writePolity,
                                            parallelOptions);

		/// <inheritdoc cref="LPDHelpers.CopyRecords(IEnumerable{ARecord}, SetRecords, WritePolicy, ParallelOptions)"/>
		public SetRecords<C> CopyRecords<C>([NotNull] SetRecords<C> targetSet,
												WritePolicy writePolity = null,
												ParallelOptions parallelOptions = null)
            where C : ARecord
        => (SetRecords<C>) LPDHelpers.CopyRecords<T>(this.AsEnumerable(),
                                                        targetSet,
                                                        writePolity,
                                                        parallelOptions);

		#endregion
	}

	/// <summary>
	/// Represents information about an Aerospike set within a namespace. 
	/// It also contains the complete result set of this Aerospike set and is Enumerable returning a collection of <see cref="ARecord"/>s.
	/// </summary>
	[DebuggerDisplay("{ToString()}")]
    public class SetRecords : IEnumerable<ARecord>, IEquatable<ARecord>, IEquatable<SetRecords>
    {
        #region Constructors
        public SetRecords([NotNull] LPSet lpSet,
                            [NotNull] ANamespaceAccess setAccess, 
                            [NotNull] string setName,
                            params string[] bins)
            : this(setAccess, setName, bins) 
        {
            this.LPset = lpSet;            
        }

        public SetRecords([NotNull] ANamespaceAccess setAccess,
                            [NotNull] string setName,
                            params string[] bins)
        {            
            this.SetName =  setName == LPSet.NullSetName ? null : setName;
            this.SetAccess = setAccess;
            this.SetFullName = $"{this.Namespace}.{this.SetName ?? LPSet.NullSetName}";
            this._bins = Helpers.RemoveDups(bins);
            this.IsNullSet = setName == LPSet.NullSetName;
            this.AerospikeTxn = this.SetAccess.AerospikeTxn;
			this.DefaultWritePolicy = new WritePolicy(this.SetAccess.DefaultWritePolicy);
            this.DefaultReadPolicy = new Policy(this.SetAccess.DefaultReadPolicy);
            this.DefaultQueryPolicy = new QueryPolicy(this.SetAccess.DefaultQueryPolicy);
            this.DefaultScanPolicy = new ScanPolicy(this.SetAccess.DefaultScanPolicy);
            this.DefaultRecordView = this.SetAccess.AerospikeConnection?.RecordView ?? ARecord.DumpTypes.Dynamic;
        }

        public SetRecords([NotNull] SetRecords clone,
                                    Policy readPolicy = null,
									WritePolicy writePolicy = null,
									QueryPolicy queryPolicy = null,
									ScanPolicy scanPolicy = null)
        {
            this.LPset = clone.LPset;
            this.SetName = clone.SetName;
            this.SetAccess = clone.SetAccess;
            this._bins = clone._bins;
            this._binsHashCode= clone._binsHashCode;
            this.FKBins = clone.FKBins;
            this.SetFullName= clone.SetFullName;
			this.DefaultRecordView = clone.DefaultRecordView;
			this.IsNullSet = clone.IsNullSet;

            if(writePolicy?.Txn is not null)
                this.AerospikeTxn = writePolicy.Txn;
            else if(readPolicy?.Txn is not null)
				this.AerospikeTxn = readPolicy.Txn;
            else
                this.AerospikeTxn = clone.AerospikeTxn;

			this.DefaultWritePolicy = writePolicy ?? new WritePolicy(clone.DefaultWritePolicy);
            this.DefaultReadPolicy = readPolicy ?? new Policy(clone.DefaultReadPolicy);
            this.DefaultQueryPolicy = queryPolicy ?? new QueryPolicy(clone.DefaultQueryPolicy);
            this.DefaultScanPolicy = scanPolicy ?? new ScanPolicy(clone.DefaultScanPolicy);            
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SetRecords"/> as an Aerospike transactional unit.
        /// If <see cref="Commit"/> method is not called the server will abort (rollback) this transaction.
        /// </summary>
        /// <param name="baseSet">Base Aerospike Set instance</param>
        /// <param name="txn">
        /// The Aerospike <see cref="Txn"/> instance or null to create a new transactional unit.
        /// </param>
        /// <param name="newNSAccess">
        /// An new <see cref="ANamespaceAccess"/> instance to use with the transaction. 
        /// </param>
        /// <seealso cref="CreateTransaction"/>
        /// <seealso cref="Commit"/>
        /// <seealso cref="Abort"/>
        public SetRecords([NotNull] SetRecords baseSet, 
                            [AllowNull] Txn txn,
                            [AllowNull] ANamespaceAccess newNSAccess = null)
        {
            this.LPset = baseSet.LPset;
            this.SetName = baseSet.SetName;
            this.SetAccess = newNSAccess ?? baseSet.SetAccess;
            this._bins = baseSet._bins;
            this._binsHashCode = baseSet._binsHashCode;
            this.FKBins = baseSet.FKBins;
			this.SetFullName = baseSet.SetFullName;
			this.DefaultRecordView = baseSet.DefaultRecordView;
            this.IsNullSet = baseSet.IsNullSet;

            txn ??= new Txn();

            this.AerospikeTxn = txn;
            this.DefaultWritePolicy = new(baseSet.DefaultWritePolicy)
            {
                Txn = txn
            };
			this.DefaultReadPolicy = new(baseSet.DefaultReadPolicy)
            {
                Txn = txn
            };
			this.DefaultQueryPolicy = new(baseSet.DefaultQueryPolicy)
            {
                Txn= txn
            };
			this.DefaultScanPolicy = new(baseSet.DefaultScanPolicy)
            {
                Txn= txn
            };

		}

		/// <summary>
		/// Clones the specified instance providing new policies, if provided.
		/// </summary>
		/// <param name="newReadPolicy">The new read policy.</param>
		/// <param name="newWritePolicy">The new write policy.</param>
		/// <param name="newQueryPolicy">The new query policy.</param>
		/// <param name="newScanPolicy">The new scan policy.</param>
		/// <returns>New clone of <see cref="SetRecords"/> instance.</returns>
		public SetRecords Clone(Policy newReadPolicy = null,
								WritePolicy newWritePolicy = null,
								QueryPolicy newQueryPolicy = null,
								ScanPolicy newScanPolicy = null)
			=> new SetRecords(this,
								newReadPolicy,
								newWritePolicy,
								newQueryPolicy,
								newScanPolicy);

		#endregion

		#region Settings, Record State, etc.

		public LPSet LPset { get; }

        internal bool TryAddBin(string binName, Type dataType, bool updateNamespace)
        {
            lock (this)
            {
                var added = this.LPset?.AddBin(binName, dataType ?? typeof(AValue)) ?? false;

                if (updateNamespace)
                    added = this.SetAccess.TryAddBin(binName) || added;

                if (this._bins.Length == 0)
                {
                    if (this.BinNames.Contains(binName)) return added;
                    this._bins = this.SetAccess.BinNames;
                }

                this._bins = this._bins.Append(binName).ToArray();
                this._binsHashCode = 0;

                return true;
            }
        }

        internal bool TryRemoveBin(string removeBinName, bool updateNamespace)
        {
            lock (this)
            {
                var removed = this.LPset?.RemoveBin(removeBinName) ?? false;

                if (updateNamespace)
                    removed = this.SetAccess.TryRemoveBin(removeBinName) || removed;

                if (this._bins.Length == 0 || !this._bins.Any(n => n == removeBinName))
                    return false;

                this._bins = this._bins
                                .Where(n => n != removeBinName)
                                .ToArray();
                this._binsHashCode = 0;

                return true;
            }
        }

        /// <summary>
        /// Sets how records are displayed using the LinqPad <see cref="LINQPad.Extensions.Dump{T}(T)"/> method.
        /// See <see cref="ARecord.DumpTypes"/> for more information.
        /// </summary>
        /// <seealso cref="ARecord.DumpTypes"/>
        /// <seealso cref="ChangeRecordView(ARecord.DumpTypes)"/>
        public ARecord.DumpTypes DefaultRecordView { get; set; }
        
        /// <summary>
        /// Changes how records are displayed using the LinqPad <see cref="LINQPad.Extensions.Dump{T}(T)"/> method.        
        /// </summary>
        /// <param name="newRecordView">See <see cref="ARecord.DumpTypes"/> for more information.</param>
        /// <returns>This instance</returns>
        /// <seealso cref="ARecord.DumpTypes"/>
        /// <seealso cref="DefaultRecordView"/>
        public SetRecords ChangeRecordView(ARecord.DumpTypes newRecordView)
        {
            this.DefaultRecordView = newRecordView;
            return this;
        }

        public bool IsNullSet { get; }

        public ANamespaceAccess SetAccess { get; }

        private int _binsHashCode = 0;

        /// <summary>
        /// Returns the hash Code for the defined bins for this Set&apos;s records. 
        /// </summary>
        public int BinsHashCode
        {
            get
            {
                if (this._binsHashCode != 0)
                    return this._binsHashCode;

                if (this._bins.Length == 0)
                {
                    return this._binsHashCode = Helpers.GetStableHashCode(this.BinNames);
                }

                return this._binsHashCode = Helpers.GetStableHashCode(this._bins);
            }
        }

		/// <summary>
		/// Creates an Aerospike Digest byte array (always length of 20 bytes) based on <paramref name="value"/>.
		/// </summary>
		/// <param name="value">
		/// The value that will be converted to a digest. 
		/// </param>
		/// <returns>
		/// A byte array of length 20.
		/// </returns>
		public byte[] CreateDigest(object value)
				=> Key.ComputeDigest(this.SetName,
										Value.Get(value));

        public LPSet.BinType[] FKBins { get; set; }

		protected IEnumerable<LPSet.BinType> DetermineFKBins(Client.Record record)
		{
			if(this.FKBins is not null)
			{
				foreach(var fkBin in this.FKBins)
				{
                    if(fkBin.FKSetNameBin is not null)
					    yield return new LPSet.BinType(fkBin,
											            record.GetString(fkBin.FKSetNameBin));
                    else
                        yield return fkBin;
				}
			}
		}

        public virtual SetRecords TurnIntoTrx([NotNull] ANamespaceAccess txnNS)
             => new SetRecords(this, txnNS.AerospikeTxn, txnNS);
		#endregion

		#region Aerospike Client Properties, Policies, Put, Get, Query, etc.
		/// <summary>
		/// Returns the Aerospike &quot;Namespace&apos;s&quot; name
		/// </summary>
		public string Namespace { get { return this.SetAccess.Namespace; } }

        /// <summary>
        /// Returns the Aerospike &quot;Set&apos;s&quot; name
        /// </summary>
        public string SetName { get; }

        /// <summary>
        /// Returns the Set&apos;s name prefixed with the namespace.
        /// </summary>
        public string SetFullName { get; }

        /// <summary>
        /// The default write policy used for writing.
        /// <see href="https://docs.aerospike.com/apidocs/csharp/html/t_aerospike_client_writepolicy"/>
        /// </summary>
        public WritePolicy DefaultWritePolicy { get; set; }

        /// <summary>
        /// <see href="https://docs.aerospike.com/apidocs/csharp/html/t_aerospike_client_policy"/>
        /// </summary>
        public Policy DefaultReadPolicy { get; set; }
        /// <summary>
        /// <see href="https://docs.aerospike.com/apidocs/csharp/html/t_aerospike_client_querypolicy"/>
        /// </summary>
        public QueryPolicy DefaultQueryPolicy { get; set; }

		/// <summary>
		/// <see href="https://docs.aerospike.com/apidocs/csharp/html/t_aerospike_client_scanpolicy"/>
		/// </summary>
		public ScanPolicy DefaultScanPolicy { get; set; }

		/// <summary>
		/// Gets the aerospike <see cref="Aerospike.Client.Txn"/> instance or null to indicate that it is not within a transaction.
		/// </summary>
		/// <value>The aerospike <see cref="Aerospike.Client.Txn"/> instance or null</value>
		public Txn AerospikeTxn { get; }

		/// <summary>
		/// Returns the transaction identifier or null to indicate not a transactional unit.
		/// </summary>
		public long? TransactionId => this.AerospikeTxn?.Id;

		/// <summary>
		/// Creates an Aerospike transaction where all operations will be included in this transactional unit.
        /// Note: This will copy the current policies for this Set!
		/// </summary>
        /// <param name="txn">
        /// If provided, this Aerospike Transaction is used instead of creating a new transaction instance.
        /// </param>
		/// <returns>Transaction Set instance</returns>
		/// <seealso cref="Commit"/>
		/// <seealso cref="Abort"/>
		public SetRecords CreateTransaction(Txn txn = null) => new SetRecords(this, txn);

		/// <summary>
		/// Attempt to commit the given multi-record transaction. First, the expected record versions are
		/// sent to the server nodes for verification.If all nodes return success, the command is
		/// committed. Otherwise, the transaction is aborted.
		/// <p>
		/// Requires server version 8.0+
		/// </p>
		/// </summary>
		/// <seealso cref="CreateTransaction"/>
		/// <seealso cref="Abort"/>
		public CommitStatus.CommitStatusType Commit()
			=> this.AerospikeTxn is null
				? CommitStatus.CommitStatusType.CLOSE_ABANDONED
				: this.SetAccess.Commit(this.AerospikeTxn);

		/// <summary>
		/// Abort and rollback the given multi-record transaction.
		/// <p>
		/// Requires server version 8.0+
		/// </p>
		/// </summary>
		/// <seealso cref="CreateTransaction"/>
		/// <seealso cref="Commit"/>
		public AbortStatus.AbortStatusType Abort()
			 => this.AerospikeTxn is null
				? AbortStatus.AbortStatusType.ROLL_BACK_ABANDONED
				: this.SetAccess.Abort(this.AerospikeTxn);


		protected string[] _bins = Array.Empty<string>();
        /// <summary>
        /// Returns all the bin names possible for this set.
        /// </summary>
        public string[] BinNames { get => this._bins; }
        
        /// <summary>
        /// Determines the Expiration in seconds of a record TTL
        /// </summary>
        /// <param name="ttl"></param>
        /// <returns></returns>
        public static int DetermineExpiration(TimeSpan ttl)
        {
            return (int)ttl.TotalSeconds;
        }

        /// <summary>
        /// Determines the Expiration based on when the record should be expired.
        /// </summary>
        /// <param name="expirationDate"></param>
        /// <returns>Expiration of a record in seconds</returns>
        public static int DetermineExpiration(DateTimeOffset expirationDate)
        {
            return (int)expirationDate.Subtract(DateTimeOffset.UtcNow).TotalSeconds;
        }

        /// <summary>
        /// Determines the TTL based on an expiration date.
        /// </summary>
        /// <param name="expirationDate"></param>
        /// <returns></returns>
        public static TimeSpan DetermineTTL(DateTimeOffset expirationDate)
        {
            return expirationDate.Subtract(DateTimeOffset.UtcNow);
        }

		#region Put Methods
		/// <summary>
		/// Puts (Writes) a DB record based on the provided record including Expiration.
		/// Note that if the namespace and/or set is different, this instances&apos;s values are used, except 
		/// in the case where the primary key is a digest. In these cases, an <see cref="InvalidOperationException"/> is thrown.
		/// </summary>        
		/// <param name="record">
		/// A <see cref="ARecord"/> object used to add or update the associated record.
		/// </param>
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
		public void Put([NotNull] ARecord record,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
            => this.SetAccess.Put(this.SetName, 
                                    record.Aerospike.Key, 
                                    record.Aerospike.GetValues(), 
                                    writePolicy: writePolicy, 
                                    ttl: ttl ?? record.Aerospike.TTL);        

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
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>
        public void Put<V>([NotNull] dynamic primaryKey,
                            [NotNull] IDictionary<string, V> binValues,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
            => this.SetAccess.Put(this.SetName, primaryKey, binValues, 
                                     writePolicy: writePolicy, ttl: ttl);        

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
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>
        public void Put([NotNull] dynamic primaryKey,
                            [NotNull] string bin,
                            [NotNull] object binValue,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
            => this.SetAccess.Put(this.SetName, primaryKey, bin, binValue, writePolicy: writePolicy, ttl: ttl);

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
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>        
        public void Put([NotNull] dynamic primaryKey,
                            [NotNull] string bin,
                            [NotNull] string binValue,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
            => this.SetAccess.Put(this.SetName, primaryKey, bin, binValue, writePolicy: writePolicy, ttl: ttl);


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
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>        
        public void Put<T>([NotNull] dynamic primaryKey,
                            [NotNull] string bin,
                            [NotNull] IList<T> listValue,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
            => this.SetAccess.Put(this.SetName, primaryKey, bin, listValue, writePolicy: writePolicy, ttl: ttl);

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
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>        
        public void Put<K,V>([NotNull] dynamic primaryKey,
                                [NotNull] string bin,
                                [NotNull] IDictionary<K,V> collectionValue,
                                WritePolicy writePolicy = null,
                                TimeSpan? ttl = null)
            => this.SetAccess.Put(this.SetName, primaryKey, bin, collectionValue, writePolicy: writePolicy, ttl: ttl);


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
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>        
        public void Put<T>([NotNull] dynamic primaryKey,
                            [NotNull] string bin,
                            [NotNull] IEnumerable<T> collectionValue,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
            => this.SetAccess.Put(this.SetName, primaryKey, bin, collectionValue, writePolicy: writePolicy, ttl: ttl);        

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
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>
        public void Put([NotNull] dynamic primaryKey,
                            [NotNull] IEnumerable<Bin> binsToWrite,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null)
            => this.SetAccess.Put(this.SetName, primaryKey, binsToWrite, writePolicy: writePolicy, ttl: ttl);
        
        #endregion

        /// <summary>
        /// Writes the instance where each field/property is a bin name and the associated value the bin's value.
        /// <see cref="Aerospike.Client.BinNameAttribute"/> which allows you to use this name instead of the property/field name.
        /// <see cref="Aerospike.Client.BinIgnoreAttribute"/> which will ignore the property/field name (not written to the DB)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryKey"></param>
        /// <param name="instance"></param>
        /// <param name="transform">
        /// A action that is called to perform customized transformation. 
        /// First argument -- the name of the property/field
        /// Second argument -- the name of the bin (can be different from property/field name, if <see cref="Aerospike.Client.BinNameAttribute"/> is defined)
        /// Third argument -- the instance being transformed
        /// Fourth argument -- if true the instance is within another object.
        /// Returns the new transformed object or null to indicate that this bin should be skipped.
        /// </param>
        /// <param name="documentBinName">
        /// If provided the record is created as a document and this will be the name of the bin. 
        /// </param>
        /// <param name="writePolicy"></param>
        /// <param name="ttl"></param>
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/>
        /// <seealso cref="Aerospike.Client.BinIgnoreAttribute"/>
        public void WriteObject<T>([NotNull] dynamic primaryKey,
                                    [NotNull] T instance,
                                    Func<string, string, object, bool, object> transform = null,
                                    string documentBinName = null,
                                    WritePolicy writePolicy = null,
                                    TimeSpan? ttl = null)
            => this.SetAccess.WriteObject<T>(this.SetName, primaryKey, instance, 
                                                transform: transform, documentBinName: documentBinName, writePolicy: writePolicy, ttl: ttl);        

        #region Delete/Trunc Methods
        /// <summary>
        /// Deletes a DB record based on the provided record.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used.
        /// </summary>        
        /// <param name="record">
        /// A <see cref="ARecord"/> object used to add or update the associated record.
        /// </param>
        /// <param name="writePolicy">
        /// The write policy. If noy provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <returns>
        /// Returns true, if the DB record is deleted.
        /// </returns>
        public bool Delete([NotNull] ARecord record,
                            WritePolicy writePolicy = null)
            => this.Delete(record.Aerospike.Key, writePolicy: writePolicy ?? this.DefaultWritePolicy);        

        /// <summary>
        /// Deletes the DB record associated with the primary key.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used.
        /// </summary>
        /// <param name="primaryKey">
        /// Primary AerospikeKey.
        /// This can be a <see cref="Aerospike.Client.Key"/>, <see cref="Value"/>, or <see cref="Bin"/> object besides a native, collection, etc. value/object.
        /// </param>
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <returns>
        /// Returns true, if the DB record is deleted.
        /// </returns>        
        public bool Delete([NotNull] dynamic primaryKey, WritePolicy writePolicy = null)
        {
            var writePolicyDelete = writePolicy ?? this.DefaultWritePolicy;
            
            return this.SetAccess
                            .AerospikeConnection
                            .AerospikeClient.Delete(writePolicyDelete,
                                                    Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, this.SetName));
        }

        /// <summary>
        /// Truncates the Set
        /// </summary>
        /// <param name="infoPolicy">
        /// The <see cref="InfoPolicy"/> used for the truncate. If not provided, the default is used.
        /// </param>
        /// <param name="before">
        /// A Date/time used to truncate the set. Records before this time will be truncated. 
        /// The default is everything up to when this was executed (DateTime.Now).
        /// </param>
        /// <seealso cref="ANamespaceAccess.Truncate(InfoPolicy, DateTime?)"/>
        /// <exception cref="InvalidOperationException">Thrown if the cluster is a production cluster. Can disable this by going into the connection properties.</exception>
        public void Truncate(InfoPolicy infoPolicy = null, DateTime? before = null)
        {
            if (this.SetAccess.AerospikeConnection.CXInfo.IsProduction)
                throw new InvalidOperationException("Cannot Truncate a Cluster marked \"In Production\"");

            var useTime = before ?? DateTime.Now;

            try
            {
                this.SetAccess
                        .AerospikeConnection
                        .AerospikeClient.Truncate(infoPolicy, this.Namespace, this.SetName, useTime);
            }
            catch(AerospikeException e)
            {
				if(Client.Log.InfoEnabled())
				{
					Client.Log.Info($"Trying truncation to {this.SetFullName} but an exception of '{e}' occurred using Time {useTime:yyyyMMdd-HHmmss.FFFF}.");
				}

				if(before.HasValue || e.Message != "Error -1: Truncate failed: ERROR:4:would truncate in the future") throw;
				//Try again
				if(Client.Log.WarnEnabled())
				{
					Client.Log.Warn($"Retrying truncation due to future error for {this.SetFullName} with time {useTime:yyyyMMdd-HHmmss.FFFF}.");
				}
                Thread.Sleep(500);
				this.Truncate(infoPolicy, useTime);
            }
        }
        #endregion

        #region Get Methods
        /// <summary>
        /// Returns the record based on the primary key
        /// </summary>
        /// <param name="primaryKey">
        /// The primary key can be a <see cref="Aerospike.Client.Key"/>, <see cref="Aerospike.Client.Value"/>, digest (byte[]), or a .net type.
        /// </param>
        /// <param name="bins">
        /// An optional arguments, if provided only those bins are returned.
        /// </param>
        /// <returns>
        /// A record if the primary key is found otherwise null.
        /// </returns>        
        /// <seealso cref="Get(dynamic, Expression, string[])"/>
        public ARecord Get([NotNull] dynamic primaryKey, params string[] bins)
        {            
            var key = Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, this.SetName);

            var record = this.SetAccess
                                .AerospikeConnection
                                .AerospikeClient
                                .Get(this.DefaultReadPolicy, key, bins.Length == 0 ? null : bins);

            if (record == null) return null;

            return new ARecord(this.SetAccess,
                                    key,
                                    record,
                                    this._bins,
                                    dumpType: this.DefaultRecordView,
									fkBins: this.DetermineFKBins(record));
        }

        /// <summary>
        /// Returns the record based on the primary key
        /// </summary>
        /// <param name="primaryKey">
        /// The primary key can be a <see cref="Aerospike.Client.Key"/>, <see cref="Aerospike.Client.Value"/>, digest (byte[]), or a .net type.
        /// </param>
        /// <param name="filterExpression">
        /// A filter <see cref="Aerospike.Client.Expression"/> that is applied after obtaining the record via the primary key.
        /// </param>
        /// <param name="bins">
        /// An optional arguments, if provided only those bins are returned.
        /// </param>
        /// <returns>
        /// A record if the primary key is found otherwise null.
        /// </returns>
        /// <seealso cref="Get(dynamic, string[])"/>        
        /// <seealso cref="Query(Exp, string[])"/>
        public ARecord Get([NotNull] dynamic primaryKey, Expression filterExpression, params string[] bins)
        {
            var key = Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, this.SetName);

            var policy = new Client.Policy(this.DefaultReadPolicy) {  filterExp = filterExpression };

            var record = this.SetAccess
                                .AerospikeConnection
                                .AerospikeClient
                                .Get(policy, key, bins.Length == 0 ? null : bins);

            if (record == null) return null;

            return new ARecord(this.SetAccess,
                                    key,
                                    record,
                                    this._bins,
                                    dumpType: this.DefaultRecordView,
									fkBins: this.DetermineFKBins(record));
        }
        #endregion

        #region Query Methods

        /// <summary>
        /// Returns all the records based on the provided bins.
        /// </summary>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records defined by <paramref name="bins"/>
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Exp, string[])"/>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(string[])"/>
        public IEnumerable<ARecord> Query(params string[] bins)
        {
            var queryPolicy = new QueryPolicy(this.DefaultQueryPolicy);
            var stmt = new Statement();

            stmt.SetNamespace(this.Namespace);

            if (!string.IsNullOrEmpty(this.SetName) && this.SetName != LPSet.NullSetName)
                stmt.SetSetName(this.SetName);

            stmt.SetBinNames(bins);

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy, stmt);

            while (recordset.Next())
            {
                yield return new ARecord(this.SetAccess,
                                            recordset.Key,
                                            recordset.Record,
                                            this._bins,
                                            setBinsHashCode: this.BinsHashCode,
                                            dumpType: this.DefaultRecordView,
											fkBins: this.DetermineFKBins(recordset.Record));
            }
        }

        /// <summary>
        /// Performs a <see cref="Client.AerospikeClient.Query(QueryPolicy, Statement)"/> applying the expression filter.
        /// </summary>
        /// <param name="filterExpression">
        /// The Aerospike filter <see cref="Client.Exp"/> that will be applied.
        /// <seealso cref="Aerospike.Client.ListExp"/>
        /// <seealso cref="Aerospike.Client.MapExp"/>
        /// <seealso cref="Aerospike.Client.BitExp"/>
        /// <seealso cref="Aerospike.Client.HLLExp"/>
        /// </param>
        /// <param name="bins">Return only the provided bins in the result set</param>
        /// <returns>
        /// The result set based on the expression filter.
        /// </returns>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(string[])"/>        
        /// <seealso cref="Operate(dynamic, Operation[])"/>
        public IEnumerable<ARecord> Query([NotNull] Client.Exp filterExpression, params string[] bins)
        {
            var queryPolicy = new QueryPolicy(this.DefaultQueryPolicy) { filterExp = Exp.Build(filterExpression) };

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy,
                                           string.IsNullOrEmpty(this.SetName) || this.SetName == LPSet.NullSetName
                                                   ? new Statement() { Namespace = this.Namespace, BinNames = bins }
                                                   : new Statement() { Namespace = this.Namespace, SetName = this.SetName, BinNames = bins });

            while (recordset.Next())
            {
                yield return new ARecord(this.SetAccess,
                                            recordset.Key,
                                            recordset.Record,
                                            this._bins,
                                            setBinsHashCode: this.BinsHashCode,
                                            dumpType: this.DefaultRecordView,
											fkBins: this.DetermineFKBins(recordset.Record));
            }
        }

        /// <summary>
        /// Performs a secondary index query using the provided <see cref="Client.Filter"/>.
        /// See <see cref="ASecondaryIndexAccess"/> for directly using secondary indexes.
        /// </summary>
        /// <param name="secondaryIdxFilter">The filter used against the secondary index</param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filter.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, Exp, string[])"/>     
        /// <seealso cref="Query(Exp, string[])"/>
        /// <seealso cref="Query(string[])"/>
        /// <seealso cref="CreateIndex(string, string, IndexType)"/>
        /// <seealso cref="CreateIndex(string, string, IndexType, IndexCollectionType, CTX[])"/>
        /// <seealso cref="ASecondaryIndexAccess.Query(Filter, Exp, string[])"/>
        /// <seealso cref="ASecondaryIndexAccess.Query(long, long, string[])"/>
        /// <seealso cref="ASecondaryIndexAccess.Query(dynamic, string[])"/>
        /// <seealso cref="ASecondaryIndexAccess.GetFilter(object, CTX[])"/>
        /// <seealso cref="ASecondaryIndexAccess.GetFilter(long, long, CTX[])"/>
        public IEnumerable<ARecord> Query([NotNull] Client.Filter secondaryIdxFilter, params string[] bins)
        {
            var queryPolicy = new QueryPolicy(this.DefaultQueryPolicy);
            var stmt = new Statement();

            stmt.SetNamespace(this.Namespace);

            if (!string.IsNullOrEmpty(this.SetName) && this.SetName != LPSet.NullSetName)
                stmt.SetSetName(this.SetName);

            stmt.SetFilter(secondaryIdxFilter);
            stmt.SetBinNames(bins);

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy, stmt);

            while (recordset.Next())
            {
                yield return new ARecord(this.SetAccess,
                                            recordset.Key,
                                            recordset.Record,
                                            this._bins,
                                            setBinsHashCode: this.BinsHashCode,
                                            dumpType: this.DefaultRecordView,
											fkBins: this.DetermineFKBins(recordset.Record));
            }
        }
        
        /// <summary>
        /// Performs a secondary index query using the provided <see cref="Client.Filter"/> and than apply the filter expression.
        /// See <see cref="ASecondaryIndexAccess"/> for directly using secondary indexes.
        /// </summary>
        /// <param name="secondaryIdxFilter">The filter used against the secondary index</param>
        /// <param name="filterExpression">
        /// The Aerospike filter <see cref="Client.Exp"/> that will be applied after the index filter is applied.
        /// <seealso cref="Aerospike.Client.ListExp"/>
        /// <seealso cref="Aerospike.Client.MapExp"/>
        /// <seealso cref="Aerospike.Client.BitExp"/>
        /// <seealso cref="Aerospike.Client.HLLExp"/>
        /// </param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filters.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(string[])"/>
        /// <seealso cref="Query(Exp, string[])"/>
        /// <seealso cref="ASecondaryIndexAccess.Query(Filter, Exp, string[])"/>
        /// <seealso cref="ASecondaryIndexAccess.Query(long, long, string[])"/>
        /// <seealso cref="ASecondaryIndexAccess.Query(dynamic, string[])"/>
        /// <seealso cref="ASecondaryIndexAccess.GetFilter(object, CTX[])"/>
        /// <seealso cref="ASecondaryIndexAccess.GetFilter(long, long, CTX[])"/>
        /// <seealso cref="CreateIndex(string, string, IndexType)"/>
        /// <seealso cref="CreateIndex(string, string, IndexType, IndexCollectionType, CTX[])"/>
        public IEnumerable<ARecord> Query([NotNull] Client.Filter secondaryIdxFilter, [NotNull] Client.Exp filterExpression, params string[] bins)
        {            
            var queryPolicy = new QueryPolicy(this.DefaultQueryPolicy) { filterExp = Exp.Build(filterExpression) };

            var stmt = new Statement();

            stmt.SetNamespace(this.Namespace);

            if (!string.IsNullOrEmpty(this.SetName) && this.SetName != LPSet.NullSetName)
                stmt.SetSetName(this.SetName);

            stmt.SetFilter(secondaryIdxFilter);
            stmt.SetBinNames(bins);

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy, stmt);

            while (recordset.Next())
            {
                yield return new ARecord(this.SetAccess,
                                            recordset.Key,
                                            recordset.Record,
                                            this._bins,
                                            setBinsHashCode: this.BinsHashCode,
                                            dumpType: this.DefaultRecordView,
											fkBins: this.DetermineFKBins(recordset.Record));
            }
        }
        
        /// <summary>
        /// Performs a secondary index query or expression query using the provided arguments.
        /// </summary>
        /// <param name="idxName">The name of the index. Can be null.</param>
        /// <param name="secondaryIdxFilter">The filter used to obtain the result of an index query. Can be null.</param>
        /// <param name="filterExpression">The expression that will be applied to the result set. Can be null.</param>
        /// <param name="bins">The bins that will be returned from the result set. Can be null.</param>
        /// <returns>
        /// The result set of the query. 
        /// </returns>
        /// <remarks>To just provide the <paramref name="idxName"/> you must explicitly provide the argument name &quot;idxName:&quot;, otherwise it will be treated as a bin name.</remarks>
        public IEnumerable<ARecord> Query([AllowNull] string idxName = null,
                                            [AllowNull] Client.Filter secondaryIdxFilter = null,
                                            [AllowNull] Client.Exp filterExpression = null,
                                            [AllowNull] string[] bins = null)
        {
            var queryPolicy = new QueryPolicy(this.DefaultQueryPolicy) 
                                                { filterExp = filterExpression == null
                                                                ? null
                                                                : Exp.Build(filterExpression) };

            var stmt = new Statement();

            stmt.SetNamespace(this.Namespace);

            if (!string.IsNullOrEmpty(this.SetName) && this.SetName != LPSet.NullSetName)
                stmt.SetSetName(this.SetName);

            if(secondaryIdxFilter != null)
                stmt.SetFilter(secondaryIdxFilter);
            if(!string.IsNullOrEmpty(idxName))
                stmt.SetIndexName(idxName);
            if(bins != null)
                stmt.SetBinNames(bins);

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy, stmt);

            while (recordset.Next())
            {
                yield return new ARecord(this.SetAccess,
                                            recordset.Key,
                                            recordset.Record,
                                            this._bins,
                                            setBinsHashCode: this.BinsHashCode,
                                            dumpType: this.DefaultRecordView,
											fkBins: this.DetermineFKBins(recordset.Record));
            }
        }

        #endregion

        #region Operate Methods
        /// <summary>
        /// Executes an Aerospike operation against the set based on the primary key.
        /// </summary>
        /// <param name="primaryKey">
        /// The primary key which can be a Aerospike AerospikeKey or Value or a .Net type. 
        /// </param>
        /// <param name="operations">
        /// Aerospike operations (Expression). <see cref="Aerospike.Client.Operation"/>, <see cref="Aerospike.Client.ExpOperation"/>, <see cref="Aerospike.Client.Operation"/>, <see cref="Aerospike.Client.MapOperation"/>, or <see cref="Aerospike.Client.ListOperation"/>
        /// </param>
        /// <returns>
        /// The resulting record or an exception... 
        /// </returns>       
        public ARecord Operate([NotNull] dynamic primaryKey, params Operation[] operations)
        {
            var key = Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, this.SetName);

            var record = this.SetAccess
                                .AerospikeConnection
                                .AerospikeClient
                                .Operate(this.DefaultWritePolicy, key, operations);

            return new ARecord(this.SetAccess,
                                    key,
                                    record,
                                    null,
                                    dumpType: this.DefaultRecordView,
									fkBins: this.DetermineFKBins(record));
        }

        #endregion

        #region Idx Methods
        /// <summary>
        /// Creates a secondary index on this set for a bin <see href="https://docs.aerospike.com/server/guide/query"/>
        /// </summary>
        /// <param name="idxName">The name of the index</param>
        /// <param name="idxOnBin">The bin&apos;s values that will be used to build the index</param>
        /// <param name="indexType">The type of index to be built</param>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="CreateIndex(string, string, IndexType, IndexCollectionType, CTX[])"/>
        /// <seealso cref="DropIndex(string)"/>
        public SetRecords CreateIndex(string idxName, string idxOnBin, Client.IndexType indexType)
        {
            var policy = new Policy();
            var task = this.SetAccess.AerospikeConnection.AerospikeClient
                                .CreateIndex(policy, this.Namespace, this.SetName, idxName, idxOnBin, indexType);

            task.Wait();

            DynamicDriver._Connection.CXInfo.ForceRefresh();

            return this;
        }

        /// <summary>
        /// Creates a secondary index on this set for a bin <see href="https://docs.aerospike.com/server/guide/query"/>
        /// </summary>
        /// <param name="idxName">The name of the index</param>
        /// <param name="idxOnBin">The bin&apos;s values that will be used to build the index</param>
        /// <param name="indexType">The type of index to be built</param>
        /// <param name="indexCollectionType">The bin must be a collection and this determines on to build the index on the collection.</param>
        /// <param name="ctx">Provides additional processing of the collection</param>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="CreateIndex(string, string, IndexType)"/>
        /// <seealso cref="DropIndex(string)"/>
        public SetRecords CreateIndex(string idxName, string idxOnBin,
                                            Client.IndexType indexType,
                                            Client.IndexCollectionType indexCollectionType, params Client.CTX[] ctx)
        {
            var policy = new Policy();
            var task = this.SetAccess.AerospikeConnection.AerospikeClient
                                .CreateIndex(policy, this.Namespace, this.SetName,
                                                idxName, idxOnBin, indexType, indexCollectionType, ctx);

            task.Wait();

            DynamicDriver._Connection.CXInfo.ForceRefresh();

            return this;
        }

        /// <summary>
        /// Drops a secondary index.
        /// </summary>
        /// <param name="idxName">The name of the index</param>
        /// <returns></returns>
        /// <seealso cref="CreateIndex(string, string, IndexType)"/>
        /// <seealso cref="CreateIndex(string, string, IndexType, IndexCollectionType, CTX[])"/>
        public SetRecords DropIndex(string idxName)
        {
            var policy = new Policy();

            var task = this.SetAccess.AerospikeConnection.AerospikeClient.DropIndex(policy, this.Namespace, this.SetName, idxName);
            task.Wait();

            DynamicDriver._Connection.CXInfo.ForceRefresh();

            return this;
        }
		#endregion

		#region Batch Methods

		#region Batch Write
		/// <summary>
		/// Writes a collection of <see cref="ARecord"/> as a <seealso cref="Aerospike.Client.BatchPolicy"/> operation.
		/// </summary>
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
		/// <seealso cref="ANamespaceAccess.BatchWriteRecord{R}(IEnumerable{R}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
		public bool BatchWrite([NotNull] IEnumerable<ARecord> writeRecords,
									BatchPolicy batchPolicy = null,
									BatchWritePolicy batchWritePolicy = null,
									ParallelOptions parallelOptions = null)
			=> this.SetAccess.BatchWriteRecord(writeRecords, batchPolicy, batchWritePolicy, parallelOptions);


		/// <summary>
		/// Writes a collection of items to this set.
		/// </summary>
		/// <typeparam name="P">The Primary Key Type</typeparam>
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
		/// <seealso cref="ANamespaceAccess.BatchWrite{P}(string, IEnumerable{ValueTuple{P, IEnumerable{Bin}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
		public bool BatchWrite<P>([NotNull] IEnumerable<(P pk, IEnumerable<Bin> bins)> binRecords,
									BatchPolicy batchPolicy = null,
									BatchWritePolicy batchWritePolicy = null,
									ParallelOptions parallelOptions = null)
			=> this.SetAccess.BatchWrite(this.SetName, binRecords, batchPolicy, batchWritePolicy, parallelOptions);

		/// <summary>
		/// Writes a collection of items to this set.
		/// </summary>
		/// <typeparam name="P">The Primary Key Type</typeparam>
		/// <typeparam name="V">Bin&apos;s value type</typeparam>
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
		/// <seealso cref="BatchWrite{P, V}(IEnumerable{ValueTuple{P, IDictionary{string, V}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
		/// <seealso cref="ANamespaceAccess.BatchWrite{P, V}(string, IEnumerable{ValueTuple{P, IEnumerable{ValueTuple{string, V}}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
		public bool BatchWrite<P, V>([NotNull] IEnumerable<(P pk, IEnumerable<(string binName, V value)> bins)> binRecords,
									BatchPolicy batchPolicy = null,
									BatchWritePolicy batchWritePolicy = null,
									ParallelOptions parallelOptions = null)
			=> this.SetAccess.BatchWrite(this.SetName, binRecords, batchPolicy, batchWritePolicy, parallelOptions);

		/// <summary>
		/// Writes a collection of items to this set.
		/// </summary>
		/// <typeparam name="P">The Primary Key Type</typeparam>
		/// <typeparam name="V">Bin&apos;s value type</typeparam>
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
		/// <seealso cref="BatchWrite{P, V}(IEnumerable{ValueTuple{P, IEnumerable{ValueTuple{string, V}}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
		/// <seealso cref="ANamespaceAccess.BatchWrite{P,V}(string, IEnumerable{ValueTuple{P, IDictionary{string, V}}}, BatchPolicy, BatchWritePolicy, ParallelOptions)"/>
		public bool BatchWrite<P, V>([NotNull] IEnumerable<(P pk, IDictionary<string, V> bins)> binRecords,
									BatchPolicy batchPolicy = null,
									BatchWritePolicy batchWritePolicy = null,
									ParallelOptions parallelOptions = null)
			=> this.SetAccess.BatchWrite(this.SetName, binRecords, batchPolicy, batchWritePolicy, parallelOptions);

		/// <summary>
		/// Writes a collection of <typeparamref name="T"/> objects to this set.
		/// </summary>
		/// <typeparam name="P">The Primary Key Type</typeparam>
		/// <typeparam name="T"></typeparam>
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
		/// <seealso cref="ANamespaceAccess.BatchWriteObject{P,T}(string, IEnumerable{ValueTuple{P, T}}, BatchPolicy, BatchWritePolicy, ParallelOptions, Func{string, string, object, bool, object}, string)"/>
		public bool BatchWriteObject<P, T>([NotNull] IEnumerable<(P pk, T instance)> objRecords,
										BatchPolicy batchPolicy = null,
										BatchWritePolicy batchWritePolicy = null,
										ParallelOptions parallelOptions = null,
										Func<string, string, object, bool, object> transform = null,
										string documentBinName = null)
			=> this.SetAccess.BatchWriteObject<P, T>(this.SetName, objRecords, batchPolicy, batchWritePolicy, parallelOptions, transform, documentBinName);

		/// <summary>
		/// Deletes records defined in <paramref name="primaryKeys"/>.
		/// </summary>
		/// <typeparam name="R">Primary Key Type</typeparam>
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
		/// <seealso cref="ANamespaceAccess.BatchDelete{R}(string, IEnumerable{R}, BatchPolicy, BatchDeletePolicy, Expression)"/>
		public bool BatchDelete<R>([NotNull] IEnumerable<R> primaryKeys,
									BatchPolicy batchPolicy = null,
									BatchDeletePolicy deletePolicy = null,
									Expression filterExpression = null)
			=> this.SetAccess.BatchDelete(this.SetName, primaryKeys, batchPolicy, deletePolicy, filterExpression);

		#endregion

		#region Batch Read

		/// <summary>
		/// Return a collection of <see cref="ARecord"/> based on <paramref name="primaryKeys"/>
		/// </summary>
		/// <typeparam name="P">Primary Key Type</typeparam>
		/// <param name="primaryKeys">A collection of Primarily Keys that will be part of the collection</param>
		/// <param name="batchPolicy">
		/// <seealso cref="BatchPolicy"/>
		/// </param>
		/// <param name="batchReadPolicy">
		/// <seealso cref="BatchReadPolicy"/>
		/// </param>
		/// <param name="filterExpression">The expression that will be applied to the result set. Can be null.</param>
		/// <param name="returnBins">A collection of bins that are returned</param>
		/// <returns>A collection of records based on <paramref name="primaryKeys"/> or an empty collection</returns>
		public IEnumerable<ARecord> BatchRead<P>([NotNull] IEnumerable<P> primaryKeys,
													BatchPolicy batchPolicy = null,
													BatchReadPolicy batchReadPolicy = null,
													Expression filterExpression = null,
													string[] returnBins = null)
			=> this.SetAccess.BatchRead(this.SetName,
										primaryKeys,
										batchPolicy: batchPolicy,
										batchReadPolicy: batchReadPolicy,
										filterExpression: filterExpression,
										returnBins: returnBins,
										definedBins: this._bins,
										dumpType: this.DefaultRecordView);

		#endregion

		#endregion

		#endregion

		#region Linq Type Methods

		public SetRecords Clone() => new SetRecords(this);

        /// <summary>
        /// Returns the top number of records from the set based on <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
        /// </summary>
        /// <param name="numberRecords">Number of records to return</param>
        /// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
        /// <returns>A collection of records or empty set</returns>
        /// <seealso cref="First(Client.Exp)"/>
        /// <seealso cref="FirstOrDefault(Client.Exp)"/>
        /// <seealso cref="AsEnumerable(Client.Exp)"/>
        /// <seealso cref="Get(dynamic, string[])"/>
        /// <seealso cref="Operate(dynamic, Operation[])"/>
        /// <seealso cref="DefaultQueryPolicy"/>
        public IEnumerable<ARecord> Take(int numberRecords, Client.Exp filterExpression = null)
        {
            if (numberRecords <= 0 || this.SetAccess.AerospikeConnection is null) yield break;

            var queryPolicy = filterExpression == null
                                    ? this.DefaultQueryPolicy
                                    : new QueryPolicy(this.DefaultQueryPolicy)
                                        {
                                            filterExp = Exp.Build(filterExpression),
                                            Txn = this.AerospikeTxn
                                        };

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy,
                                           string.IsNullOrEmpty(this.SetName) || this.SetName == LPSet.NullSetName
                                           ? new Statement() { Namespace = this.Namespace, MaxRecords = numberRecords }
                                           : new Statement() { Namespace = this.Namespace, SetName = this.SetName, MaxRecords = numberRecords });

            while (recordset.Next())
            {
                yield return new ARecord(this.SetAccess, 
                                            recordset.Key,
                                            recordset.Record,
                                            this._bins,
                                            setBinsHashCode: this.BinsHashCode,
                                            dumpType: this.DefaultRecordView,
											fkBins: this.DetermineFKBins(recordset.Record));
            }
        }

        /// <summary>
        /// Returns the first record from the set based on <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
        /// </summary>
        /// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
        /// <returns></returns>
        /// <seealso cref="Take(int, Client.Exp)"/>
        /// <seealso cref="First(Func{ARecord, bool}, Exp)"/>
        /// <seealso cref="FirstOrDefault(Client.Exp)"/>
        /// <seealso cref="FirstOrDefault(Func{ARecord, bool}, Exp)"/>
        /// <seealso cref="AsEnumerable(Client.Exp)"/>
        /// <seealso cref="Get(dynamic, string[])"/>
        /// <seealso cref="Operate(dynamic, Operation[])"/>
        /// <seealso cref="DefaultQueryPolicy"/>
        public ARecord First(Client.Exp filterExpression = null)
                => this.Take(1, filterExpression).First();

        /// <summary>
        /// Returns the first record or null from the set based on <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
        /// </summary>
        /// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
        /// <returns></returns>
        /// <seealso cref="Take(int, Client.Exp)"/>
        /// <seealso cref="First(Client.Exp)"/>
        /// <seealso cref="First(Func{ARecord, bool}, Exp)"/>
        /// <seealso cref="FirstOrDefault(Func{ARecord, bool}, Exp)"/>
        /// <seealso cref="AsEnumerable(Client.Exp)"/>
        /// <seealso cref="Get(dynamic, string[])"/>
        /// <seealso cref="Operate(dynamic, Operation[])"/>
        /// <seealso cref="DefaultQueryPolicy"/>
        public ARecord FirstOrDefault(Client.Exp filterExpression = null)
                    => this.Take(1, filterExpression).FirstOrDefault();        

		/// <summary>
		/// Returns the first record from the set based on <see cref="DefaultScanPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="predicate">
		/// Predicate used to find the first occurrence.
		/// </param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp)"/>
		/// <seealso cref="First(Exp)"/>
		/// <seealso cref="First(Func{ARecord, bool}, Exp)"/>
		/// <seealso cref="AsEnumerable(Client.Exp)"/>
		/// <seealso cref="Get(dynamic, string[])"/>
		/// <seealso cref="Operate(dynamic, Operation[])"/>
		/// <seealso cref="DefaultScanPolicy"/>
		public ARecord First(Func<ARecord, bool> predicate, Client.Exp filterExpression = null)
						=> this.AsEnumerable(filterExpression).First(predicate);


		/// <summary>
		/// Returns the first record or null from the set based on <see cref="DefaultScanPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="predicate">
		/// Predicate used to find the first occurrence.
		/// </param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp)"/>
		/// <seealso cref="First(Client.Exp)"/>
		/// <seealso cref="First(Func{ARecord, bool}, Exp)"/>
		/// <seealso cref="FirstOrDefault(Exp)"/>
		/// <seealso cref="AsEnumerable(Client.Exp)"/>
		/// <seealso cref="Get(dynamic, string[])"/>
		/// <seealso cref="Operate(dynamic, Operation[])"/>
		/// <seealso cref="DefaultScanPolicy"/>
		public ARecord FirstOrDefault(Func<ARecord, bool> predicate, Client.Exp filterExpression = null)
						=> this.AsEnumerable(filterExpression).FirstOrDefault(predicate);

		/// <summary>
		/// Skips the number of records from the set based on <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="numberRecords">Number of records to skip</param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp)"/>
		/// <seealso cref="First(Client.Exp)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp)"/>
		/// <seealso cref="AsEnumerable(Client.Exp)"/>
		/// <seealso cref="Get(dynamic, string[])"/>
		/// <seealso cref="Operate(dynamic, Operation[])"/>
		/// <seealso cref="DefaultQueryPolicy"/>
		public IEnumerable<ARecord> Skip(int numberRecords, Client.Exp filterExpression = null)
        {
			if(this.SetAccess.AerospikeConnection is null) yield break;

			int currentIdx = 0;
            var queryPolicy = filterExpression == null
                                    ? this.DefaultQueryPolicy
                                    : new QueryPolicy(this.DefaultQueryPolicy) { filterExp = Exp.Build(filterExpression) };

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy,
                                           string.IsNullOrEmpty(this.SetName) || this.SetName == LPSet.NullSetName
                                           ? new Statement() { Namespace = this.Namespace }
                                           : new Statement() { Namespace = this.Namespace, SetName = this.SetName });
               
                while (recordset.Next())
                {
                    if(++currentIdx > numberRecords)
                        yield return new ARecord(this.SetAccess,
                                                    recordset.Key,
                                                    recordset.Record,
                                                    this._bins,
                                                    setBinsHashCode: this.BinsHashCode,
                                                    dumpType: this.DefaultRecordView,
													fkBins: this.DetermineFKBins(recordset.Record));
                }
        }

        /// <summary>
        /// Filters a collection based on <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">A function that is used to determine if the item should be returned</param>
        /// <returns>
        /// A collection of filtered items.
        /// </returns>
        public IEnumerable<ARecord> Where(Func<ARecord,bool> predicate)
                    => this.AsEnumerable().Where(predicate);
        
        /// <summary>
        /// Projects each element of an <see cref="ARecord"/> into a new form.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the value returned by <paramref name="selector"/>.
        /// </typeparam>
        /// <param name="selector">
        /// A transform function to apply to each element.
        /// </param>
        /// <returns>
        /// An IEnumerable&lt;T&gt; whose elements are the result of invoking the transform function on each element of <paramref name="selector"/>.
        /// </returns>
        public IEnumerable<TResult> Select<TResult>(Func<ARecord, TResult> selector)
            => this.AsEnumerable().Select(selector);

		/// <summary>
		/// Returns IEnumerable&gt;<see cref="ARecord"/>&lt; for the records of this set based on <see cref="DefaultScanPolicy"/> or <paramref name="filterExpression"/>.
		/// Note: The records&apos; return order may vary between executions. 
		/// </summary>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <seealso cref="Take(int, Client.Exp)"/>
		/// <seealso cref="First(Client.Exp)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp)"/>
		/// <seealso cref="Get(dynamic, string[])"/>
		/// <seealso cref="Operate(dynamic, Operation[])"/>
		/// <seealso cref="GetRecords(string[])"/>
		/// <seealso cref="DefaultScanPolicy"/>        
		public IEnumerable<ARecord> AsEnumerable(Client.Exp filterExpression = null)
        {
			if(this.SetAccess.AerospikeConnection is null) yield break;

			var scanPolicy = filterExpression == null
									? this.DefaultScanPolicy
									: new ScanPolicy(this.DefaultScanPolicy)
                                            { filterExp = Exp.Build(filterExpression) };            

			var allRecords = new ConcurrentQueue<ARecord>();

			var allTask = Task.Factory.StartNew(() =>
							this.SetAccess.AerospikeConnection
								.AerospikeClient
								.ScanAll(scanPolicy,
											this.Namespace,
											string.IsNullOrEmpty(this.SetName) || this.SetName == LPSet.NullSetName
												? null
												: this.SetName,
										(key, record)
											=> allRecords
												.Enqueue(new ARecord(this.SetAccess,
																		key,
																		record,
																		this._bins,
																		setBinsHashCode: this.BinsHashCode,
																		dumpType: this.DefaultRecordView,
																		fkBins: this.DetermineFKBins(record)))),
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
            {
                if(allTask.Exception.InnerExceptions.Count == 1)
                {
                    bool NodeNotFoundPartition(AerospikeException ex)
                    {
                        if(ex.Result != -3) return false;
                        var match = new Regex(@"^Node\s+not\s+found\s+for\s+partition\s+(?<ns>[^:]+):\s*0");
                        if(match.IsMatch(ex.Message))
                        {
							if(Client.Log.InfoEnabled())
							{
								Client.Log.Info($"SetRecords.AsEnumerable() Node not found for partition exception will be IGNORED. Namespace: {this.SetFullName} Filter: '{filterExpression}' Exception: '{ex}'");
							}
							return true;
                        }
                        return false;
                    }

					var ex = allTask.Exception.InnerExceptions[0];
                    if(ex is AerospikeException aex
                        && NodeNotFoundPartition(aex))
                    {
                        yield break;
                    }
                    throw ex;
				}
                else
                {
                    throw allTask.Exception;
                }
            }
        }

		/// <summary>
		/// Gets all records in this set        
		/// </summary>
		/// <param name="bins">bins you wish to get. If not provided all bins for a record</param>
		/// <returns>An array of records in the set</returns>
		/// <seealso cref="AsEnumerable(Exp)"/>
        /// <seealso cref="DefaultQueryPolicy"/>
		public ARecord[] GetRecords(params string[] bins)
		{
			if(this.SetAccess.AerospikeConnection is null)Array.Empty<ARecord>();

			var recordSets = new List<ARecord>();

			using(var recordset = this.SetAccess.AerospikeConnection
									.AerospikeClient
									.Query(this.DefaultQueryPolicy,
											string.IsNullOrEmpty(this.SetName) || this.SetName == LPSet.NullSetName
												? new Statement() { Namespace = this.Namespace, BinNames = bins }
												: new Statement() { Namespace = this.Namespace, SetName = this.SetName, BinNames = bins }))
				while(recordset.Next())
				{
					recordSets.Add(new ARecord(this.SetAccess,
												recordset.Key,
												recordset.Record,
												bins,
                                                setBinsHashCode: this.BinsHashCode,
												dumpType: this.DefaultRecordView,
												fkBins: this.DetermineFKBins(recordset.Record)));
				}

			return recordSets.ToArray();
		}

		/// <summary>
		/// Returns true if primary key exists
		/// </summary>
		/// <param name="primaryKey">
		/// The primary key can be a <see cref="Aerospike.Client.Key"/>, <see cref="Aerospike.Client.Value"/>, digest (byte[]), or a .net type.
		/// </param>  
		/// <param name="filterExpression">
		/// A filter <see cref="Aerospike.Client.Expression"/> that is applied after obtaining the record via the primary key.
		/// </param>
		/// <returns>
		/// True if the <paramref name="primaryKey"/> exists, otherwise false.
		/// </returns>
		/// <seealso cref="Get(dynamic, Expression, string[])"/>
		/// <seealso cref="Query(Exp, string[])"/>
		public bool Exists([NotNull] dynamic primaryKey, Expression filterExpression)
        {
			if(this.SetAccess.AerospikeConnection is null) return false;

			var key = Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, this.SetName);

            var policy = new Client.Policy(this.DefaultReadPolicy) { filterExp = filterExpression };

            return this.SetAccess
                        .AerospikeConnection
                        .AerospikeClient
                        .Exists(policy, key);
        }

        /// <summary>
        /// Placeholder for <see cref="System.Linq.Enumerable.Count{TSource}(IEnumerable{TSource})"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException">Always throw since it must be executed from <see cref="AsEnumerable(Exp)"/></exception>
        public int Count()
        {
            throw new NotImplementedException($"Count must be executed from the \"AsEnumerable\" method. Ex: {this.SetFullName}.AsEnumerable().Count()");
        }

        /// <summary>
        /// Placeholder for <see cref="System.Linq.Enumerable.OrderBy{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey})"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException">Always throw since it must be executed from <see cref="AsEnumerable(Exp)"/></exception>
        public IEnumerable<ARecord> OrderBy(Func<ARecord,int> orderby)            
        {
            throw new NotImplementedException("OrderBy must be executed from the \"AsEnumerable\" method. Ex: mySet.AsEnumerable().OrderBy(r => r.PK)");
        }

        /// <summary>
        /// Placeholder for <see cref="System.Linq.Enumerable.OrderByDescending{TSource, TKey}(IEnumerable{TSource}, Func{TSource, TKey})"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException">Always throw since it must be executed from <see cref="AsEnumerable(Exp)"/></exception>
        public IEnumerable<ARecord> OrderByDescending(Func<ARecord, int> orderby)
        {
            throw new NotImplementedException("OrderByDescending must be executed from the \"AsEnumerable\" method. Ex: mySet.AsEnumerable().OrderByDescending(r => r.PK)");
        }
        #endregion

        #region Export/Import/JSON

        /// <summary>
        /// Exports the records in this set to a JSON file based on <see cref="JsonExportStructure"/>.
        /// </summary>
        /// <param name="exportJSONFile">
        /// The JSON file where the JSON will be written.
        /// If this is an existing directory, the file name will be generated where the name contains the namespace and set names with the JSON extension.
        /// If the file exists it will be overwritten or created.
        /// </param>
        /// <param name="filterExpression">A filter expression that will be applied that will determine the result set.</param>
        /// <param name="indented">If true the JSON string is formatted for readability</param>
        /// <returns>Number of records written</returns>
        /// <seealso cref="ANamespaceAccess.Export(string, Exp, bool)"/>
        /// <seealso cref="Import(string, WritePolicy, TimeSpan?, bool, int, BatchPolicy, BatchWritePolicy, bool, CancellationToken)"/>
        /// <seealso cref="ANamespaceAccess.Import(string, string, WritePolicy, TimeSpan?, bool, int, BatchPolicy, BatchWritePolicy, bool, CancellationToken)"/>
        /// <seealso cref="ANamespaceAccess.Import(string, WritePolicy, TimeSpan?, bool, int, BatchPolicy, BatchWritePolicy, bool, CancellationToken)"/>
        /// <seealso cref="ARecord.Export(bool, JsonSerializerSettings)"/>
        public int Export([NotNull] string exportJSONFile, Client.Exp filterExpression = null, bool indented = true)
        {
            var jsonStr = new StringBuilder();
            int cnt = 0;
            var jsonSettings = new JsonSerializerSettings
                                {
                                    TypeNameHandling = TypeNameHandling.All,
                                    DateParseHandling = DateParseHandling.DateTimeOffset,
                                    NullValueHandling = NullValueHandling.Ignore
                                };

            jsonStr.AppendLine("[");

            foreach(var rec in this.AsEnumerable(filterExpression))
            {                
                jsonStr.Append(rec.Export(indented, jsonSettings));
                jsonStr.AppendLine(",");

                cnt++;
            }

            jsonStr.AppendLine("]");

            if(Directory.Exists(exportJSONFile))
            {
                exportJSONFile = Path.Combine(exportJSONFile, $"{this.Namespace}.{this.SetName}.json");
            }

            File.WriteAllText(exportJSONFile, jsonStr.ToString());

            return cnt;
        }

		/// <summary>
		/// Imports a <see cref="Export(string, Exp, bool)"/> generated JSON file based on <see cref="JsonExportStructure"/>. 
		/// </summary>
		/// <param name="importJSONFile">The JSON file that will be read</param>
		/// <param name="writePolicy">
		/// The write policy. If not provided , the default policy is used.
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
        /// <seealso cref="ImportJsonFile(string, string, string, bool, WritePolicy, TimeSpan?, int, BatchPolicy, BatchWritePolicy, bool, bool, CancellationToken)"/>
		/// <seealso cref="ANamespaceAccess.Import(string, string, WritePolicy, TimeSpan?, bool, int, BatchPolicy, BatchWritePolicy, bool, CancellationToken)"/>
		/// <seealso cref="ANamespaceAccess.Import(string, WritePolicy, TimeSpan?, bool, int, BatchPolicy, BatchWritePolicy, bool, CancellationToken)"/>
		/// <seealso cref="AClusterAccess.Import(string, string, string, WritePolicy, int, CancellationToken)"/>
		/// <exception cref="InvalidOperationException">Thrown if the cluster is a production cluster. Can disable this by going into the connection properties.</exception>
		public int Import([NotNull] string importJSONFile,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                            bool useImportRecTTL = false,
                            int maxDegreeOfParallelism = -1,
							BatchPolicy batchPolicy = null,
							BatchWritePolicy batchWritePolicy = null,
							bool useParallelPuts = false,
							CancellationToken cancellationToken = default)
            => this.SetAccess.Import(importJSONFile,
                                            this.SetName,
                                            writePolicy: writePolicy ?? this.DefaultWritePolicy,
                                            ttl: ttl,
                                            useImportRecTTL: useImportRecTTL,
                                            maxDegreeOfParallelism: maxDegreeOfParallelism,
                                            batchPolicy: batchPolicy,
                                            batchWritePolicy: batchWritePolicy,
                                            useParallelPuts: useParallelPuts,
                                            cancellationToken);

        /// <summary>
        /// This will import a Json file and convert it into a DB record for update into the DB.
        /// Each line in the file is treated as a new DB record.
        /// 
        /// Each top-level property in the Json is translated into a bin and value. Json Arrays and embedded objects are transformed into an Aerospike List or Map&lt;string,object&gt;.
        /// Note: If the Json string is a Json Object, the following behavior occurs:
        ///         If <paramref name="jsonBinName"/> is provided, the Json object is treated as an Aerospike document which will be associated with that bin.
        ///         if <paramref name="jsonBinName"/> is null, each json property in that Json object is treated as a separate bin/value.
        /// You can also insert individual records by calling <see cref="FromJson(string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>.
        /// </summary>
        /// <param name="importJSONFile">
        /// The file containing Json value where each line is a separate DB record.
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
        /// <seealso cref="FromJson(string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="FromJson(string, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="ToJson(Exp, string, bool)"/>
        /// <seealso cref="ANamespaceAccess.ImportJsonFile(string, string, string, string, bool, WritePolicy, TimeSpan?, int, BatchPolicy, BatchWritePolicy, bool, bool, CancellationToken)"/>       
        /// <exception cref="InvalidOperationException">Thrown if the cluster is a production cluster. Can disable this by going into the connection properties.</exception>
        public int ImportJsonFile([NotNull] string importJSONFile,
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
            => this.SetAccess.ImportJsonFile(importJSONFile,
                                            this.SetName,
                                            pkPropertyName,
                                            jsonBinName,
                                            writePKPropertyName,
                                            writePolicy ?? this.DefaultWritePolicy,
                                            ttl,
                                            maxDegreeOfParallelism,
                                            batchPolicy,
                                            batchWritePolicy,
                                            useParallelPuts,
                                            treatEmptyStrAsNull,
                                            cancellationToken);

		/// <summary>
		/// Creates a Json Array of all records in the set based on the <paramref name="filterExpression"/>, if provided.
		/// </summary>
		/// <param name="filterExpression"></param>
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
		/// <seealso cref="FromJson(string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
		/// <seealso cref="FromJson(string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
		/// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
		/// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
		/// <seealso cref="ARecord.ToJson(string, bool)"/>
		/// <seealso cref="Aerospike.Client.Exp"/>
		public JArray ToJson(Exp filterExpression = null, [AllowNull] string pkPropertyName = "_id", bool useDigest = false)
        {
            var jsonArray = new JArray();

            foreach(var rec in this.AsEnumerable(filterExpression))
            {
                jsonArray.Add(rec.ToJson(pkPropertyName, useDigest));
            }

            return jsonArray;
        }

        /// <summary>
        /// Converts a Json string into an <see cref="ARecord"/> which is than put into this set.
        /// Each top-level property in the Json is translated into a bin and value. Json Arrays and embedded objects are transformed into an Aerospike List or Map&lt;string,object&gt;.
        /// Note: If the Json string is an Json Array, each element is treated as a separate record. 
        ///         If the Json string is a Json Object, the following behavior occurs:
        ///             If <paramref name="jsonBinName"/> is provided, the Json object is treated as an Aerospike document which will be associated with that bin.
        ///             if <paramref name="jsonBinName"/> is null, each json property in that Json object is treated as a separate bin/value.
        ///         You can also insert individual records by calling <see cref="FromJson(string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>.
        /// </summary>
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
        /// <returns>The number of items put.</returns>
        /// <seealso cref="ToJson(Exp, string, bool)"/>
        /// <seealso cref="ARecord.ToJson(string, bool)"/>
        /// <seealso cref="FromJson(string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
        /// <seealso cref="ARecord.FromJson(string, string, string, string, string, ANamespaceAccess, bool, bool)"/>
        /// <seealso cref="ANamespaceAccess.FromJson(string, string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="Put(ARecord, WritePolicy, TimeSpan?)"/>
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
        public int FromJson(string json, 
                                string pkPropertyName = "_id",
                                string jsonBinName = null,
                                WritePolicy writePolicy = null,
                                TimeSpan? ttl = null,
                                bool writePKPropertyName = false)
        {
            return this.SetAccess.FromJson(this.SetName,
                                            json,
                                            pkPropertyName: pkPropertyName,
                                            jsonBinName: jsonBinName,
                                            writePolicy: writePolicy,
                                            ttl: ttl,
                                            writePKPropertyName: writePKPropertyName);
        }

        /// <summary>
        /// Converts a Json string into an <see cref="ARecord"/> which is than put into this set.
        /// Each top-level property in the Json is translated into a bin and value. Json Arrays and embedded objects are transformed into an Aerospike List or Map&lt;string,object&gt;.
        /// 
        /// Note: If <paramref name="jsonBinName"/> is provided the Json item will completely be placed into this bin as its' value.
        /// </summary>
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
        /// <seealso cref="ToJson(Exp, string, bool)"/>
        /// <seealso cref="ARecord.ToJson(string, bool)"/>
        /// <seealso cref="FromJson(string, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
        /// <seealso cref="ARecord.FromJson(string, string, string, string, string, ANamespaceAccess, bool, bool)"/>
        /// <seealso cref="ANamespaceAccess.FromJson(string, string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="Put(ARecord, WritePolicy, TimeSpan?)"/>
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
        public int FromJson(string json,
                                [AllowNull]
                                dynamic primaryKey,
                                string pkPropertyName = "_id",
                                string jsonBinName = null,
                                WritePolicy writePolicy = null,
                                TimeSpan? ttl = null,
                                bool writePKPropertyName = false)
        {
            return this.SetAccess.FromJson(this.SetName,
                                            json,
                                            primaryKey,
                                            pkPropertyName: pkPropertyName,
                                            jsonBinName: jsonBinName,
                                            writePolicy: writePolicy,
                                            ttl: ttl,
                                            writePKPropertyName: writePKPropertyName);
        }

        #endregion

        #region Overrides
        public override bool Equals(object obj)
        {
            if(obj is SetRecords set) return this.Equals(set);
            if(obj is ARecord rec) return this.Equals(rec);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Helpers.GetStableHashCode(this.ToString());
        }

        public override string ToString()
        {
            string txn = string.Empty;
            if(this.TransactionId.HasValue)
                txn = " TXN";

            if(this._bins == null || this._bins.Length == 0)
                return $"{this.Namespace}.{this.SetName}{txn}";

            return $"{this.Namespace}.{this.SetName}{{{string.Join(',',this._bins)}}} {txn}";
        }

        #endregion

        #region IEquatable

        public bool Equals([AllowNull] ARecord other)
        {
            if(other is null) return false;
            if(ReferenceEquals(other, this)) return true;

            return this.SetName == other.Aerospike.SetName && this.Namespace == other.Aerospike.Namespace;
        }

        public bool Equals([AllowNull] SetRecords other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;

            return this.SetName == other.SetName && this.Namespace == other.Namespace;
        }

		#endregion

		#region IEnumerable

		public IEnumerator<ARecord> GetEnumerator()
        {
			var allRecords = new ConcurrentQueue<ARecord>();

			var allTask = Task.Factory.StartNew(() =>
						    this.SetAccess.AerospikeConnection
                                .AerospikeClient
                                .ScanAll(this.DefaultScanPolicy,
										    this.Namespace,
										    string.IsNullOrEmpty(this.SetName) || this.SetName == LPSet.NullSetName
                                                ? null
											    : this.SetName,
									    (key, record) 
                                            => allRecords
                                                .Enqueue(new ARecord(this.SetAccess,
											                            key,
											                            record,
											                            this._bins,
											                            setBinsHashCode: this.BinsHashCode,
											                            dumpType: this.DefaultRecordView,
                                                                        fkBins: this.DetermineFKBins(record)))),
                            cancellationToken: CancellationToken.None,
                            creationOptions: TaskCreationOptions.DenyChildAttach
                                                | TaskCreationOptions.LongRunning,
                            scheduler: TaskScheduler.Current );

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

        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();

		public ARecord[] ToArray() => this.AsEnumerable().ToArray();
        public List<ARecord> ToList() => this.AsEnumerable().ToList();

		#endregion
	}
}
