using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
	/// <summary>
	/// Builds AI-facing Markdown and prompt text for the current Aerospike LINQPad connection.
	///
	/// Important:
	/// This class intentionally does NOT call LINQPad's Util.Markdown.
	/// The driver returns plain Markdown strings. LINQPad query samples can render those strings
	/// with Util.Markdown(markdown).Dump() at query runtime.
	/// </summary>
	public sealed class AerospikeAIContext
	{
		private readonly AClusterAccess cluster;
		private readonly AerospikeConnection connection;

		private AerospikeAIContext(AClusterAccess cluster)
		{
			this.cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));

			this.connection = cluster.AerospikeConnection
				?? throw new InvalidOperationException("The Aerospike LINQPad connection is not available.");
		}

		public static AerospikeAIContext From(AClusterAccess cluster)
		{
			return new AerospikeAIContext(cluster);
		}

		public static AerospikeAIContext FromCurrent()
		{
			if(AClusterAccess.Instance is null)
			{
				throw new InvalidOperationException(
					"No current Aerospike LINQPad connection instance is available.");
			}

			return new AerospikeAIContext(AClusterAccess.Instance);
		}

		/// <summary>
		/// Creates Markdown context suitable for display or for inclusion in Util.AI.Ask(...).
		/// </summary>
		public string ToMarkdown(AerospikeAIContextOptions options = null)
		{
			options ??= new AerospikeAIContextOptions();

			var sb = new StringBuilder(Math.Min(options.MaxChars + 2048, 128_000));

			AppendHeader(sb);

			if(options.IncludeDriverGuide)
			{
				AppendDriverGuide(sb, options);
			}

			if(options.IncludeClusterSummary)
			{
				AppendClusterSummary(sb);
			}

			if(options.IncludeNamespaces)
			{
				var namespaces = GetNamespaces(options);
				AppendNamespaces(sb, namespaces, options);
			}

			if(options.IncludeUdfs)
			{
				AppendUdfs(sb);
			}

			if(options.IncludeExamples)
			{
				AppendExamples(sb, options);
			}

			AppendFooter(sb, options);

			return TrimToMaxChars(sb.ToString(), options.MaxChars);
		}

		/// <summary>
		/// Builds a complete AI prompt using this connection's context and a user request.
		/// </summary>
		public string BuildPrompt(
			string userRequest,
			AerospikeAIContextOptions options = null,
			string systemInstruction = null)
		{
			if(string.IsNullOrWhiteSpace(userRequest))
			{
				throw new ArgumentException("User request cannot be blank.", nameof(userRequest));
			}

			options ??= new AerospikeAIContextOptions();

			systemInstruction ??= BuildDefaultSystemInstruction(options);

			var contextMarkdown = ToMarkdown(options);

			return $@"
{systemInstruction}

# Aerospike LINQPad Context

{contextMarkdown}

# User Request

{userRequest}
";
		}

		/// <summary>
		/// Builds a narrower prompt for one namespace/set.
		/// </summary>
		public string BuildSetPrompt(
			string namespaceName,
			string setName,
			string userRequest,
			AerospikeAIContextOptions options = null,
			string systemInstruction = null)
		{
			if(string.IsNullOrWhiteSpace(namespaceName))
			{
				throw new ArgumentException("Namespace name cannot be blank.", nameof(namespaceName));
			}

			if(string.IsNullOrWhiteSpace(setName))
			{
				throw new ArgumentException("Set name cannot be blank.", nameof(setName));
			}

			options ??= new AerospikeAIContextOptions();

			options.IncludeClusterSummary = false;
			options.MaxNamespaces = 1;
			options.MaxSetsPerNamespace = 1;
			options.NamespaceName = namespaceName;
			options.SetName = setName;

			return BuildPrompt(userRequest, options, systemInstruction);
		}

		private static string BuildDefaultSystemInstruction(AerospikeAIContextOptions options)
		{
			var linqSyntaxInstruction =
				options.LinqSyntaxPreference == AerospikeLinqSyntaxPreference.MethodSyntax
					? @"
Important LINQ syntax preference:
The configured LINQ syntax preference is MethodSyntax.
Prefer LINQ method syntax.
Generate chained LINQ methods such as .Where(...), .OrderBy(...), .Select(...), .Join(...), and .GroupBy(...).
"
					: @"
Important LINQ syntax preference:
The configured LINQ syntax preference is QuerySyntax.
Use C# LINQ query syntax as the default form for query logic.
For filters, projections, sorting, joins, and grouping, generate query syntax using from, where, select, orderby, join, and group.
Do not generate method-chain query logic such as .Where(...), .Select(...), .OrderBy(...), .Join(...), or .GroupBy(...) when an equivalent query-syntax form is available.
Method syntax is allowed only for terminal or non-query-expression operations such as .Take(100), .Skip(...), .ToList(), .Count(), .FirstOrDefault(), .Any(), .Dump(), or operations that cannot be expressed cleanly in query syntax.
For joins, generate:
    from left in Namespace.LeftSet.AsEnumerable()
    join right in Namespace.RightSet.AsEnumerable()
        on left.SomeKey equals right.SomeKey
    select new { ... }
Do not generate:
    Namespace.LeftSet.AsEnumerable().Join(...)
";

			return $@"
You are generating LINQPad C# statements for the Aerospike LINQPad driver.

Use only the APIs, generated members, namespaces, sets, bins, and examples described in the supplied context.
Return runnable LINQPad C# statements unless the user asks for explanation.
Prefer safe, bounded, read-only queries unless the user explicitly asks for writes.
Use Dump() for output.
Do not assume every Aerospike record has every bin.
Treat bin/type information as observed/inferred because Aerospike is schemaless.

{linqSyntaxInstruction}

Important generated-property rule:
When accessing Aerospike bin values from generated record objects, prefer generated C# properties when available.
For example, generate customer.userid instead of customer[""userid""] when the userid property exists.
Only use record[""binName""] string-indexer access when no generated property is available, when the bin name is not a valid C# identifier, or when dynamic bin access is specifically required.

Important primary-key rule:
When the Aerospike primary key value is required, prefer the generated/default primary-key property when available.
For example, use record.PK when PK exists.
If no generated/default primary-key property is available, use record.GetPK().
Do not use string bin access for the primary key unless the context explicitly says the primary key is stored as a normal bin.

Important LINQ rule:
Generated Aerospike set objects are SetRecords / SetRecords<T> instances.
For LINQ collection operations such as Join, GroupJoin, OrderBy, GroupBy, SelectMany, Concat, Union, Distinct, Except, Intersect, ToDictionary, and similar methods, call AsEnumerable() on the set first.
When using query syntax, generate from record in NamespaceName.SetName.AsEnumerable().
When using method syntax, generate NamespaceName.SetName.AsEnumerable() before Join, OrderBy, GroupBy, and similar methods.
";
		}

		private void AppendHeader(StringBuilder sb)
		{
			sb.AppendLine("# Aerospike LINQPad Driver AI Context");
			sb.AppendLine();
			sb.AppendLine("This context describes the current Aerospike LINQPad connection.");
			sb.AppendLine("Aerospike is schemaless, so bin/type information is observed or inferred from driver metadata.");
			sb.AppendLine();
		}

		private void AppendDriverGuide(StringBuilder sb, AerospikeAIContextOptions options)
		{
			sb.AppendLine("## Driver Usage Rules");
			sb.AppendLine();
			sb.AppendLine("- Generate C# statements intended to run inside LINQPad.");
			sb.AppendLine("- Use `Dump()` to display results.");
			sb.AppendLine("- Prefer generated namespace and set members when they are present.");
			sb.AppendLine("- Keep scans and queries bounded with `Take(...)`, filters, or secondary indexes where possible.");
			sb.AppendLine("- Do not assume every Aerospike record in a set has every bin.");
			sb.AppendLine("- Do not assume a bin has only one type unless metadata clearly indicates it.");
			sb.AppendLine("- Treat bin names as case-sensitive.");
			sb.AppendLine("- Prefer read-only query/exploration code unless the user explicitly asks for writes.");
			sb.AppendLine("- Ask before destructive deletes/truncates unless the user explicitly requested them.");
			sb.AppendLine("- Use the native Aerospike client only when the high-level driver API does not cover the request.");
			sb.AppendLine();

			AppendLinqSyntaxGuide(sb, options);

			sb.AppendLine("### Important Record Property Rule");
			sb.AppendLine();
			sb.AppendLine("- When accessing Aerospike bin values from generated record objects, prefer generated C# properties when available.");
			sb.AppendLine("- Example: use `customer.userid` instead of `customer[\"userid\"]` when the `userid` property exists.");
			sb.AppendLine("- Use `record[\"binName\"]` only when no generated property exists, the bin name is not a valid C# identifier, or dynamic access is specifically required.");
			sb.AppendLine("- Prefer property access in projections, filters, joins, sorts, and groups.");
			sb.AppendLine("- The set-level bin metadata below lists the raw Aerospike bin name and, when available, the generated C# property name.");
			sb.AppendLine();

			sb.AppendLine("### Important Primary Key Rule");
			sb.AppendLine();
			sb.AppendLine("- When the Aerospike primary key value is required, prefer the generated/default primary-key property when available.");
			sb.AppendLine("- Example: use `record.PK` when the `PK` property exists.");
			sb.AppendLine("- If no generated/default primary-key property is available, use `record.GetPK()`.");
			sb.AppendLine("- Do not access the primary key through `record[\"PK\"]` or another string-indexer expression unless the context explicitly says the primary key is stored as a normal bin.");
			sb.AppendLine();

			sb.AppendLine("### Important LINQ Rule for SetRecords");
			sb.AppendLine();
			sb.AppendLine("- Generated Aerospike set objects are `SetRecords` / `SetRecords<T>` instances.");
			sb.AppendLine("- When using LINQ extension methods that require `IEnumerable<T>` semantics, call `AsEnumerable()` on the set first.");
			sb.AppendLine("- This applies to LINQ operations such as `Join`, `GroupJoin`, `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`, `GroupBy`, `SelectMany`, `Concat`, `Union`, `Distinct`, `Except`, `Intersect`, `ToDictionary`, and similar collection-style LINQ methods.");
			sb.AppendLine("- With query syntax, use `from record in NamespaceName.SetName.AsEnumerable()`.");
			sb.AppendLine("- With method syntax, use `NamespaceName.SetName.AsEnumerable()` as the LINQ source.");
			sb.AppendLine("- Do not generate `NamespaceName.SetName.Join(...)`, `NamespaceName.SetName.OrderBy(...)`, or `NamespaceName.SetName.GroupBy(...)` directly.");
			sb.AppendLine("- Instead generate query syntax such as `from record in NamespaceName.SetName.AsEnumerable()` when QuerySyntax is configured.");
			sb.AppendLine("- Use method syntax such as `NamespaceName.SetName.AsEnumerable().Join(...)` only when MethodSyntax is configured or query syntax cannot express the operation cleanly.");
			sb.AppendLine();
		}

		private static void AppendLinqSyntaxGuide(StringBuilder sb, AerospikeAIContextOptions options)
		{
			sb.AppendLine("### Important LINQ Syntax Preference");
			sb.AppendLine();

			if(options.LinqSyntaxPreference == AerospikeLinqSyntaxPreference.MethodSyntax)
			{
				sb.AppendLine("- Preferred LINQ style: `method syntax`.");
				sb.AppendLine("- Prefer chained methods such as `.Where(...)`, `.OrderBy(...)`, `.Select(...)`, `.Join(...)`, and `.GroupBy(...)`.");
				sb.AppendLine("- Use query syntax only when explicitly requested by the user or when it is materially clearer.");
			}
			else
			{
				sb.AppendLine("- Preferred LINQ style: `query syntax`.");
				sb.AppendLine("- Use query syntax as the default form for query logic.");
				sb.AppendLine("- For filters, projections, sorting, joins, and grouping, use `from`, `where`, `select`, `orderby`, `join`, and `group`.");
				sb.AppendLine("- Do not use method-chain forms such as `.Where(...)`, `.Select(...)`, `.OrderBy(...)`, `.Join(...)`, or `.GroupBy(...)` when an equivalent query-syntax form is available.");
				sb.AppendLine("- Method syntax is allowed only for terminal or non-query-expression operations such as `.Take(100)`, `.Skip(...)`, `.ToList()`, `.Count()`, `.FirstOrDefault()`, `.Any()`, `.Dump()`, or operations that cannot be expressed cleanly in query syntax.");
				sb.AppendLine("- For joins, generate `from left in Namespace.LeftSet.AsEnumerable() join right in Namespace.RightSet.AsEnumerable() on left.Key equals right.Key select ...`.");
				sb.AppendLine("- Do not generate `.Join(...)` when a query-syntax `join` clause can express the same logic.");
			}

			sb.AppendLine();
		}

		private void AppendClusterSummary(StringBuilder sb)
		{
			sb.AppendLine("## Cluster Summary");
			sb.AppendLine();

			AppendKeyValue(sb, "Cluster name", connection.Database);
			AppendKeyValue(sb, "Connection string", connection.ConnectionString);
			AppendKeyValue(sb, "Record view", connection.RecordView);
			AppendKeyValue(sb, "Document API enabled", connection.DocumentAPI);
			AppendKeyValue(sb, "Always use AValue", connection.AlwaysUseAValues);
			AppendKeyValue(sb, "Send user key", connection.SendPK);
			AppendKeyValue(sb, "Network compression", connection.NetworkCompression);

			var version = connection.CXInfo?.DatabaseInfo?.DbVersion;
			AppendKeyValue(sb, "Server version", version);

			var nodes = connection.Nodes;
			if(nodes is not null && nodes.Length > 0)
			{
				sb.AppendLine($"- Nodes: `{nodes.Length}`");

				foreach(var node in nodes.Take(20))
				{
					sb.AppendLine($"  - `{Safe(node?.Name)}` active=`{node?.Active}`");
				}

				if(nodes.Length > 20)
				{
					sb.AppendLine("  - ... truncated after 20 nodes");
				}
			}

			var features = connection.DBFeatures;
			if(features is not null && features.Any())
			{
				sb.AppendLine("- Server/driver features:");

				foreach(var feature in features.Take(60))
				{
					sb.AppendLine($"  - `{Safe(feature)}`");
				}

				if(features.Count() > 60)
				{
					sb.AppendLine("  - ... truncated after 60 features");
				}
			}

			sb.AppendLine();
		}

		private IEnumerable<LPNamespace> GetNamespaces(AerospikeAIContextOptions options)
		{
			var dbInfo = connection.GetDBInfo(options.ForceRefreshMetadata);

			var namespaces = dbInfo?
				.OfType<LPNamespace>()
				.AsEnumerable()
				?? Enumerable.Empty<LPNamespace>();

			if(!string.IsNullOrWhiteSpace(options.NamespaceName))
			{
				namespaces = namespaces.Where(ns =>
					string.Equals(ns.Name, options.NamespaceName, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(ns.SafeName, options.NamespaceName, StringComparison.OrdinalIgnoreCase));
			}

			return namespaces
				.OrderBy(ns => ns.Name)
				.Take(options.MaxNamespaces)
				.ToArray();
		}

		private void AppendNamespaces(
			StringBuilder sb,
			IEnumerable<LPNamespace> namespaces,
			AerospikeAIContextOptions options)
		{
			sb.AppendLine("## Namespaces, Sets, Bins, and Secondary Indexes");
			sb.AppendLine();

			var namespaceArray = namespaces.ToArray();

			if(namespaceArray.Length == 0)
			{
				sb.AppendLine("_No namespace metadata was found for this connection._");
				sb.AppendLine();
				return;
			}

			foreach(var ns in namespaceArray)
			{
				AppendNamespace(sb, ns, options);
			}
		}

		private void AppendNamespace(
			StringBuilder sb,
			LPNamespace ns,
			AerospikeAIContextOptions options)
		{
			sb.AppendLine($"### Namespace `{Safe(ns.Name)}`");
			sb.AppendLine();

			AppendKeyValue(sb, "Generated C# name", ns.SafeName);
			AppendKeyValue(sb, "Strong consistency", ns.IsStrongConsistencyMode);

			if(ns.Bins is not null && ns.Bins.Any())
			{
				sb.AppendLine("- Namespace-level observed bins:");

				foreach(var bin in ns.Bins.OrderBy(b => b).Take(options.MaxBinsPerSet))
				{
					sb.AppendLine($"  - Bin `{Safe(bin)}`");
				}

				if(ns.Bins.Count() > options.MaxBinsPerSet)
				{
					sb.AppendLine($"  - ... truncated after {options.MaxBinsPerSet} bins");
				}
			}

			if(ns.ConfigParams is not null && ns.ConfigParams.Any())
			{
				sb.AppendLine("- Selected namespace configuration:");

				foreach(var cfg in ns.ConfigParams.Take(40))
				{
					sb.AppendLine($"  - `{Safe(cfg.Key)}={Safe(string.Join(",", cfg))}`");
				}

				if(ns.ConfigParams.Count() > 40)
				{
					sb.AppendLine("  - ... truncated after 40 config entries");
				}
			}

			if(options.IncludeSets)
			{
				AppendSets(sb, ns, options);
			}

			sb.AppendLine();
		}

		private void AppendSets(
			StringBuilder sb,
			LPNamespace ns,
			AerospikeAIContextOptions options)
		{
			if(ns.Sets is null || !ns.Sets.Any())
			{
				sb.AppendLine();
				sb.AppendLine("_No set metadata was found for this namespace._");
				return;
			}

			IEnumerable<LPSet> sets = ns.Sets;

			if(!string.IsNullOrWhiteSpace(options.SetName))
			{
				sets = sets.Where(set =>
					string.Equals(set.Name, options.SetName, StringComparison.OrdinalIgnoreCase)
					|| string.Equals(set.SafeName, options.SetName, StringComparison.OrdinalIgnoreCase));
			}

			var selectedSets = sets
				.OrderBy(set => set.IsNullSet)
				.ThenBy(set => set.Name)
				.Take(options.MaxSetsPerNamespace)
				.ToArray();

			sb.AppendLine();
			sb.AppendLine("#### Sets");
			sb.AppendLine();

			if(selectedSets.Length == 0)
			{
				sb.AppendLine("_No matching set metadata was found for this namespace._");
				sb.AppendLine();
				return;
			}

			foreach(var set in selectedSets)
			{
				AppendSet(sb, ns, set, options);
			}

			var totalSets = sets.Count();
			if(totalSets > options.MaxSetsPerNamespace)
			{
				sb.AppendLine($"_Sets truncated: showing {options.MaxSetsPerNamespace} of {totalSets}._");
				sb.AppendLine();
			}
		}

		private void AppendSet(
			StringBuilder sb,
			LPNamespace ns,
			LPSet set,
			AerospikeAIContextOptions options)
		{
			var displaySetName = set.IsNullSet ? "<null-set>" : set.Name;

			sb.AppendLine($"##### Set `{Safe(displaySetName)}`");
			sb.AppendLine();

			AppendKeyValue(sb, "Generated C# name", set.SafeName);
			AppendKeyValue(sb, "Suggested access pattern", $"{ns.SafeName}.{set.SafeName}");

			sb.AppendLine("- Primary key access: prefer generated/default property `PK` when available; otherwise use `GetPK()`.");
			sb.AppendLine("- Record bin access: prefer generated C# properties listed below; use string-indexer access only as a fallback.");
			sb.AppendLine("- LINQ collection operations: call `.AsEnumerable()` on this set before `Join`, `OrderBy`, `GroupBy`, and similar methods.");

			if(options.LinqSyntaxPreference == AerospikeLinqSyntaxPreference.QuerySyntax)
			{
				sb.AppendLine($"- Preferred query source: `from record in {ns.SafeName}.{set.SafeName}.AsEnumerable()`.");
			}
			else
			{
				sb.AppendLine($"- Preferred method source: `{ns.SafeName}.{set.SafeName}.AsEnumerable()`.");
			}

			AppendKeyValue(sb, "Is null set", set.IsNullSet);
			AppendKeyValue(sb, "Is vector-index-like set", set.IsVectorIdx);

			if(set.LastException is not null)
			{
				AppendKeyValue(sb, "Metadata warning", set.LastException.Message);
			}

			if(options.IncludeBins)
			{
				AppendBins(sb, set, options);
			}

			if(options.IncludeSecondaryIndexes)
			{
				AppendSecondaryIndexes(sb, set);
			}

			sb.AppendLine();
		}

		private void AppendBins(
			StringBuilder sb,
			LPSet set,
			AerospikeAIContextOptions options)
		{
			var bins = set.BinTypes?
				.OrderBy(bin => bin.BinName)
				.Take(options.MaxBinsPerSet)
				.ToArray();

			if(bins is null || bins.Length == 0)
			{
				sb.AppendLine("- Bins: none detected or not sampled.");
				return;
			}

			sb.AppendLine("- Observed bins:");

			foreach(var bin in bins)
			{
				var typeName = bin.DataType is null
					? "unknown"
					: Helpers.GetRealTypeName(bin.DataType);

				var propertyName = GetGeneratedPropertyName(bin);

				var propertyPart = string.IsNullOrWhiteSpace(propertyName)
					? ", generated property=`<not available; use string-indexer fallback>`"
					: $", generated property=`{Safe(propertyName)}`";

				var notes = new List<string>();

				if(bin.Duplicate)
				{
					notes.Add("multiple observed types");
				}

				if(!bin.FndAllRecs)
				{
					notes.Add("not present in all sampled records");
				}

				if(bin.Detected)
				{
					notes.Add("detected after initial scan");
				}

				if(bin.IsFK)
				{
					notes.Add($"foreign/reference hint to {bin.FKSetname}");
				}

				var suffix = notes.Count == 0
					? string.Empty
					: $" // {string.Join(", ", notes)}";

				sb.AppendLine($"  - Bin `{Safe(bin.BinName)}`: type=`{Safe(typeName)}`{propertyPart}{suffix}");
			}

			var totalBins = set.BinTypes?.Count() ?? 0;
			if(totalBins > options.MaxBinsPerSet)
			{
				sb.AppendLine($"  - ... truncated after {options.MaxBinsPerSet} bins");
			}
		}

		private void AppendSecondaryIndexes(StringBuilder sb, LPSet set)
		{
			var indexes = set.SIndexes?
				.OrderBy(idx => idx.Name)
				.ToArray();

			if(indexes is null || indexes.Length == 0)
			{
				return;
			}

			sb.AppendLine("- Secondary indexes:");

			foreach(var idx in indexes)
			{
				var context = string.IsNullOrWhiteSpace(idx.Context)
					? string.Empty
					: $", context=`{Safe(idx.Context)}`";

				sb.AppendLine(
					$"  - `{Safe(idx.Name)}` on bin `{Safe(idx.Bin)}` " +
					$"type=`{Safe(idx.Type)}`, indexType=`{Safe(idx.IndexType)}`{context}");
			}
		}

		private void AppendUdfs(StringBuilder sb)
		{
			var modules = connection.UDFModules?
				.OfType<LPModule>()
				.OrderBy(module => module.Name)
				.ToArray();

			if(modules is null || modules.Length == 0)
			{
				return;
			}

			sb.AppendLine("## UDF Modules");
			sb.AppendLine();

			foreach(var module in modules)
			{
				sb.AppendLine($"- Module `{Safe(module.Name)}` type=`{Safe(module.Type)}` package=`{Safe(module.PackageName)}`");

				if(module.UDFs is not null)
				{
					foreach(var udf in module.UDFs.OrderBy(udf => udf.Name))
					{
						sb.AppendLine($"  - `{Safe(udf.Name)}`");
					}
				}
			}

			sb.AppendLine();
		}

		private void AppendExamples(StringBuilder sb, AerospikeAIContextOptions options)
		{
			sb.AppendLine("## Canonical LINQPad Examples");
			sb.AppendLine();

			AppendGeneralExamples(sb);

			if(options.LinqSyntaxPreference == AerospikeLinqSyntaxPreference.MethodSyntax)
			{
				AppendMethodSyntaxExamples(sb);
			}
			else
			{
				AppendQuerySyntaxExamples(sb);
			}

			AppendNativeClientExample(sb);
		}

		private static void AppendGeneralExamples(StringBuilder sb)
		{
			sb.AppendLine("### Show this AI context as plain text");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("AIContext.ToMarkdown().Dump(\"Aerospike AI Context Markdown\");");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Show this AI context as rendered Markdown in LINQPad");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("var markdown = AIContext.ToMarkdown();");
			sb.AppendLine("Util.Markdown(markdown).Dump(\"Aerospike AI Context\");");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Ask LINQPad AI using this context");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("var prompt = AIContext.BuildPrompt(\"Show me 100 records from the most relevant set.\");");
			sb.AppendLine("var response = await Util.AI.Ask(prompt).GetResponseAsync();");
			sb.AppendLine("response.Text.Dump(\"AI-generated LINQPad C#\");");
			sb.AppendLine("```");
			sb.AppendLine();
		}

		private static void AppendQuerySyntaxExamples(StringBuilder sb)
		{
			sb.AppendLine("### Query a generated set");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// Replace NamespaceName and SetName with generated names from the context.");
			sb.AppendLine("var records =");
			sb.AppendLine("    (from r in NamespaceName.SetName.AsEnumerable()");
			sb.AppendLine("     select r)");
			sb.AppendLine("    .Take(100);");
			sb.AppendLine();
			sb.AppendLine("records.Dump();");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Query with a bin/property filter");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// Prefer generated properties when available.");
			sb.AppendLine("// Example: use r.status instead of r[\"status\"] when the status property exists.");
			sb.AppendLine("var activeRecords =");
			sb.AppendLine("    (from r in NamespaceName.SetName.AsEnumerable()");
			sb.AppendLine("     where r.status == \"active\"");
			sb.AppendLine("     select r)");
			sb.AppendLine("    .Take(100);");
			sb.AppendLine();
			sb.AppendLine("activeRecords.Dump();");
			sb.AppendLine();
			sb.AppendLine("// Use string-indexer access only when no generated property exists or dynamic access is required.");
			sb.AppendLine("var dynamicRecords =");
			sb.AppendLine("    (from r in NamespaceName.SetName.AsEnumerable()");
			sb.AppendLine("     where r[\"some-dynamic-bin\"] == \"active\"");
			sb.AppendLine("     select r)");
			sb.AppendLine("    .Take(100);");
			sb.AppendLine();
			sb.AppendLine("dynamicRecords.Dump();");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Sort records from an Aerospike set");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// Use query syntax when practical, and call AsEnumerable() on the set first.");
			sb.AppendLine("// Prefer generated properties over string-indexer bin access.");
			sb.AppendLine("var ordered =");
			sb.AppendLine("    (from r in NamespaceName.SetName.AsEnumerable()");
			sb.AppendLine("     orderby r.status, r.PK");
			sb.AppendLine("     select new");
			sb.AppendLine("     {");
			sb.AppendLine("         r.PK,");
			sb.AppendLine("         r.status");
			sb.AppendLine("     })");
			sb.AppendLine("    .Take(100);");
			sb.AppendLine();
			sb.AppendLine("ordered.Dump();");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Join two Aerospike sets");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// Replace NamespaceName, Users, Orders, userid, and amount with actual generated names.");
			sb.AppendLine("// Prefer generated properties when available.");
			sb.AppendLine("var joined =");
			sb.AppendLine("    (from user in NamespaceName.Users.AsEnumerable()");
			sb.AppendLine("     join order in NamespaceName.Orders.AsEnumerable()");
			sb.AppendLine("        on user.userid equals order.userid");
			sb.AppendLine("     select new");
			sb.AppendLine("     {");
			sb.AppendLine("         UserId = user.userid,");
			sb.AppendLine("         UserPK = user.PK,");
			sb.AppendLine("         OrderPK = order.PK,");
			sb.AppendLine("         OrderAmount = order.amount");
			sb.AppendLine("     })");
			sb.AppendLine("    .Take(100);");
			sb.AppendLine();
			sb.AppendLine("joined.Dump();");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Query syntax join rule");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// Preferred when LinqSyntaxPreference is QuerySyntax:");
			sb.AppendLine("var joined =");
			sb.AppendLine("    (from customer in test.Customer.AsEnumerable()");
			sb.AppendLine("     join invoice in test.Invoice.AsEnumerable()");
			sb.AppendLine("        on customer.PK equals invoice.CustomerId");
			sb.AppendLine("     select new");
			sb.AppendLine("     {");
			sb.AppendLine("         CustomerPK = customer.PK,");
			sb.AppendLine("         customer.FirstName,");
			sb.AppendLine("         customer.LastName,");
			sb.AppendLine("         customer.Email,");
			sb.AppendLine("         InvoicePK = invoice.PK,");
			sb.AppendLine("         invoice.InvoiceDate,");
			sb.AppendLine("         invoice.Total,");
			sb.AppendLine("         invoice.BillingCity,");
			sb.AppendLine("         invoice.BillingCtry");
			sb.AppendLine("     })");
			sb.AppendLine("    .Take(100);");
			sb.AppendLine();
			sb.AppendLine("joined.Dump();");
			sb.AppendLine();
			sb.AppendLine("// Avoid this method-syntax form when an equivalent query-syntax join is available:");
			sb.AppendLine("// test.Customer.AsEnumerable().Join(test.Invoice.AsEnumerable(), ...)");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Group records from an Aerospike set");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// Prefer query syntax where practical.");
			sb.AppendLine("var grouped =");
			sb.AppendLine("    from r in NamespaceName.SetName.AsEnumerable()");
			sb.AppendLine("    group r by r.status into g");
			sb.AppendLine("    orderby g.Count() descending");
			sb.AppendLine("    select new");
			sb.AppendLine("    {");
			sb.AppendLine("        Status = g.Key,");
			sb.AppendLine("        Count = g.Count()");
			sb.AppendLine("    };");
			sb.AppendLine();
			sb.AppendLine("grouped.Dump();");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Access primary keys");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// Prefer the generated/default primary-key property when available.");
			sb.AppendLine("var records =");
			sb.AppendLine("    (from r in NamespaceName.SetName.AsEnumerable()");
			sb.AppendLine("     select new");
			sb.AppendLine("     {");
			sb.AppendLine("         PrimaryKey = r.PK,");
			sb.AppendLine("         r.status");
			sb.AppendLine("     })");
			sb.AppendLine("    .Take(100);");
			sb.AppendLine();
			sb.AppendLine("records.Dump();");
			sb.AppendLine();
			sb.AppendLine("// Fallback if the generated PK property is not available:");
			sb.AppendLine("var recordsWithFallbackPK =");
			sb.AppendLine("    (from r in NamespaceName.SetName.AsEnumerable()");
			sb.AppendLine("     select new");
			sb.AppendLine("     {");
			sb.AppendLine("         PrimaryKey = r.GetPK(),");
			sb.AppendLine("         r.status");
			sb.AppendLine("     })");
			sb.AppendLine("    .Take(100);");
			sb.AppendLine();
			sb.AppendLine("recordsWithFallbackPK.Dump();");
			sb.AppendLine("```");
			sb.AppendLine();
		}

		private static void AppendMethodSyntaxExamples(StringBuilder sb)
		{
			sb.AppendLine("### Query a generated set");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// Replace NamespaceName and SetName with generated names from the context.");
			sb.AppendLine("NamespaceName.SetName");
			sb.AppendLine("    .AsEnumerable()");
			sb.AppendLine("    .Take(100)");
			sb.AppendLine("    .Dump();");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Query with a bin/property filter");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// Prefer generated properties when available.");
			sb.AppendLine("// Example: use r.status instead of r[\"status\"] when the status property exists.");
			sb.AppendLine("NamespaceName.SetName");
			sb.AppendLine("    .AsEnumerable()");
			sb.AppendLine("    .Where(r => r.status == \"active\")");
			sb.AppendLine("    .Take(100)");
			sb.AppendLine("    .Dump();");
			sb.AppendLine();
			sb.AppendLine("// Use string-indexer access only when no generated property exists or dynamic access is required.");
			sb.AppendLine("NamespaceName.SetName");
			sb.AppendLine("    .AsEnumerable()");
			sb.AppendLine("    .Where(r => r[\"some-dynamic-bin\"] == \"active\")");
			sb.AppendLine("    .Take(100)");
			sb.AppendLine("    .Dump();");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Use LINQ collection operations with SetRecords");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// For LINQ methods such as Join, OrderBy, GroupBy, SelectMany, etc.,");
			sb.AppendLine("// call AsEnumerable() on the Aerospike set first.");
			sb.AppendLine("// Prefer generated properties over string-indexer bin access.");
			sb.AppendLine("var ordered = NamespaceName.SetName");
			sb.AppendLine("    .AsEnumerable()");
			sb.AppendLine("    .OrderBy(r => r.status)");
			sb.AppendLine("    .ThenBy(r => r.PK)");
			sb.AppendLine("    .Take(100);");
			sb.AppendLine();
			sb.AppendLine("ordered.Dump();");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Join two Aerospike sets");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// Replace NamespaceName, Users, Orders, userid, and amount with actual generated names.");
			sb.AppendLine("// Prefer generated properties when available.");
			sb.AppendLine("var joined = NamespaceName.Users");
			sb.AppendLine("    .AsEnumerable()");
			sb.AppendLine("    .Join(");
			sb.AppendLine("        NamespaceName.Orders.AsEnumerable(),");
			sb.AppendLine("        user => user.userid,");
			sb.AppendLine("        order => order.userid,");
			sb.AppendLine("        (user, order) => new");
			sb.AppendLine("        {");
			sb.AppendLine("            UserId = user.userid,");
			sb.AppendLine("            UserPK = user.PK,");
			sb.AppendLine("            OrderPK = order.PK,");
			sb.AppendLine("            OrderAmount = order.amount");
			sb.AppendLine("        })");
			sb.AppendLine("    .Take(100);");
			sb.AppendLine();
			sb.AppendLine("joined.Dump();");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Group records from an Aerospike set");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// Prefer generated properties when available.");
			sb.AppendLine("var grouped = NamespaceName.SetName");
			sb.AppendLine("    .AsEnumerable()");
			sb.AppendLine("    .GroupBy(r => r.status)");
			sb.AppendLine("    .Select(g => new");
			sb.AppendLine("    {");
			sb.AppendLine("        Status = g.Key,");
			sb.AppendLine("        Count = g.Count()");
			sb.AppendLine("    })");
			sb.AppendLine("    .OrderByDescending(x => x.Count);");
			sb.AppendLine();
			sb.AppendLine("grouped.Dump();");
			sb.AppendLine("```");
			sb.AppendLine();

			sb.AppendLine("### Access primary keys");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("// Prefer the generated/default primary-key property when available.");
			sb.AppendLine("var records = NamespaceName.SetName");
			sb.AppendLine("    .AsEnumerable()");
			sb.AppendLine("    .Take(100)");
			sb.AppendLine("    .Select(r => new");
			sb.AppendLine("    {");
			sb.AppendLine("        PrimaryKey = r.PK,");
			sb.AppendLine("        r.status");
			sb.AppendLine("    });");
			sb.AppendLine();
			sb.AppendLine("records.Dump();");
			sb.AppendLine();
			sb.AppendLine("// Fallback if the generated PK property is not available:");
			sb.AppendLine("var recordsWithFallbackPK = NamespaceName.SetName");
			sb.AppendLine("    .AsEnumerable()");
			sb.AppendLine("    .Take(100)");
			sb.AppendLine("    .Select(r => new");
			sb.AppendLine("    {");
			sb.AppendLine("        PrimaryKey = r.GetPK(),");
			sb.AppendLine("        r.status");
			sb.AppendLine("    });");
			sb.AppendLine();
			sb.AppendLine("recordsWithFallbackPK.Dump();");
			sb.AppendLine("```");
			sb.AppendLine();
		}

		private static void AppendNativeClientExample(StringBuilder sb)
		{
			sb.AppendLine("### Use the native Aerospike client");
			sb.AppendLine();
			sb.AppendLine("```csharp");
			sb.AppendLine("var client = AerospikeClient;");
			sb.AppendLine("client.Dump();");
			sb.AppendLine("```");
			sb.AppendLine();
		}

		private void AppendFooter(StringBuilder sb, AerospikeAIContextOptions options)
		{
			sb.AppendLine("## AI Query Guidance");
			sb.AppendLine();
			sb.AppendLine("- Prefer bounded queries.");
			sb.AppendLine("- Use secondary indexes when available.");
			sb.AppendLine("- Treat bin/type information as inferred because Aerospike is schemaless.");
			sb.AppendLine("- Prefer generated record properties over string-indexer bin access.");
			sb.AppendLine("- Use `PK` for the primary key when available; otherwise use `GetPK()`.");
			sb.AppendLine("- Use `.AsEnumerable()` before collection-style LINQ operations on `SetRecords` instances.");

			if(options.LinqSyntaxPreference == AerospikeLinqSyntaxPreference.MethodSyntax)
			{
				sb.AppendLine("- Preferred LINQ syntax for generated code: method syntax.");
			}
			else
			{
				sb.AppendLine("- Preferred LINQ syntax for generated code: query syntax for query logic; method syntax only for terminal/non-query operations or operations query syntax cannot express cleanly.");
				sb.AppendLine("- For joins, use a query-syntax `join` clause instead of `.Join(...)` whenever possible.");
			}

			sb.AppendLine("- Avoid destructive operations unless explicitly requested.");
			sb.AppendLine();
		}

		private static void AppendKeyValue(StringBuilder sb, string key, object value)
		{
			if(value is null)
			{
				return;
			}

			sb.AppendLine($"- {key}: `{Safe(value)}`");
		}

		private static string TrimToMaxChars(string value, int maxChars)
		{
			if(maxChars <= 0 || value.Length <= maxChars)
			{
				return value;
			}

			return value.Substring(0, maxChars)
				+ Environment.NewLine
				+ Environment.NewLine
				+ $"_AI context truncated at {maxChars:n0} characters._";
		}

		private static string GetGeneratedPropertyName(LPSet.BinType bin)
		{
			if(bin is null)
			{
				return null;
			}

			
			return Helpers.CheckName(bin.BinName, "Bin");
		}

		

		private static string Safe(object value)
		{
			return value?.ToString()?.Replace("`", "\\`") ?? string.Empty;
		}
	}
}