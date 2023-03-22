using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LINQPad;
using System.Linq;

namespace Aerospike.Database.LINQPadDriver
{
    internal static class DumpCompilerError
    {
        const string stdHeader = @"
<Query Kind=""Program"">
  <NuGetReference>Aerospike.Client</NuGetReference>
  <Reference Relative=""..\..\AppData\Local\LINQPad\Drivers\DataContext\NetCore\Aerospike.Database.LINQPadDriver\Aerospike.Database.LINQPadDriver.dll"">&lt;LocalApplicationData&gt;\LINQPad\Drivers\DataContext\NetCore\Aerospike.Database.LINQPadDriver\Aerospike.Database.LINQPadDriver.dll</Reference>  
  <Namespace>Aerospike.Client</Namespace>
  <Namespace>Aerospike.Database.LINQPadDriver</Namespace>
  <Namespace>Aerospike.Database.LINQPadDriver.Extensions</Namespace>
</Query>
";
       
        public static string GetFrameWorkInfo()
        {
            return System.Reflection.Assembly.GetEntryAssembly()
                    .GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), true)
                    ?.Cast<System.Runtime.Versioning.TargetFrameworkAttribute>()
                    .FirstOrDefault()
                    ?.FrameworkName
                    ?? System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        }

        public static void ToLinqPadFile(string sourceCode, string[] errors)
        {            
            var fileBuilder = new StringBuilder(stdHeader);
            var fixedSourceCode = sourceCode.Replace("using ", "//using ");

            fileBuilder.AppendLine();

            {
                fileBuilder.AppendLine("// Platform Information:");
                fileBuilder.AppendLine(string.Format("//\t\t{0}",
                                                        System.Runtime.InteropServices.RuntimeInformation.OSDescription));
                fileBuilder.AppendLine(string.Format("//\t\t{0}",
                                                        GetFrameWorkInfo()));
                var lqDriver = typeof(Aerospike.Database.LINQPadDriver.AerospikeConnection).Assembly.GetName();
                fileBuilder.AppendLine(string.Format("//\t\tLinqPad Driver: {0} Version: {1} VersionCompatibility: {2}",
                                                        lqDriver?.Name,
                                                        lqDriver?.Version,
                                                        lqDriver?.VersionCompatibility));
                var asyncClient = typeof(Aerospike.Client.Connection).Assembly.GetName();
                fileBuilder.AppendLine(string.Format("//\t\tAerospike Driver: {0} Version: {1} VersionCompatibility: {2}",
                                                        asyncClient?.Name,
                                                        asyncClient?.Version,
                                                        asyncClient?.VersionCompatibility));
            }

            foreach (var error in errors)
            {
                fileBuilder.AppendLine("// " + error);
            }

            fileBuilder.AppendLine();

            fileBuilder.AppendLine(fixedSourceCode);

            var name = errors.Length == 0 ? "Debug" : "Errors";
            File.WriteAllText(Path.Combine(LINQPad.Util.MyQueriesFolder, $"Aerospike.LINQPadDriver {name} {DateTime.Now.ToString("yyMMddHHmmss")}.linq"), fileBuilder.ToString());
        }
    }
}
