using System;
using System.Collections.Generic;
using Aerospike.Client;
using System.Data;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Linq;
using LPU = LINQPad.Util;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    public class AUDFAccess
    {

        public AUDFAccess(IDbConnection dbConnection, string moduleName, string udfName, string udfSourceCode)
        {
            this.AerospikeConnection = dbConnection as AerospikeConnection;
            this.DefaultQueryPolicy = new QueryPolicy(this.AerospikeConnection.AerospikeClient.QueryPolicyDefault);
            this.DefaultWritePolicy = new WritePolicy(this.AerospikeConnection.AerospikeClient.WritePolicyDefault);
            this.Name = udfName;

            if (moduleName?.EndsWith(".lua") == true)
                this.Module = moduleName?.Replace(".lua", string.Empty);
            else
                this.Module = moduleName;

            this.SourceCode = udfSourceCode;
            this.Type = Language.LUA;
            this.FullName = $"{this.Module}.{this.Name}";

        }

        public AUDFAccess(AUDFAccess cloneUDF)
        {
            this.AerospikeConnection = cloneUDF.AerospikeConnection;
            this.DefaultQueryPolicy = new QueryPolicy(cloneUDF.DefaultQueryPolicy);
            this.DefaultWritePolicy = new WritePolicy(cloneUDF.DefaultWritePolicy);
            this.Name = cloneUDF.Name;
            this.Module = cloneUDF.Module;
            this.SourceCode = cloneUDF.SourceCode;
            this.Type = cloneUDF.Type;
        }

        public AUDFAccess Clone() => new AUDFAccess(this);

        public AerospikeConnection AerospikeConnection { get; }
        public QueryPolicy DefaultQueryPolicy { get; set; }
        public WritePolicy DefaultWritePolicy { get; set; }
        public string Module { get; }
        public string Name { get; }
        public string FullName { get; }
        public string SourceCode { get; }
        public Client.Language Type { get; }


        public IEnumerable<object> QueryAggregate(Statement statement, Client.Exp filterExpression, params object[] functionArgs)
        {
            /*
             rs = client.QueryAggregate(null, stmt, "aggregationByRegion", "sum");
           */
            var funcValues = new Value[functionArgs.Length];

            for (int idx = 0; idx < functionArgs.Length; ++idx)
            {
                if (functionArgs[idx] is Value vArg)
                    funcValues[idx] = vArg;
                else
                    funcValues[idx] = Value.Get(functionArgs[idx]);
            }

            var queryPolicy = filterExpression == null
                                    ? this.DefaultQueryPolicy
                                    : new QueryPolicy(this.DefaultQueryPolicy) { filterExp = Exp.Build(filterExpression) };

            using var resultSet = this.AerospikeConnection.AerospikeClient.QueryAggregate(queryPolicy,
                                                                                            statement,
                                                                                            this.Module,
                                                                                            this.Name, funcValues);

            while (resultSet.Next())
            {
                yield return resultSet.Object;
            }
        }

        /// <summary>
        /// Executes the UDF based on <paramref name="statement"/> and <paramref name="functionArgs"/>
        /// </summary>
        /// <param name="statement">Aerospike Statement instance</param>
        /// <param name="functionArgs">
        /// The values that are passed to the UDF.
        /// These values can be a <see cref="Client.Value"/> or a C# native type.
        /// </param>
        /// <returns>An Aerospike Result Set from the UDF</returns>
        /// <example>
        /// Calls a UDF that produces a aggregate value in namespace &quot;test&quot; set &quot;users&quot; on &quot;binstweetcount&quot; and &quot;region&quot;
        /// It also uses a index with a filter. 
        /// <code>
        ///         var bins = { &quot;tweetcount&quot;, &quot;region&quot; };
        ///         var stmt = new Statement();
        ///
        ///         stmt.SetNamespace(&quot;test&quot;);
        ///         stmt.SetSetName(&quot;users&quot;);
        ///         stmt.SetIndexName(&quot;tweetcount_index&quot;);
        ///         stmt.SetBinNames(bins);
        ///         stmt.SetFilters(Filter.Range(&quot;tweetcount&quot;, min, max));
        ///
        ///         var result = this.QueryAggregate(stmt);
        /// </code>
        /// </example>
        public IEnumerable<object> QueryAggregate(Statement statement, params object[] functionArgs) => this.QueryAggregate(statement, null, functionArgs);

        public IEnumerable<object> QueryAggregate(SetRecords set, params object[] functionArgs) => this.QueryAggregate(set, null, functionArgs);

        public IEnumerable<object> QueryAggregate(SetRecords set, Client.Exp filterExpression, params object[] functionArgs)
        {
            var statement = new Statement();

            statement.SetNamespace(set.Namespace);
            statement.SetSetName(set.SetName);

            return this.QueryAggregate(statement, filterExpression, functionArgs);
        }

        /// <summary>
        /// Executes the UDF based on <paramref name="key"/> and <paramref name="functionArgs"/>
        /// </summary>
        /// <param name="key">The primary key for the target set</param>
        /// <param name="functionArgs">
        /// The values that are passed to the UDF.
        /// These values can be a <see cref="Client.Value"/> or a C# native type.
        /// </param>
        /// <returns>
        /// The value returned from the UDF or null.
        /// </returns>
        /// <example>
        /// Executing a UDF that will create a new record in namespace and set &quot;MyNS.MySet&quot;
        /// <code>
        ///         Key key = new Key("MyNS", "MySet", "udfkey1");
        ///         BinName bin = new BinName("udfbin1", "string value");
        ///
        ///         this.Execute(Value.Get(bin.name), bin.value);
        ///
        ///         //Check to see if record was actually written
		///	        Record record = client.Get(null, key, bin.name);
        ///         AssertBinEqual(key, record, bin);
        /// </code>
        /// </example>
        public object Execute(Aerospike.Client.Key key, params object[] functionArgs)
        {
            var funcValues = new Value[functionArgs.Length];

            for (int idx = 0; idx < functionArgs.Length; ++idx)
            {
                if (functionArgs[idx] is Value vArg)
                    funcValues[idx] = vArg;
                else
                    funcValues[idx] = Value.Get(functionArgs[idx]);
            }

            return this.AerospikeConnection.AerospikeClient.Execute(this.DefaultWritePolicy, key, this.Module, this.Name, funcValues);
        }

        /// <summary>
        /// Executes the UDF based on <paramref name="primaryKey"/> and <paramref name="functionArgs"/>
        /// </summary>
        /// <param name="set">The Aerospike Set</param>
        /// <param name="primaryKey">
        /// The primary value. This can be a <see cref="Client.Key"/>, a <see cref="Client.Value"/>, or any object value.
        /// </param>
        /// <param name="functionArgs">
        /// Arguments passed to the UDF
        /// </param>
        /// <returns>
        /// The UDF result or null.
        /// </returns>
        public object Execute(SetRecords set, object primaryKey, params object[] functionArgs)
        {
            Client.Key key;

            if (primaryKey is Client.Key valueKey)
            {
                key = new Client.Key(set.Namespace, set.SetName, valueKey.userKey);
            }
            else if (primaryKey is Value value)
                key = new Client.Key(set.Namespace, set.SetName, value);
            else
                key = new Client.Key(set.Namespace, set.SetName, Value.Get(primaryKey));

            return this.Execute(key, functionArgs);
        }

        public object Execute(ARecord asRecord, params object[] functionArgs)
        {
            return this.Execute(asRecord.Aerospike.Key, functionArgs);
        }

        virtual public object ToDump()
        {
            return LPU.ToExpando(this, include: "Module,Name,SourceCode,Type");
        }
    }
}
