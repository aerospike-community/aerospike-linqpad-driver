using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Aerospike.Client;
using Newtonsoft.Json;
using LPU = LINQPad.Util;
using System.Linq;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    /// <summary>
    /// This class is used to represent an Aerospike Cluster 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{ConnectionString}")]
    public class AClusterAccess
    {
        protected AClusterAccess(System.Data.IDbConnection dbConnection)
        {
            this.AerospikeConnection = dbConnection as AerospikeConnection;
            _instance = this;
        }

        public AerospikeConnection AerospikeConnection { get; }

        private static AClusterAccess _instance = null;

        public static AClusterAccess Instance { get => _instance; }

        /// <summary>
        /// A connection string
        /// </summary>
        public string ConnectionString { get => this.AerospikeConnection.ConnectionString; }

        /// <summary>
        /// Returns the seed nodes used to establish the connection.
        /// </summary>
        public Host[] SeedHosts { get => this.AerospikeConnection.SeedHosts; }

        /// <summary>
        /// All the nodes within the cluster
        /// </summary>
        public Node[] Nodes { get => this.AerospikeConnection.Nodes; }
                
        /// <summary>
        /// The actual Aerospike client connection 
        /// </summary>
        public AerospikeClient AerospikeClient { get => this.AerospikeConnection.AerospikeClient; }

        /// <summary>
        /// The name of the cluster.
        /// </summary>
        public string ClusterName { get => this.AerospikeConnection.Database; }

        virtual protected object ToDump()
        {
            return LPU.ToExpando(this, include: "ClusterName,ConnectionString,SeedHosts,Nodes,Namespaces,UDFModules,AerospikeClient");
        }

        /// <summary>
        /// Imports a <see href="SetRecords.Export(string, Exp)"/> generated JSON file. 
        /// </summary>
        /// <param name="nameSpace">The name of the namespace</param>
        /// <param name="setName">The set name</param>
        /// <param name="importJSONFile">The JSON file where the JSON will be written</param>
        /// <param name="writePolicy">The write policy</param>
        /// <returns>The number of records imported</returns>
        /// <seealso cref="ANamespaceAccess.Import(string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="ANamespaceAccess.Import(string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="SetRecords.Export(string, Exp, bool)"/>        
        public int Import([NotNull] string nameSpace,
                            [NotNull] string setName,
                            [NotNull] string importJSONFile,
                            WritePolicy writePolicy = null)
        {
            var jsonStr = System.IO.File.ReadAllText(importJSONFile);
            var jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            var jsonStructs = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonExportStructure[]>(jsonStr, jsonSettings);

            foreach (var item in jsonStructs)
            {
                var key = item.KeyValue == null
                            ? new Client.Key(nameSpace, item.Digest, setName, Value.AsNull)
                            : new Client.Key(nameSpace, setName, Value.Get(item.KeyValue));

                this.AerospikeClient.Put(writePolicy,
                                            key,
                                            item.Values.Select(v => new Client.Bin(v.Key, v.Value)).ToArray());
            }

            return jsonStructs.Length;
        }

        /// <summary>
        /// Imports a <see href="SetRecords.Export(string, Exp)"/> generated JSON file into the original namespace and set. 
        /// </summary>
        /// <param name="importJSONFile">The JSON file where the JSON will be written</param>
        /// <param name="writePolicy">The write policy</param>
        /// <returns>The number of records imported</returns>
        /// <seealso cref="ANamespaceAccess.Import(string, string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="ANamespaceAccess.Import(string, WritePolicy, TimeSpan?, bool)"/>
        /// <seealso cref="SetRecords.Export(string, Exp, bool)"/>        
        public int Import([NotNull] string importJSONFile,
                            WritePolicy writePolicy = null)
        {
            var jsonStr = System.IO.File.ReadAllText(importJSONFile);
            var jsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };
            var jsonStructs = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonExportStructure[]>(jsonStr, jsonSettings);

            foreach (var item in jsonStructs)
            {
                var key = item.KeyValue == null
                            ? new Client.Key(item.NameSpace, item.Digest, item.SetName, Value.AsNull)
                            : new Client.Key(item.NameSpace, item.SetName, Value.Get(item.KeyValue));

                this.AerospikeClient.Put(writePolicy,
                                            key,
                                            item.Values.Select(v => new Client.Bin(v.Key, v.Value)).ToArray());
            }

            return jsonStructs.Length;
        }
    }
}
