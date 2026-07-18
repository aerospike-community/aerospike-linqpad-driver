using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aerospike.Database.LINQPadDriver.Extensions;
using System;
using System.Reflection;

namespace Aerospike.Database.LINQPadDriver.Tests
{
	/// <summary>
	/// Unit tests for AI helper functions in LINQPadAIGeneratedQuery.
	/// Tests focus on comment stripping and request processing utilities.
	/// </summary>
	[TestClass]
	public class AIHelpersTests
	{
		/// <summary>
		/// Helper method to invoke private StripAIRequestCommentLines via reflection.
		/// </summary>
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

		#region StripAIRequestCommentLines Tests

		[TestMethod]
		public void StripAIRequestCommentLines_EmptyString_ReturnsEmpty()
		{
			// Arrange
			string request = "";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_NullString_ReturnsNull()
		{
			// Arrange
			string request = null;

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.IsNull(result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_WhitespaceOnly_TrimmedToEmpty()
		{
			// Arrange
			// Lines with only whitespace are kept but the whole result is trimmed
			string request = "   \n\t  \r\n   ";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			// After removing comment lines and trimming, whitespace-only lines result in empty
			Assert.AreEqual("", result.Trim());
		}

		[TestMethod]
		public void StripAIRequestCommentLines_NoComments_ReturnsUnchanged()
		{
			// Arrange
			string request = "Query all customers from the set";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Query all customers from the set", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_SingleLineComment_Removed()
		{
			// Arrange
			string request = "// This is a comment\nQuery all customers";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Query all customers", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_MultipleLineComments_AllRemoved()
		{
			// Arrange
			string request = @"// First comment
Query all customers
// Another comment
Filter by region";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Query all customers\r\nFilter by region", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_HashComments_Removed()
		{
			// Arrange
			string request = @"# Python comment
Query the database
#  Another hash comment";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Query the database", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_SqlComments_Removed()
		{
			// Arrange
			string request = @"-- SQL comment
SELECT * FROM customers
-- Another SQL comment";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("SELECT * FROM customers", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_MixedCommentStyles_AllRemoved()
		{
			// Arrange
			string request = @"// C# comment
-- SQL comment
# Python comment
Final request text";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Final request text", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_CommentWithLeadingWhitespace_Removed()
		{
			// Arrange
			string request = @"  // Indented comment
Query data";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Query data", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_WindowsLineEndings_Preserved()
		{
			// Arrange
			string request = "Line 1\r\nLine 2\r\nLine 3";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			StringAssert.Contains(result, "Line 1");
			StringAssert.Contains(result, "Line 2");
			StringAssert.Contains(result, "Line 3");
		}

		[TestMethod]
		public void StripAIRequestCommentLines_UnixLineEndings_Handled()
		{
			// Arrange
			string request = "Line 1\nLine 2\nLine 3";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			StringAssert.Contains(result, "Line 1");
			StringAssert.Contains(result, "Line 2");
			StringAssert.Contains(result, "Line 3");
		}

		[TestMethod]
		public void StripAIRequestCommentLines_MacLineEndings_Handled()
		{
			// Arrange
			string request = "Line 1\rLine 2\rLine 3";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			StringAssert.Contains(result, "Line 1");
			StringAssert.Contains(result, "Line 2");
			StringAssert.Contains(result, "Line 3");
		}

		[TestMethod]
		public void StripAIRequestCommentLines_CommentWithoutWhitespace_Removed()
		{
			// Arrange
			string request = @"//No space comment
// Space comment
Actual request";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Actual request", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_OnlyComments_ReturnsEmpty()
		{
			// Arrange
			string request = @"// Comment 1
// Comment 2
-- Comment 3
# Comment 4";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_CommentAtEnd_Removed()
		{
			// Arrange
			string request = @"Important request
// Trailing comment";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Important request", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_EmptyLinesPreserved()
		{
			// Arrange
			string request = @"First line

Third line";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			StringAssert.Contains(result, "First line");
			StringAssert.Contains(result, "Third line");
		}

		[TestMethod]
		public void StripAIRequestCommentLines_TextWithSlashesNotComments_Preserved()
		{
			// Arrange
			string request = "Calculate result/value ratio";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Calculate result/value ratio", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_URLInRequest_Preserved()
		{
			// Arrange
			string request = "Check https://example.com for data";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Check https://example.com for data", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_HashInHashtag_Preserved()
		{
			// Arrange
			string request = "Search for #aerospike hashtag";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Search for #aerospike hashtag", result);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_ComplexRealWorldScenario()
		{
			// Arrange
			string request = @"// Get customer data
// Filter by active status
Query all customers
-- This retrieves records from the customer set
Where status = 'active'
# Show top 10 results
Limit 10";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.IsTrue(result.Contains("Query all customers"));
			Assert.IsTrue(result.Contains("Where status = 'active'"));
			Assert.IsTrue(result.Contains("Limit 10"));
			Assert.IsFalse(result.Contains("//"));
			Assert.IsFalse(result.Contains("--"));
			Assert.IsFalse(result.Contains("#"));
		}

		[TestMethod]
		public void StripAIRequestCommentLines_ResultIsTrimmed()
		{
			// Arrange
			string request = "  \n  Query data  \n  ";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Query data", result);
		}

		#endregion

		#region Edge Cases and Robustness

		[TestMethod]
		public void StripAIRequestCommentLines_VeryLongRequest_Handled()
		{
			// Arrange
			string request = new string('a', 10000);

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(10000, result.Length);
		}

		[TestMethod]
		public void StripAIRequestCommentLines_SpecialCharacters_Preserved()
		{
			// Arrange
			string request = "Query data with special chars: @#$%^&*()_+-=[]{}|;':\",./<>?";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.IsTrue(result.Contains("@"));
			Assert.IsTrue(result.Contains("&"));
			Assert.IsTrue(result.Contains("*"));
		}

		[TestMethod]
		public void StripAIRequestCommentLines_MultipleConsecutiveComments_AllRemoved()
		{
			// Arrange
			string request = @"//
//
//
Query text";

			// Act
			var result = StripAIRequestCommentLines(request);

			// Assert
			Assert.AreEqual("Query text", result);
		}

		#endregion
	}
}
