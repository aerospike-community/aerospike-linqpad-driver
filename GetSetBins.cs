using Aerospike.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Aerospike.Database.LINQPadDriver.Extensions;
using System.Threading.Tasks;
using static Aerospike.Database.LINQPadDriver.LPSet;

namespace Aerospike.Database.LINQPadDriver
{
    internal sealed class GetSetBins
    {
        
        public GetSetBins(AerospikeClient connection, int timeout, bool compression)
        {
            this.Connection = connection;
            this.QueryPolicy = new QueryPolicy() { socketTimeout = timeout, totalTimeout = 0, compress = compression };
        }

        AerospikeClient Connection { get; }

        /// <summary>
        /// <see href="https://docs.aerospike.com/apidocs/csharp/html/t_aerospike_client_querypolicy"/>
        /// </summary>
        QueryPolicy QueryPolicy { get; }
        
        IEnumerable<Aerospike.Client.Record> GetRecords(string nsName, string setName, int maxRecords)
        {
            this.QueryPolicy.shortQuery = maxRecords <= 100;

            using var recordset = this.Connection
                                   .Query(this.QueryPolicy,
                                           new Statement() { Namespace = nsName, SetName = setName, MaxRecords = maxRecords });

            while (recordset.Next())
            {
                yield return recordset.Record;
            }
        }

        static int IsDoc(object value)
        {
            if (value is IDictionary<object, object> dict)
            {
                if (dict.All(d => d.Key is string))
                    return 1;
            }
            else if (value is IList<object> list)
            {
                return list.Sum(i => IsDoc(i)) == list.Count ? 1 : 0;
            }

            return 0;
        }

        static Type GetdocType(object value, bool determineDocType)
        {
            if (value is null) return typeof(object);

            if (!value.GetType().IsGenericType)
                return value.GetType();

            try
            {
                if (value is IDictionary<object, object> dict)
                {
                    if (dict.All(d => d.Key is string))
                    {
                        if (determineDocType)
                            return typeof(JsonDocument);

                        var valueType = GetdocType(dict.Values.ToList(), determineDocType);

                        return value.GetType()
                                    .GetGenericTypeDefinition()
                                    .MakeGenericType(typeof(string),
                                                        valueType.GetGenericArguments().First());
                    }
                    else
                    {
                        var keyType = GetdocType(dict.Keys.ToList(), determineDocType);
                        var valueType = GetdocType(dict.Values.ToList(), determineDocType);

                        return value.GetType()
                                    .GetGenericTypeDefinition()
                                    .MakeGenericType(keyType.GetGenericArguments().First(),
                                                        valueType.GetGenericArguments().First());
                    }
                }
                else if (value is IList<object> lst)
                {
                    var typeLst = new List<Type>();

                    foreach (var item in lst)
                    {
                        //if (!item.GetType().IsGenericType) return value.GetType();

                        typeLst.Add(item is null ? typeof(object) : GetdocType(item, determineDocType));
                    }

                    var commonType = typeLst.GroupBy(i => i).Select(i => i.Key);

                    if (commonType.Count() == 1)
                    {
                        return value.GetType()
                                    .GetGenericTypeDefinition()
                                    .MakeGenericType(commonType.First());
                    }
                }
            }
            catch { }

            return value.GetType();
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private static (string name, Type type) GetBinDocType(string nsName, string setName, string binName, object binValue, bool determineDocType)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            return (binName, GetdocType(binValue, determineDocType));
        }
        
        public List<LPSet.BinType> Get(string nsName, 
                                        string setName,
                                        bool determineDocType, 
                                        int maxRecords,
                                        int minRecs)
        {
            if (maxRecords > 0)
                try
                {
                    var records = GetRecords(nsName, setName, maxRecords);
                    var nbrRecs = records.Count();

                    if (nbrRecs >= minRecs)
                    {
                        return records
                                .SelectMany(r => r.bins.Select(b => GetBinDocType(nsName, setName, b.Key, b.Value, determineDocType)))
                                    .GroupBy(x => x)
                                    .Select(y => (y.Key.name, y.Key.type, y.Count()))
                                    .GroupBy(y => y.name)
                                    .SelectMany(x => x.Select(i => new LPSet.BinType(i.name, i.type, x.Count() > 1, x.Sum(y => y.Item3) >= nbrRecs))).ToList();
                    }
                }
                catch
                {                    
                }

            return new List<LPSet.BinType>(0);
        }
    }
}
