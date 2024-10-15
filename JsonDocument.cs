using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public JsonDocument(IDictionary<object, object> dict)
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

        public IDictionary<string, object> ToDictionary() => CDTConverter.ConvertToDictionary(this);
         
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
        private long _oid = 0;
		bool _emptyStrIsNull = true;

		public CDTConverter(bool treatEmptyStrAsNull = true)
        {
            this._emptyStrIsNull = treatEmptyStrAsNull;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var t = JToken.FromObject(value, serializer);

            t.WriteTo(writer);
        }

        private object ReadObject(string propertyName, JToken jToken, JsonSerializer serializer)
        {
            if (propertyName[0] == '$')
            {
                if (propertyName.ToLower() == "$type")
                {
                    var json = jToken.ToString();

                    var jSerial = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All
                    };

                    return JsonConvert.DeserializeObject<object>(json, jSerial);
                }

                return ReadJsonType(propertyName, jToken, null, serializer);
            }
            
            return jToken.Value<object>();
        }

        private object ReadObject(JObject jObject, Dictionary<string, object> existingValue, JsonSerializer serializer)
        {
            bool onlyJsonType = false;

            if (jObject.First is JProperty jProp
                    && jProp.Name.ToLower() == "$type")
            {
                var newobject = ReadObject(jProp.Name, jObject, serializer);
                existingValue.Add(jProp.Name,
                                    newobject);
                return newobject;
            }

            foreach (var kvp in jObject)
            {
                if (kvp.Key[0] == '$')
                {
                    existingValue.Add(kvp.Key,
                                        ReadObject(kvp.Key, kvp.Value, serializer));
                    onlyJsonType = true;
                }
                else if (kvp.Value is JObject subObject)
                {
                    var newObject = new Dictionary<string, object>();
                    existingValue.Add(kvp.Key,
                                        this.ReadObject(subObject, newObject, serializer));

                }
                else if (kvp.Value is JArray jArray)
                {
                    var newList = new List<object>();
                    this.ReadObject(jArray, newList, serializer);
                    existingValue.Add(kvp.Key, newList);
                }
				else if(this._emptyStrIsNull 
                            && kvp.Value.Type == JTokenType.String
                            && kvp.Value is JValue jStrValue)
				{
					var strValue = jStrValue.Value<string>();
					existingValue.Add(kvp.Key,
                                        strValue == string.Empty
                                            ? null
                                            : strValue);
				}
				else if (kvp.Value is JValue jValue)
                {
                    existingValue.Add(kvp.Key, jValue.Value);
                }
                else
                {
                    existingValue.Add(kvp.Key, kvp.Value.Value<object>());
                }
            }

            return onlyJsonType && existingValue.Count == 1 ? existingValue.Values.First() : existingValue;
        }

        private void ReadObject(JArray jToken, List<object> existingValue, JsonSerializer serializer)
        {
            foreach (var element in jToken)
            {
                if (element is JObject subObject)
                {
                    var newObject = new Dictionary<string, object>();
                    existingValue.Add(this.ReadObject(subObject, newObject, serializer));
                }
                else if (element is JArray jArray)
                {
                    var newList = new List<object>();
                    this.ReadObject(jArray, newList, serializer);
                    existingValue.Add(newList);
                }
				else if(this._emptyStrIsNull
							&& element.Type == JTokenType.String
							&& element is JValue jStrValue)
				{
					var strValue = jStrValue.Value<string>();
					existingValue.Add(strValue == string.Empty
											? null
											: strValue);
				}
				else if (element is JValue jValue)
                {
                    existingValue.Add(jValue.Value);
                }
                else
                {
                    existingValue.Add(element.Value<object>());
                }
            }
        }

        private void ReadObject(JProperty jToken, Dictionary<string, object> existingValue, JsonSerializer serializer)
        {
            foreach (var element in jToken.Value)
            {
                if (element is JObject subObject)
                {
                    var newObject = new Dictionary<string, object>();
                    existingValue.Add(jToken.Name, this.ReadObject(subObject, newObject, serializer));
                }
                else if (element is JArray jArray)
                {
                    var newList = new List<object>();
                    this.ReadObject(jArray, newList, serializer);
                    existingValue.Add(jToken.Name, newList);
                }
				else if(this._emptyStrIsNull
							&& element.Type == JTokenType.String
							&& element is JValue jStrValue)
				{
					var strValue = jStrValue.Value<string>();
					existingValue.Add(jToken.Name,
										strValue == string.Empty
											? null
											: strValue);
				}
				else if (element is JValue jValue)
                {
                    existingValue.Add(jToken.Name, jValue.Value);
                }
                else
                {
                    existingValue.Add(jToken.Name, element.Value<object>());
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
        ///     &quot;$oid&quot; or &quot;$id&quot;,
        ///         If the Json string value equals 40 in length it will be treated as a digest and converted into a byte array.
        ///         Example:
        ///             &quot; _id&quot;: { &quot;$oid&quot;:&quot;0080a245fabe57999707dc41ced60edc4ac7ac40&quot; } ==&gt; &quot;_id&quot;:[00 80 A2 45 FA BE 57 99 97 07 DC 41 CE D6 0E DC 4A C7 AC 40]
        ///         This type can also take an optional keyword as a value. They are:
        ///             $guid or $uuid -- If provided, a new guid/uuid is generate as a unique value used
        ///             $numeric -- a sequential number starting at 1 will be used
        ///         Example:
        ///             &quot; _id&quot;: { &quot;$oid&quot;: &quot;$uuid&quot; } ==&gt; Generates a new uuid as the _id value
        ///     &quot;$numberint64&quot; or &quot;$numberlong&quot; or &quot;$long&quot;,
        ///     &quot;$numberint32&quot;, or &quot;$numberint&quot; or &quot;$int&quot;,
        ///     &quot;$numberdecimal&quot; or &quot;$decimal&quot;,
        ///     &quot;$numberdouble&quot; or &quot;$double&quot; or &quot;$number&quot; or &quot;$numeric&quot;,
        ///     &quot;$numberfloat&quot; or &quot;$single&quot; or &quot;$float&quot;,
        ///     &quot;$numberint16&quot; or &quot;$numbershort&quot; or &quot;$short&quot;,
        ///     &quot;$numberuint32&quot; or &quot;$numberuint&quot; or &quot;$uint&quot;,
        ///     &quot;$numberuint64&quot; or &quot;$numberulong&quot; or &quot;$ulong&quot;,
        ///     &quot;$numberuint16&quot; or &quot;$numberushort&quot; or &quot;$ushort&quot;,
        ///     &quot;$bool&quot; or &quot;$boolean&quot;
        /// </code>
        /// </summary>
        /// <param name="propType"></param>
        /// <param name="jToken"></param>
        /// <param name="_"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        private object ReadJsonType(string propType, JToken jToken, object _, JsonSerializer serializer)
        {           
            var tToken = jToken.Type;
            object vToken;

            if (jToken is JObject jObject)
            {
                var newObject = new Dictionary<string, object>();
                vToken = this.ReadObject(jObject, newObject, serializer);
            }
            else if (jToken is JValue jValue)
            {
                vToken = jValue.Value;
            }
            else
                vToken = jToken.Value<object>();

            switch (propType.ToLower())
            {
                case "$string":
                    return vToken?.ToString() ?? string.Empty;
                case "$date":
                case "$datetime":
                case "$isodate":
                case "$isodatetime":
                    {
                        if (vToken is DateTime tDateTime)
                            return tDateTime;
                        if (vToken is long lDateTime)
                            return DateTimeOffset.FromUnixTimeMilliseconds(lDateTime).DateTime;
                        if (vToken is int iDateTime)
                            return DateTimeOffset.FromUnixTimeMilliseconds(iDateTime).DateTime;

                        if (tToken == JTokenType.Integer)
                        {
                            return DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(vToken)).DateTime;
                        }

                        return Aerospike.Database.LINQPadDriver.Helpers.CastToNativeType(propType, typeof(DateTime), propType, vToken);
                    }
                case "$datetimeoffset":
                case "$isodatetimeoffset":
                    {
                        if (vToken is DateTimeOffset tDateTime)
                            return tDateTime;
                        if (vToken is long lDateTime)
                            return DateTimeOffset.FromUnixTimeMilliseconds(lDateTime);
                        if (vToken is int iDateTime)
                            return DateTimeOffset.FromUnixTimeMilliseconds(iDateTime).DateTime;

                        if (tToken == JTokenType.Integer)
                        {
                            return DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(vToken));
                        }

                        return Aerospike.Database.LINQPadDriver.Helpers.CastToNativeType(propType, typeof(DateTimeOffset), propType, vToken);
                    }
                case "$timespan":
                case "$time":
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
                case "$id":
                    {
                        if (vToken is string oid)
                        {
                            if (oid.Length == 40)
                                return Aerospike.Database.LINQPadDriver.Helpers.StringToByteArray(oid);

                            switch (oid.ToLower())
                            {
                                case "$guid":
                                case "$uuid":
                                    return Guid.NewGuid().ToString();
                                case "$number":
                                case "$numeric":
                                    return System.Threading.Interlocked.Increment(ref this._oid);
                                default:
                                    break;
                            }
                        }
                        return vToken;
                    }
                case "$numberint64":
                case "$numberlong":
                case "$long":
                    return Convert.ToInt64(vToken);
                case "$numberint32":
                    return Convert.ToInt32(vToken);
                case "$numberint":
                case "$int":
                    return Convert.ToInt32(vToken);
                case "$numberdecimal":
                case "$decimal":                    
                    return Convert.ToDecimal(vToken);
                case "$numberdouble":
                case "$double":
                case "$number":
                case "$numeric":
                    return Convert.ToDouble(vToken);
                case "$numberfloat":
                case "$float":
                case "$single":
                    return Convert.ToSingle(vToken);
                case "$numberint16":
                case "$numbershort":
                case "$short":
                    return Convert.ToInt16(vToken);
                case "$numberuint32":
                case "$numberuint":
                case "$uint":
                    return Convert.ToUInt32(vToken);
                case "$numberuint64":
                case "$numberulong":
                case "$ulong":
                    return Convert.ToUInt64(vToken);
                case "$numberuint16":
                case "$numberushort":
                case "$ushort":
                    return Convert.ToUInt16(vToken);
                case "$bool":
                case "$boolean":
                    return Convert.ToBoolean(vToken);
                default:                    
                    return vToken;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {

            var jToken = JToken.ReadFrom(reader);

            if (jToken is JArray jArray)
            {
                var arrayToken = new List<object>();
                this.ReadObject(jArray, arrayToken, serializer);
                return arrayToken;
            }
            else if (jToken is JObject jObject)
            {
                var objectToken = new Dictionary<string, object>();
                this.ReadObject(jObject, objectToken, serializer);

                if(objectToken.Count == 1 && objectToken.First().Key.ToLower() == "$type")
                {
                    return Helpers.TransForm(objectToken.First().Value);
                }

                return objectToken;
            }
            else
                throw new InvalidOperationException($"Must be either a JObect or JArray Json Token, not {reader.TokenType} ({reader.TokenType.GetType()})");
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        static public IDictionary<string,object> ConvertToDictionary(JObject jObject)
        {
            var converter = new CDTConverter();
            var newDict = new Dictionary<string,object>();

            converter.ReadObject(jObject, newDict, null);

            return newDict;
        }

        static public IDictionary<string, object> ConvertToDictionary(JProperty jProperty)
        {
            var converter = new CDTConverter();
            var newDict = new Dictionary<string, object>();

            converter.ReadObject(jProperty, newDict, null);

            return newDict;
        }

        static public IList<object> ConvertToList(JArray jObject)
        {
            var converter = new CDTConverter();
            var newList = new List<object>();

            converter.ReadObject(jObject, newList, null);

            return newList;
        }
    }
}