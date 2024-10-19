using Aerospike.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
	/// <summary>
	/// Helpers that will translate an <see cref="ARecord"/> into Aerospike API Code template
	/// </summary>
	public static  class ToCodeAPI
	{
		[Flags]
		public enum Options
		{
			None = 0,
			GetCode = 0x0001,
			PutCode = 0x0010,
			GetPutCode = GetCode | PutCode
		}

		static string GenerateCode(ARecord record, Options optCode, bool useAerospikeAPI)
		{
			string ConvertValue(AValue aValue, bool useAerospikeAPI = false)
			{
				string strValue = null;

				if(useAerospikeAPI)
				{
					var cvtValue = Helpers.ConvertToAerospikeType(aValue);
					if(cvtValue is null || cvtValue is Value.NullValue)
						return "Value.AsNull";

					if(cvtValue is Value cValue)
					{
						var dtStr = Helpers.GetRealTypeName(cvtValue.GetType());
						strValue = ConvertValue(cValue.Object.ToAValue());
						return $"new {dtStr}({strValue})";
					}
					strValue = ConvertValue(cvtValue.ToAValue());
					return $"Value.Get({strValue})";
				}

				if(aValue is null)
					return "null";
				if(aValue.IsString)
					strValue = Helpers.ToLiteral(aValue.Convert<string>());
				else if(aValue.IsBool)
					strValue = aValue.Value.ToString();
				else if(aValue.IsNumeric)
				{
					switch(aValue.Value)
					{
						case long lValue:
							strValue = $"{lValue}L";
							break;
						case float fValue:
							strValue = $"{fValue}F";
							break;
						case double dValue:
							strValue = $"{dValue}D";
							break;
						case decimal dValue:
							strValue = $"{dValue}M";
							break;
						case sbyte sbValue:
							strValue = $"(sbyte) {sbValue}";
							break;
						case byte bValue:
							strValue = $"(byte) {bValue}";
							break;
						default:
							strValue = aValue.ToString();
							break;
					}
				}
				else if(aValue.IsJson || aValue.IsGeoJson)
				{
					strValue = aValue.ToJson().ToString(Newtonsoft.Json.Formatting.None);
					strValue = $"new JObject(\"{strValue}\")";
				}
				else if(aValue.IsCDT)
				{
					var lstValue = aValue.AsEnumerable();
					strValue = String.Join(",", lstValue.Select(i => ConvertValue(i)));
					var dtStr = Helpers.GetRealTypeName(aValue.UnderlyingType);
					var dtParam = "()";

					if(dtStr[^2..] == "[]")
						dtParam = string.Empty;

					if(string.IsNullOrEmpty(dtStr))
						strValue = $"new {dtStr}{dtParam}";
					else 
						strValue = $"new {dtStr}{dtParam} {{ {strValue} }}";
				}
				else if(aValue.IsKeyValuePair)
				{
					dynamic kvp = aValue.Value;

					var kValue = ConvertValue(((object)kvp.Key).ToAValue());
					var vValue = ConvertValue(((object)kvp.Value).ToAValue());

					var dtStr = Helpers.GetRealTypeName(aValue.UnderlyingType);
					strValue = $"{{{kValue},{vValue}}}";
				}
				else if(aValue.IsDateTime)
				{
					strValue = $"DateTime.Parse({Helpers.ToLiteral(aValue.ToString(Helpers.DateTimeFormat))})";
				}
				else if(aValue.IsDateTimeOffset)
				{
					strValue = $"DateTimeOffset.Parse({Helpers.ToLiteral(aValue.ToString(Helpers.DateTimeOffsetFormat))})";
				}
				else if(aValue.IsTimeSpan)
				{
					strValue = $"TimeSpan.Parse({Helpers.ToLiteral(aValue.ToString(Helpers.TimeSpanFormat))})";
				}
				else
				{
					var dtStr = Helpers.GetRealTypeName(aValue.UnderlyingType);
					strValue = $"new {dtStr}({Helpers.ToLiteral(aValue.ToString())})";
				}
				return strValue.Replace("AValue", "Object");	
			}

			var strCode = new StringBuilder();

			switch(optCode)
			{
				case Options.None:
					return string.Empty;
				case Options.GetCode:
					if(useAerospikeAPI)
						strCode.Append($@"ASClient.Get(null,
		new Key(""{record.Aerospike.Namespace}"",{Helpers.ToLiteral(record.Aerospike.SetName)},");
					else
					{
						var safeNSName = Helpers.CheckName(record.Aerospike.Namespace, "Namespace");
						var safeSetName = Helpers.CheckName(record.Aerospike.SetName, "Set");

						strCode.Append($"{safeNSName}.{safeSetName}.Get(");
					}
					strCode.Append(ConvertValue(record.GetPK(), false));
					if(useAerospikeAPI)
						strCode.Append(')');
					break;
				case Options.PutCode:
					if(useAerospikeAPI)
						strCode.Append($@"ASClient.Put(null,
		new Key(""{record.Aerospike.Namespace}"",{Helpers.ToLiteral(record.Aerospike.SetName)},");
					else						
					{
						var safeNSName = Helpers.CheckName(record.Aerospike.Namespace, "Namespace");
						var safeSetName = Helpers.CheckName(record.Aerospike.SetName, "Set");

						strCode.Append($"{safeNSName}.{safeSetName}.Put(");
					}

					strCode.Append(ConvertValue(record.GetPK(), false));
					if(useAerospikeAPI)
					{
						strCode.Append(')');
					}
					strCode.AppendLine(", ");
					if(!useAerospikeAPI)
					{
						strCode.Append("new Dictionary<string,object>() {");
					}
					var binCode = new List<string>();
					foreach(var kvp in record.ToDictionary())
					{
						if(useAerospikeAPI)
						{
							binCode.Add($"new Bin({Helpers.ToLiteral(kvp.Key)}, {ConvertValue(kvp.Value?.ToAValue(), true)})");
						}
						else
						{
							binCode.Add($"{{{Helpers.ToLiteral(kvp.Key)}, {ConvertValue(kvp.Value?.ToAValue(), false)}}}");
						}
					}
					strCode.AppendJoin(",\r\n", binCode);
					if(!useAerospikeAPI)					
					{
						strCode.Append('}');
					}
					break;
				case Options.GetPutCode:
				default:
					throw new ArgumentException($"Invalid Code Option of {optCode}. must be Put or Get...");
			}

			strCode.Append(')');
			return strCode.ToString();
		}

		public static IEnumerable<string> ToAPICode(this IEnumerable<ARecord> records,
													Options codeOptions = Options.GetPutCode,
													bool useAerospikeAPI = false)
		{
			if(Options.None == codeOptions) yield break;

			
			foreach(var record in records)
			{
				if(codeOptions.HasFlag(Options.GetCode))
				{
					yield return GenerateCode(record, Options.GetCode, useAerospikeAPI);
				}

				if(codeOptions.HasFlag(Options.PutCode))
				{
					yield return GenerateCode(record, Options.PutCode, useAerospikeAPI);
				}
			}
		}
	}
}
