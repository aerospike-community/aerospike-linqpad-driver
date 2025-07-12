using System;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace NuGetUpdPkgStruct
{
	internal static class PkgUpdate
	{
		public static void Apply(string nugetFilePath,
									string regexFrameworkStr,
									string regexPkgNameStr)
		{
			var frameworkRegEx = new Regex(regexFrameworkStr,
											RegexOptions.Singleline
												| RegexOptions.IgnoreCase
												| RegexOptions.Compiled);
			
			using(ZipArchive archive = ZipFile.Open(nugetFilePath, ZipArchiveMode.Update))
			{
				var frameworkNetList = new List<string>();
				var pkgMatch = Regex.Match(Path.GetFileNameWithoutExtension(nugetFilePath),
											regexPkgNameStr);

				var nuspecEntry = pkgMatch.Success
									? archive.GetEntry(pkgMatch.Groups["pkgname"].Value + ".nuspec")
									: null;

				// Iterate through all entries in the archive		
				foreach(ZipArchiveEntry entry in archive.Entries.ToArray())
				{
					// Check if the entry's full name matches the framework for windows
					var match = frameworkRegEx.Match(entry.FullName);

					if(match.Success)
					{
						// Construct the new full name by replacing the old folder name with the new one
						string newFullName = entry.FullName.Replace(match.Groups["framework"].Value, match.Groups["netver"].Value);

						// Create a new entry with the updated name
						ZipArchiveEntry newEntry = archive.CreateEntry(newFullName);

						// Copy the content from the old entry to the new entry
						using(Stream oldStream = entry.Open())
						using(Stream newStream = newEntry.Open())
						{
							oldStream.CopyTo(newStream);
						}

						// Delete the old entry
						entry.Delete();

						//update nuspec file, if not already updated
						if(nuspecEntry != null && !frameworkNetList.Contains(match.Groups["framework"].Value))
						{
							StringBuilder nuspecDoc;

							using(var readStream = nuspecEntry.Open())
							using(StreamReader reader = new StreamReader(readStream))
							{
								nuspecDoc = new StringBuilder(reader.ReadToEnd());
							}

							nuspecEntry.Delete();
							var newNuspecEntry = archive.CreateEntry(nuspecEntry.FullName);
							nuspecEntry = newNuspecEntry;
							nuspecDoc.Replace(match.Groups["framework"].Value, match.Groups["netver"].Value);

							using(StreamWriter writer = new StreamWriter(newNuspecEntry.Open()))
							{
								writer.Write(nuspecDoc);
							}
						}
						frameworkNetList.Add(match.Groups["framework"].Value);
					}
				}
			}
		}
	}
}
