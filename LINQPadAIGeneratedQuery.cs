using Aerospike.Client;
using LINQPad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
	/// <summary>
	/// LINQPad AI helper that submits an AI request, classifies the response,
	/// and creates a C# Statements query file when the AI response contains runnable C# code.
	/// </summary>
	public static partial class LINQPadAIGeneratedQuery
	{
		/// <summary>
		/// Sends a natural-language request to LINQPad AI, inspects the returned content,
		/// and, when runnable C# is detected, creates a new C# Statements query.
		/// Driver-mode queries may copy the current LINQPad connection metadata.
		/// Native Aerospike client queries are generated as standalone LINQPad scripts with an Aerospike.Client NuGet reference.
		/// </summary>
		public static async Task<string> SubmitRequestAsync(
				AerospikeAIContext aiContext,
				string userRequest,
				AerospikeAIContextOptions options = null,
				string systemInstruction = null,
				bool progression = true,
				CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(aiContext);

			if(string.IsNullOrWhiteSpace(userRequest))
				throw new ArgumentException("AI request cannot be blank.", nameof(userRequest));

			_ = new
			{
				Version = AIContextVersion.Current
			}.Dump("AI Context");

			DumpAIContextTruncationWarningIfNeeded(aiContext, options);

			var aiUserRequest = StripAIRequestCommentLines(userRequest);
			if(string.IsNullOrWhiteSpace(aiUserRequest))
				throw new ArgumentException("AI request cannot be blank after removing comment lines.", nameof(userRequest));

			EnsureDriverModeAValueEnabled(aiUserRequest);

			var response = await aiContext
				.SubmitRequestAsync(
						aiUserRequest,
						options,
						systemInstruction,
						progression,
						cancellationToken)
				.ConfigureAwait(true);

			DumpAIResponse(response);

			var responseKind = ClassifyAIResponse(response);

			if(responseKind != AIResponseKind.RunnableCSharp)
			{
				_ = "AI response appears to be explanatory text, not a generated runnable C# query. No generated query file was created."
					.Dump("Generated Query");

				return response;
			}

			var csharpCode = ExtractCSharpCode(response);

			if(string.IsNullOrWhiteSpace(csharpCode))
			{
				_ = "No runnable C# code block was detected in the AI response."
					.Dump("Generated Query");

				return response;
			}

			var queryMode = DetermineGeneratedQueryMode(aiUserRequest, response, csharpCode, out var modeWarning);

			if(!string.IsNullOrWhiteSpace(modeWarning))
			{
				_ = modeWarning.Dump("AI Generation Mode Warning");
			}

			if(queryMode == AIGeneratedQueryMode.NativeAerospikeClient)
			{
				_ = "Using Native Mode: the generated .linq file will not copy LINQPad driver connection metadata, will reference the Aerospike.Client NuGet package, and should create an explicit AerospikeClient connection."
					.Dump("AI Generation Mode");
			}
			else
			{
				_ = "Using LINQPad Driver Mode: the generated .linq file may copy the current Aerospike LINQPad connection metadata when available."
					.Dump("AI Generation Mode");
			}

			var generatedQueryPath = CreateGeneratedQuery(
				csharpCode,
				queryMode,
				out var copiedCurrentConnection);

			var linkText = queryMode == AIGeneratedQueryMode.NativeAerospikeClient
				? "Open generated native Aerospike C# Statements query"
				: copiedCurrentConnection
					? "Open generated C# Statements query with this Aerospike connection"
					: "Open generated C# Statements query";

			_ = new Hyperlinq(generatedQueryPath, linkText)
				.Dump("Generated Query");

			if(queryMode == AIGeneratedQueryMode.NativeAerospikeClient)
			{
				_ = "Review the native AerospikeClient host, port, TLS, authentication, and policy placeholders before running. " +
					"The generated .linq file includes a NuGet reference to Aerospike.Client and does not copy LINQPad driver connection metadata."
					.Dump("Native Connection Notice");
			}
			else if(!copiedCurrentConnection)
			{
				_ = "The generated query was created, but the current LINQPad connection could not be copied. " +
					"Save the AI query first and run it again if you want generated queries to automatically reuse the Aerospike connection."
					.Dump("Connection Notice");
			}

			return response;
		}

		private static string StripAIRequestCommentLines(string request)
		{
			if(string.IsNullOrWhiteSpace(request))
				return request;

			var filteredLines = request
				.Replace("\r\n", "\n")
				.Replace('\r', '\n')
				.Split('\n')
				.Where(line =>
				{
					var trimmed = line.TrimStart();
					return !(trimmed.StartsWith("//", StringComparison.Ordinal)
						|| trimmed.StartsWith("#", StringComparison.Ordinal)
						|| trimmed.StartsWith("--", StringComparison.Ordinal));
				});

			return string.Join(Environment.NewLine, filteredLines).Trim();
		}

		private static void EnsureDriverModeAValueEnabled(string aiUserRequest)
		{
			if(string.IsNullOrWhiteSpace(aiUserRequest))
				return;

			if(TryGetExplicitModeOverride(aiUserRequest, out var overrideMode)
				&& overrideMode == AIGeneratedQueryMode.NativeAerospikeClient)
			{
				return;
			}

			if(HasStrongNativeModeCue(aiUserRequest)
				|| (HasMediumNativeModeCue(aiUserRequest) && !HasDriverModeCue(aiUserRequest)))
			{
				return;
			}

			var connection = AClusterAccess.Instance?.AerospikeConnection;
			var guidance = "Enable 'Always use AValue' in the Aerospike connection properties dialog, then rerun the AI request.";

			if(connection is null)
			{
				var missingConnectionMessage = "Driver-mode AI guardrail blocked request: no active Aerospike LINQPad connection is available to verify AValue mode. " + guidance;
				_ = missingConnectionMessage.Dump("AI Guardrail Error");
				throw new InvalidOperationException(missingConnectionMessage);
			}

			if(connection.AlwaysUseAValues)
				return;

			var message = "Driver-mode AI guardrail blocked request: 'Always use AValue' is disabled for the current connection. " + guidance;
			_ = message.Dump("AI Guardrail Error");
			throw new InvalidOperationException(message);
		}

		private enum AIResponseKind
		{
			None,
			Explanation,
			RunnableCSharp
		}

		private enum AIGeneratedQueryMode
		{
			Driver,
			NativeAerospikeClient
		}

		private sealed class FencedCodeBlock
		{
			public string Language { get; set; }

			public string Code { get; set; }
		}

		private static AIResponseKind ClassifyAIResponse(string responseText)
		{
			if(string.IsNullOrWhiteSpace(responseText))
				return AIResponseKind.None;

			var codeBlocks = GetFencedCodeBlocks(responseText).ToList();

			if(codeBlocks.Count == 1)
			{
				var code = NormalizeCSharpStatements(codeBlocks[0].Code);

				if(LooksLikeRunnableCSharp(code))
					return AIResponseKind.RunnableCSharp;

				return AIResponseKind.Explanation;
			}

			if(codeBlocks.Count > 1)
			{
				var prose = RemoveFencedCodeBlocks(responseText).Trim();

				if(prose.Length < 300
					&& codeBlocks.All(block => LooksLikeRunnableCSharp(NormalizeCSharpStatements(block.Code))))
				{
					return AIResponseKind.RunnableCSharp;
				}

				return AIResponseKind.Explanation;
			}

			if(LooksLikeRunnableCSharp(responseText))
				return AIResponseKind.RunnableCSharp;

			return AIResponseKind.Explanation;
		}

		private static string ExtractCSharpCode(string responseText)
		{
			if(string.IsNullOrWhiteSpace(responseText))
				return null;

			var fullLinqCode = ExtractCodeFromFullLinqFile(responseText);

			if(!string.IsNullOrWhiteSpace(fullLinqCode))
				return NormalizeCSharpStatements(fullLinqCode);

			var fencedCode = ExtractFencedCSharpCode(responseText);

			if(!string.IsNullOrWhiteSpace(fencedCode))
				return NormalizeCSharpStatements(fencedCode);

			if(LooksLikeRunnableCSharp(responseText))
				return NormalizeCSharpStatements(responseText);

			return null;
		}

		private static string ExtractFencedCSharpCode(string responseText)
		{
			var codeBlocks = GetFencedCodeBlocks(responseText)
				.Select(block => NormalizeCSharpStatements(block.Code))
				.Where(code => !string.IsNullOrWhiteSpace(code))
				.ToList();

			if(codeBlocks.Count == 0)
				return null;

			return string.Join(
				Environment.NewLine + Environment.NewLine,
				codeBlocks);
		}

		[GeneratedRegex(@"```(?<lang>csharp|cs|c#|CSharp|C#|linq)?\s*(?<code>[\s\S]*?)```", RegexOptions.IgnoreCase, "en-US")]
		private static partial Regex FindLangBlockRegex();

		private static IEnumerable<FencedCodeBlock> GetFencedCodeBlocks(string responseText)
		{
			if(string.IsNullOrWhiteSpace(responseText))
				yield break;

			var matches = FindLangBlockRegex().Matches(responseText);

			foreach(Match match in matches)
			{
				var lang = match.Groups["lang"].Value;
				var code = match.Groups["code"].Value;

				if(string.IsNullOrWhiteSpace(lang)
					|| string.Equals(lang, "csharp", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(lang, "cs", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(lang, "c#", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(lang, "linq", StringComparison.OrdinalIgnoreCase))
				{
					yield return new FencedCodeBlock
					{
						Language = lang,
						Code = code
					};
				}
			}
		}

		[GeneratedRegex(@"```[\s\S]*?```", RegexOptions.IgnoreCase, "en-US")]
		private static partial Regex RemoveFencedCodeBlkRegex();

		private static string RemoveFencedCodeBlocks(string responseText)
		{
			if(string.IsNullOrWhiteSpace(responseText))
				return string.Empty;

			return RemoveFencedCodeBlkRegex().Replace(responseText, string.Empty);
		}

		private static string ExtractCodeFromFullLinqFile(string text)
		{
			if(string.IsNullOrWhiteSpace(text))
				return null;

			var endTag = "</Query>";
			var endIndex = text.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

			if(endIndex < 0)
				return null;

			return text.Substring(endIndex + endTag.Length).Trim();
		}

		private static string NormalizeCSharpStatements(string code)
		{
			if(string.IsNullOrWhiteSpace(code))
				return null;

			code = code.Trim();

			var fullLinqCode = ExtractCodeFromFullLinqFile(code);

			if(!string.IsNullOrWhiteSpace(fullLinqCode))
				code = fullLinqCode.Trim();

			code = StripUsingStatements(code);
			code = StripMainWrapper(code);

			return code.Trim();
		}

		private static string StripUsingStatements(string code)
		{
			if(string.IsNullOrWhiteSpace(code))
				return code;

			var lines = code
				.Replace("\r\n", "\n")
				.Replace('\r', '\n')
				.Split('\n')
				.ToList();

			while(lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
				lines.RemoveAt(0);

			while(lines.Count > 0 && lines[0].TrimStart().StartsWith("using ", StringComparison.Ordinal))
				lines.RemoveAt(0);

			return string.Join(Environment.NewLine, lines);
		}

		[GeneratedRegex(@"^\s*(?:public\s+|private\s+|protected\s+|internal\s+|static\s+|async\s+)*\s*(?:Task|void)\s+Main\s*\([^)]*\)\s*\{(?<body>[\s\S]*)\}\s*$", RegexOptions.IgnoreCase, "en-US")]
		private static partial Regex StripMainRegex();

		private static string StripMainWrapper(string code)
		{
			if(string.IsNullOrWhiteSpace(code))
				return code;

			var match = StripMainRegex().Match(code);

			if(!match.Success)
				return code;

			var body = match.Groups["body"].Value;

			return Unindent(body).Trim();
		}

		private static string Unindent(string text)
		{
			if(string.IsNullOrWhiteSpace(text))
				return text;

			var lines = text
				.Replace("\r\n", "\n")
				.Replace('\r', '\n')
				.Split('\n');

			var nonBlankLines = lines
				.Where(line => !string.IsNullOrWhiteSpace(line))
				.ToArray();

			if(nonBlankLines.Length == 0)
				return text.Trim();

			var minIndent = nonBlankLines
				.Select(line => line.TakeWhile(char.IsWhiteSpace).Count())
				.Min();

			if(minIndent <= 0)
				return text.Trim();

			return string.Join(
				Environment.NewLine,
				lines.Select(line =>
					line.Length >= minIndent
						? line.Substring(minIndent)
						: line));
		}

		private static bool LooksLikeRunnableCSharp(string text)
		{
			if(string.IsNullOrWhiteSpace(text))
				return false;

			var trimmed = text.Trim();

			if(trimmed.StartsWith("<Query", StringComparison.OrdinalIgnoreCase))
				return true;

			var hasExecutableShape =
				trimmed.StartsWith("var ", StringComparison.Ordinal)
				|| trimmed.StartsWith("from ", StringComparison.Ordinal)
				|| trimmed.StartsWith("let ", StringComparison.Ordinal)
				|| trimmed.StartsWith("Client.Exp ", StringComparison.Ordinal)
				|| trimmed.StartsWith("Aerospike.Client.Exp ", StringComparison.Ordinal)
				|| trimmed.Contains(".Dump(")
				|| trimmed.Contains(" = ")
				|| trimmed.Contains(";");

			var hasQueryShape =
				trimmed.Contains("from ")
				|| trimmed.Contains("select ")
				|| trimmed.Contains("where ")
				|| trimmed.Contains("join ")
				|| trimmed.Contains("orderby ")
				|| trimmed.Contains("ScanAll(")
				|| trimmed.Contains("client.Query(")
				|| trimmed.Contains("new AerospikeClient")
				|| trimmed.Contains(".AsEnumerable()")
				|| trimmed.Contains(".Query(")
				|| trimmed.Contains(".Dump(");

			var looksLikeNarrativeSnippet =
				trimmed.StartsWith("test.", StringComparison.Ordinal)
				&& !trimmed.Contains(";")
				&& !trimmed.Contains(".Dump(");

			return hasExecutableShape && hasQueryShape && !looksLikeNarrativeSnippet;
		}

		private static AIGeneratedQueryMode DetermineGeneratedQueryMode(
			string userRequest,
			string responseText,
			string csharpCode,
			out string modeWarning)
		{
			modeWarning = null;
			var requestText = userRequest ?? string.Empty;

			if(TryGetExplicitModeOverride(requestText, out var overrideMode))
				return overrideMode;

			var hasStrongNativeCue = HasStrongNativeModeCue(requestText);
			var hasMediumNativeCue = HasMediumNativeModeCue(requestText);
			var hasDriverCue = HasDriverModeCue(requestText);

			if((hasStrongNativeCue || hasMediumNativeCue) && hasDriverCue)
			{
				modeWarning = "Mode conflict detected (native and driver cues). Defaulting to LINQPad Driver mode. Add mode:native or mode:driver to force mode selection.";
				return AIGeneratedQueryMode.Driver;
			}

			if(hasStrongNativeCue)
				return AIGeneratedQueryMode.NativeAerospikeClient;

			if(hasMediumNativeCue && !hasDriverCue)
				return AIGeneratedQueryMode.NativeAerospikeClient;

			return AIGeneratedQueryMode.Driver;
		}

		private static bool TryGetExplicitModeOverride(string text, out AIGeneratedQueryMode mode)
		{
			mode = AIGeneratedQueryMode.Driver;

			if(string.IsNullOrWhiteSpace(text))
				return false;

			if(text.Contains("mode:native", StringComparison.OrdinalIgnoreCase))
			{
				mode = AIGeneratedQueryMode.NativeAerospikeClient;
				return true;
			}

			if(text.Contains("mode:driver", StringComparison.OrdinalIgnoreCase))
			{
				mode = AIGeneratedQueryMode.Driver;
				return true;
			}

			return false;
		}

		private static bool HasStrongNativeModeCue(string text)
		{
			if(string.IsNullOrWhiteSpace(text))
				return false;

			return text.Contains("Aerospike native API", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("Aerospike native API driver", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("native Aerospike API", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("native Aerospike C# client", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("native C# client", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("native API driver", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("native driver", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("no LINQPad driver", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("total Aerospike native", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("AerospikeClient", StringComparison.OrdinalIgnoreCase);
		}

		private static bool HasMediumNativeModeCue(string text)
		{
			if(string.IsNullOrWhiteSpace(text))
				return false;

			if(!text.Contains("native API", StringComparison.OrdinalIgnoreCase))
				return false;

			return text.Contains("Aerospike", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("client", StringComparison.OrdinalIgnoreCase);
		}

		private static bool HasDriverModeCue(string text)
		{
			if(string.IsNullOrWhiteSpace(text))
				return false;

			return text.Contains("LINQPad driver", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("AsEnumerable()", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("SetRecords", StringComparison.OrdinalIgnoreCase)
				|| text.Contains("from ", StringComparison.OrdinalIgnoreCase);
		}

		private static bool TryGetCurrentQueryHeader(out string header)
		{
			header = null;

			try
			{
				var currentQueryPath = LINQPad.Util.CurrentQueryPath;

				if(string.IsNullOrWhiteSpace(currentQueryPath))
					return false;

				if(!File.Exists(currentQueryPath))
					return false;

				var currentQueryText = File.ReadAllText(currentQueryPath);

				header = ExtractQueryHeader(currentQueryText);

				return !string.IsNullOrWhiteSpace(header);
			}
			catch
			{
				header = null;
				return false;
			}
		}

		private static string CreateGeneratedQuery(
							string csharpCode,
							AIGeneratedQueryMode queryMode,
							out bool copiedCurrentConnection)
		{
			copiedCurrentConnection = false;

			if(string.IsNullOrWhiteSpace(csharpCode))
				throw new ArgumentException("C# code cannot be blank.", nameof(csharpCode));

			string generatedHeader;
			string warningComment = null;
			var normalizedCode = csharpCode;

			if(queryMode == AIGeneratedQueryMode.NativeAerospikeClient)
			{
				generatedHeader = EnsureNativeAerospikeClientHeader("<Query Kind=\"Statements\" />");
				normalizedCode = NormalizeNativeAerospikeClientCode(normalizedCode);
				warningComment =
					"// NOTE: Native Aerospike client mode. This query intentionally does not copy LINQPad driver connection metadata." + Environment.NewLine +
					"// Confirm host, port, TLS, authentication, and policy settings before running." + Environment.NewLine +
					"// The Aerospike.Client NuGet package is referenced in the LINQPad query header." + Environment.NewLine +
					Environment.NewLine;
			}
			else if(TryGetCurrentQueryHeader(out var currentHeader))
			{
				generatedHeader = EnsureStatementsQueryKind(currentHeader);
				generatedHeader = EnsureCommonNamespaces(generatedHeader);
				copiedCurrentConnection = true;
			}
			else
			{
				generatedHeader = EnsureCommonNamespaces("<Query Kind=\"Statements\" />");

				warningComment =
					"// NOTE: This query was generated without copying the current LINQPad connection." + Environment.NewLine +
					"// To preserve the Aerospike connection automatically, save the AI query first and run it again." + Environment.NewLine +
					"// You can still manually select the Aerospike connection in LINQPad." + Environment.NewLine +
					Environment.NewLine;
			}

			normalizedCode = NormalizeAerospikeExpressionAliasUsage(normalizedCode);
			normalizedCode = EnsureAIContextVersionComment(normalizedCode);

			var generatedQueryText =
				generatedHeader
				+ Environment.NewLine
				+ Environment.NewLine
				+ (warningComment ?? string.Empty)
				+ normalizedCode.Trim()
				+ Environment.NewLine;

			var outputFolder = Path.Combine(
				Path.GetTempPath(),
				"AerospikeLinqPadAI");

			Directory.CreateDirectory(outputFolder);

			var outputPath = Path.Combine(
				outputFolder,
				(queryMode == AIGeneratedQueryMode.NativeAerospikeClient
					? "Generated-Aerospike-Native-AI-Query-"
					: "Generated-Aerospike-AI-Query-")
						+ DateTime.Now.ToString("yyyyMMdd-HHmmss")
						+ ".linq");

			File.WriteAllText(outputPath, generatedQueryText, Encoding.UTF8);

			return outputPath;
		}

		private static string EnsureAIContextVersionComment(string code)
		{
			if(string.IsNullOrWhiteSpace(code))
				return code;

			var normalized = Regex.Replace(
				code.TrimStart(),
				@"(?m)^\s*//\s*-?\s*AI Context Version:.*(?:\r?\n)?",
				string.Empty);

			var lines = normalized
				.Replace("\r\n", "\n")
				.Replace('\r', '\n')
				.Split('\n')
				.ToList();

			while(lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
				lines.RemoveAt(0);

			var versionLine = $"// - AI Context Version: {AIContextVersion.Current}";

			if(lines.Count > 0 && lines[0].TrimStart().StartsWith("//", StringComparison.Ordinal))
			{
				var insertIndex = 0;

				while(insertIndex < lines.Count
					&& lines[insertIndex].TrimStart().StartsWith("//", StringComparison.Ordinal))
				{
					insertIndex++;
				}

				lines.Insert(insertIndex, versionLine);

				return string.Join(Environment.NewLine, lines).Trim();
			}

			var header = string.Join(
				Environment.NewLine,
				"// Request summary:",
				"// - AI-generated LINQPad C# Statements query.",
				versionLine,
				string.Empty);

			return header + string.Join(Environment.NewLine, lines).Trim();
		}

		[GeneratedRegex(@"(?m)^\s*var\s+client\s*=\s*\w+\.Client\s*;\s*$")]
		private static partial Regex NorNatCodeReplaceClientPol();

		[GeneratedRegex(@"(?m)^\s*var\s+host\s*=\s*\""[^\""\r\n]*\""\s*;\s*$")]
		private static partial Regex NorNatCodeReplaceHostLine();

		[GeneratedRegex(@"(?m)^\s*var\s+port\s*=\s*\d+\s*;\s*$")]
		private static partial Regex NorNatCodeReplacePortLine();

		private static readonly Regex NorNatCodeReplacePlaceholderNativeBlockRegex = new Regex(
			@"(?ms)^\s*var\s+host\s*=\s*\""[^\""\r\n]*\""\s*;\s*\r?\n\s*var\s+port\s*=\s*\d+\s*;\s*\r?\n(?:\s*\r?\n)?(?<namespaceSet>\s*var\s+namespaceName\s*=\s*\""[^\""\r\n]*\""\s*;\s*\r?\n\s*var\s+setName\s*=\s*\""[^\""\r\n]*\""\s*;\s*\r?\n)(?:\s*\r?\n)?\s*var\s+clientPolicy\s*=\s*new\s+ClientPolicy\s*\(\s*\)\s*;\s*\r?\n(?:\s*\r?\n)?\s*using\s+var\s+client\s*=\s*new\s+AerospikeClient\s*\(\s*clientPolicy\s*,\s*host\s*,\s*port\s*\)\s*;\s*$",
			RegexOptions.Compiled);
		private static string NormalizeNativeAerospikeClientCode(string code)
		{
			if(string.IsNullOrWhiteSpace(code))
				return code;

			// Aerospike.Client.ClientPolicy uses public fields whose names are mostly lowercase/camelCase.
			// Normalize common PascalCase AI mistakes before writing the generated native .linq file.
			code = NormalizeNativeClientPolicyMemberNames(code);

			var nativeBootstrap = BuildNativeConnectionBootstrapCode();
			var nativeHostPortLines = BuildNativeHostPortLines();

			// Pure native code must not acquire the client through the LINQPad driver context.
			code = NorNatCodeReplaceClientPol().Replace(code, nativeBootstrap);

			// Always prefer the active connection endpoint for explicit native host/port variables.
			code = NorNatCodeReplaceHostLine().Replace(code, nativeHostPortLines.HostLine);
			code = NorNatCodeReplacePortLine().Replace(code, nativeHostPortLines.PortLine);

			// Upgrade common placeholder native bootstrap blocks while preserving AI-generated namespace/set values.
			var placeholderMatch = NorNatCodeReplacePlaceholderNativeBlockRegex.Match(code);
			if(placeholderMatch.Success)
			{
				var namespaceSetLines = placeholderMatch.Groups["namespaceSet"].Value.TrimEnd('\r', '\n');
				var mergedBootstrap = BuildNativeConnectionBootstrapCode(namespaceSetLines);
				code = NorNatCodeReplacePlaceholderNativeBlockRegex.Replace(code, mergedBootstrap);
			}

			return code;
		}

		private static (string HostLine, string PortLine) BuildNativeHostPortLines()
		{
			var host = "localhost";
			var port = 3000;

			var cluster = AClusterAccess.Instance;
			var connection = cluster?.AerospikeConnection;

			if(connection is not null && TryGetSeedHost(connection, out var currentHost, out var currentPort))
			{
				host = currentHost;
				port = currentPort;
			}

			return ($"var host = {ToCSharpStringLiteral(host)};", $"var port = {port};");
		}

		private static string BuildNativeConnectionBootstrapCode(string namespaceSetLines = null)
		{
			var host = "localhost";
			var port = 3000;
			var namespaceName = "test";
			var setName = "Customer";
			var user = string.Empty;
			var password = string.Empty;
			var hasTls = false;
			var tlsName = string.Empty;
			var timeout = 1000;
			var loginTimeout = 1000;
			var useServicesAlternate = false;
			var connPoolsPerNode = 0;

			var cluster = AClusterAccess.Instance;
			var connection = cluster?.AerospikeConnection;

			if(connection is not null)
			{
				if(TryGetSeedHost(connection, out var currentHost, out var currentPort))
				{
					host = currentHost;
					port = currentPort;
				}

				var cxInfo = connection.CXInfo?.DatabaseInfo;
				user = cxInfo?.UserName ?? string.Empty;
				password = cxInfo?.Password ?? string.Empty;

				hasTls = connection.TLS is not null || !string.IsNullOrWhiteSpace(connection.TLSCertName);
				tlsName = connection.TLSCertName ?? string.Empty;
				timeout = connection.SocketTimeout;
				loginTimeout = connection.ConnectionTimeout;
				useServicesAlternate = connection.UseExternalIP;
				connPoolsPerNode = connection.ConnectionsPerNode;

				var discoveredNamespace = connection.Namespaces?.FirstOrDefault(ns => !string.IsNullOrWhiteSpace(ns?.Name));
				if(discoveredNamespace is not null)
				{
					namespaceName = discoveredNamespace.Name;

					var discoveredSet = discoveredNamespace.Sets?.FirstOrDefault(set => !set.IsNullSet && !string.IsNullOrWhiteSpace(set.Name));
					if(discoveredSet is not null)
						setName = discoveredSet.Name;
				}
			}

			var policyLines = new List<string>
			{
				"var clientPolicy = new ClientPolicy()",
				"{",
				$"    timeout = {timeout},",
				$"    loginTimeout = {loginTimeout},",
				$"    useServicesAlternate = {useServicesAlternate.ToString().ToLowerInvariant()},",
				$"    connPoolsPerNode = {connPoolsPerNode}",
			};

			if(!string.IsNullOrWhiteSpace(user))
				policyLines.Insert(2, $"    user = {ToCSharpStringLiteral(user)},");

			if(!string.IsNullOrWhiteSpace(password))
				policyLines.Insert(!string.IsNullOrWhiteSpace(user) ? 3 : 2, $"    password = {ToCSharpStringLiteral(password)},");

			if(hasTls)
				policyLines.Insert(!string.IsNullOrWhiteSpace(user) || !string.IsNullOrWhiteSpace(password) ? 4 : 2, "    tlsPolicy = new TlsPolicy(),");

			policyLines.Add("};");

			var defaultNamespaceSetLines = string.Join(
				Environment.NewLine,
				$"var namespaceName = {ToCSharpStringLiteral(namespaceName)};",
				$"var setName = {ToCSharpStringLiteral(setName)};");

			var finalNamespaceSetLines = string.IsNullOrWhiteSpace(namespaceSetLines)
				? defaultNamespaceSetLines
				: namespaceSetLines;

			return string.Join(
				Environment.NewLine,
				$"var host = {ToCSharpStringLiteral(host)};",
				$"var port = {port};",
				string.Empty,
				finalNamespaceSetLines,
				string.Empty,
				$"var tlsName = {ToCSharpStringLiteral(tlsName)};",
				"var seedHost = new Host(host, string.IsNullOrWhiteSpace(tlsName) ? null : tlsName, port);",
				string.Empty,
				string.Join(Environment.NewLine, policyLines),
				string.Empty,
				"using var client = new AerospikeClient(clientPolicy, seedHost);");
		}

		private static bool TryGetSeedHost(AerospikeConnection connection, out string host, out int port)
		{
			host = null;
			port = 0;

			if(connection?.SeedHosts is null || connection.SeedHosts.Length == 0)
				return false;

			var seedHost = connection.SeedHosts[0];
			if(seedHost is null)
				return false;

			host = TryReadHostMember(seedHost, "name", "Name");
			var portText = TryReadHostMember(seedHost, "port", "Port");

			if(string.IsNullOrWhiteSpace(host) || !int.TryParse(portText, out port))
				return false;

			return true;
		}

		private static string TryReadHostMember(Host host, params string[] memberNames)
		{
			if(host is null || memberNames is null || memberNames.Length == 0)
				return null;

			foreach(var memberName in memberNames)
			{
				var property = typeof(Host).GetProperty(memberName);
				if(property is not null)
				{
					var value = property.GetValue(host);
					if(value is not null)
						return value.ToString();
				}

				var field = typeof(Host).GetField(memberName);
				if(field is not null)
				{
					var value = field.GetValue(host);
					if(value is not null)
						return value.ToString();
				}
			}

			return null;
		}

		private static string ToCSharpStringLiteral(string value)
		{
			if(value is null)
				return "null";

			return "\"" + value
				.Replace("\\", "\\\\")
				.Replace("\"", "\\\"")
				.Replace("\r", "\\r")
				.Replace("\n", "\\n")
				+ "\"";
		}

		[GeneratedRegex(@"(?m)^\s*using\s+Exp\s*=\s*Aerospike\.Client\.Exp\s*;\s*$")]
		private static partial Regex ExpAliasUsingRegex();

		[GeneratedRegex(@"(?<!Aerospike\.)\bClient\.Exp\b")]
		private static partial Regex ClientExpTypeRegex();

		private static string NormalizeAerospikeExpressionAliasUsage(string code)
		{
			if(string.IsNullOrWhiteSpace(code))
				return code;

			// Guard: when an Exp alias is present, normalize legacy/incorrect Client.Exp type references.
			if(!ExpAliasUsingRegex().IsMatch(code))
				return code;

			return ClientExpTypeRegex().Replace(code, "Exp");
		}

		private static string NormalizeNativeClientPolicyMemberNames(string code)
		{
			if(string.IsNullOrWhiteSpace(code))
				return code;

			var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["User"] = "user",
				["Password"] = "password",
				["ClusterName"] = "clusterName",
				["AuthMode"] = "authMode",
				["Timeout"] = "timeout",
				["LoginTimeout"] = "loginTimeout",
				["MinConnsPerNode"] = "minConnsPerNode",
				["MaxConnsPerNode"] = "maxConnsPerNode",
				["ConnPoolsPerNode"] = "connPoolsPerNode",
				["MaxSocketIdle"] = "maxSocketIdle",
				["MaxErrorRate"] = "maxErrorRate",
				["ErrorRateWindow"] = "errorRateWindow",
				["TendInterval"] = "tendInterval",
				["FailIfNotConnected"] = "failIfNotConnected",
				["ReadPolicyDefault"] = "readPolicyDefault",
				["WritePolicyDefault"] = "writePolicyDefault",
				["ScanPolicyDefault"] = "scanPolicyDefault",
				["QueryPolicyDefault"] = "queryPolicyDefault",
				["BatchPolicyDefault"] = "batchPolicyDefault"
			};

			foreach(var replacement in replacements)
			{
				code = Regex.Replace(
					code,
					@"\b" + Regex.Escape(replacement.Key) + @"\s*=",
					replacement.Value + " =");
			}

			return code;
		}

		private static string ExtractQueryHeader(string queryText)
		{
			if(string.IsNullOrWhiteSpace(queryText))
				return null;

			var endTag = "</Query>";
			var endIndex = queryText.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

			if(endIndex < 0)
				return null;

			return queryText.Substring(0, endIndex + endTag.Length);
		}

		[GeneratedRegex(@"<Query\b[^>]*\bKind\s*=", RegexOptions.IgnoreCase, "en-US")]
		private static partial Regex QueryKindMatchRegex();

		[GeneratedRegex(@"(<Query\b[^>]*\bKind\s*=\s*"")[^"" ]*("")", RegexOptions.IgnoreCase, "en-US")]
		private static partial Regex QueryKindReplaceRegex();

		[GeneratedRegex(@"<Query\b", RegexOptions.IgnoreCase, "en-US")]
		private static partial Regex QueryKindReplace2Regex();

		private static string EnsureStatementsQueryKind(string header)
		{
			if(string.IsNullOrWhiteSpace(header))
				return "<Query Kind=\"Statements\" />";

			if(QueryKindMatchRegex().IsMatch(header))
				return QueryKindReplaceRegex().Replace(header, "$1Statements$2");

			return QueryKindReplace2Regex().Replace(header, "<Query Kind=\"Statements\"");
		}

		private static string EnsureCommonNamespaces(string header)
		{
			header = EnsureNamespace(header, "System");
			header = EnsureNamespace(header, "System.Linq");
			header = EnsureNamespace(header, "System.Collections.Generic");
			header = EnsureNamespace(header, "System.Text.RegularExpressions");
			header = EnsureNamespace(header, "System.Threading.Tasks");
			header = EnsureNamespace(header, "Aerospike");
			header = EnsureNamespace(header, "Aerospike.Client");

			return header;
		}

		private static string EnsureNativeAerospikeClientHeader(string header)
		{
			// Native Aerospike client generated queries are standalone LINQPad Statements queries.
			// Do not depend on a copied LINQPad-driver connection header.
			// Build the header explicitly so the Aerospike.Client NuGet reference cannot be lost
			// when the source header is the self-closing form: <Query Kind="Statements" />.
			var nativeHeader =
				"<Query Kind=\"Statements\">" + Environment.NewLine +
				"  <NuGetReference>Aerospike.Client</NuGetReference>" + Environment.NewLine +
				"  <Namespace>System</Namespace>" + Environment.NewLine +
				"  <Namespace>System.Collections</Namespace>" + Environment.NewLine +
				"  <Namespace>System.Collections.Generic</Namespace>" + Environment.NewLine +
				"  <Namespace>System.Linq</Namespace>" + Environment.NewLine +
				"  <Namespace>System.Security.Authentication</Namespace>" + Environment.NewLine +
				"  <Namespace>System.Security.Cryptography.X509Certificates</Namespace>" + Environment.NewLine +
				"  <Namespace>Aerospike.Client</Namespace>" + Environment.NewLine +
				"</Query>";

			return nativeHeader;
		}

		private static string EnsureNamespace(string header, string namespaceName)
		{
			if(string.IsNullOrWhiteSpace(header))
				return header;

			var namespaceLine = "<Namespace>" + namespaceName + "</Namespace>";

			if(header.IndexOf(namespaceLine, StringComparison.OrdinalIgnoreCase) >= 0)
				return header;

			return InsertIntoQueryHeader(header, "  " + namespaceLine + Environment.NewLine);
		}

		private static string EnsureNuGetReference(string header, string packageName)
		{
			if(string.IsNullOrWhiteSpace(header))
				return header;

			var referenceLine = "<NuGetReference>" + packageName + "</NuGetReference>";

			if(header.IndexOf(referenceLine, StringComparison.OrdinalIgnoreCase) >= 0)
				return header;

			return InsertIntoQueryHeader(header, "  " + referenceLine + Environment.NewLine);
		}

		private static string InsertIntoQueryHeader(string header, string line)
		{
			var endTag = "</Query>";
			var endIndex = header.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

			if(endIndex >= 0)
				return header.Insert(endIndex, line);

			if(header.TrimEnd().EndsWith("/>", StringComparison.Ordinal))
			{
				var openTag = header.Trim();
				openTag = openTag.Substring(0, openTag.Length - 2).TrimEnd() + ">";

				return openTag
					+ Environment.NewLine
					+ line
					+ "</Query>";
			}

			return header;
		}

		readonly static MethodInfo utilMarkdownMethod = typeof(LINQPad.Util)
															.GetMethod("Markdown",[typeof(string)]);

		readonly static MethodInfo utilRawHtmlMethod = typeof(LINQPad.Util)
													.GetMethod("RawHtml", [typeof(string)]);

		private static void DumpAIContextTruncationWarningIfNeeded(
			AerospikeAIContext aiContext,
			AerospikeAIContextOptions options)
		{
			if(options?.DumpTruncationWarning == false)
			{
				return;
			}

			try
			{
				var buildResult = aiContext.BuildMarkdown(options);

				if(buildResult?.WasTruncated != true)
				{
					return;
				}

				var message =
					$"AI context was truncated from {buildResult.OriginalLength:n0} characters " +
					$"to {buildResult.MaxChars:n0} content characters. " +
					$"Final Markdown length is {buildResult.FinalLength:n0} characters including the truncation notice. " +
					"Some schema, rules, examples, or validation guidance may be missing. " +
					"Consider increasing MaxChars, using a more focused context profile, or disabling examples/full diagnostic sections.";

				var topSections = buildResult.SectionLengthSummary?
					.Take(3)
					.ToArray();

				if(topSections is { Length: > 0 })
				{
					message += " Top context-size contributors: " + string.Join(", ", topSections) + ".";
				}

				DumpAIContextWarning(message);
			}
			catch(Exception ex)
			{
				_ = $"WARNING: Unable to evaluate AI context truncation status. {ex.Message}"
					.Dump("AI Context Warning");
			}
		}

		private static void DumpAIContextWarning(string message)
		{
			const string title = "AI Context Warning";

			if(string.IsNullOrWhiteSpace(message))
			{
				return;
			}

			try
			{
				if(utilRawHtmlMethod != null)
				{
					var encodedMessage = System.Net.WebUtility.HtmlEncode(message);
					var html =
						$"<div style=\"color:#b00020;font-weight:bold;font-size:16px;\">WARNING</div>" +
						$"<div style=\"color:#b00020;\">{encodedMessage}</div>";
					var rendered = utilRawHtmlMethod.Invoke(null, [html]);

					_ = rendered.Dump(title);
					return;
				}
			}
			catch
			{
				// Fall back to raw text if the LINQPad runtime does not expose a RawHtml renderer.
			}

			_ = $"WARNING: {message}".Dump(title);
		}

		private static void DumpAIResponse(string response)
		{
			const string title = "AI-generated LINQPad response";

			if(string.IsNullOrWhiteSpace(response))
			{
				_ = response.Dump(title);
				return;
			}

			try
			{
				if(utilMarkdownMethod != null)
				{
					var rendered = utilMarkdownMethod.Invoke(null, [response]);

					_ = rendered.Dump(title);
					return;
				}
			}
			catch
			{
				// Fall back to raw text if the LINQPad runtime does not expose a Markdown renderer.
			}

			_ = response.Dump(title);
		}
	}
}
