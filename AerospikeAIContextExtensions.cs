using System.Threading.Tasks;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
	public static class AerospikeAIContextExtensions
	{
		public static AerospikeAIContext ToAIContext(this AClusterAccess cluster)
		{
			return AerospikeAIContext.From(cluster);
		}

		public static string ToAIContextMarkdown(
			this AClusterAccess cluster,
			AerospikeAIContextOptions options = null)
		{
			return AerospikeAIContext.From(cluster).ToMarkdown(options);
		}

		public static string BuildAIPrompt(
			this AClusterAccess cluster,
			string userRequest,
			AerospikeAIContextOptions options = null,
			string systemInstruction = null)
		{
			return AerospikeAIContext.From(cluster)
				.BuildPrompt(userRequest, options, systemInstruction);
		}
	}
}