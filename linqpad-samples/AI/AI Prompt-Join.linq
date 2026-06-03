<Query Kind="Program">
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

/*
This will demo some of the capabilities of working with the LINQPad's AI Prompt and obtaining the AI context using the Aerospike LINQPAd driver.

Note: It is recommended to read the ReadMeFirst file in the Native samples folder.
*/

async Task Main()
{
	//Load the Demo's Cluster Data if not Already Loaded
	if (!test.Exists("Customer"))
	{
	  //Import Aerospike Set Records...
	  test.Import(LINQPad.Util.GetFullPath("aerospike.json"))
	  .Dump("Number of Records Imported");
	}
	
	var userRequest =
	"I would like a list of customers with invoices and associated artist they purchased";
	
	var prompt = AIContext.BuildPrompt(userRequest);
	
	var response = await LINQPad.Util.AI.Ask(prompt).GetResponseAsync();
	
	response.Text.Dump("AI-generated LINQPad response");
	
	var csharpCode = ExtractCSharpCode(response.Text);
	
	if (string.IsNullOrWhiteSpace(csharpCode))
	{
		"No C# code block was detected in the AI response.".Dump("Open Generated Query");
		return;
	}
	
	var generatedQueryPath = CreateConnectedGeneratedQuery(csharpCode);
	
	new Hyperlinq(generatedQueryPath, "Open generated C# query with this Aerospike connection")
		.Dump("Generated Query");
}

static string ExtractCSharpCode(string responseText)
{
	if (string.IsNullOrWhiteSpace(responseText))
	{
		return null;
	}

	var fencedCode = ExtractFencedCSharpCode(responseText);
	
	if (!string.IsNullOrWhiteSpace(fencedCode))
	{
		return fencedCode.Trim();
	}

	if (LooksLikeCSharp(responseText))
	{
		return responseText.Trim();
	}
	
	return null;
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
    {
        return false;
    }

    var trimmed = text.Trim();

    if (trimmed.StartsWith("var ", StringComparison.Ordinal)
        || trimmed.StartsWith("from ", StringComparison.Ordinal)
        || trimmed.StartsWith("async ", StringComparison.Ordinal)
        || trimmed.StartsWith("void ", StringComparison.Ordinal)
        || trimmed.StartsWith("public ", StringComparison.Ordinal)
        || trimmed.StartsWith("using ", StringComparison.Ordinal))
    {
        return true;
    }

    return trimmed.Contains(";")
        && (trimmed.Contains(".Dump(")
            || trimmed.Contains(" from ")
            || trimmed.Contains("select ")
            || trimmed.Contains("where ")
            || trimmed.Contains("var "));
}

static string CreateConnectedGeneratedQuery(string csharpCode)
{
    var currentQueryPath = LINQPad.Util.CurrentQueryPath;

    if (string.IsNullOrWhiteSpace(currentQueryPath) || !File.Exists(currentQueryPath))
    {
        throw new InvalidOperationException(
            "The current query must be saved before a connected generated query can be created. " +
            "Save this AI Test query first, then run it again.");
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
    if (string.IsNullOrWhiteSpace(queryText))
    {
        throw new ArgumentException("Query text cannot be empty.", nameof(queryText));
    }

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
	{
		return "<Query Kind=\"Statements\" />";
	}

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