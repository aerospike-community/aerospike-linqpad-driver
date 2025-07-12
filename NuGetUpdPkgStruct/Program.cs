
using System.CommandLine;

namespace NuGetUpdPkgStruct;

public class Program
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "<Pending>")]
	static int Main(string[] args)
	{
		RootCommand rootCommand = new("This program will update a NuGet Package so that it can support LINQPad drivers on MacOS.")
		{
			new Argument<FileInfo> ("package")
			{
				Description = "The NuGet File Package Path",
				HelpName = "NuGet File Package Path"
			},
			new Option<FileInfo?>("--newpkg", "-n")
			{
				Description = "If provided, the updates will be saved to this NuGet File Package"
			},
			new Option<string> ("--regexFramework")
			{
				Description = @"This regex is used to parse the folder names looking to change based on the RegEx.
The RegEx must define a group named ""framework"" which is the name that will be replaced and ""netver"" is the new name.
Example: lib/net6.0-windows7.0/Aerospike.Database.LINQPadDriver.dll => lib/net6.0/Aerospike.Database.LINQPadDriver.dll",
				Required = false,
				DefaultValueFactory = argResult => @"(?<framework>(?<netver>net\d+\.\d+)\-windows\d+\.\d+)"
			},
			new Option<string> ("--regexPkgName")
			{
				Description = @"This regex is used to parse the NuGet File Path to obtain the NuGet Package name.
The RegEx must define a group named ""pkgname"" which is the NuGet Package.
Example: E:\Aerospike.Database.LINQPadDriver.6.0.5.nupkg => Aerospike.Database.LINQPadDriver",
				Required = false,
				DefaultValueFactory = argResult => @"(?<pkgname>.+)\.\d+\.\d+\.\d+"
			}
		};

		rootCommand.SetAction(static parseResult =>
		{
			var nuGetFile = parseResult.GetValue<FileInfo>("package");
			if(nuGetFile is null || !nuGetFile.Exists)
			{
				if(nuGetFile is null)
					throw new ArgumentNullException("package", "Must supply a NuGet Path File. none Provided.");

				throw new FileNotFoundException($"NuGet File \"{nuGetFile.FullName}\" was not found");
			}
			var nuGetNewFile = parseResult.GetValue<FileInfo?>("--newpkg");
			var regexFramework = parseResult.GetValue<string>("--regexFramework");
			var regexPkgname = parseResult.GetValue<string>("--regexPkgName");

			if(string.IsNullOrEmpty(regexFramework))
				throw new ArgumentNullException("--regexFramework", "Must supply a RegEx string.");
			if(string.IsNullOrEmpty(regexPkgname))
				throw new ArgumentNullException("--regexPkgName", "Must supply a RegEx string.");

			string? tempFilename = null;

			try
			{
				if(nuGetNewFile != null)
				{
					if(nuGetNewFile.Exists)
					{
						tempFilename = CopyFileToTemp(nuGetNewFile.FullName);
					}
					File.Copy(nuGetFile.FullName, nuGetNewFile.FullName, true);
					nuGetFile = nuGetNewFile;
				}

				PkgUpdate.Apply(nuGetFile.FullName,
									regexFramework,
									regexPkgname);
			}
			catch(Exception ex)
			{
				Console.WriteLine($"Error: {ex.GetType().Name} Message: {ex.Message}");

				if(nuGetNewFile != null && tempFilename != null && File.Exists(tempFilename))
				{
					File.Copy(tempFilename, nuGetNewFile.FullName, true);
				}
				Console.WriteLine($"NuGet Package Update Failed for \"{nuGetFile}\"");
				return -1;
			}
			finally
			{
				if(tempFilename != null && File.Exists(tempFilename))
				{
					File.Delete(tempFilename);
				}
			}

			Console.WriteLine($"NuGet Package Update Completed for \"{nuGetFile}\"");
			return 0;
		});

		ParseResult parseResult = rootCommand.Parse(args);
		return parseResult.Invoke();
	}

	public static string? CopyFileToTemp(string sourceFilePath)
	{
		// Generate a unique temporary file path
		string? tempFilePath = Path.GetTempFileName();

		try
		{
			// Copy the source file to the temporary file path
			File.Copy(sourceFilePath, tempFilePath, true); // Overwrite if the temporary file already exists
		}
		catch(FileNotFoundException)
		{
			tempFilePath = null;
			Console.WriteLine($"Error: Source file not found: {sourceFilePath}");
		}
		catch(Exception)
		{
			tempFilePath = null;
			throw;
		}
		return tempFilePath;
	}
}



