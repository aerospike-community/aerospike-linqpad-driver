using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
	internal static class EmbeddedMarkdownLoader
	{
		private static readonly Assembly Assembly = typeof(EmbeddedMarkdownLoader).Assembly;
		private static readonly string ResourcePrefix = "Aerospike.Database.LINQPadDriver.AIContext.";
		private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>(StringComparer.Ordinal);

		public static string Load(string fileName)
		{
			if(string.IsNullOrWhiteSpace(fileName))
			{
				throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));
			}

			if(Cache.TryGetValue(fileName, out var cached))
			{
				return cached;
			}

			var resourceName = ResourcePrefix + fileName;

			using(var stream = Assembly.GetManifestResourceStream(resourceName))
			{
				if(stream is null)
				{
					throw new InvalidOperationException(
						$"Embedded markdown resource '{resourceName}' was not found.");
				}

				using(var reader = new StreamReader(stream))
				{
					var content = reader.ReadToEnd();
					Cache[fileName] = content;
					return content;
				}
			}
		}

		public static string LoadAndReplace(string fileName, IReadOnlyDictionary<string, string> replacements = null)
		{
			var content = Load(fileName);

			if(replacements is null || replacements.Count == 0)
			{
				return content;
			}

			foreach(var replacement in replacements)
			{
				content = content.Replace("{{" + replacement.Key + "}}", replacement.Value ?? string.Empty);
			}

			return content;
		}
	}
}
