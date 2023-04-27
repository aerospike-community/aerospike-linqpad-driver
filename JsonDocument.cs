using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Controls;

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

        public JsonDocument(Newtonsoft.Json.Linq.JObject jObject)
            : base(jObject)
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

    /// <summary>
    /// Converts Json string into a List&lt;object&gt; or Directory&lt;string,object&gt;. 
    /// The Json string can consist of in-line Json typing. 
    ///  Example:
    ///  <code>
    ///  &quot;_id&quot;: {&quot;$oid&quot;: &quot;578f6fa2df35c7fbdbaed8c6&quot;}
    ///  &quot;bucket_start_date&quot;: {&quot;$date&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}
    ///  </code>
    /// </summary>
    public class CDTConverter : JsonConverter
    {

        public CDTConverter()
        { }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var t = JToken.FromObject(value, serializer);

            t.WriteTo(writer);
        }

        private object ReadObject(string propertyName, JsonReader reader, Dictionary<string, object> existingValue, JsonSerializer serializer)
        {
            string currentPropName = propertyName;
            bool onlyJsonType = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var possiblePropName = (string)reader.Value;

                    if (possiblePropName[0] == '$')
                    {
                        existingValue.Add(currentPropName ?? possiblePropName,
                                            this.ReadJsonType(possiblePropName, reader, existingValue, serializer));
                        onlyJsonType = true;
                        continue;
                    }

                    currentPropName = possiblePropName;
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    var newObject = new Dictionary<string, object>();
                    existingValue.Add(currentPropName ?? $"Property{existingValue.Count}",
                                        this.ReadObject(currentPropName, reader, newObject, serializer));
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    var newList = new List<object>();
                    this.ReadObject(reader, newList, serializer);
                    existingValue.Add(currentPropName, newList);
                }
                else
                {
                    existingValue.Add(currentPropName, reader.Value);
                }
            }

            return onlyJsonType && existingValue.Count == 1 ? existingValue.Values.First() : existingValue;
        }

        private void ReadObject(JsonReader reader, List<object> existingValue, JsonSerializer serializer)
        {
            string currentPropName = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                {
                    break;
                }

                if (reader.TokenType == JsonToken.StartObject)
                {
                    var newObject = new Dictionary<string, object>();
                    existingValue.Add(this.ReadObject(currentPropName, reader, newObject, serializer));
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    var newList = new List<object>();
                    this.ReadObject(reader, newList, serializer);
                    existingValue.Add(newList);
                }
                else
                {
                    existingValue.Add(reader.Value);
                }
            }
        }

        /// <summary>
        /// Determines the in-line Json Type and converts the Json value to that C# type
        /// <code>
        ///     &quot;$date&quot; or &quot;$datetime&quot;,
        ///         This can include an optional sub Json Type.Example:
        ///             &quot; bucket_start_date&quot;: {&quot;$date&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}
        ///     &quot;$datetimeoffset&quot;,
        ///         This can include an optional sub Json Type. Example:
        ///             &quot; bucket_start_date &quot;: { &quot;$datetimeoffset&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot; } },
        ///     &quot;$timespan&quot;,
        ///         This can include an optional sub Json Type. Example:
        ///             &quot; bucket_start_time &quot;: { &quot;$timespan&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot; } },
        ///     &quot;$timestamp&quot;,
        ///     &quot;$guid&quot; or &quot;$uuid&quot;,
        ///     &quot;$oid&quot;,
        ///         If the Json string value equals 40 in length it will be treated as a digest and converted into a byte array.
        ///         Example:
        ///             &quot; _id&quot;: { &quot;$oid&quot;:&quot;0080a245fabe57999707dc41ced60edc4ac7ac40&quot; } == &gt; &quot;_id&quot;:[00 80 A2 45 FA BE 57 99 97 07 DC 41 CE D6 0E DC 4A C7 AC 40]
        ///     &quot;$numberint64&quot; or &quot;$numberlong&quot;,
        ///     &quot;$numberint32&quot;, or &quot;$numberint&quot;,
        ///     &quot;$numberdecimal&quot;,
        ///     &quot;$numberdouble&quot;,
        ///     &quot;$numberfloat&quot; or &quot;$single&quot;,
        ///     &quot;$numberint16&quot; or &quot;$numbershort&quot;,
        ///     &quot;$numberuint32&quot; or &quot;$numberuint&quot;,,
        ///     &quot;$numberuint64&quot; or &quot;$numberulong&quot;,,
        ///     &quot;$numberuint16&quot; or &quot;$numberushort&quot;,,
        ///     &quot;$bool&quot; or &quot;$boolean&quot;
        /// </code>
        /// </summary>
        /// <param name="propType"></param>
        /// <param name="reader"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        private object ReadJsonType(string propType, JsonReader reader, object existingValue, JsonSerializer serializer)
        {
            if (reader.Read())
            {
                var tToken = reader.TokenType;
                object vToken;

                if (tToken == JsonToken.StartObject)
                {
                    var newObject = new Dictionary<string, object>();
                    vToken = this.ReadObject(propType, reader, newObject, serializer);
                }
                else
                    vToken = reader.Value;

                switch (propType.ToLower())
                {
                    case "$string":
                        return vToken?.ToString() ?? string.Empty;
                    case "$date":
                    case "$datetime":
                        {
                            if (vToken is DateTime tDateTime)
                                return tDateTime;
                            if (vToken is long lDateTime)
                                return DateTimeOffset.FromUnixTimeMilliseconds(lDateTime).DateTime;

                            if (tToken == JsonToken.Integer)
                            {
                                return DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(vToken)).DateTime;
                            }

                            return Aerospike.Database.LINQPadDriver.Helpers.CastToNativeType(propType, typeof(DateTime), propType, vToken);
                        }
                    case "$datetimeoffset":
                        {
                            if (vToken is DateTimeOffset tDateTime)
                                return tDateTime;
                            if (vToken is long lDateTime)
                                return DateTimeOffset.FromUnixTimeMilliseconds(lDateTime);

                            if (tToken == JsonToken.Integer)
                            {
                                return DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(vToken));
                            }

                            return Aerospike.Database.LINQPadDriver.Helpers.CastToNativeType(propType, typeof(DateTimeOffset), propType, vToken);
                        }
                    case "$timespan":
                        return Aerospike.Database.LINQPadDriver.Helpers.CastToNativeType(propType, typeof(TimeSpan), propType, vToken);
                    case "$timestamp":
                        return vToken;
                    case "$guid":
                    case "$uuid":
                        {
                            if (vToken is Guid guid)
                                return guid;
                            if (vToken is string sGuid)
                                return Guid.Parse(sGuid);
                            return vToken;
                        }
                    case "$oid":
                        {
                            if (vToken is string oid && oid.Length == 40)
                            {
                                return Aerospike.Database.LINQPadDriver.Helpers.StringToByteArray(oid);
                            }
                            return vToken;
                        }
                    case "$numberint64":
                    case "$numberlong":
                        return Convert.ToInt64(vToken);
                    case "$numberint32":
                        return Convert.ToInt32(vToken);
                    case "$numberint":
                        return Convert.ToInt32(vToken);
                    case "$numberdecimal":
                        return Convert.ToDecimal(vToken);
                    case "$numberdouble":
                        return Convert.ToDouble(vToken);
                    case "$numberfloat":
                    case "$single":
                        return Convert.ToSingle(vToken);
                    case "$numberint16":
                    case "$numbershort":
                        return Convert.ToInt16(vToken);
                    case "$numberuint32":
                    case "$numberuint":
                        return Convert.ToUInt32(vToken);
                    case "$numberuint64":
                    case "$numberulong":
                        return Convert.ToUInt64(vToken);
                    case "$numberuint16":
                    case "$numberushort":
                        return Convert.ToUInt16(vToken);
                    case "$bool":
                    case "$boolean":
                        return Convert.ToBoolean(vToken);
                    default:
                        return vToken;
                }
            }

            return null;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {

            if (reader.TokenType == JsonToken.StartArray)
            {
                var arrayToken = new List<object>();
                this.ReadObject(reader, arrayToken, serializer);
                return arrayToken;
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                var objectToken = new Dictionary<string, object>();
                this.ReadObject(null, reader, objectToken, serializer);
                return objectToken;
            }
            else
                throw new InvalidOperationException($"Must be either a StartArray or StartObject Json Token, not {reader.TokenType}");
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}
