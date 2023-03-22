using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Aerospike.Client;
using LINQPad;
using Newtonsoft.Json;
using LPU = LINQPad.Util;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
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

        public ANamespaceAccess(ANamespaceAccess clone, Expression expression)
        {
            this.Namespace = clone.Namespace;
            this.BinNames = clone.BinNames;
            this.AerospikeConnection = clone.AerospikeConnection;

            this.DefaultWritePolicy = clone.DefaultWritePolicy;
            this.DefaultQueryPolicy = new QueryPolicy(clone.DefaultQueryPolicy)
            {
                filterExp = expression
            };
            this.DefaultReadPolicy = new QueryPolicy(this.DefaultQueryPolicy);
        }

        private void NamespaceRefresh(string setName, bool forceRefresh)
        {
            this._sets = Enumerable.Empty<SetRecords>();

            Helpers.CheckForNewSetNameRefresh(this.Namespace, setName, forceRefresh);
        }

        private IEnumerable<SetRecords> _sets = Enumerable.Empty<SetRecords>();

        /// <summary>
        /// Returns the associated set instances for this namespace.
        /// </summary>
        /// <remarks>
        /// The drag and drop set instances from the connection pane in LinqPad are different instances as defined here...
        /// </remarks>
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

                return this._sets = setInstances.ToArray();
            }
        }

        /// <summary>
        /// Returns the set name from <paramref name="setName"/>
        /// </summary>
        /// <param name="setName">The name of the Aerospike set</param>
        /// <returns>The set instance or null</returns>
        public SetRecords this[string setName]
        {
            get => this.Sets.FirstOrDefault(s => s.SetName == setName);
        }

        /// <summary>
        /// Returns the Aerospike Null Set.
        /// </summary>
        public SetRecords NullSet { get => this[ASet.NullSetName]; }

        #region Aerospike API items

        public string Namespace { get; }
        //public string Name { get; }

        public string[] BinNames { get; }

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
        /// Gets all records in a namespace and/or set
        /// </summary>
        /// <param name="nsName">namespace</param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="bins">bins you wish to get. If not provided all bins for a record</param>
        /// <returns></returns>
        public ARecord[] GetRecords([NotNull] string nsName, string setName, params string[] bins)
        {            
            var recordSets = new List<ARecord>();

            using (var recordset = this.AerospikeConnection
                                    .AerospikeClient
                                    .Query(this.DefaultQueryPolicy,
                                            string.IsNullOrEmpty(setName) || setName == ASet.NullSetName
                                                ? new Statement() { Namespace = nsName}
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

        public ARecord Get(Client.Key partitionKey, params string[] bins)
        {
            var record = this.AerospikeConnection
                                .AerospikeClient
                                .Get(this.DefaultReadPolicy, partitionKey, bins);

            return new ARecord(this,
                                partitionKey,
                                record,
                                bins,
                                dumpType: this.AerospikeConnection.RecordView);
        }

        /// <summary>
        /// Puts (Writes) a DB record based on the provided record including Expiration.
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used.
        /// </summary>        
        /// <param name="record">
        /// A <see cref="ARecord"/> object used to add or update the associated record.
        /// </param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>
        public void Put([NotNull] ARecord record,
                            string setName = null,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                             bool refreshOnNewSet = true)
        {
            this.Put(record.Aerospike.Key, record.Aerospike.GetValues(), setName, writePolicy, ttl, refreshOnNewSet);
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
        /// Note the values cannot be a <see cref="Bin"/> or <see cref="Value"/> object.
        /// </param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>
        public void Put<V>([NotNull] dynamic primaryKey,
                            [NotNull] IDictionary<string, V> binValues,
                            string setName = null,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                            bool refreshOnNewSet = true)
        {
            var writePolicyPut = writePolicy ?? this.DefaultWritePolicy;

            if (ttl.HasValue)
            {
                writePolicyPut = new WritePolicy(writePolicyPut) { expiration = SetRecords.DetermineExpiration(ttl.Value) };
            }

            this.AerospikeConnection
                .AerospikeClient.Put(writePolicyPut,
                                        Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName),
                                        Helpers.CreateBinRecord(binValues));

            if(refreshOnNewSet)
                this.NamespaceRefresh(setName, false);
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
        /// Note the values cannot be a <see cref="Bin"/> or <see cref="Value"/> object.
        /// </param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="writePolicy">
        /// The write policy. If not provided , the default policy is used.
        /// <seealso cref="WritePolicy"/>
        /// </param>
        /// <param name="ttl">Time-to-live of the record</param>
        public void Put<V>([NotNull] dynamic primaryKey,
                            [NotNull] string bin,
                            [NotNull] V binValue,
                            string setName = null,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                             bool refreshOnNewSet = true)
        {
            var writePolicyPut = writePolicy ?? this.DefaultWritePolicy;

            if (ttl.HasValue)
            {
                writePolicyPut = new WritePolicy(writePolicyPut) { expiration = SetRecords.DetermineExpiration(ttl.Value) };
            }

            this.AerospikeConnection
                .AerospikeClient.Put(writePolicyPut,
                                        Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName),
                                        Helpers.CreateBinRecord(binValue, bin));

            if(refreshOnNewSet)
                this.NamespaceRefresh(setName, false);
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
        public void Put<T>([NotNull] dynamic primaryKey,
                            [NotNull] string bin,
                            [NotNull] IList<T> listValue,
                            string setName = null,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                            bool refreshOnNewSet = true)
        {
            var writePolicyPut = writePolicy ?? this.DefaultWritePolicy;

            if (ttl.HasValue)
            {
                writePolicyPut = new WritePolicy(writePolicyPut) { expiration = SetRecords.DetermineExpiration(ttl.Value) };
            }

            this.AerospikeConnection
                .AerospikeClient.Put(writePolicyPut,
                                        Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName),
                                        Helpers.CreateBinRecord(listValue, bin));

            if (refreshOnNewSet)
                this.NamespaceRefresh(setName, false);
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
        public void Put<T>([NotNull] dynamic primaryKey,
                            [NotNull] string bin,
                            [NotNull] IEnumerable<T> collectionValue,
                            string setName = null,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                            bool refreshOnNewSet = true)
        {
            var writePolicyPut = writePolicy ?? this.DefaultWritePolicy;

            if (ttl.HasValue)
            {
                writePolicyPut = new WritePolicy(writePolicyPut) { expiration = SetRecords.DetermineExpiration(ttl.Value) };
            }
            
            this.AerospikeConnection
                .AerospikeClient.Put(writePolicyPut,
                                        Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName),
                                        Helpers.CreateBinRecord(collectionValue, bin));

            if (refreshOnNewSet)
                this.NamespaceRefresh(setName, false);
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
        public void Put([NotNull] dynamic primaryKey,
                            [NotNull] IEnumerable<Bin> binsToWrite,
                            string setName = null,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                            bool refreshOnNewSet = true)
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

            if(refreshOnNewSet)
                this.NamespaceRefresh(setName, false);
        }

        /// <summary>
        /// Writes the instance where each field/property is a bin name and the associated value the bin's value.
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="primaryKey"></param>
        /// <param name="instance"></param>
        /// <param name="setName">Set name or null for the null set</param>
        /// <param name="transform">
        /// A action that is called to perform customized transformation. 
        /// First argument -- the name of the kvPair
        /// Second argument -- the name of the bin (can be different from kvPair if JsonPropertyNameAttribute is defined)
        /// Third argument -- the instance being transformed
        /// Fourth argument -- if true the instance is within another object.
        /// Returns the new transformed object or null to indicate that this kvPair should be skipped.
        /// </param>
        /// <param name="doctumentBinName">
        /// If provided the record is created as a document and this will be the name of the bin. 
        /// </param>
        /// <param name="writePolicy"></param>
        /// <param name="ttl"></param>
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/>
        /// <seealso cref="Aerospike.Client.BinIgnoreAttribute"/>
        public void WriteObject<T>([NotNull] dynamic primaryKey,
                                    [NotNull] T instance,
                                    string setName = null,
                                    Func<string, string, object, bool, object> transform = null,
                                    string doctumentBinName = null,
                                    WritePolicy writePolicy = null,
                                    TimeSpan? ttl = null,
                                     bool refreshOnNewSet = true)
        {
           
            var writePolicyPut = writePolicy ?? this.DefaultWritePolicy;

            if (ttl.HasValue)
            {
                writePolicyPut = new WritePolicy(writePolicyPut) { expiration = SetRecords.DetermineExpiration(ttl.Value) };
            }

            var key = Helpers.DetermineAerospikeKey(primaryKey, this.Namespace, setName);

            var dictItem = instance is ARecord asRecord2
                                ? asRecord2.Aerospike.GetValues()
                                : Helpers.TransForm(instance, transform);

            if(string.IsNullOrEmpty(doctumentBinName))
            {
                var bins = Helpers.CreateBinRecord(dictItem);

                this.AerospikeConnection
                    .AerospikeClient.Put(writePolicyPut,
                                            key,
                                            bins);
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
            }

            if (refreshOnNewSet)
                this.NamespaceRefresh(setName, false);
        }

        #endregion

        #region Import/Export
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
        /// <seealso cref="Import(string, WritePolicy, TimeSpan?, bool, bool)"/>
        /// <seealso cref="SetRecords.Export(string, Exp, bool)"/>
        /// <seealso cref="SetRecords.Import(string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="AClusterAccess.Import(string, string, string, WritePolicy)"/>
        /// <exception cref="InvalidOperationException">Thrown if the cluster is a production cluster. Can disable this by going into the connection properties.</exception>
        public int Import([NotNull] string importJSONFile,
                            string setName,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                            bool useImportRecTTL = false,
                            bool refreshOnNewSet = true)
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
                this.Put(item.KeyValue ?? item.Digest,
                            item.Values,
                            setName,
                            writePolicy,
                            useImportRecTTL
                                ? ARecord.AerospikeAPI.CalcTTLTimeSpan(item.TimeToLive)
                                : ttl,
                            false);
            }

            if (refreshOnNewSet)
                this.NamespaceRefresh(setName, false);

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
        /// <seealso cref="Import(string, string, WritePolicy, TimeSpan?, bool, bool)"/>
        /// <seealso cref="SetRecords.Export(string, Exp, bool)"/>
        /// <seealso cref="AClusterAccess.Import(string, string, string, WritePolicy)"/>
        public int Import([NotNull] string importJSONFile,
                            WritePolicy writePolicy = null,
                            TimeSpan? ttl = null,
                            bool useImportRecTTL = false,
                            bool refreshOnNewSet = true)
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
                this.Put(item.KeyValue ?? item.Digest,
                            item.Values,
                            item.SetName,
                            writePolicy,
                            useImportRecTTL
                                ? ARecord.AerospikeAPI.CalcTTLTimeSpan(item.TimeToLive)
                                : ttl,
                            false);
            }

            if (refreshOnNewSet)
                this.NamespaceRefresh(null, true);

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
        /// <seealso cref="ANamespaceAccess.Import(string, string, WritePolicy, TimeSpan?, bool, bool)"/>
        /// <seealso cref="ANamespaceAccess.Import(string, WritePolicy, TimeSpan?, bool, bool)"/>
        /// <seealso cref="ARecord.ToJson(bool, JsonSerializerSettings)"/>
        public int Export([NotNull] string exportJSONFile, Client.Exp filterExpression = null, bool indented = true)
        {
            if (Directory.Exists(exportJSONFile))
            {
                exportJSONFile = Path.Combine(exportJSONFile, $"{this.Namespace}.json");
            }

            return NullSet.Export(exportJSONFile, filterExpression, indented);
        }

        #endregion

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
        /// <seealso cref="SetRecords.Truncate(InfoPolicy, DateTime?)"/>
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

        protected object ToDump()
        {
            return LPU.ToExpando(this, include: "Namespace, BinNames, AerospikeConnection, DefaultQueryPolicy, DefaultWritePolicy");            
        }
    }
}
