namespace Aerospike.Database.LINQPadDriver.Extensions
{
	/// <summary>
	/// Preferred LINQ syntax style for AI-generated LINQPad queries.
	/// </summary>
	public enum AerospikeLinqSyntaxPreference
	{
		/// <summary>
		/// Prefer LINQ query syntax whenever practical.
		///
		/// The AI context will steer generated code toward C# query syntax using
		/// from, where, orderby, join, group, and select clauses.
		///
		/// Method syntax may still be used when query syntax cannot express the
		/// operation cleanly, or for terminal operations such as Take, Count,
		/// FirstOrDefault, ToList, and Dump.
		/// </summary>
		QuerySyntax = 0,

		/// <summary>
		/// Prefer LINQ method-chain syntax.
		///
		/// The AI context will steer generated code toward chained methods such as
		/// Where(...), OrderBy(...), Select(...), Join(...), GroupBy(...),
		/// Take(...), and Dump().
		/// </summary>
		MethodSyntax = 1
	}

	/// <summary>
	/// Controls how much Aerospike/LINQPad metadata and usage guidance is generated
	/// for AI prompts.
	///
	/// The generated context is plain Markdown text. It can be displayed in LINQPad
	/// or passed to LINQPad AI through Util.AI.Ask(...).
	/// </summary>
	public sealed class AerospikeAIContextOptions
	{
		/// <summary>
		/// Includes general guidance explaining how the Aerospike LINQPad driver should be used.
		///
		/// This section tells AI to generate LINQPad C# statements, use Dump() for output,
		/// prefer generated namespace/set members, keep scans bounded, and avoid destructive
		/// operations unless explicitly requested.
		///
		/// Default: true.
		/// </summary>
		public bool IncludeDriverGuide { get; set; } = true;

		/// <summary>
		/// Includes a high-level summary of the current Aerospike connection.
		///
		/// This may include cluster name, connection string, record view settings,
		/// Document API settings, AValue settings, user-key behavior, compression settings,
		/// server version, node list, and detected server/driver features.
		///
		/// Default: true.
		/// </summary>
		public bool IncludeClusterSummary { get; set; } = true;

		/// <summary>
		/// Includes namespace metadata from the current Aerospike connection.
		///
		/// Namespace metadata may include namespace names, generated C# namespace-access names,
		/// strong-consistency status, namespace-level observed bins, and selected namespace
		/// configuration values.
		///
		/// Default: true.
		/// </summary>
		public bool IncludeNamespaces { get; set; } = true;

		/// <summary>
		/// Includes set metadata under each included namespace.
		///
		/// Set metadata may include raw set names, generated C# set property names,
		/// recommended access patterns, null-set/vector-index hints, primary-key guidance,
		/// record-bin access guidance, and LINQ AsEnumerable() guidance.
		///
		/// This option only has an effect when IncludeNamespaces is also true.
		///
		/// Default: true.
		/// </summary>
		public bool IncludeSets { get; set; } = true;

		/// <summary>
		/// Includes observed bin metadata under each included set.
		///
		/// Bin metadata may include raw Aerospike bin name, observed/inferred .NET type,
		/// generated C# property name, duplicate-type hints, sparse-bin hints,
		/// detected-after-scan hints, and foreign/reference hints.
		///
		/// This is one of the most important sections for AI code generation because it tells
		/// the model to prefer generated properties such as customer.userid over string-indexer
		/// access such as customer["userid"].
		///
		/// This option only has an effect when IncludeNamespaces and IncludeSets are also true.
		///
		/// Default: true.
		/// </summary>
		public bool IncludeBins { get; set; } = true;

		/// <summary>
		/// Includes secondary-index metadata under each included set.
		///
		/// Secondary-index metadata may include index name, indexed bin, index type,
		/// collection/index context, and related type information.
		///
		/// This helps AI prefer indexed query shapes when possible.
		///
		/// This option only has an effect when IncludeNamespaces and IncludeSets are also true.
		///
		/// Default: true.
		/// </summary>
		public bool IncludeSecondaryIndexes { get; set; } = true;

		/// <summary>
		/// Includes UDF module metadata.
		///
		/// UDF metadata may include module names, module types, package names, and UDF names.
		///
		/// This is disabled by default because most AI query-generation prompts are focused on
		/// read/query exploration rather than invoking server-side Lua UDFs.
		///
		/// Default: false.
		/// </summary>
		public bool IncludeUdfs { get; set; } = false;

		/// <summary>
		/// Includes canonical LINQPad examples in the generated AI context.
		///
		/// Examples show how to display the AI context, call Util.AI.Ask(...), query sets,
		/// filter by generated record properties, use AsEnumerable() for LINQ collection
		/// operations, join sets, group records, access primary keys, use the native
		/// Aerospike client, and optionally include data-operation examples controlled by
		/// IncludeDataOperationExamples.
		///
		/// Examples are generated according to LinqSyntaxPreference.
		///
		/// Default: true.
		/// </summary>
		public bool IncludeExamples { get; set; } = true;

		/// <summary>
		/// Includes canonical examples for non-read-only data operations such as insert,
		/// update, delete, import, export, copy, put, and native-client write operations.
		///
		/// These examples are intentionally safety-oriented. They should show preview/bounded
		/// selection logic before mutation where practical, require explicit user intent for
		/// destructive operations, and make namespace, set, bin, and primary-key sources explicit.
		///
		/// Default: true.
		/// </summary>
		public bool IncludeDataOperationExamples { get; set; } = true;

		/// <summary>
		/// Controls whether generated AI guidance and examples prefer LINQ query syntax
		/// or LINQ method syntax.
		///
		/// QuerySyntax tells AI to prefer C# query expressions such as:
		///
		/// from r in NamespaceName.SetName.AsEnumerable()
		/// where r.status == "active"
		/// orderby r.PK
		/// select r
		///
		/// MethodSyntax tells AI to prefer chained LINQ methods such as:
		///
		/// NamespaceName.SetName.AsEnumerable()
		///     .Where(r => r.status == "active")
		///     .OrderBy(r => r.PK)
		///     .Select(r => r)
		///
		/// Regardless of this setting, SetRecords instances should call AsEnumerable()
		/// before collection-style LINQ operations such as Join, OrderBy, GroupBy, and SelectMany.
		///
		/// Default: QuerySyntax.
		/// </summary>
		public AerospikeLinqSyntaxPreference LinqSyntaxPreference { get; set; }
			= AerospikeLinqSyntaxPreference.QuerySyntax;

		/// <summary>
		/// Controls whether generated AI code should include concise inline comments that explain
		/// important steps, mode choices, expression filters, nested CDT traversal, conversions,
		/// and safety boundaries.
		///
		/// This does not disable the short request-summary comment block at the top of generated
		/// scripts; that summary remains enabled so generated queries document their intent.
		///
		/// Default: true.
		/// </summary>
		public bool IncludeInlineComments { get; set; } = true;

		/// <summary>
		/// Forces the driver to refresh Aerospike metadata before building the AI context.
		///
		/// When false, the context builder uses the driver's existing cached metadata when available.
		/// When true, it requests a fresh metadata load through AerospikeConnection.GetDBInfo(true).
		///
		/// Default: false.
		/// </summary>
		public bool ForceRefreshMetadata { get; set; } = false;

		/// <summary>
		/// Maximum number of namespaces to include in the generated AI context.
		///
		/// Default: 25.
		/// </summary>
		public int MaxNamespaces { get; set; } = 25;

		/// <summary>
		/// Maximum number of sets to include per namespace.
		///
		/// Default: 25.
		/// </summary>
		public int MaxSetsPerNamespace { get; set; } = 25;

		/// <summary>
		/// Maximum number of bins to include per set.
		///
		/// Default: 75.
		/// </summary>
		public int MaxBinsPerSet { get; set; } = 75;

		/// <summary>
		/// Maximum number of characters in the generated Markdown context.
		///
		/// If the generated context exceeds this limit, it is truncated and a truncation
		/// note is appended.
		///
		/// Default: 100,000.
		/// </summary>
		public int MaxChars { get; set; } = 100_000;

		/// <summary>
		/// Controls whether LINQPad AI submission helpers should display a visible warning
		/// when the generated AI context exceeds MaxChars and is truncated.
		///
		/// This warning is intended to make missing schema/rules/examples obvious before
		/// the request is sent to the AI provider.
		///
		/// Default: true.
		/// </summary>
		public bool DumpTruncationWarning { get; set; } = true;

		/// <summary>
		/// Optional namespace filter.
		///
		/// When set, only the namespace with a matching raw Aerospike namespace name or
		/// generated C# namespace-access name is included. Matching is case-insensitive.
		///
		/// Default: null, meaning all namespaces up to MaxNamespaces are included.
		/// </summary>
		public string NamespaceName { get; set; }

		/// <summary>
		/// Optional set filter.
		///
		/// When set, only the set with a matching raw Aerospike set name or generated C# set
		/// property name is included. Matching is case-insensitive.
		///
		/// NamespaceName should normally also be set so the filter is scoped to the intended
		/// namespace.
		///
		/// Default: null, meaning all sets up to MaxSetsPerNamespace are included.
		/// </summary>
		public string SetName { get; set; }
	}
}