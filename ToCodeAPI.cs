using Aerospike.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
	/// <summary>
	/// Helpers that will translate an <see cref="ARecord"/> into Aerospike API Code template
	/// </summary>
	public static  class ToCodeAPI
	{
		/// <summary>
		/// Enum Options used to Generate API Code
		/// </summary>
		[Flags]
		public enum Options
		{
			None = 0,
			/// <summary>
			/// Generates Get API Code.
			/// </summary>
			GetCode = 0x0001,
			/// <summary>
			/// Generates Put API Code.
			/// </summary>
			PutCode = 0x0010,
			/// <summary>
			/// Generates Get and Put API Code.
			/// </summary>
			GetPutCode = GetCode | PutCode
		}

		static string ConvertValue(AValue aValue, bool useAerospikeAPI = false)
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

				var kValue = ConvertValue(((object) kvp.Key).ToAValue());
				var vValue = ConvertValue(((object) kvp.Value).ToAValue());

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

		static string CreateKey(ARecord record)
						=> $@"new Key(""{record.Aerospike.Namespace}"", {Helpers.ToLiteral(record.Aerospike.SetName)}, {ConvertValue(record.GetPK(), false)})";

		static string CreateBin(string binName, AValue value)
						=> $"new Bin({Helpers.ToLiteral(binName)}, {ConvertValue(value, true)})";

		static string CreateBin(string binName, object value)
						=> CreateBin(binName, (AValue) value?.ToAValue());

		static string CreateKVPBin(string binName, AValue value)
						=> $"{{{Helpers.ToLiteral(binName)}, {ConvertValue(value, false)}}}";

		static string CreateKVPBin(string binName, object value)
						=> CreateKVPBin(binName, (AValue) value?.ToAValue());

		static string GenerateCode(ARecord record,
									Options optCode,
									bool useAerospikeAPI,
									bool useBatchStmt)
		{
			var strCode = new StringBuilder();

			switch(optCode)
			{
				case Options.None:
					return string.Empty;
				case Options.GetCode:
					if(useAerospikeAPI)
					{
						if(useBatchStmt)
							strCode.Append($"new BatchRead(null, {CreateKey(record)}, true)");
						else
						{
							strCode.AppendLine("ASClient.Get(null,");
							strCode.Append($"{CreateKey(record)})");
						}
					}
					else if(useBatchStmt)
					{
						strCode.Append(ConvertValue(record.GetPK(), false));
					}
					else
					{
						var safeNSName = Helpers.CheckName(record.Aerospike.Namespace, "Namespace");
						var safeSetName = Helpers.CheckName(record.Aerospike.SetName, "Set");

						strCode.Append($"{safeNSName}.{safeSetName}.Get(");
						strCode.Append(ConvertValue(record.GetPK(), false));
						strCode.Append(')');
					}
					break;
				case Options.PutCode:
					if(useBatchStmt)
					{
						if(useAerospikeAPI)
						{							
							var binOpCode = new List<string>();
							foreach(var kvp in record.ToDictionary())
							{
								binOpCode.Add($"Operation.Put({CreateBin(kvp.Key, kvp.Value)})");
							}

							strCode.AppendLine("new BatchWrite(null,");
							strCode.Append(CreateKey(record));
							strCode.AppendLine(",");
							strCode.AppendLine("new Operation[] {");
							strCode.AppendJoin(",\r\n", binOpCode);
							strCode.Append("})");
						}
						else
						{
							strCode.Append("new (");
							strCode.Append(ConvertValue(record.GetPK(), false));
							strCode.AppendLine(", ");
							strCode.Append("new Dictionary<string,object>() {");
							var binCode = new List<string>();
							foreach(var kvp in record.ToDictionary())
							{
								binCode.Add(CreateKVPBin(kvp.Key, kvp.Value));
							}
							strCode.AppendJoin(",\r\n", binCode);
							strCode.Append("})");
						}
					}
					else
					{
						if(useAerospikeAPI)
						{
							strCode.AppendLine("ASClient.Put(null,");
							strCode.AppendLine($"{CreateKey(record)}, ");
						}
						else
						{
							var safeNSName = Helpers.CheckName(record.Aerospike.Namespace, "Namespace");
							var safeSetName = Helpers.CheckName(record.Aerospike.SetName, "Set");

							strCode.Append($"{safeNSName}.{safeSetName}.Put(");
							strCode.Append(ConvertValue(record.GetPK(), false));
							strCode.AppendLine(", ");
						}
						if(!useAerospikeAPI)
						{
							strCode.Append("new Dictionary<string,object>() {");
						}
						var binCode = new List<string>();
						foreach(var kvp in record.ToDictionary())
						{
							if(useAerospikeAPI)
							{
								binCode.Add(CreateBin(kvp.Key, kvp.Value));
							}
							else
							{
								binCode.Add(CreateKVPBin(kvp.Key, kvp.Value));
							}
						}
						strCode.AppendJoin(",\r\n", binCode);
						if(!useAerospikeAPI)
						{
							strCode.Append('}');
						}
						strCode.Append(')');
					}					
					break;				
				default:
					throw new ArgumentException($"Invalid Code Option of {optCode}. must be Put or Get...");
			}
			
			return strCode.ToString();
		}

		/// <summary>
		/// Generates either the Aerospike or LINQPad API code.
		/// </summary>
		/// <param name="records">
		/// Collection of <see cref="ARecord"/> that will be used to generate API Code.
		/// </param>
		/// <param name="codeOptions">
		/// Code Generation Options.
		/// </param>
		/// <param name="useAerospikeAPI">
		/// if set to <c>true</c>, the code generated will be based on the native Aerospike API driver.
		/// </param>
		/// <returns>
		/// Returns a collection of generated API code where each element is a separate Get or Put statement.
		/// </returns>
		/// <seealso cref="ToAPICodeBatch(IEnumerable{ARecord}, Options, bool)"/>
		/// <seealso cref="ToAPICode(ARecord, Options, bool)"/>
		public static IEnumerable<string> ToAPICode(this IEnumerable<ARecord> records,
													Options codeOptions = Options.GetPutCode,
													bool useAerospikeAPI = false)
		{
			if(Options.None == codeOptions) yield break;

			foreach(var record in records)
			{
				if(codeOptions.HasFlag(Options.GetCode))
				{
					yield return GenerateCode(record, Options.GetCode, useAerospikeAPI, false);
				}

				if(codeOptions.HasFlag(Options.PutCode))
				{
					yield return GenerateCode(record, Options.PutCode, useAerospikeAPI, false);
				}
			}
		}

		/// <summary>
		/// Generates Aerospike or LINQPad API batch code based on <see cref="Options"/>.
		/// </summary>
		/// <param name="records">
		/// Collection of <see cref="ARecord"/> that will be used to generate batch API Code.
		/// </param>
		/// <param name="codeOptions">
		/// Code Generation Options.
		/// </param>
		/// <param name="useAerospikeAPI">
		/// if set to <c>true</c>, the code generated will be based on the native Aerospike API driver.
		/// </param>
		/// <returns>
		/// Returns a collection of generated API code where each element is a separate Batch statement.
		/// </returns>
		/// <seealso cref="ToAPICodeBatch(IEnumerable{ARecord}, Options, bool)"/>
		/// <seealso cref="ToAPICode(ARecord, Options, bool)"/>
		public static IEnumerable<string> ToAPICodeBatch(this IEnumerable<ARecord> records,
															Options codeOptions = Options.GetPutCode,
															bool useAerospikeAPI = false)
		{
			if(Options.None == codeOptions) return Enumerable.Empty<string>();
			var batchCode = new List<string>();

			if(useAerospikeAPI)
			{
				if(codeOptions.HasFlag(Options.GetCode))
				{
					var strBatch = new StringBuilder();
					var keys = new List<string>();
					foreach(var record in records)
					{
						keys.Add(GenerateCode(record, Options.GetCode, true, true));
					}
					strBatch.AppendLine("ASClient.Get(null, new List<BatchRead>(){");
					strBatch.AppendJoin(",\r\n", keys);
					strBatch.Append("})");
					batchCode.Add(strBatch.ToString());
				}

				if(codeOptions.HasFlag(Options.PutCode))
				{
					var strBatch = new StringBuilder();
					var pairRecs = new List<string>();
					foreach(var record in records)
					{
						pairRecs.Add(GenerateCode(record, Options.PutCode, true, true));
					}
					strBatch.AppendLine("ASClient.Operate(null, new List<BatchRecord>(){");
					strBatch.AppendJoin(",\r\n", pairRecs);
					strBatch.Append("})");
					batchCode.Add(strBatch.ToString());
				}
			}
			else
			{
				var grouping = records.GroupBy(s => (s.Aerospike.Namespace, s.Aerospike.SetName));

				foreach(var group in grouping)
				{
					var safeNSName = Helpers.CheckName(group.Key.Namespace, "Namespace");
					var safeSetName = Helpers.CheckName(group.Key.SetName, "Set");

					if(codeOptions.HasFlag(Options.GetCode))
					{
						var strBatch = new StringBuilder();
						var keys = new List<string>();
						foreach(var record in group)
						{
							keys.Add(GenerateCode(record, Options.GetCode, false, true));
						}
						strBatch.AppendLine($"{safeNSName}.{safeSetName}.BatchRead(new object[] {{");
						strBatch.AppendJoin(",\r\n", keys);
						strBatch.Append("})");
						batchCode.Add(strBatch.ToString());
					}

					if(codeOptions.HasFlag(Options.PutCode))
					{
						var strBatch = new StringBuilder();
						var pairRecs = new List<string>();
						foreach(var record in group)
						{
							pairRecs.Add(GenerateCode(record, Options.PutCode, false, true));
						}
						strBatch.AppendLine($"{safeNSName}.{safeSetName}.BatchWrite(new (object key,IDictionary<string,object> binvaluePair)[] {{");
						strBatch.AppendJoin(",\r\n", pairRecs);
						strBatch.Append("})");
						batchCode.Add(strBatch.ToString());
					}
				}
			}

			return batchCode;
		}

		/// <summary>
		/// Generates a Aerospike or LINQPad API code segment.
		/// </summary>
		/// <param name="record">
		/// A record used to generate the code segment.
		/// </param>
		/// <param name="codeOptions">
		/// Code Generation Options.
		/// </param>
		/// <param name="useAerospikeAPI">
		/// if set to <c>true</c>, the code generated will be based on the native Aerospike API driver.
		/// </param>
		/// <returns>
		/// Returns generated API code based on <paramref name="record"/>. 
		/// The returned generated code may only be a code segment.
		/// </returns>
		/// <seealso cref="ToAPICodeBatch(IEnumerable{ARecord}, Options, bool)"/>
		/// <seealso cref="ToAPICode(IEnumerable{ARecord}, Options, bool)"/>
		public static string ToAPICode(this ARecord record,
										Options codeOptions = Options.PutCode,
										bool useAerospikeAPI = false) => GenerateCode(record, codeOptions, useAerospikeAPI, false);
	}
}
