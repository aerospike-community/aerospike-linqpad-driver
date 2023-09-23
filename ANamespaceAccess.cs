using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Aerospike.Client;
using LINQPad;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LPU = LINQPad.Util;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    /// <summary>
    /// A class used to define Aerospike Namespaces.
    /// </summary>
    public class ANamespaceAccess
    {

        public ANamespaceAccess(IDbConnection dbConnection, string ns, string[] binNames)
        {
            this.AerospikeConnection = dbConnection as AerospikeConnection;
            this.Namespace = ns;
            //this.Name = setName;
            this.BinNames = Helpers.RemoveDups(binNames);

            this.DefaultWritePolicy = new WritePolicy(this.AerospikeConnection.AerospikeClient.WritePolicyDefault);
            this.DefaultQueryPolicy = new QueryPolicy(this.AerospikeConnection.AerospikeClient.QueryPolicyDefault);
            this.DefaultReadPolicy = new QueryPolicy(this.AerospikeConnection.AerospikeClient.QueryPolicyDefault);

        }

        public ANamespaceAccess(IDbConnection dbConnection, LPNamespace lpNamespace, string ns, string[] binNames)
            : this(dbConnection, ns, binNames)
        {
            this.LPnamespace = lpNamespace;
        }

        public ANamespaceAccess(ANamespaceAccess clone, Expression expression)
        {
            this.Namespace = clone.Namespace;
            this.BinNames = clone.BinNames;
            this.AerospikeConnection = clone.AerospikeConnection;
            this._sets = clone._sets;

            this.DefaultWritePolicy = clone.DefaultWritePolicy;
            this.DefaultQueryPolicy = new QueryPolicy(clone.DefaultQueryPolicy)
            {
                filterExp = expression
            };
            this.DefaultReadPolicy = new QueryPolicy(this.DefaultQueryPolicy);
        }

        internal static long ForceExplorerRefresh = 0;

        /// <summary>
        /// Refreshes the Connection Explorer
        /// </summary>        
#pragma warning disable CA1822 // Mark members as static
        public async void RefreshExplorer()
#pragma warning restore CA1822 // Mark members as static
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
            var removedBins = bins.Where(b => b.value.Object is null);
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

            var recordSet = this.Sets.FirstOrDefault(s => s.SetName == setName);

            if (recordSet == null)
            {
                var binNames = bins?.Select(b => b.BinName).ToArray();
                var lpSet = new LPSet(this.LPnamespace, setName, bins);
                var accessSet = new SetRecords(lpSet, this, setName, binNames);

                this.LPnamespace?.TryAddSet(setName, bins);

                lock (this)
                {
                    this._sets.Add(accessSet);
                }

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

        private bool RemoveBinsFromSet(string setName, IEnumerable<LPSet.BinType> removeBins)
        {
            if (string.IsNullOrEmpty(setName))
                return false;

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
                if (this._sets.Any()) return this._sets;

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
        /// Returns the Set instance or null indicating the set doesn't exists in this namespace.
        /// </summary>
        /// <param name="setName">The name of the Aerospike set</param>
        /// <returns>A <see cref="SetRecords"/> instance or null</returns>
        /// <seealso cref="Exists(string)"/>
        public SetRecords this[string setName]
        {
            get => this.Sets.FirstOrDefault(s => s.SetName == setName);
        }

        /// <summary>
        /// Determines if a set exists within this namespace.
        /// </summary>
        /// <param name="setName">set name</param>
        /// <returns>
        /// True if the sets exists, otherwise false.
        /// </returns>
        /// <seealso cref="this[string]"/>
        public bool Exists(string setName) => this.Sets.Any(s => s.SetName == setName);

        /// <summary>
        /// Returns the Aerospike Null Set for this namespace.
        /// The Null Set will contain all the records with a namespace.
        /// </summary>
        public SetRecords NullSet { get => this[LPSet.NullSetName]; }

        #region Aerospike API items

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
        /// Gets all records in a set
        /// </summary>        
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="bins">bins you wish to get. If not provided all bins for a record</param>
        /// <returns>An array of records in the set</returns>
        public ARecord[] GetRecords(string setName, params string[] bins)
        {            
            var recordSets = new List<ARecord>();

            using (var recordset = this.AerospikeConnection
                                    .AerospikeClient
                                    .Query(this.DefaultQueryPolicy,
                                            string.IsNullOrEmpty(setName) || setName == LPSet.NullSetName
                                                ? new Statement() { Namespace = this.Namespace}
                                                : new Statement() { Namespace = this.Namespace, SetName = setName }))

                while (recordset.Next())
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
        /// Gets all records in a namespace and/or set
        /// </summary>
        /// <param name="nsName">namespace</param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="bins">bins you wish to get. If not provided all bins for a record</param>
        /// <returns>An array of records in the set</returns>
        public ARecord[] GetRecords([NotNull] string nsName, string setName, params string[] bins)
        {
            var recordSets = new List<ARecord>();

            using (var recordset = this.AerospikeConnection
                                    .AerospikeClient
                                    .Query(this.DefaultQueryPolicy,
                                            string.IsNullOrEmpty(setName) || setName == LPSet.NullSetName
                                                ? new Statement() { Namespace = nsName }
                                                : new Statement() { Namespace = nsName, SetName = setName }))

                while (recordset.Next())
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
            var pk = Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName);

            var record = this.AerospikeConnection
                                .AerospikeClient
                                .Get(this.DefaultReadPolicy, pk, bins);
            var setAccess = this[setName];

            return new ARecord(this,
                                pk,
                                record,
                                setAccess?.BinNames,
                                dumpType: this.AerospikeConnection.RecordView);
        }

        /// <summary>
        /// Puts (Writes) a DB record based on the provided record including Expiration.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used.
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
        /// <seealso cref="Get(string, dynamic, string[])"/>
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
        /// <param name="doctumentBinName">
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
        /// <exception cref="TypeAccessException">Thrown if cannot write <paramref name="instance"/></exception>
        public void WriteObject<T>(string setName,
                                    [NotNull] dynamic primaryKey,
                                    [NotNull] T instance,
                                    Func<string, string, object, bool, object> transform = null,
                                    string doctumentBinName = null,
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
                throw new TypeAccessException($"Don't know how to Write an IEnumerable Object (\"{instanceType}\"). Try using a \"Put\" method or call this method on each item in the collection.");
            }
            else
                dictItem = Helpers.TransForm(instance, transform);

            if(string.IsNullOrEmpty(doctumentBinName))
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
                                                                        doctumentBinName,
                                                                        dictItem));
                this.AddDynamicSet(setName, new LPSet.BinType[] 
                                                    { new LPSet.BinType(doctumentBinName, typeof(JsonDocument), false, true)});
            }          
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
        /// <see cref="ARecord.AerospikeAPI.TTL"/>
        /// <see cref="ARecord.AerospikeAPI.Expiration"/>
        /// </param>
        /// <param name="useImportRecTTL">
        /// If true, the TTL of the record at export is used.
        /// Otherwise, <paramref name="ttl"/> is used, if provided.
        /// </param>
        /// <returns>The number of records imported</returns>
        /// <seealso cref="Export(string, Exp, bool)"/>
        /// <seealso cref="Import(string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="SetRecords.Export(string, Exp, bool)"/>
        /// <seealso cref="SetRecords.Import(string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="AClusterAccess.Import(string, string, string, WritePolicy)"/>
        /// <exception cref="InvalidOperationException">Thrown if the cluster is a production cluster. Can disable this by going into the connection properties.</exception>        
        public int Import([NotNull] string importJSONFile,
                            string setName,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                            bool useImportRecTTL = false)
        {
            if (this.AerospikeConnection.CXInfo.IsProduction)
                throw new InvalidOperationException("Cannot Import into a Cluster marked \"In Production\"");

            var jsonStr = System.IO.File.ReadAllText(importJSONFile);
            var jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                DateParseHandling = DateParseHandling.DateTimeOffset,
                NullValueHandling = NullValueHandling.Ignore,
            };

            var jsonStructs = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonExportStructure[]>(jsonStr, jsonSettings);

            foreach (var item in jsonStructs)
            {
                this.Put(setName,
                            item.KeyValue ?? item.Digest,
                            item.Values,                            
                            writePolicy,
                            useImportRecTTL
                                ? ARecord.AerospikeAPI.CalcTTLTimeSpan(item.TimeToLive)
                                : ttl);
            }

            return jsonStructs.Length;
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
        /// <see cref="ARecord.AerospikeAPI.TTL"/>
        /// <see cref="ARecord.AerospikeAPI.Expiration"/>
        /// </param>
        /// <param name="useImportRecTTL">
        /// If true, the TTL of the record at export is used.
        /// Otherwise, <paramref name="ttl"/> is used, if provided.
        /// </param>
        /// <returns>The number of records imported</returns>
        /// <seealso cref="Export(string, Exp, bool)"/>
        /// <seealso cref="Import(string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="SetRecords.Export(string, Exp, bool)"/>
        /// <seealso cref="AClusterAccess.Import(string, string, string, WritePolicy)"/>
        public int Import([NotNull] string importJSONFile,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                            bool useImportRecTTL = false)
        {
            //Debugger.Launch();
            if (this.AerospikeConnection.CXInfo.IsProduction)
                throw new InvalidOperationException("Cannot Truncate a Cluster marked \"In Production\"");

            var jsonStr = System.IO.File.ReadAllText(importJSONFile);
            var jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                DateParseHandling = DateParseHandling.DateTimeOffset
            };
            var jsonStructs = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonExportStructure[]>(jsonStr, jsonSettings);

            foreach (var item in jsonStructs)
            {               
                this.Put(item.SetName, 
                            item.KeyValue ?? item.Digest,
                            item.Values,                            
                            writePolicy,
                            useImportRecTTL
                                ? ARecord.AerospikeAPI.CalcTTLTimeSpan(item.TimeToLive)
                                : ttl);
            }

            return jsonStructs.Length;
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
        /// <seealso cref="SetRecords.Import(string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="ANamespaceAccess.Import(string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="ANamespaceAccess.Import(string, WritePolicy, TimeSpan?, bool)"/>
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
        /// <seealso cref="FromJson(string, string, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
        /// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
        /// <seealso cref="ARecord.ToJson(string, bool)"/>
        /// <seealso cref="SetRecords.ToJson(Exp, string, bool)"/>
        /// <seealso cref="Aerospike.Client.Exp"/>
        public JArray ToJson(string setName, [AllowNull] string pkPropertyName = "_id", bool useDigest = false)
        {
            var jsonArray = new JArray();

            foreach (var rec in this.GetRecords(setName))
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
        /// <returns>The number of items put.</returns>
        /// <seealso cref="ToJson(string, string, bool)"/>
        /// <seealso cref="ARecord.ToJson(string, bool)"/>
        /// <seealso cref="FromJson(string, string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="SetRecords.FromJson(string, dynamic, string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="ARecord.FromJson(string, string, dynamic, string, string, string, ANamespaceAccess)"/>
        /// <seealso cref="ARecord.FromJson(string, string, string, string, string, ANamespaceAccess, bool)"/>
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
                                bool writePKPropertyName = false)
        {
            
            var converter = new CDTConverter();
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

                this.Put(setName,
                            pk,
                            recBins,
                            writePolicy: writePolicy,
                            ttl: ttl);
                cnt++;
            }
            else if (bins is List<object> binList)
            {
                foreach (var item in binList)
                {
                    if (item is Dictionary<string, object> binDict)
                    {
                        var (pk, recBins) = GetRecord(binDict);

                        this.Put(setName,
                                    pk,
                                    recBins,
                                    writePolicy: writePolicy,
                                    ttl: ttl);
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
        /// <seealso cref="ARecord.FromJson(string, string, string, string, string, ANamespaceAccess, bool)"/>
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
            if (this.AerospikeConnection.CXInfo.IsProduction)
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
            var set = this.Sets.FirstOrDefault(s => s.SetName == setName);
            if (set != null)
            {
                set.Truncate(infoPolicy, before);
                return true;
            }

            return false;
        }

        protected object ToDump()
        {
            return LPU.ToExpando(this, include: "Namespace, BinNames, AerospikeConnection, DefaultQueryPolicy, DefaultWritePolicy");            
        }
    }
}
