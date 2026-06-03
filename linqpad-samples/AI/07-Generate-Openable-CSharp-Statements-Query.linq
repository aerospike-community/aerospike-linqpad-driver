<Query Kind="Statements">
  <Connection>
    <ID>973104d1-5fc3-4e74-a869-59441d5e370d</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Driver Assembly="Aerospike.Database.LINQPadDriver" PublicKeyToken="no-strong-name">Aerospike.Database.LINQPadDriver.DynamicDriver</Driver>
    <Server>localhost</Server>
    <DisplayName>Aerospike Cluster (Demo)</DisplayName>
    <DriverData>
      <UseExternalIP>false</UseExternalIP>
      <Debug>false</Debug>
      <RecordView>Record</RecordView>
      <DocumentAPI>true</DocumentAPI>
    </DriverData>
  </Connection>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

// Ask AI for C# Statements code. If the response contains C#,
// create a new .linq file with this query's connection header and dump a link to open it.
//
// Save this query first so Util.CurrentQueryPath is available.

var request = LINQPad.Util.ReadLine(
    "Ask AI to generate a LINQPad C# Statements query:",
    "Generate a query-syntax join between test.Customer and test.Invoice, limit to 100 rows, and Dump the results.");

if (string.IsNullOrWhiteSpace(request))
    return;

var prompt = AIContext.BuildPrompt(request);

var response = await LINQPad.Util.AI.Ask(prompt).GetResponseAsync();

response.Text.Dump("AI Response");

var csharpCode = ExtractCSharpCode(response.Text);

if (string.IsNullOrWhiteSpace(csharpCode))
{
    "No C# code was detected in the AI response.".Dump("Generated Query");
    return;
}

var generatedQueryPath = CreateConnectedGeneratedQuery(csharpCode);

new Hyperlinq(generatedQueryPath, "Open generated C# Statements query with this Aerospike connection")
    .Dump("Generated Query");

static string ExtractCSharpCode(string responseText)
{
    if (string.IsNullOrWhiteSpace(responseText))
        return null;

    var fencedCode = ExtractFencedCSharpCode(responseText);

    if (!string.IsNullOrWhiteSpace(fencedCode))
        return fencedCode.Trim();

    return LooksLikeCSharp(responseText) ? responseText.Trim() : null;
}

static string ExtractFencedCSharpCode(string responseText)
{
    var matches = Regex.Matches(
        responseText,
        @"```(?<lang>csharp|cs|c#|CSharp|C#)?\s*(?<code>[\s\S]*?)```",
        RegexOptions.IgnoreCase);

    foreach (Match match in matches)
    {
        var lang = match.Groups["lang"].Value;

        if (string.IsNullOrWhiteSpace(lang)
            || string.Equals(lang, "csharp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lang, "cs", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lang, "c#", StringComparison.OrdinalIgnoreCase))
        {
            return match.Groups["code"].Value;
        }
    }

    return null;
}

static bool LooksLikeCSharp(string text)
{
    if (string.IsNullOrWhiteSpace(text))
        return false;

    var trimmed = text.Trim();

    return trimmed.StartsWith("var ", StringComparison.Ordinal)
        || trimmed.StartsWith("from ", StringComparison.Ordinal)
        || trimmed.StartsWith("using ", StringComparison.Ordinal)
        || trimmed.Contains(".Dump(")
        || trimmed.Contains(" select ")
        || trimmed.Contains(" where ");
}

static string CreateConnectedGeneratedQuery(string csharpCode)
{
    var currentQueryPath = LINQPad.Util.CurrentQueryPath;

    if (string.IsNullOrWhiteSpace(currentQueryPath) || !File.Exists(currentQueryPath))
    {
        throw new InvalidOperationException(
            "Save this AI query first, then run it again so the current .linq header can be copied.");
    }

    var currentQueryText = File.ReadAllText(currentQueryPath);
    var currentHeader = ExtractQueryHeader(currentQueryText);
    var generatedHeader = EnsureStatementsQueryKind(currentHeader);

    var generatedQueryText =
        generatedHeader
        + Environment.NewLine
        + Environment.NewLine
        + csharpCode
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

static string ExtractQueryHeader(string queryText)
{
    var endTag = "</Query>";
    var endIndex = queryText.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

    if (endIndex < 0)
    {
        throw new InvalidOperationException(
            "Unable to find the LINQPad <Query> header in the current query file.");
    }

    return queryText.Substring(0, endIndex + endTag.Length);
}

static string EnsureStatementsQueryKind(string header)
{
    if (string.IsNullOrWhiteSpace(header))
        return "<Query Kind=\"Statements\" />";

    if (Regex.IsMatch(header, @"<Query\b[^>]*\bKind\s*=", RegexOptions.IgnoreCase))
    {
        return Regex.Replace(
            header,
            @"(<Query\b[^>]*\bKind\s*=\s*"")[^""]*("")",
            "$1Statements$2",
            RegexOptions.IgnoreCase);
    }

    return Regex.Replace(
        header,
        @"<Query\b",
        "<Query Kind=\"Statements\"",
        RegexOptions.IgnoreCase);
}
