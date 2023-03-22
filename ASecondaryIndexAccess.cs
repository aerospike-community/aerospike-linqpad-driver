using Aerospike.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LPU = LINQPad.Util;

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
        /// <param name="secondaryIdxFilter">The filter used against the secondary index</param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filter.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, Exp, string[])"/>    
        /// <seealso cref="SetRecords.Query(Filter, string[])"/>
        new public IEnumerable<T> Query([NotNull] Client.Filter secondaryIdxFilter, params string[] bins)
        {
            return this.SetRecords.Query(secondaryIdxFilter, bins);
        }

        /// <summary>
        /// Performs a secondary index query using the provided <see cref="Client.Filter"/> and than apply the filter expression.
        /// </summary>
        /// <param name="secondaryIdxFilter">The filter used against the secondary index</param>
        /// <param name="filterExpression">The Aerospike filter <see cref="Client.Exp"/> that will be applied after the index filter is applied.</param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filters.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, string[])"/>              
        /// <seealso cref="SetRecords.Query(Filter, Exp, string[])"/>
        new public IEnumerable<T> Query([NotNull] Client.Filter secondaryIdxFilter, [NotNull] Client.Exp filterExpression, params string[] bins)
        {
            return this.SetRecords.Query(secondaryIdxFilter, filterExpression, bins);
        }

        Client.Filter GetFilter(Client.Value searchValue)
        {
            return GetFilter(searchValue.Object);
        }

        Client.Filter GetFilter(object searchValue)
        {

            if (this.CollectionType == Client.IndexCollectionType.MAPKEYS)
            {
                return searchValue switch
                {
                    string strValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, strValue),
                    long lValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, lValue),
                    int iValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, (long)iValue),
                    short sValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, (long)sValue),
                    ulong ulValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, (long)ulValue),
                    ushort usValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, (long)usValue),
                    uint uiValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, (long)uiValue),
                    _ => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPKEYS, searchValue?.ToString())
                };
            }            
            else if (this.CollectionType == Client.IndexCollectionType.MAPVALUES)
            {
                return searchValue switch
                {
                    string strValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, strValue),
                    long lValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, lValue),
                    int iValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, (long)iValue),
                    short sValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, (long)sValue),
                    ulong ulValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, (long)ulValue),
                    ushort usValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, (long)usValue),
                    uint uiValue => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, (long)uiValue),
                    _ => Client.Filter.Contains(this.BinName, IndexCollectionType.MAPVALUES, searchValue?.ToString())
                }; 
            }
            else if (this.CollectionType == Client.IndexCollectionType.LIST)
            {
                return searchValue switch
                {
                    string strValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, strValue),
                    long lValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, lValue),
                    int iValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, (long)iValue),
                    short sValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, (long)sValue),
                    ulong ulValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, (long)ulValue),
                    ushort usValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, (long)usValue),
                    uint uiValue => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, (long)uiValue),
                    _ => Client.Filter.Contains(this.BinName, IndexCollectionType.LIST, searchValue?.ToString())
                };
            }
            
            return searchValue switch
            {
                string strValue => Client.Filter.Equal(this.BinName, strValue),
                long lValue => Client.Filter.Equal(this.BinName, lValue),
                int iValue => Client.Filter.Equal(this.BinName, (long)iValue),
                short sValue => Client.Filter.Equal(this.BinName, (long)sValue),
                ulong ulValue => Client.Filter.Equal(this.BinName, (long)ulValue),
                ushort usValue => Client.Filter.Equal(this.BinName, (long)usValue),
                uint uiValue => Client.Filter.Equal(this.BinName, (long)uiValue),
                _ => Client.Filter.Equal(this.BinName, searchValue?.ToString())
            };
        }


        /// <summary>
        /// Performs a search on the secondary index based <paramref name="idxBinValue"/> and the properties associated with the index. 
        /// </summary>
        /// <param name="idxBinValue">The searchValue used to conduct the search associated with bin</param>
        /// <param name = "bins" > Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filters.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, string[])"/>
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

        #endregion

        #region IEnumerable        

        /// <summary>
        /// Returns IEnumerable&gt;<see cref="AQueryRecord"/>&lt; based on the index and <paramref name="filterExpression"/>.
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
        /// Sets the default filter for this index. It can be overridden by using the <see cref="Query(Filter, string[])"/> methods
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
        /// <param name="secondaryIdxFilter"></param>
        /// <returns>This object</returns>
        /// <seealso cref="DefaultFilter"/>
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
        /// <param name="secondaryIdxFilter">The filter used against the secondary index</param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filter.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, Exp, string[])"/>    
        /// <seealso cref="SetRecords.Query(Filter, string[])"/>
        public IEnumerable<ARecord> Query([NotNull] Client.Filter secondaryIdxFilter, params string[] bins)
        {
            return this.SetRecords.Query(secondaryIdxFilter, bins);
        }

        /// <summary>
        /// Performs a secondary index query using the provided <see cref="Client.Filter"/> and than apply the filter expression.
        /// </summary>
        /// <param name="secondaryIdxFilter">The filter used against the secondary index</param>
        /// <param name="filterExpression">The Aerospike filter <see cref="Client.Exp"/> that will be applied after the index filter is applied.</param>
        /// <param name="bins">Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filters.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="Query(Filter, string[])"/>              
        /// <seealso cref="SetRecords.Query(Filter, Exp, string[])"/>
        public IEnumerable<ARecord> Query([NotNull] Client.Filter secondaryIdxFilter, [NotNull] Client.Exp filterExpression, params string[] bins)
        {
            return this.SetRecords.Query(secondaryIdxFilter, filterExpression, bins);
        }

        /// <summary>
        /// Performs a search on the secondary index based <paramref name="idxBinValue"/> and the index associated bin (<seealso cref="BinName"/>). 
        /// </summary>
        /// <param name="idxBinValue">The searchValue used to conduct the search associated with <see cref="BinName"/></param>
        /// <param name = "bins" > Only include these bins in the result.</param>
        /// <returns>
        /// A collection of records that match the filters.
        /// </returns>
        /// <exception cref="AerospikeException">Thrown if an index cannot be found to match the filter</exception>
        /// <seealso cref="BinName"/>
        /// <seealso cref="Query(Filter, string[])"/>
        public IEnumerable<ARecord> Query([NotNull] dynamic idxBinValue, params string[] bins)
        {            
            Filter GetValue(Client.Value value)
            {
                return value.Object switch
                {
                    string strValue => Filter.Equal(this.BinName, strValue),
                    long lValue => Filter.Equal(this.BinName, lValue),
                    int iValue => Filter.Equal(this.BinName, iValue),
                    short sValue => Filter.Equal(this.BinName, sValue),
                    ulong ulValue => Filter.Equal(this.BinName, (long) ulValue),
                    ushort usValue => Filter.Equal(this.BinName, usValue),
                    uint uiValue => Filter.Equal(this.BinName, uiValue),
                    _ => Filter.Equal(this.BinName, value.Object?.ToString())
                };                
            }

            return idxBinValue switch
            {
                Client.Key keyValue => this.Query(GetValue(keyValue.userKey), bins),
                Client.Value vValue => this.Query(GetValue(vValue), bins),
                string strValue => this.Query(Filter.Equal(this.BinName, strValue), bins),
                long lValue => this.Query(Filter.Equal(this.BinName, lValue), bins),
                int iValue => this.Query(Filter.Equal(this.BinName, iValue), bins),
                short sValue => this.Query(Filter.Equal(this.BinName, sValue), bins),
                ulong ulValue => this.Query(Filter.Equal(this.BinName, (long)ulValue), bins),
                ushort usValue => this.Query(Filter.Equal(this.BinName, usValue), bins),
                uint uiValue => this.Query(Filter.Equal(this.BinName, uiValue), bins),
                _ => this.Query(Filter.Equal(this.BinName, idxBinValue?.ToString()), bins)
            };
        }

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
                    return GetGrpKeys((Dictionary<object, object>)jsonDoc, collectionType, rec, returningOnlyMatching);
                }

                if (collectionType == IndexCollectionType.MAPKEYS)
                {
                    if (idxValue is IDictionary<object, object> mapItems)
                    {                       
                        return mapItems.Keys
                                    .Select(k => new GroupRecord()
                                    {
                                        GrpkeyValue = DetermineGrpValue(k),
                                        Record = rec
                                    });
                    }
                    else if (idxValue is IDictionary<string, object> mapsItems)
                    {                        
                        return mapsItems.Keys
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
                                    .Select(k => new GroupRecord()
                                    {
                                        GrpkeyValue = DetermineGrpValue(k),
                                        Record = rec
                                    });
                    }
                    else if (idxValue is IDictionary<string, object> mapsItems)
                    {
                        return mapsItems.Values
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
                        return listItems.Select(k => new GroupRecord()
                        {
                            GrpkeyValue = DetermineGrpValue(k),
                            Record = rec
                        });
                    }
                    else if (idxValue is IList<JsonDocument> jlistItems)
                    {
                        return jlistItems.Select(k => new GroupRecord()
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

        /// <summary>
        /// Returns IEnumerable&gt;<see cref="AQueryRecord"/>&lt; based on the index and <see cref="DefaultQueryPolicy"/> or <paramref name="filterExpression"/>.
        /// </summary>
        /// <param name="filterExpression">A Filter <see cref="Client.Exp"/> used to obtain the collection of records.</param>        
        /// <param name="returningOnlyMatchingCT">
        /// if true (default), only index values that match the collection type are returned.
        /// If false values that don't match the collection type are returned but will be wrapped with &quot;!|&apos;&lt;value&gt;&apos;|!&quot;
        /// </param>
        /// <returns/>
        /// <seealso cref="DefaultQueryPolicy"/>
        /// <seealso cref="Query(dynamic, string[])"/>
        /// <seealso cref="Query(Filter, Exp, string[])"/>
        /// <seealso cref="Query(Filter, string[])"/>   
        /// <seealso cref="DefaultFilter"/>
        public IEnumerable<AQueryRecord> AsEnumerable(Client.Exp filterExpression = null, bool returningOnlyMatchingCT = true)
        {
            var setRecs = this.DefaultFilter == null
                            ? this.SetRecords.AsEnumerable(filterExpression)
                            : this.SetRecords.Query(secondaryIdxFilter: this.DefaultFilter, filterExpression: filterExpression);
            
            return setRecs.SelectMany(r => GroupRecord.GetGrpKeys(r.Aerospike.GetValue(this.BinName),
                                                                    this.CollectionType,
                                                                    r,
                                                                    returningOnlyMatchingCT))
                    .GroupBy(dnis => dnis.GrpkeyValue)
                            .Select(dnis => new AQueryRecord(dnis.Key, dnis.Select(r => r.Record)));
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
