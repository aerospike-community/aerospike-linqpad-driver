using Aerospike.Client;
using LINQPad;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

		private const string DriverRepositoryUrl =
			"https://github.com/aerospike-community/aerospike-linqpad-driver";

		private const string DriverRepositoryName =
			"aerospike-community/aerospike-linqpad-driver";

		private const string AValueReadmeFileName =
			"AValues_Readme.md";

		private const string AutoValuesBlogUrl =
			"https://aerospike.com/developer/blog/how-to-use-auto-values-in-nosql-linqpad-driver";

		private static Dictionary<string, string> CreateMarkdownReplacements()
		{
			return new Dictionary<string, string>(StringComparer.Ordinal)
			{
				[nameof(DriverRepositoryUrl)] = DriverRepositoryUrl,
				[nameof(DriverRepositoryName)] = DriverRepositoryName,
				[nameof(AValueReadmeFileName)] = AValueReadmeFileName,
				[nameof(AutoValuesBlogUrl)] = AutoValuesBlogUrl,
				["DefaultASPIKeyName"] = ARecord.DefaultASPIKeyName
			};
		}

		private static Dictionary<string, string> CreateMarkdownReplacements(
			params (string Key, string Value)[] additionalValues)
		{
			var replacements = CreateMarkdownReplacements();

			if(additionalValues is not null)
			{
				foreach(var additionalValue in additionalValues)
				{
					replacements[additionalValue.Key] = additionalValue.Value ?? string.Empty;
				}
			}

			return replacements;
		}

		private static void AppendMarkdownResource(
			StringBuilder sb,
			string fileName,
			IReadOnlyDictionary<string, string> replacements = null)
		{
			sb.Append(EmbeddedMarkdownLoader.LoadAndReplace(fileName, replacements));
		}

		private AerospikeAIContext(AClusterAccess cluster)
		{
			this.cluster = cluster ?? throw new ArgumentNullException(nameof(cluster));

			this.connection = cluster.AerospikeConnection
				?? throw new InvalidOperationException("The Aerospike LINQPad connection is not available.");			
		}

		/// <summary>
		/// Creates a context builder bound to the provided cluster access instance.
		/// </summary>
		/// <param name="cluster">The cluster access wrapper that exposes connection metadata and runtime state.</param>
		/// <returns>A new <see cref="AerospikeAIContext"/> instance.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="cluster"/> is <see langword="null"/>.</exception>
		/// <exception cref="InvalidOperationException">Thrown when the underlying Aerospike connection is unavailable.</exception>
		public static AerospikeAIContext From(AClusterAccess cluster)
		{
			return new AerospikeAIContext(cluster);
		}

		/// <summary>
		/// Creates a context builder from the current global LINQPad cluster instance.
		/// </summary>
		/// <returns>A new <see cref="AerospikeAIContext"/> instance for <c>AClusterAccess.Instance</c>.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown when no current cluster instance is registered for the active LINQPad connection.
		/// </exception>
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
		/// Builds Markdown context suitable for display or inclusion in an AI prompt.
		/// </summary>
		/// <param name="options">
		/// Optional generation controls for sections, truncation limits, metadata filtering, and syntax preference.
		/// </param>
		/// <returns>A Markdown document representing connection guidance and metadata.</returns>
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
		/// Submits a request to the configured AI provider using the generated Aerospike context prompt.
		/// </summary>
		/// <param name="userRequest">The natural-language user request appended to the generated prompt.</param>
		/// <param name="options">Optional context-generation options.</param>
		/// <param name="systemInstruction">Optional system instruction override; defaults to embedded guidance.</param>
		/// <param name="progression">Reserved for compatibility with progressive-response workflows.</param>
		/// <returns>The AI response text, or <see langword="null"/> when the request/response is blank.</returns>
		public string SubmitRequest(string userRequest,
									AerospikeAIContextOptions options = null,
									string systemInstruction = null,
									bool progression = true)
			=> SubmitRequestAsync(userRequest, options, systemInstruction, progression).Result;

		/// <summary>
		/// Asynchronously submits a request to the configured AI provider using the generated Aerospike context prompt.
		/// </summary>
		/// <param name="userRequest">The natural-language user request appended to the generated prompt.</param>
		/// <param name="options">Optional context-generation options.</param>
		/// <param name="systemInstruction">Optional system instruction override; defaults to embedded guidance.</param>
		/// <param name="progression">Reserved for compatibility with progressive-response workflows.</param>
		/// <param name="cancellationToken">A cancellation token for cooperative cancellation.</param>
		/// <returns>
		/// A task that resolves to the AI response text, or <see langword="null"/> when the request/response is blank.
		/// </returns>
		public async System.Threading.Tasks.Task<string> SubmitRequestAsync(
			string userRequest,
			AerospikeAIContextOptions options = null,
			string systemInstruction = null,
			bool progression = true,
			CancellationToken cancellationToken = default)
		{
			if(String.IsNullOrWhiteSpace(userRequest)) return null;

			var prompt = BuildPrompt(userRequest.Trim(), options, systemInstruction);
			var response = await AerospikeAIContextExtensions.Ask(prompt);

			if(String.IsNullOrWhiteSpace(response))
			{
				return null;
			}

			return response;
		}

		/// <summary>
		/// Submits a request through <see cref="LINQPadAIGeneratedQuery"/> and creates a generated LINQPad query when applicable.
		/// </summary>
		/// <param name="userRequest">The natural-language request to send to the AI service.</param>
		/// <param name="options">Optional context-generation settings used to build the prompt.</param>
		/// <param name="systemInstruction">Optional system instruction override for the AI prompt.</param>
		/// <param name="progression">Reserved for compatibility with progressive response workflows.</param>
		/// <param name="cancellationToken">A token that can be used to cancel the request.</param>
		/// <returns>
		/// A task that resolves to the raw AI response text returned by the submission pipeline.
		/// </returns>
		/// <remarks>
		/// This method is intended for interactive LINQPad use. It sends the request through
		/// <see cref="SubmitRequestAsync(string, bool, CancellationToken)"/>, classifies the
		/// AI response, and creates a connected generated query when runnable C# is detected.
		///
		/// When the current LINQPad query has been saved, the generated query copies the current
		/// query header so the same Aerospike connection is reused. When the current query has
		/// not been saved, the generated query is still created as a C# Statements query, but
		/// it may not automatically include the current Aerospike connection.
		///
		/// The generated query target is LINQPad <c>C# Statements</c>, not <c>C# Program</c>.
		///
		/// <para>
		/// Example: generate a query from the current Aerospike connection.
		/// </para>
		/// <code>
		/// var response = await AIContext.SubmitRequestAndCreateQueryAsync(
		///     "Generate a query-syntax LINQPad C# query that shows 100 customers.");
		///
		/// response.Dump("Raw AI response");
		/// </code>
		///
		/// <para>
		/// Example: generate a query-syntax join.
		/// </para>
		/// <code>
		/// await AIContext.SubmitRequestAndCreateQueryAsync(
		///     "Generate a LINQPad C# Statements query using query syntax. " +
		///     "Join test.Customer and test.Invoice using customer.PK and invoice.CustomerId. " +
		///     "Use AsEnumerable() on both sets, project customer and invoice fields, " +
		///     "limit to 100 rows, and Dump the result.");
		/// </code>
		///
		/// <para>
		/// Example: generate AValue-safe code.
		/// </para>
		/// <code>
		/// await AIContext.SubmitRequestAndCreateQueryAsync(
		///     "Generate LINQPad C# Statements examples for AValue-backed properties. " +
		///     "Use query syntax where practical. Use test.Customer.AsEnumerable(). " +
		///     "Show FirstName.TryApply&lt;string,bool&gt;(name =&gt; name.StartsWith(\"a\")), " +
		///     "FirstName.Apply&lt;string,int&gt;(name =&gt; name.Length), " +
		///     "and CanConvert&lt;decimal&gt;()/Convert&lt;decimal&gt;(). " +
		///     "Use generated properties, limit each example to 100 rows, and Dump().");
		/// </code>
		///
		/// <para>
		/// Example: request an explanation instead of code. In this case the method displays
		/// the explanation but does not create a generated query file.
		/// </para>
		/// <code>
		/// var explanation = await AIContext.SubmitRequestAndCreateQueryAsync(
		///     "Explain whether this query is client-side LINQ or a server-side Aerospike expression: " +
		///     "from customer in test.Customer.AsEnumerable() " +
		///     "where customer.FirstName.TryApply&lt;string,bool&gt;(name =&gt; name.StartsWith(\"a\")) " +
		///     "select customer");
		/// </code>
		/// </remarks>
		public Task<string> SubmitRequestAndCreateQueryAsync(
			string userRequest,
			AerospikeAIContextOptions options = null,
			string systemInstruction = null,
			bool progression = true,
			CancellationToken cancellationToken = default)
		{
			return LINQPadAIGeneratedQuery.SubmitRequestAsync(
						this,
						userRequest,
						options,
						systemInstruction,
						progression,
						cancellationToken);
		}

		/// <summary>
		/// Submits a request through <see cref="LINQPadAIGeneratedQuery"/> and synchronously creates a generated LINQPad query when applicable.
		/// </summary>
		/// <param name="userRequest">The natural-language request to send to the AI service.</param>
		/// <param name="options">Optional context-generation settings used to build the prompt.</param>
		/// <param name="systemInstruction">Optional system instruction override for the AI prompt.</param>
		/// <param name="progression">Reserved for compatibility with progressive response workflows.</param>
		/// <returns>
		/// The raw AI response text returned by the submission pipeline, or <see langword="null"/> when no response is produced.
		/// </returns>
		/// <remarks>
		/// This is a synchronous wrapper over <see cref="SubmitRequestAndCreateQueryAsync(string, AerospikeAIContextOptions, string, bool, CancellationToken)"/>.
		/// </remarks>
		public string SubmitRequestAndCreateQuery(
			string userRequest,
			AerospikeAIContextOptions options = null,
			string systemInstruction = null,
			bool progression = true) => SubmitRequestAndCreateQueryAsync(userRequest, options, systemInstruction, progression).Result;

		private const string DefaultUserRequest =
	"This should generate a safe, read-only LINQPad C# Statements query that explores the Aerospike Cluster associated with this connection. " +
	"Use the available namespace, set, bin, index, AValue, APrimaryKey, and expression context. " +
	"Prefer query syntax when practical, use generated properties instead of string-indexer access, " +
	"use PK for primary keys when available, call AsEnumerable() for LINQ collection operations, " +
	"limit output to 100 records, and display results with Dump().";

		/// <summary>
		/// Builds a complete AI prompt by combining system instruction, generated Aerospike Markdown context, and user request.
		/// </summary>
		/// <param name="userRequest">
		/// The request to append to the prompt. When <see langword="null"/>, a safe read-only default request is used.
		/// </param>
		/// <param name="options">Optional context-generation options.</param>
		/// <param name="systemInstruction">
		/// Optional system instruction override. When omitted, embedded instruction content is selected by syntax preference.
		/// </param>
		/// <returns>A complete prompt string ready to send to an AI completion API.</returns>
		public string BuildPrompt(
			string userRequest,
			AerospikeAIContextOptions options = null,
			string systemInstruction = null)
		{
			if(userRequest is null)
			{
				userRequest = DefaultUserRequest;
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
		/// Builds a scoped AI prompt for a single namespace and set, reducing unrelated metadata.
		/// </summary>
		/// <param name="namespaceName">Target namespace name (or generated safe namespace name).</param>
		/// <param name="setName">Target set name (or generated safe set name).</param>
		/// <param name="userRequest">The request to append to the scoped prompt.</param>
		/// <param name="options">Optional context-generation options that are constrained to the specified namespace/set.</param>
		/// <param name="systemInstruction">Optional system instruction override.</param>
		/// <returns>A prompt string scoped to the requested namespace and set.</returns>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="namespaceName"/> or <paramref name="setName"/> is blank.
		/// </exception>
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
			var fileName =
				options.LinqSyntaxPreference == AerospikeLinqSyntaxPreference.MethodSyntax
					? "SystemInstruction.MethodSyntax.md"
					: "SystemInstruction.QuerySyntax.md";

			return EmbeddedMarkdownLoader.LoadAndReplace(
				fileName,
				CreateMarkdownReplacements());
		}

		private void AppendHeader(StringBuilder sb)
		{
			AppendMarkdownResource(
				sb,
				"Header.md",
				CreateMarkdownReplacements());
		}

		private void AppendDriverGuide(StringBuilder sb, AerospikeAIContextOptions options)
		{
			var fileName =
				options.LinqSyntaxPreference == AerospikeLinqSyntaxPreference.MethodSyntax
					? "DriverGuide.MethodSyntax.md"
					: "DriverGuide.QuerySyntax.md";

			AppendMarkdownResource(
				sb,
				fileName,
				CreateMarkdownReplacements(
					("AlwaysUseAValues", connection.AlwaysUseAValues.ToString())));
		}

		private void AppendClusterSummary(StringBuilder sb)
		{
			sb.AppendLine("## Cluster Summary");
			sb.AppendLine();

			AppendKeyValue(sb, "Cluster name", connection.Database);
			AppendKeyValue(sb, "Connection string", connection.ConnectionString);
			AppendKeyValue(sb, "Record view", connection.RecordView);
			AppendKeyValue(sb, "Document API enabled", connection.DocumentAPI);
			AppendKeyValue(sb, "Always use AValue / AutoValue behavior", connection.AlwaysUseAValues);
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

			sb.AppendLine($"- Primary key access: prefer generated/default property `{ARecord.DefaultASPIKeyName}` when available; otherwise use `GetPK()`.");
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

			AppendMarkdownResource(sb, "Examples.General.md");

			AppendMarkdownResource(
				sb,
				options.LinqSyntaxPreference == AerospikeLinqSyntaxPreference.MethodSyntax
					? "Examples.MethodSyntax.md"
					: "Examples.QuerySyntax.md",
				CreateMarkdownReplacements());

			AppendMarkdownResource(sb, "Examples.NativeClient.md");
		}

		private void AppendFooter(StringBuilder sb, AerospikeAIContextOptions options)
		{
			var alwaysUseAValuesGuidance = connection.AlwaysUseAValues
				? "- `Always use AValue` is enabled; respect AValue-backed property behavior and avoid unsafe primitive casts."
				: "- `Always use AValue` is disabled; use generated typed properties normally when metadata provides concrete CLR types.";

			var linqSyntaxGuidance =
				options.LinqSyntaxPreference == AerospikeLinqSyntaxPreference.MethodSyntax
					? "- Preferred LINQ syntax for generated code: method syntax."
					: "- Preferred LINQ syntax for generated code: query syntax for query logic; method syntax only for terminal/non-query operations or operations query syntax cannot express cleanly."
						+ Environment.NewLine
						+ "- For joins, use a query-syntax `join` clause instead of `.Join(...)` whenever possible.";

			AppendMarkdownResource(
				sb,
				"Footer.md",
				CreateMarkdownReplacements(
					("AlwaysUseAValuesGuidance", alwaysUseAValuesGuidance),
					("LinqSyntaxGuidance", linqSyntaxGuidance)));
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

			return string.Concat(value.AsSpan(0, maxChars)
, Environment.NewLine
, Environment.NewLine
, $"_AI context truncated at {maxChars:n0} characters._");
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