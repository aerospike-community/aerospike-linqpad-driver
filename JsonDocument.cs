using System;
using System.Collections.Generic;

namespace Aerospike.Database.LINQPadDriver.Extensions
{

    /// <summary>
    /// The structure used for Exporting records from a set. 
    /// </summary>
    public struct JsonExportStructure
    {
        /// <summary>
        /// The namespace associated with this record
        /// </summary>
        public string NameSpace;
        /// <summary>
        /// The Set associated with this record
        /// </summary>
        public string SetName;
        /// <summary>
        /// The record&apos;s Generation.
        /// <see cref="Aerospike.Client.Record.generation"/>
        /// </summary>
        public int Generation;
        /// <summary>
        /// The record&apos;s Time-to-Live.
        /// <see cref="Aerospike.Client.Record.TimeToLive"/>
        /// <see cref="ARecord.AerospikeAPI.Expiration"/>
        /// </summary>
        public int? TimeToLive;
        /// <summary>
        /// The Record&apos;s Digest (Required)
        /// </summary>
        public byte[] Digest;
        /// <summary>
        /// The primary key of the record (optional)
        /// </summary>
        public object KeyValue;
        /// <summary>
        /// The associated record&apos;s bins (required)
        /// </summary>
        public IDictionary<string, object> Values;
    }

    public class JsonDocument : Newtonsoft.Json.Linq.JObject
    {

        public JsonDocument(IDictionary<object,object> dict)
            : base(Newtonsoft.Json.Linq.JObject.FromObject(dict))
        {
        }

        public JsonDocument(IDictionary<string, object> dict)
            : base(Newtonsoft.Json.Linq.JObject.FromObject(dict))
        {
        }

        /// <summary>
        /// Selects a <see cref="T:Newtonsoft.Json.Linq.JToken" /> using a JSONPath expression. Selects the token that matches the object path.
        /// <seealso cref="Newtonsoft.Json.Linq.JToken.SelectTokens(string)"/>
        /// </summary>
        /// <param name="jsonPath">
        /// A <see cref="T:System.String" /> that contains a JSONPath expression.
        /// </param>
        /// <returns>A <see cref="T:Newtonsoft.Json.Linq.JToken" />, or <c>null</c>.</returns>
        /// <seealso cref="Newtonsoft.Json.Linq.JToken.SelectToken(string, Newtonsoft.Json.Linq.JsonSelectSettings?)"/>
        /// <seealso cref="Newtonsoft.Json.Linq.JToken.SelectTokens(string)"/>
        public Newtonsoft.Json.Linq.JToken JsonPath(string jsonPath)
        {
            return this.SelectToken(jsonPath);
        }

        /// <summary>
        ///  Selects a Newtonsoft.Json.Linq.JToken using a JSONPath expression. Selects the
        ///     token that matches the object path.
        /// <seealso cref="Newtonsoft.Json.Linq.JToken.SelectTokens(string, bool)"/>
        /// </summary>
        /// <param name="jsonPath">A System.String that contains a JSONPath expression.</param>
        /// <param name="errorWhenNoMatch">
        /// A flag to indicate whether an error should be thrown if no tokens are found when
        ///     evaluating part of the expression.
        /// </param>
        /// <returns>
        /// <see cref="Newtonsoft.Json.Linq.JToken"/>
        /// </returns>
        /// <seealso cref="Newtonsoft.Json.Linq.JToken.SelectToken(string, Newtonsoft.Json.Linq.JsonSelectSettings?)"/>
        /// <seealso cref="Newtonsoft.Json.Linq.JToken.SelectTokens(string, bool)"/>
        public Newtonsoft.Json.Linq.JToken JsonPath(string jsonPath, bool errorWhenNoMatch)
        {
            return this.SelectToken(jsonPath, errorWhenNoMatch);
        }
                
        static public explicit operator Dictionary<object, object>(JsonDocument jObject)
                                    => jObject.ToObject<Dictionary<object, object>>();

        static public explicit operator JsonDocument(Dictionary<object, object> dict)
                                    => new JsonDocument(dict);

        static public explicit operator JsonDocument(SortedDictionary<object, object> dict)
                                    => new JsonDocument(dict);

        static public Dictionary<object, object> ToDictionary(Newtonsoft.Json.Linq.JToken jToken)
        {
            return jToken.ToObject<Dictionary<object, object>>();
        }
    }
}
