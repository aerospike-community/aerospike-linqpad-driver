using Aerospike.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;

namespace Aerospike.Database.LINQPadDriver
{
    /// <summary>
    /// A class used to represent Aerospike UDG Modules
    /// </summary>
    public sealed class LPModule
    {
        public enum Types
        {
            LUA
        }
        
        public LPModule(string name,
                        string type,
                        string hash)
        {
            this.Name = name;
            this.SafeName = Helpers.CheckName(name, "Module");
            this.Type = Enum.Parse<Types>(type, true);
            this.Hash = hash;
            this.PackageName = Path.GetFileNameWithoutExtension(name);
            this.SafePackageName = Helpers.CheckName(this.PackageName, "Pkg");
        }

        [JsonConstructor]
        public LPModule(string name,
                        string safename,
                        Types type,
                        string hash,
                        string packagename,
                        string safepackagenamne,
                        IEnumerable<LPUDF> udfs)
        {
            this.Name = name;
            this.SafeName = safename;
            this.Type = type;
            this.Hash = hash;
            this.PackageName = packagename;
            this.SafePackageName = safepackagenamne;
            this.UDFs = udfs.ToArray();
        }

        /// <summary>
        /// Module Name
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Module safe name that can be sued in a C# class name or property
        /// </summary>
        public string SafeName { get; }
        /// <summary>
        /// The Module BinType
        /// </summary>
        public Types Type { get; }
        /// <summary>
        /// The Module Hash
        /// </summary>
        public string Hash { get; }
        /// <summary>
        /// The Module package name
        /// </summary>
        public string PackageName { get; }
        /// <summary>
        /// The Module safe package name which can be used in a C# class name or property
        /// </summary>
        public string SafePackageName { get; }

        /// <summary>
        /// All associated UDFs in the module
        /// </summary>
        public LPUDF[] UDFs { get; private set; }

        public static readonly Regex ModuleRegEx = new Regex(@"filename\s*=\s*(?<filename>[^,;]+)((,|;)\s*hash\s*=\s*(?<hash>[^,;]+)(,|;)\s*type\s*=\s*(?<type>[^,;]+))",
                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static IEnumerable<LPModule> Create(Connection connection)
        {            
            var modules = new List<LPModule>();
            
            /*
             * filename=record_example.lua,hash=f1070efb0343c3b5296175eb3da6a6d9c5148a2e,type=LUA;filename=average_example.lua,hash=cfbd1d1aeb2c4a47687058673296ec36b14ebec2,type=LUA;filename=filter_example.lua,hash=289f38fb9f8afefd3d02f58c79763b0408ae88e3,type=LUA;
             */

            var udflist = Info.Request(connection, "udf-list");
            var udflistMatches = ModuleRegEx.Matches(udflist);

            foreach (Match udflistMatch in udflistMatches)
            {
                var name = udflistMatch.Groups["filename"].Value;
                var type = udflistMatch.Groups["type"].Value;
                var hash = udflistMatch.Groups["hash"].Value;

                var module = new LPModule(name, type, hash);
                modules.Add(module);

                module.UDFs = LPUDF.Create(connection, module);
            }


            return modules;
        }
    }
}
