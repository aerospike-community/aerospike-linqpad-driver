# AI Helpers Unit Tests

## Overview

A comprehensive test suite for AI helper functions in the LINQPad Aerospike driver, specifically focusing on the `StripAIRequestCommentLines` preprocessing utility used in `LINQPadAIGeneratedQuery.cs`.

## File Location

**File**: `TestProjects/Aerospike.Database.LINQPadDriverTests/AIHelpersTests.cs`

**Test Class**: `Aerospike.Database.LINQPadDriver.Tests.AIHelpersTests`

## Why These Tests Matter

The AI integration layer in the LINQPad driver includes helper methods that preprocess user requests before sending them to the AI service. These tests ensure:

1. **Offline Testing** - Tests run without making live AI requests
2. **Request Normalization** - Comment lines are correctly stripped before AI processing
3. **Edge Case Handling** - Line ending variations, special characters, and long inputs are handled gracefully
4. **Real-World Scenarios** - Mixed comment styles and complex requests are tested

## Test Coverage

### Total Tests: 25

#### Core Functionality (8 tests)
- `EmptyString_ReturnsEmpty` - Empty input handling
- `NullString_ReturnsNull` - Null input handling  
- `WhitespaceOnly_TrimmedToEmpty` - Whitespace normalization
- `NoComments_ReturnsUnchanged` - Non-commented requests pass through
- `SingleLineComment_Removed` - C# comment removal (`//`)
- `MultipleLineComments_AllRemoved` - Multiple C# comments
- `HashComments_Removed` - Python/hash comment removal (`#`)
- `SqlComments_Removed` - SQL comment removal (`--`)

#### Mixed Scenarios (6 tests)
- `MixedCommentStyles_AllRemoved` - Multiple comment style types
- `CommentWithLeadingWhitespace_Removed` - Indented comments
- `CommentWithoutWhitespace_Removed` - Comments without space after marker
- `OnlyComments_ReturnsEmpty` - Request with only comments
- `CommentAtEnd_Removed` - Trailing comments
- `ComplexRealWorldScenario` - Mixed request with multiple comment types

#### Line Ending Handling (3 tests)
- `WindowsLineEndings_Preserved` - CRLF (`\r\n`) handling
- `UnixLineEndings_Handled` - LF (`\n`) handling
- `MacLineEndings_Handled` - CR (`\r`) handling

#### Negative/Preservation Tests (5 tests)
- `TextWithSlashesNotComments_Preserved` - URLs and ratios preserved
- `URLInRequest_Preserved` - HTTPS URLs not treated as comments
- `HashInHashtag_Preserved` - Mid-line `#` not treated as comments
- `EmptyLinesPreserved` - Blank lines between content
- `ResultIsTrimmed` - Leading/trailing whitespace removed from result

#### Robustness Tests (3 tests)
- `VeryLongRequest_Handled` - 10,000+ character inputs
- `SpecialCharacters_Preserved` - Special characters in requests
- `MultipleConsecutiveComments_AllRemoved` - Comment stacking

## Method Under Test

```csharp
private static string StripAIRequestCommentLines(string request)
{
	// Removes lines starting with //, #, or -- (after trimming leading whitespace)
	// Normalizes line endings (CRLF, LF, CR)
	// Rejoins remaining lines with Environment.NewLine
	// Trims leading/trailing whitespace from result
}
```

## Test Implementation Details

### Reflection-Based Access

Since `StripAIRequestCommentLines` is private static, tests use reflection to invoke it:

```csharp
private static string StripAIRequestCommentLines(string request)
{
	var method = typeof(LINQPadAIGeneratedQuery).GetMethod(
		"StripAIRequestCommentLines",
		BindingFlags.NonPublic | BindingFlags.Static,
		null,
		new[] { typeof(string) },
		null);

	Assert.IsNotNull(method, "StripAIRequestCommentLines method should exist");
	return (string)method!.Invoke(null, new object[] { request })!;
}
```

This approach:
- ✅ Tests private methods without exposing public APIs
- ✅ Maintains encapsulation of internal preprocessing logic
- ✅ Works with existing code without requiring refactoring
- ✅ Provides clear feedback if the method signature changes

## Running the Tests

### Run All AI Helper Tests
```
dotnet test --filter "FullyQualifiedName~Aerospike.Database.LINQPadDriver.Tests.AIHelpersTests"
```

### Run Tests in Visual Studio Test Explorer
Search for: `AIHelpersTests`

### Run Specific Test
```
dotnet test --filter "Name=StripAIRequestCommentLines_ComplexRealWorldScenario"
```

## Test Results

```
========== Test run finished: 25 Tests (25 Passed, 0 Failed, 0 Skipped) ==========
```

## Future Enhancements

Potential tests for additional AI helpers:
- `ClassifyAIResponse` - Response classification logic
- `ExtractCSharpCode` - Code block extraction from AI responses
- `DetermineGeneratedQueryMode` - Mode inference from requests/responses
- `CreateMarkdownReplacements` - String replacement dictionary building
- Context building and truncation logic in `AerospikeAIContext.cs`

## Notes

- Tests follow AAA pattern (Arrange, Act, Assert)
- No external dependencies or live AI service calls
- All tests are deterministic and can run offline
- Tests follow MSTest conventions used throughout the project
