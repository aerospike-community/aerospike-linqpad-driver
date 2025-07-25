﻿using Aerospike.Client;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aerospike.Database.LINQPadDriver.Extensions
{

    public class ASecondaryIndexAccess<T> : ASecondaryIndexAccess,
                                            IEnumerable<AQueryRecord<T>>        
        where T : ARecord
    {
        public ASecondaryIndexAccess([NotNull] SetRecords<T> setRecords,
                                        [NotNull] string name,
                                        [NotNull] string idxBin,
                                        [NotNull] string idxType,
                                        [NotNull] string idxCollectionType,
                                        [NotNull] Type idxBinDataType)
            : base(setRecords, name, idxBin, idxType, idxCollectionType)
        {
            this.SetRecords = setRecords;
            this.BinDataType = idxBinDataType;
        }

        /// <summary>
        /// The set instance associated with this index.
        /// <see cref="SetRecords"/> for more information.
        /// </summary>
        new public SetRecords<T> SetRecords { get; }

        public Type BinDataType { get; }

        #region Query Methods

        /// <summary>
        /// Performs a secondary index query using the provided <see cref="Client.Filter"/>.
        /// </summary>
        /// <param name="secondaryIdxFilter">The <see cref="Client.Filter"/> used against the secondary index</param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filter.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, Exp, string[])"/> 
        /// <seealso cref="Query(dynamic, string[])"/>
        /// <seealso cref="Query(long, long, string[])"/>
        /// <seealso cref="SetRecords.Query(Filter, string[])"/>
        new public IEnumerable<T> Query([NotNull] Client.Filter secondaryIdxFilter, params string[] bins)
                    => this.SetRecords.Query(secondaryIdxFilter, bins);
        
        /// <summary>
        /// Performs a secondary index query using the provided <see cref="Client.Filter"/> and than apply the filter expression.
        /// </summary>
        /// <param name="secondaryIdxFilter">The <see cref="Client.Filter"/> used against the secondary index</param>
        /// <param name="filterExpression">The Aerospike filter <see cref="Client.Exp"/> that will be applied after the index filter is applied.</param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filters.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, string[])"/>  
        /// <seealso cref="Query(long, long, string[])"/>
        /// <seealso cref="Query(dynamic, string[])"/>
        /// <seealso cref="SetRecords.Query(Filter, Exp, string[])"/>
        new public IEnumerable<T> Query([NotNull] Client.Filter secondaryIdxFilter, [NotNull] Client.Exp filterExpression, params string[] bins)
                        => this.SetRecords.Query(secondaryIdxFilter, filterExpression, bins);
        
        /// <summary>
        /// Performs a search on the secondary index based <paramref name="idxBinValue"/> and the properties associated with the index. 
        /// For more information see <see cref="Client.Filter"/>.
        /// </summary>
        /// <param name="idxBinValue">The searchValue used to conduct the search associated with bin</param>
        /// <param name = "bins" > Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filters.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(long, long, string[])"/>
        /// <seealso cref="BinDataType"/>
        /// <seealso cref="IndexCollectionType"/>
        new public IEnumerable<T> Query([NotNull] dynamic idxBinValue, params string[] bins)
        {
            return idxBinValue switch
            {
                Client.Key keyValue => this.Query(GetFilter(keyValue.userKey), bins),                
                Client.Value vValue => this.Query(GetFilter(vValue), bins),                         
                _ => this.Query(GetFilter(idxBinValue), bins)
            };
        }

        /// <summary>
        /// Performs a secondary index range search based on <paramref name="inclusiveStartRange"/> and <paramref name="inclusiveEndRange"/>, inclusively.
        /// For more information see <see cref="Client.Filter"/>.
        /// </summary>
        /// <param name="inclusiveStartRange">Start Rage, inclusive</param>
        /// <param name="inclusiveEndRange">End Range, inclusive</param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>A collection of records that match the filters.</returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="inclusiveStartRange"/> is greater than <paramref name="inclusiveEndRange"/></exception>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(dynamic, string[])"/>
        /// <seealso cref="SetRecords.Query(Filter, Exp, string[])"/>        
        new public IEnumerable<T> Query(long inclusiveStartRange, long inclusiveEndRange, params string[] bins)
                    => this.Query(GetFilter(inclusiveStartRange, inclusiveEndRange), bins);

		#endregion


		#region LINQ Methods

		/// <summary>
		/// Returns the top number of records from the Index based on <see cref="ASecondaryIndexAccess.DefaultFilter"/> and <see cref="ASecondaryIndexAccess.DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="numberRecords">Number of records to return</param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>        
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns>A collection of records or empty set</returns>
		/// <seealso cref="AsEnumerable(Exp, bool)"/>
		/// <seealso cref="ASecondaryIndexAccess.DefaultQueryPolicy"/>
		/// <seealso cref="ASecondaryIndexAccess.DefaultFilter"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public new IEnumerable<AQueryRecord<T>> Take(int numberRecords, Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
			=> this.AsEnumerable(filterExpression, returningOnlyMatchingCT)
					.Take(numberRecords);

		/// <summary>
		/// Returns the first record from the Index based on <see cref="ASecondaryIndexAccess.DefaultFilter"/> and <see cref="ASecondaryIndexAccess.DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp, bool)"/>
		/// <seealso cref="First(Func{AQueryRecord{T}, bool}, Exp, bool)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp, bool)"/>
		/// <seealso cref="AsEnumerable(Client.Exp, bool)"/>
		/// <seealso cref="ASecondaryIndexAccess.DefaultFilter"/>
		/// <seealso cref="ASecondaryIndexAccess.DefaultQueryPolicy"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public new AQueryRecord<T> First(Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
				=> this.Take(1, filterExpression, returningOnlyMatchingCT).First();

		/// <summary>
		/// Returns the first record or null from the Index based on <see cref="ASecondaryIndexAccess.DefaultFilter"/> and <see cref="ASecondaryIndexAccess.DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp, bool)"/>
		/// <seealso cref="First(Client.Exp, bool)"/>
		/// <seealso cref="AsEnumerable(Client.Exp, bool)"/>
		/// <seealso cref="ASecondaryIndexAccess.DefaultFilter"/>
		/// <seealso cref="ASecondaryIndexAccess.DefaultQueryPolicy"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public new AQueryRecord<T> FirstOrDefault(Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
					=> this.Take(1, filterExpression, returningOnlyMatchingCT).FirstOrDefault();

		/// <summary>
		/// Returns the first record from the Index based on <see cref="ASecondaryIndexAccess.DefaultFilter"/> and <see cref="ASecondaryIndexAccess.DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="predicate">
		/// Predicate used to find the first occurrence.
		/// </param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp, bool)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp, bool)"/>
		/// <seealso cref="First(Exp, bool)"/>
		/// <seealso cref="AsEnumerable(Client.Exp, bool)"/>
		/// <seealso cref="ASecondaryIndexAccess.DefaultFilter"/>
		/// <seealso cref="ASecondaryIndexAccess.DefaultQueryPolicy"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public AQueryRecord<T> First(Func<AQueryRecord<T>, bool> predicate, Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
						=> this.AsEnumerable(filterExpression, returningOnlyMatchingCT)
							.First(predicate);

		/// <summary>
		/// Returns the first record or null from the Index based on <see cref="ASecondaryIndexAccess.DefaultFilter"/> and <see cref="ASecondaryIndexAccess.DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="predicate">
		/// Predicate used to find the first occurrence.
		/// </param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp, bool)"/>
		/// <seealso cref="First(Client.Exp, bool)"/>
		/// <seealso cref="FirstOrDefault(Exp, bool)"/>
		/// <seealso cref="AsEnumerable(Client.Exp, bool)"/>
		/// <seealso cref="ASecondaryIndexAccess.DefaultFilter"/>
		/// <seealso cref="ASecondaryIndexAccess.DefaultQueryPolicy"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public AQueryRecord<T> FirstOrDefault(Func<AQueryRecord<T>, bool> predicate, Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
						=> this.AsEnumerable(filterExpression, returningOnlyMatchingCT)
							.FirstOrDefault(predicate);

		/// <summary>
		/// Skips the number of records from the Index based on <see cref="ASecondaryIndexAccess.DefaultFilter"/> and <see cref="ASecondaryIndexAccess.DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="numberRecords">Number of records to skip</param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp, bool)"/>
		/// <seealso cref="First(Client.Exp, bool)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp, bool)"/>
		/// <seealso cref="AsEnumerable(Client.Exp, bool)"/>
		/// <seealso cref="ASecondaryIndexAccess.DefaultQueryPolicy"/>
		/// <seealso cref="ASecondaryIndexAccess.DefaultFilter"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public new IEnumerable<AQueryRecord<T>> Skip(int numberRecords, Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
			=> this.AsEnumerable(filterExpression, returningOnlyMatchingCT)
								.Skip(numberRecords);

		/// <summary>
		/// Filters a collection based on <paramref name="predicate"/>.
		/// </summary>
		/// <param name="predicate">A function that is used to determine if the item should be returned</param>
		/// <returns>
		/// A collection of filtered items.
		/// </returns>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		/// <seealso cref="AsEnumerable(Exp, bool)"/>
		public IEnumerable<AQueryRecord<T>> Where(Func<AQueryRecord<T>, bool> predicate)
					=> this.AsEnumerable().Where(predicate);

		/// <summary>
		/// Projects each element of an <see cref="AQueryRecord"/> into a new form.
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
		public IEnumerable<TResult> Select<TResult>(Func<AQueryRecord<T>, TResult> selector)
			=> this.AsEnumerable().Select(selector);


		#endregion


		#region IEnumerable        

		/// <summary>
		/// Returns IEnumerable&gt;<see cref="AQueryRecord"/>&lt; based on <see cref="ASecondaryIndexAccess.DefaultFilter"/> and <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>        
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>   
		/// <seealso cref="ASecondaryIndexAccess.DefaultFilter"/>
		/// <seealso cref="ASecondaryIndexAccess.AsEnumerable(Exp, bool)"/>
		public new IEnumerable<AQueryRecord<T>> AsEnumerable(Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
        {
            var setRecs = this.DefaultFilter == null
                            ? this.SetRecords.AsEnumerable(filterExpression)
                            : this.SetRecords.Query(secondaryIdxFilter: this.DefaultFilter, filterExpression: filterExpression);

            return setRecs.SelectMany(r => GroupRecord.GetGrpKeys(r.Aerospike.GetValue(this.BinName),
                                                                    this.CollectionType,
                                                                    r,
                                                                    returningOnlyMatchingCT))
                    .GroupBy(dnis => dnis.GrpkeyValue)
                            .Select(dnis => new AQueryRecord<T>(dnis.Key, dnis.Select(r => (T) r.Record)));
        }

        public new IEnumerator<AQueryRecord<T>> GetEnumerator()
        {
            foreach(var queryRec in this.AsEnumerable())
            {
                yield return queryRec;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion
    }

    [DebuggerDisplay("{ToString()}")]
    public class ASecondaryIndexAccess : IEquatable<ARecord>,
                                            IEquatable<SetRecords>,
                                            IEquatable<ASecondaryIndexAccess>,
                                            IEnumerable<AQueryRecord>
    {
        public ASecondaryIndexAccess([NotNull] SetRecords setRecords,
                                        [NotNull] string name,
                                        [NotNull] string idxBin,
                                        [NotNull] string idxType,
                                        [NotNull] string idxCollectionType)
        {
            this.SetRecords = new SetRecords(setRecords);
            this.Name = name;
            this.FullName = $"{SetRecords.SetFullName}.{this.Name}";
            this.BinName = idxBin;
            this.BinType = string.IsNullOrEmpty(idxType) 
                                ? Client.IndexType.STRING
                                : Enum.Parse<Client.IndexType>(idxType, true);      
            this.CollectionType = string.IsNullOrEmpty(idxCollectionType)
                                    ? IndexCollectionType.DEFAULT
                                    : Enum.Parse<Client.IndexCollectionType>(idxCollectionType, true);

        }

        /// <summary>
        /// Sets how records are displayed using the LinqPad <see cref="LINQPad.Extensions.Dump{T}(T)"/> method.
        /// See <see cref="ARecord.DumpTypes"/> for more information.
        /// </summary>
        /// <seealso cref="ARecord.DumpTypes"/>
        /// <seealso cref="ChangeRecordView(ARecord.DumpTypes)"/>
        public ARecord.DumpTypes DefaultRecordView 
        {
            get => this.SetRecords.DefaultRecordView;
            set
            {
                this.SetRecords.DefaultRecordView = value;
            }
        }

        /// <summary>
        /// Changes how records are displayed using the LinqPad <see cref="LINQPad.Extensions.Dump{T}(T)"/> method.        
        /// </summary>
        /// <param name="newRecordView">See <see cref="ARecord.DumpTypes"/> for more information.</param>
        /// <returns>This instance</returns>
        /// <seealso cref="ARecord.DumpTypes"/>
        /// <seealso cref="DefaultRecordView"/>
        public ASecondaryIndexAccess ChangeRecordView(ARecord.DumpTypes newRecordView)
        {
            this.DefaultRecordView = newRecordView;
            return this;
        }

        /// <summary>
        /// The set instance associated with this index.
        /// <see cref="SetRecords"/> for more information.
        /// </summary>
        public SetRecords SetRecords { get; }

        /// <summary>
        /// Sets the default filter for this index used to obtain the record set. It can be overridden by using the <see cref="Query(Filter, string[])"/> methods
        /// </summary>
        /// <seealso cref="SetFilter(Filter)"/>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(dynamic, string[])"/>
        /// <seealso cref="AsEnumerable(Exp, bool)"/>
        public Client.Filter DefaultFilter { get; set; } = null;

        /// <summary>
        /// Set the filter that will be used by this secondary index as the default filter. See <see cref="DefaultFilter"/>
        /// </summary>
        /// <param name="secondaryIdxFilter">Set&apos;s the default filter. See <see cref="Client.Filter"/></param>
        /// <returns>This object</returns>
        /// <seealso cref="DefaultFilter"/>
        /// <seealso cref="AsEnumerable(Exp, bool)"/>
        public ASecondaryIndexAccess SetFilter(Client.Filter secondaryIdxFilter)
        {
            this.DefaultFilter= secondaryIdxFilter;
            return this;
        }

        #region Aerospike Client API Items
        /// <summary>
        /// The name of the index.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the Indexes name prefixed with the namespace and set name.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// The name of the bin that defines this index.
        /// </summary>
        public string BinName { get; }
        /// <summary>
        /// The Bin&apos;s searchValue type. <see cref="Client.IndexType"/>
        /// </summary>
        public Client.IndexType BinType { get; }

        /// <summary>
        /// The index&apos;s collection type. <see cref="Client.IndexCollectionType"/>
        /// </summary>
        public Client.IndexCollectionType CollectionType { get; }

        /// <summary>
        /// Returns the Aerospike &quot;Namespace&apos;s&quot; name
        /// </summary>
        public string Namespace { get { return this.SetRecords.Namespace; } }

        /// <summary>
        /// Returns the Aerospike &quot;Set&apos;s&quot; name
        /// </summary>
        public string SetName { get => this.SetRecords.SetName; }

        /// <summary>
        /// <see href="https://docs.aerospike.com/apidocs/csharp/html/t_aerospike_client_querypolicy"/>
        /// </summary>
        public QueryPolicy DefaultQueryPolicy
        {
            get => this.SetRecords.DefaultQueryPolicy;
            set
            {
                this.SetRecords.DefaultQueryPolicy = value;
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Performs a secondary index query using the provided <see cref="Client.Filter"/>.
        /// </summary>
        /// <param name="secondaryIdxFilter">The <see cref="Client.Filter"/> used against the secondary index</param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filter.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, Exp, string[])"/>    
        /// <seealso cref="Query(dynamic, string[])"/>
        /// <seealso cref="Query(long, long, string[])"/>   
        /// <seealso cref="SetRecords.Query(Filter, string[])"/>
        public IEnumerable<ARecord> Query([NotNull] Client.Filter secondaryIdxFilter, params string[] bins)
                    => this.SetRecords.Query(secondaryIdxFilter, bins);        

        /// <summary>
        /// Performs a secondary index query using the provided <see cref="Client.Filter"/> and than apply the filter expression.
        /// </summary>
        /// <param name="secondaryIdxFilter">The <see cref="Client.Filter"/> used against the secondary index</param>
        /// <param name="filterExpression">The Aerospike filter <see cref="Client.Exp"/> that will be applied after the index filter is applied.</param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filters.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, string[])"/>    
        /// <seealso cref="Query(dynamic, string[])"/>
        /// <seealso cref="Query(long, long, string[])"/>
        /// <seealso cref="SetRecords.Query(Filter, Exp, string[])"/>
        public IEnumerable<ARecord> Query([NotNull] Client.Filter secondaryIdxFilter, [NotNull] Client.Exp filterExpression, params string[] bins)
                    => this.SetRecords.Query(secondaryIdxFilter, filterExpression, bins);

        /// <summary>
        /// See <see cref="GetFilter(object, CTX[])"/>
        /// </summary>
        /// <param name="searchValue"></param>
        /// <param name="ctxArgs"></param>
        /// <returns>A <see cref="Client.Filter"/> to perform the search based on <paramref name="searchValue"/> and the secondary index attributes.</returns>
        /// <seealso cref="GetFilter(object, CTX[])"/>
        /// <seealso cref="GetFilter(long, long, CTX[])"/>
        public Client.Filter GetFilter(Client.Value searchValue, params Client.CTX[] ctxArgs)
                    => GetFilter(searchValue.Object,ctxArgs);

        /// <summary>
        /// Creates a <see cref="Client.Filter"/> based on <paramref name="searchValue"/> type and the secondary index attributes.
        /// </summary>
        /// <param name="searchValue">The value used to search within the secondary index.</param>
        /// <param name="ctxArgs">Advance criteria for the search. See  <see cref="Client.CTX"/>.</param>
        /// <returns>A <see cref="Client.Filter"/> to perform the search</returns>
        /// <seealso cref="GetFilter(Value, CTX[])"/>
        /// <seealso cref="GetFilter(long, long, CTX[])"/>
        /// <seealso cref="Client.Filter"/>
        /// <seealso cref="Client.CTX"/>
        /// <seealso cref="DefaultFilter"/>
        /// <seealso cref="SetFilter(Filter)"/>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(long, long, string[])"/>
        public Client.Filter GetFilter(object searchValue, params Client.CTX[] ctxArgs)
        {

            if (this.CollectionType == Client.IndexCollectionType.MAPKEYS)
            {
                return searchValue switch
                {
                    string strValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, strValue, ctxArgs),
                    long lValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, lValue, ctxArgs),
                    int iValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, (long)iValue),
                    short sValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, (long)sValue, ctxArgs),
                    ulong ulValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, (long)ulValue, ctxArgs),
                    ushort usValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, (long)usValue, ctxArgs),
                    uint uiValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, (long)uiValue, ctxArgs),
                    _ => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, searchValue?.ToString(), ctxArgs)
                };
            }
            else if (this.CollectionType == Client.IndexCollectionType.MAPVALUES)
            {
                return searchValue switch
                {
                    string strValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, strValue, ctxArgs),
                    long lValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, lValue, ctxArgs),
                    int iValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, (long)iValue, ctxArgs),
                    short sValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, (long)sValue, ctxArgs),
                    ulong ulValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, (long)ulValue, ctxArgs),
                    ushort usValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, (long)usValue, ctxArgs),
                    uint uiValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, (long)uiValue, ctxArgs),
                    _ => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, searchValue?.ToString(), ctxArgs)
                };
            }
            else if (this.CollectionType == Client.IndexCollectionType.LIST)
            {
                return searchValue switch
                {
                    string strValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, strValue, ctxArgs),
                    long lValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, lValue, ctxArgs),
                    int iValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, (long)iValue, ctxArgs),
                    short sValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, (long)sValue, ctxArgs),
                    ulong ulValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, (long)ulValue, ctxArgs),
                    ushort usValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, (long)usValue, ctxArgs),
                    uint uiValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, (long)uiValue, ctxArgs),
                    _ => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, searchValue?.ToString(), ctxArgs)
                };
            }

            return searchValue switch
            {
                string strValue => Client.Filter.Equal(this.BinName, strValue, ctxArgs),
                long lValue => Client.Filter.Equal(this.BinName, lValue, ctxArgs),
                int iValue => Client.Filter.Equal(this.BinName, (long)iValue, ctxArgs),
                short sValue => Client.Filter.Equal(this.BinName, (long)sValue, ctxArgs),
                ulong ulValue => Client.Filter.Equal(this.BinName, (long)ulValue, ctxArgs),
                ushort usValue => Client.Filter.Equal(this.BinName, (long)usValue, ctxArgs),
                uint uiValue => Client.Filter.Equal(this.BinName, (long)uiValue, ctxArgs),
                _ => Client.Filter.Equal(this.BinName, searchValue?.ToString(), ctxArgs)
            };
        }

        /// <summary>
        /// Creates a <see cref="Client.Filter.Range(string, long, long, CTX[])"/> based on the secondary index attributes.
        /// </summary>
        /// <param name="inclusiveStartRange">Start range, inclusive</param>
        /// <param name="inclusiveEndRange">End range, inclusive</param>
        /// <param name="ctxArgs">Advance criteria for the search. See  <see cref="Client.CTX"/>.</param>
        /// <returns>A <see cref="Client.Filter.Range(string, IndexCollectionType, long, long, CTX[])"/> filter</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="inclusiveStartRange"/> is greater than <paramref name="inclusiveEndRange"/></exception>
        /// <seealso cref="GetFilter(object, CTX[])"/>
        /// <seealso cref="Client.Filter"/>
        /// <seealso cref="Client.CTX"/>
        /// <seealso cref="DefaultFilter"/>
        /// <seealso cref="SetFilter(Filter)"/>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(long, long, string[])"/>
        public Client.Filter GetFilter(long inclusiveStartRange, long inclusiveEndRange, params Client.CTX[] ctxArgs)
        {
            if (inclusiveStartRange > inclusiveEndRange)
                throw new ArgumentException($"Argument {nameof(inclusiveStartRange)} ({inclusiveStartRange}) is greater than {nameof(inclusiveEndRange)} ({inclusiveEndRange}).", nameof(inclusiveStartRange));

            if (this.CollectionType == Client.IndexCollectionType.MAPKEYS)
            {
                return Client.Filter.Range(this.BinName, IndexCollectionType.MAPKEYS, inclusiveStartRange, inclusiveEndRange, ctxArgs);
                
            }
            else if (this.CollectionType == Client.IndexCollectionType.MAPVALUES)
            {
                return Client.Filter.Range(this.BinName, IndexCollectionType.MAPVALUES, inclusiveStartRange, inclusiveEndRange, ctxArgs);
               
            }
            else if (this.CollectionType == Client.IndexCollectionType.LIST)
            {
                return Client.Filter.Range(this.BinName, IndexCollectionType.LIST, inclusiveStartRange, inclusiveEndRange, ctxArgs);
               
            }

            return Client.Filter.Range(this.BinName, inclusiveStartRange, inclusiveEndRange, ctxArgs);            
        }

        /// <summary>
        /// Performs a search on the secondary index based <paramref name="idxBinValue"/> and the index associated bin (<seealso cref="BinName"/>). 
        /// For more information see <see cref="Client.Filter"/>.
        /// </summary>
        /// <param name="idxBinValue">The searchValue used to conduct the search associated with <see cref="BinName"/></param>
        /// <param name = "bins" > Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filters.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="BinName"/>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(long, long, string[])"/>
        /// <seealso cref="SetRecords.Query(Filter, Exp, string[])"/>
        public IEnumerable<ARecord> Query([NotNull] dynamic idxBinValue, params string[] bins)
        {
            return idxBinValue switch
            {
                Client.Key keyValue => this.Query(GetFilter(keyValue.userKey), bins),
                Client.Value vValue => this.Query(GetFilter(vValue), bins),
                _ => this.Query(GetFilter(idxBinValue), bins)
            };
        }

        /// <summary>
        /// Performs a secondary index range search based on <paramref name="inclusiveStartRange"/> and <paramref name="inclusiveEndRange"/>, inclusively.
        /// For more information see <see cref="Client.Filter"/>.
        /// </summary>
        /// <param name="inclusiveStartRange">Start Rage, inclusive</param>
        /// <param name="inclusiveEndRange">End Range, inclusive</param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>A collection of records that match the filters.</returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="inclusiveStartRange"/> is greater than <paramref name="inclusiveEndRange"/></exception>
        /// <seealso cref="Query(Filter, string[])"/>
        /// <seealso cref="Query(dynamic, string[])"/>
        /// <seealso cref="GetFilter(long, long, CTX[])"/>
        /// <seealso cref="SetRecords.Query(Filter, Exp, string[])"/>        
        public IEnumerable<ARecord> Query(long inclusiveStartRange, long inclusiveEndRange, params string[] bins)
                    => this.Query(GetFilter(inclusiveStartRange, inclusiveEndRange), bins);

		#endregion

		#region LINQ Methods

		/// <summary>
		/// Returns the top number of records from the Index based on <see cref="DefaultFilter"/> and <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="numberRecords">Number of records to return</param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>        
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns>A collection of records or empty set</returns>
		/// <seealso cref="AsEnumerable(Exp, bool)"/>
		/// <seealso cref="DefaultQueryPolicy"/>
		/// <seealso cref="DefaultFilter"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public IEnumerable<AQueryRecord> Take(int numberRecords, Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
            => this.AsEnumerable(filterExpression, returningOnlyMatchingCT)
                    .Take(numberRecords);

		/// <summary>
		/// Returns the first record from the Index based on <see cref="DefaultFilter"/> and <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp, bool)"/>
		/// <seealso cref="First(Func{AQueryRecord, bool}, Exp, bool)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp, bool)"/>
		/// <seealso cref="FirstOrDefault(Func{AQueryRecord, bool}, Exp, bool)"/>
		/// <seealso cref="AsEnumerable(Client.Exp, bool)"/>
		/// <seealso cref="DefaultFilter"/>
		/// <seealso cref="DefaultQueryPolicy"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public AQueryRecord First(Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
				=> this.Take(1, filterExpression, returningOnlyMatchingCT).First();

		/// <summary>
		/// Returns the first record or null from the Index based on <see cref="DefaultFilter"/> and <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp, bool)"/>
		/// <seealso cref="First(Client.Exp, bool)"/>
		/// <seealso cref="First(Func{AQueryRecord, bool}, Exp, bool)"/>
		/// <seealso cref="FirstOrDefault(Func{AQueryRecord, bool}, Exp, bool)"/>
		/// <seealso cref="AsEnumerable(Client.Exp, bool)"/>
		/// <seealso cref="DefaultFilter"/>
		/// <seealso cref="DefaultQueryPolicy"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public AQueryRecord FirstOrDefault(Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
					=> this.Take(1, filterExpression, returningOnlyMatchingCT).FirstOrDefault();

		/// <summary>
		/// Returns the first record from the Index based on <see cref="DefaultFilter"/> and <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="predicate">
		/// Predicate used to find the first occurrence.
		/// </param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp, bool)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp, bool)"/>
		/// <seealso cref="First(Exp, bool)"/>
		/// <seealso cref="First(Func{AQueryRecord, bool}, Exp, bool)"/>
		/// <seealso cref="AsEnumerable(Client.Exp, bool)"/>
		/// <seealso cref="DefaultFilter"/>
		/// <seealso cref="DefaultQueryPolicy"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public AQueryRecord First(Func<AQueryRecord, bool> predicate, Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
                        => this.AsEnumerable(filterExpression, returningOnlyMatchingCT)                       
                            .First(predicate);

		/// <summary>
		/// Returns the first record or null from the Index based on <see cref="DefaultFilter"/> and <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="predicate">
		/// Predicate used to find the first occurrence.
		/// </param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp, bool)"/>
		/// <seealso cref="First(Client.Exp, bool)"/>
		/// <seealso cref="First(Func{AQueryRecord, bool}, Exp, bool)"/>
		/// <seealso cref="FirstOrDefault(Exp, bool)"/>
		/// <seealso cref="AsEnumerable(Client.Exp, bool)"/>
		/// <seealso cref="DefaultFilter"/>
		/// <seealso cref="DefaultQueryPolicy"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public AQueryRecord FirstOrDefault(Func<AQueryRecord, bool> predicate, Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
						=> this.AsEnumerable(filterExpression, returningOnlyMatchingCT)
                            .FirstOrDefault(predicate);

		/// <summary>
		/// Skips the number of records from the Index based on <see cref="DefaultFilter"/> and <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="numberRecords">Number of records to skip</param>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp, bool)"/>
		/// <seealso cref="First(Client.Exp, bool)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp, bool)"/>
		/// <seealso cref="AsEnumerable(Client.Exp, bool)"/>
		/// <seealso cref="DefaultQueryPolicy"/>
		/// <seealso cref="DefaultFilter"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public IEnumerable<AQueryRecord> Skip(int numberRecords, Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
			=> this.AsEnumerable(filterExpression, returningOnlyMatchingCT)
							    .Skip(numberRecords);

		/// <summary>
		/// Number of records from the Index based on <see cref="DefaultFilter"/> and <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns></returns>
		/// <seealso cref="Take(int, Client.Exp, bool)"/>
		/// <seealso cref="First(Client.Exp, bool)"/>
		/// <seealso cref="FirstOrDefault(Client.Exp, bool)"/>
		/// <seealso cref="AsEnumerable(Client.Exp, bool)"/>
		/// <seealso cref="DefaultQueryPolicy"/>
		/// <seealso cref="DefaultFilter"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		public int Count(Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
			=> this.AsEnumerable(filterExpression, returningOnlyMatchingCT)
								.Count();

		/// <summary>
		/// Filters a collection based on <paramref name="predicate"/>.
		/// </summary>
		/// <param name="predicate">A function that is used to determine if the item should be returned</param>
		/// <returns>
		/// A collection of filtered items.
		/// </returns>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>  
		/// <seealso cref="AsEnumerable(Exp, bool)"/>
		public IEnumerable<AQueryRecord> Where(Func<AQueryRecord, bool> predicate)
					=> this.AsEnumerable().Where(predicate);

		/// <summary>
		/// Projects each element of an <see cref="AQueryRecord"/> into a new form.
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
		public IEnumerable<TResult> Select<TResult>(Func<AQueryRecord, TResult> selector)
			=> this.AsEnumerable().Select(selector);


		#endregion

		/// <summary>
		/// Drops this index
		/// </summary>
		public void Drop()
        {
            this.SetRecords.DropIndex(this.Name);
        }

        #region Overrides
        public override bool Equals(object obj)
        {
            if (obj is SetRecords set) return this.Equals(set);
            if (obj is ARecord rec) return this.Equals(rec);
            if (obj is ASecondaryIndexAccess idx) return this.Equals(idx);

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override string ToString()
        {            
            return $"{this.Namespace}.{this.SetName}.{this.Name}({this.BinName})";
        }

        //protected object ToDump()
        //{
        //    return LPU.ToExpando(this, include: "Namespace, SetName, Name, BinType, BinName, DefaultQueryPolicy, DefaultRecordView");
        //}

        #endregion

        #region IEquatable

        public bool Equals([AllowNull] ARecord other)
        {            
            return this.SetRecords.Equals(other);
        }

        public bool Equals([AllowNull] SetRecords other)
        {
            return this.SetRecords.Equals(other);
        }

        public bool Equals([AllowNull] ASecondaryIndexAccess other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;

            return this.SetName == other.SetName
                        && this.Namespace == other.Namespace
                        && this.Name == other.Name;
        }

        #endregion

        #region IEnumerable

        protected struct GroupRecord
        {
            public object GrpkeyValue;
            public ARecord Record;

            /// <summary>
            /// Returns the Grouping Key based on the index collection type.
            /// </summary>
            /// <param name="idxValue">Index Value</param>
            /// <param name="collectionType">Index Collection Type</param>
            /// <param name="rec">The record associated with the index</param>
            /// <param name="returningOnlyMatching">
            /// if true (default), only index values that match the collection type are returned.
            /// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
            /// </param>
            /// <returns>
            /// The collection of Grouping records based on the index value or Empty Array to indicate no collection type matches.
            /// </returns>
            public static IEnumerable<GroupRecord> GetGrpKeys(object idxValue,
                                                                Client.IndexCollectionType collectionType,
                                                                ARecord rec,
                                                                bool returningOnlyMatching = true)
            {
                if (idxValue is null)
                    return Enumerable.Empty<GroupRecord>();

                static object DetermineGrpValue(object item)
                {
                    var itemType = item.GetType();

                    if (Helpers.IsSubclassOfInterface(typeof(IDictionary<,>), itemType)
                            || Helpers.IsSubclassOfInterface(typeof(IList<>), itemType)
                            || typeof(JsonDocument) == itemType)
                    {
                        return $"CDT<{Helpers.GetRealTypeName(itemType)}-#{item.GetHashCode()}>";
                    }

                    return item;
                }

                if (idxValue is JsonDocument jsonDoc)
                {
                    return GetGrpKeys(jsonDoc.ToDictionary(), collectionType, rec, returningOnlyMatching);
                }

                if (collectionType == IndexCollectionType.MAPKEYS)
                {
                    if (idxValue is IDictionary<object, object> mapItems)
                    {                       
                        return mapItems.Keys
                                    .Where(k => k is not null)
                                    .Select(k => new GroupRecord()
                                    {
                                        GrpkeyValue = DetermineGrpValue(k),
                                        Record = rec
                                    });
                    }
                    else if (idxValue is IDictionary<string, object> mapsItems)
                    {                        
                        return mapsItems.Keys
                                    .Where(k => k is not null)
                                    .Select(k => new GroupRecord()
                                    {
                                        GrpkeyValue = k,
                                        Record = rec
                                    });
                    }
                    if (returningOnlyMatching) return Array.Empty<GroupRecord>();

                    idxValue = $"!|'{DetermineGrpValue(idxValue)}'|!";
                }
                else if(collectionType == IndexCollectionType.MAPVALUES)
                {
                    if (idxValue is IDictionary<object, object> mapItems)
                    {
                        return mapItems.Values
                                    .Where(k => k is not null)
                                    .Select(k => new GroupRecord()
                                    {
                                        GrpkeyValue = DetermineGrpValue(k),
                                        Record = rec
                                    });
                    }
                    else if (idxValue is IDictionary<string, object> mapsItems)
                    {
                        return mapsItems.Values
                                    .Where(k => k is not null)
                                    .Select(k => new GroupRecord()
                                    {
                                        GrpkeyValue = DetermineGrpValue(k),
                                        Record = rec
                                    });
                    }

                    if (returningOnlyMatching) return Array.Empty<GroupRecord>();
                    idxValue = $"!|'{DetermineGrpValue(idxValue)}'|!";
                }
                else if( collectionType == IndexCollectionType.LIST)
                {
                    if (idxValue is IList<object> listItems)
                    {
                        return listItems
                            .Where(k => k is not null)
                            .Select(k => new GroupRecord()
                            {
                                GrpkeyValue = DetermineGrpValue(k),
                                Record = rec
                            });
                    }
                    else if (idxValue is IList<JsonDocument> jlistItems)
                    {
                        return jlistItems
                            .Where(k => k is not null)
                            .Select(k => new GroupRecord()
                            {
                                GrpkeyValue = DetermineGrpValue(k),
                                Record = rec
                            });
                    }
                    if (returningOnlyMatching) return Array.Empty<GroupRecord>();

                    idxValue = $"!|'{DetermineGrpValue(idxValue)}'|!";
                }
                
                return new GroupRecord[] { new GroupRecord()
                    {
                        GrpkeyValue = DetermineGrpValue(idxValue),
                        Record = rec
                    } };
            }            
		}

		protected IEnumerable<AQueryRecord> GroupByKey(IEnumerable<ARecord> setRecs, bool returningOnlyMatchingCT) =>
				setRecs.SelectMany(r => GroupRecord.GetGrpKeys(r.Aerospike.GetValue(this.BinName),
																	this.CollectionType,
																	r,
																	returningOnlyMatchingCT))
					.GroupBy(dnis => dnis.GrpkeyValue)
							.Select(dnis => new AQueryRecord(dnis.Key, dnis.Select(r => r.Record)));

		/// <summary>
		/// Returns IEnumerable&gt;<see cref="AQueryRecord"/>&lt; based on <see cref="DefaultFilter"/> and <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
		/// </summary>
		/// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>        
		/// <param name="returningOnlyMatchingCT">
		/// if true (default), only index values that match the collection type are returned.
		/// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
		/// </param>
		/// <returns/>
		/// <seealso cref="DefaultQueryPolicy"/>
		/// <seealso cref="DefaultFilter"/>
		/// <seealso cref="Query(dynamic, string[])"/>
		/// <seealso cref="Query(Filter, Exp, string[])"/>
		/// <seealso cref="Query(Filter, string[])"/>           
		public IEnumerable<AQueryRecord> AsEnumerable(Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
        {
            var setRecs = this.DefaultFilter == null
                            ? this.SetRecords.AsEnumerable(filterExpression)
                            : this.SetRecords.Query(secondaryIdxFilter: this.DefaultFilter, filterExpression: filterExpression);
            
            return this.GroupByKey(setRecs, returningOnlyMatchingCT);
        }

        public IEnumerator<AQueryRecord> GetEnumerator()
        {
            foreach (var queryRec in this.AsEnumerable())
            {
                yield return queryRec;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
        #endregion

    }
}
