using LINQPad;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
	/// <summary>
	/// LINQPad AI helper that submits an AI request, classifies the response,
	/// and creates a connected C# Statements query file when the AI response
	/// contains runnable C# code.
	/// </summary>
	public static partial class LINQPadAIGeneratedQuery
	{
		/// <summary>
		/// Sends a natural-language request to LINQPad AI, inspects the returned content,
		/// and, when runnable C# is detected, creates a new connected C# Statements query.
		/// </summary>
		/// <param name="aiContext">
		/// The Aerospike AI context associated with the active LINQPad connection.
		/// </param>
		/// <param name="userRequest">
		/// The prompt text to submit to the AI service.
		/// </param>
		/// <param name="options">
		/// Optional request settings used to control AI behavior for this submission.
		/// </param>
		/// <param name="systemInstruction">
		/// Optional system-level instruction prepended to guide response style and constraints.
		/// </param>
		/// <param name="progression">
		/// <see langword="true"/> to show AI progress feedback while the request is running; otherwise, <see langword="false"/>.
		/// </param>
		/// <param name="cancellationToken">
		/// A token that can be used to cancel the request.
		/// </param>
		/// <returns>
		/// The raw response text returned by the AI, regardless of whether query generation succeeds.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown when <paramref name="aiContext"/> is <see langword="null"/>.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when <paramref name="userRequest"/> is empty, null, or whitespace.
		/// </exception>
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

			var response = await aiContext
				.SubmitRequestAsync(
						userRequest,
						options,
						systemInstruction,
						progression,
						cancellationToken)
				.ConfigureAwait(true);

			_ = response.Dump("AI-generated LINQPad response");

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

			var generatedQueryPath = CreateConnectedGeneratedQuery(
				csharpCode,
				out var copiedCurrentConnection);

			var linkText = copiedCurrentConnection
				? "Open generated C# Statements query with this Aerospike connection"
				: "Open generated C# Statements query";

			_ = new Hyperlinq(generatedQueryPath, linkText)
				.Dump("Generated Query");

			if(!copiedCurrentConnection)
			{
				_ = "The generated query was created, but the current LINQPad connection could not be copied. " +
				"Save the AI query first and run it again if you want generated queries to automatically reuse the Aerospike connection."
					.Dump("Connection Notice");
			}

			return response;
		}

		private enum AIResponseKind
		{
			None,
			Explanation,
			RunnableCSharp
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

			var hasExplanationMarkers =
				responseText.Contains("## What it does", StringComparison.OrdinalIgnoreCase)
				|| responseText.Contains("## Summary", StringComparison.OrdinalIgnoreCase)
				|| responseText.Contains("Performance considerations", StringComparison.OrdinalIgnoreCase)
				|| responseText.Contains("Safety considerations", StringComparison.OrdinalIgnoreCase)
				|| responseText.Contains("Client-side vs server-side", StringComparison.OrdinalIgnoreCase)
				|| responseText.Contains("This query is", StringComparison.OrdinalIgnoreCase)
				|| responseText.Contains("If you want, I can", StringComparison.OrdinalIgnoreCase)
				|| responseText.Contains("What it does", StringComparison.OrdinalIgnoreCase);

			if(hasExplanationMarkers && codeBlocks.Count > 0)
				return AIResponseKind.Explanation;

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
				|| trimmed.Contains(".AsEnumerable()")
				|| trimmed.Contains(".Query(")
				|| trimmed.Contains(".Dump(");

			var looksLikeNarrativeSnippet =
				trimmed.StartsWith("test.", StringComparison.Ordinal)
				&& !trimmed.Contains(";")
				&& !trimmed.Contains(".Dump(");

			return hasExecutableShape && hasQueryShape && !looksLikeNarrativeSnippet;
		}

		private static bool TryGetCurrentQueryHeader(out string header)
		{
			header = null;

			try
			{
				var currentQueryPath = Util.CurrentQueryPath;

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

		private static string CreateConnectedGeneratedQuery(
								string csharpCode,
								out bool copiedCurrentConnection)
		{
			copiedCurrentConnection = false;

			if(string.IsNullOrWhiteSpace(csharpCode))
				throw new ArgumentException("C# code cannot be blank.", nameof(csharpCode));

			string generatedHeader;
			string warningComment = null;

			if(TryGetCurrentQueryHeader(out var currentHeader))
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

			var generatedQueryText =
				generatedHeader
				+ Environment.NewLine
				+ Environment.NewLine
				+ (warningComment ?? string.Empty)
				+ csharpCode.Trim()
				+ Environment.NewLine;

			var outputFolder = Path.Combine(
				Path.GetTempPath(),
				"AerospikeLinqPadAI");

			Directory.CreateDirectory(outputFolder);

			var outputPath = Path.Combine(
				outputFolder,
				"Generated-Aerospike-AI-Query-"
					+ DateTime.Now.ToString("yyyyMMdd-HHmmss")
					+ ".linq");

			File.WriteAllText(outputPath, generatedQueryText, Encoding.UTF8);

			return outputPath;
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

		[GeneratedRegex(@"(<Query\b[^>]*\bKind\s*=\s*"")[^""]*("")", RegexOptions.IgnoreCase, "en-US")]
		private static partial Regex QueryKindReplaceRegex();

		[GeneratedRegex(@"<Query\b", RegexOptions.IgnoreCase, "en-US")]
		private static partial Regex QueryKindReplace2Regex();

		private static string EnsureStatementsQueryKind(string header)
		{
			if(string.IsNullOrWhiteSpace(header))
				return "<Query Kind=\"Statements\" />";

			if(QueryKindMatchRegex().IsMatch(header))
			{
				return QueryKindReplaceRegex().Replace(header, "$1Statements$2");
			}

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

		private static string EnsureNamespace(string header, string namespaceName)
		{
			if(string.IsNullOrWhiteSpace(header))
				return header;

			var namespaceLine = "<Namespace>" + namespaceName + "</Namespace>";

			if(header.IndexOf(namespaceLine, StringComparison.OrdinalIgnoreCase) >= 0)
				return header;

			var endTag = "</Query>";
			var endIndex = header.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

			if(endIndex < 0)
				return header;

			return header.Insert(
				endIndex,
				"  " + namespaceLine + Environment.NewLine);
		}
	}
}