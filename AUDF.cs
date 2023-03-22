using Aerospike.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Aerospike.Database.LINQPadDriver
{
    public sealed class AUDF
    {        
        public static readonly Regex CodeRegEx = new Regex(@"\s*(?<local>local\s+)?function\s+(?<name>[^ (]+)\s*\((?<params>[^\)]+)\)",
                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public AUDF(AModule module, string code)
        {
            this.Code = code;
            this.Module = module;

            var partsMatches = CodeRegEx.Match(code);

            this.Name = partsMatches.Groups["name"].Value.Trim();
            this.SafeName = Helpers.CheckName(this.Name, "UDF");

            this.Params = partsMatches.Groups["params"].Value.Trim();
            this.IsLocal = partsMatches.Groups["local"].Success;
        }

        public AUDF(AModule module)
        {
            this.Module = module;
            this.Name = "<Not Found>";
            this.SafeName = null;
            this.Params = string.Empty;
            this.IsNotFound = true;
        }

        /// <summary>
        /// It is a local UDF, cannot be accessed by the client
        /// </summary>
        public bool IsLocal { get; }
        /// <summary>
        /// Associated module
        /// </summary>
        public AModule Module { get; }
        /// <summary>
        /// The name of the UDF
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The UDF safe name that can be used in C# as a class name or property
        /// </summary>
        public string SafeName { get; }
        /// <summary>
        /// The UDF parameters
        /// </summary>
        public string Params { get; }
        /// <summary>
        /// The UDF code
        /// </summary>
        public string Code { get; }
        public bool IsNotFound { get; }
       
        public static readonly Regex FunctionRegEx = new Regex(@"^\s*(local\s+)?function\s",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        public static readonly Regex FunctionEndRegEx = new Regex(@"^\s*end\s*$",
                                                                    RegexOptions.RightToLeft | RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public static AUDF[]  Create(Connection connection, AModule module)
        {
            //type=LUA;content=bG9jYWwgZnVuY3Rpb24gcHV0QmluKHIsbmFtZSx2YWx1ZSkNCiAgICBpZiBub3QgYWVyb3NwaWtlOmV4aXN0cyhyKSB0aGVuIGFlcm9zcGlrZTpjcmVhdGUocikgZW5kDQogICAgcltuYW1lXSA9IHZhbHVlDQogICAgYWVyb3NwaWtlOnVwZGF0ZShyKQ0KZW5kDQoNCi0tIFNldCBhIHBhcnRpY3VsYXIgYmluDQpmdW5jdGlvbiB3cml0ZUJpbihyLG5hbWUsdmFsdWUpDQogICAgcHV0QmluKHIsbmFtZSx2YWx1ZSkNCmVuZA0KDQotLSBHZXQgYSBwYXJ0aWN1bGFyIGJpbg0KZnVuY3Rpb24gcmVhZEJpbihyLG5hbWUpDQogICAgcmV0dXJuIHJbbmFtZV0NCmVuZA0KDQotLSBSZXR1cm4gZ2VuZXJhdGlvbiBjb3VudCBvZiByZWNvcmQNCmZ1bmN0aW9uIGdldEdlbmVyYXRpb24ocikNCiAgICByZXR1cm4gcmVjb3JkLmdlbihyKQ0KZW5kDQoNCi0tIFVwZGF0ZSByZWNvcmQgb25seSBpZiBnZW4gaGFzbid0IGNoYW5nZWQNCmZ1bmN0aW9uIHdyaXRlSWZHZW5lcmF0aW9uTm90Q2hhbmdlZChyLG5hbWUsdmFsdWUsZ2VuKQ0KICAgIGlmIHJlY29yZC5nZW4ocikgPT0gZ2VuIHRoZW4NCiAgICAgICAgcltuYW1lXSA9IHZhbHVlDQogICAgICAgIGFlcm9zcGlrZTp1cGRhdGUocikNCiAgICBlbmQNCmVuZA0KDQotLSBTZXQgYSBwYXJ0aWN1bGFyIGJpbiBvbmx5IGlmIHJlY29yZCBkb2VzIG5vdCBhbHJlYWR5IGV4aXN0Lg0KZnVuY3Rpb24gd3JpdGVVbmlxdWUocixuYW1lLHZhbHVlKQ0KICAgIGlmIG5vdCBhZXJvc3Bpa2U6ZXhpc3RzKHIpIHRoZW4gDQogICAgICAgIGFlcm9zcGlrZTpjcmVhdGUocikgDQogICAgICAgIHJbbmFtZV0gPSB2YWx1ZQ0KICAgICAgICBhZXJvc3Bpa2U6dXBkYXRlKHIpDQogICAgZW5kDQplbmQNCg0KLS0gVmFsaWRhdGUgdmFsdWUgYmVmb3JlIHdyaXRpbmcuDQpmdW5jdGlvbiB3cml0ZVdpdGhWYWxpZGF0aW9uKHIsbmFtZSx2YWx1ZSkNCiAgICBpZiAodmFsdWUgPj0gMSBhbmQgdmFsdWUgPD0gMTApIHRoZW4NCiAgICAgICAgcHV0QmluKHIsbmFtZSx2YWx1ZSkNCiAgICBlbHNlDQogICAgICAgIGVycm9yKCIxMDAwOkludmFsaWQgdmFsdWUiKSANCiAgICBlbmQNCmVuZA0KDQotLSBSZWNvcmQgY29udGFpbnMgdHdvIGludGVnZXIgYmlucywgbmFtZTEgYW5kIG5hbWUyLg0KLS0gRm9yIG5hbWUxIGV2ZW4gaW50ZWdlcnMsIGFkZCB2YWx1ZSB0byBleGlzdGluZyBuYW1lMSBiaW4uDQotLSBGb3IgbmFtZTEgaW50ZWdlcnMgd2l0aCBhIG11bHRpcGxlIG9mIDUsIGRlbGV0ZSBuYW1lMiBiaW4uDQotLSBGb3IgbmFtZTEgaW50ZWdlcnMgd2l0aCBhIG11bHRpcGxlIG9mIDksIGRlbGV0ZSByZWNvcmQuIA0KZnVuY3Rpb24gcHJvY2Vzc1JlY29yZChyLG5hbWUxLG5hbWUyLGFkZFZhbHVlKQ0KICAgIGxvY2FsIHYgPSByW25hbWUxXQ0KDQogICAgaWYgKHYgJSA5ID09IDApIHRoZW4NCiAgICAgICAgYWVyb3NwaWtlOnJlbW92ZShyKQ0KICAgICAgICByZXR1cm4NCiAgICBlbmQNCg0KICAgIGlmICh2ICUgNSA9PSAwKSB0aGVuDQogICAgICAgIHJbbmFtZTJdID0gbmlsDQogICAgICAgIGFlcm9zcGlrZTp1cGRhdGUocikNCiAgICAgICAgcmV0dXJuDQogICAgZW5kDQoNCiAgICBpZiAodiAlIDIgPT0gMCkgdGhlbg0KICAgICAgICByW25hbWUxXSA9IHYgKyBhZGRWYWx1ZQ0KICAgICAgICBhZXJvc3Bpa2U6dXBkYXRlKHIpDQogICAgZW5kDQplbmQNCg0KLS0gQXBwZW5kIHRvIGVuZCBvZiByZWd1bGFyIGxpc3QgYmluDQpmdW5jdGlvbiBhcHBlbmRMaXN0QmluKHIsIGJpbm5hbWUsIHZhbHVlKQ0KICBsb2NhbCBsID0gcltiaW5uYW1lXQ0KDQogIGlmIGwgPT0gbmlsIHRoZW4NCiAgICBsID0gbGlzdCgpDQogIGVuZA0KDQogIGxpc3QuYXBwZW5kKGwsIHZhbHVlKQ0KICByW2Jpbm5hbWVdID0gbA0KICBhZXJvc3Bpa2U6dXBkYXRlKHIpDQplbmQNCg0KLS0gU2V0IGV4cGlyYXRpb24gb2YgcmVjb3JkDQotLSBmdW5jdGlvbiBleHBpcmUocix0dGwpDQotLSAgICBpZiByZWNvcmQudHRsKHIpID09IGdlbiB0aGVuDQotLSAgICAgICAgcltuYW1lXSA9IHZhbHVlDQotLSAgICAgICAgYWVyb3NwaWtlOnVwZGF0ZShyKQ0KLS0gICAgZW5kDQotLSBlbmQNCg0KLS0gRGV0ZXJtaW5lIGlmIGludGVnZXIgdmFsdWUgZXhpc3RzIGluIGEgbGlzdCBvZiBpbnRlZ2Vycy4NCmZ1bmN0aW9uIHZhbHVlRXhpc3RzKHIsbmFtZSxzZWFyY2gpDQoJLS0gQ2hlY2sgaWYgcmVjb3JkIGV4aXN0cw0KCWlmIG5vdCBhZXJvc3Bpa2U6ZXhpc3RzKHIpIHRoZW4NCgkJcmV0dXJuIDANCgllbmQNCg0KCS0tIFNlYXJjaCBmb3IgdmFsdWUgaW4gbGlzdCBiaW4uDQoJZm9yIHYgaW4gbGlzdC5pdGVyYXRvcihyW25hbWVdKSBkbw0KCQlpZiB2ID09IHNlYXJjaCB0aGVuDQoJCQlyZXR1cm4gMQ0KCQllbmQNCgllbmQNCg0KCXJldHVybiAwDQplbmQNCg==;
            var udfget = Info.Request(connection, $"udf-get:filename={module.Name}");

            if(string.IsNullOrEmpty(udfget) || udfget == "error=invalid_filename") return Array.Empty<AUDF>();

            var contentStr = udfget.Substring(udfget.IndexOf("content=") + 8);
            contentStr = contentStr.Substring(0, contentStr.Length - 1);
            
            var codeBlock = Encoding.Default.GetString(Convert.FromBase64String(contentStr));

            /*
            local function putBin(r,name,value)
                if not aerospike:exists(r) then aerospike:create(r) end
                r[name] = value
                aerospike:update(r)
            end

            -- Set a particular bin
            function writeBin(r,name,value)
                putBin(r,name,value)
            end
            */

            Tuple<string, int> GetFunctionBeginEnd(string codeBlock)
            {
                
                var beginMatch = FunctionRegEx.Match(codeBlock);

                if (!beginMatch.Success)
                {
                    return null;
                }

                var searchFuncBlock = codeBlock.Substring(beginMatch.Index + beginMatch.Length);
                var nextFuncMatch = FunctionRegEx.Match(searchFuncBlock);

                if (!nextFuncMatch.Success)
                {
                    return new Tuple<string, int>(codeBlock, -1);
                }

                var searchEndCodeBlock = searchFuncBlock.Substring(0, nextFuncMatch.Index);

                var fndEndMatch = FunctionEndRegEx.Match(searchEndCodeBlock);

                if (!fndEndMatch.Success)
                {
                    return new Tuple<string, int>(codeBlock, -1);
                }
                //var endBlockReminding = searchEndCodeBlock.Substring(fndEndMatch.Index); //Debugging
                var FuncCodeBlock = codeBlock.Substring(0, beginMatch.Index + beginMatch.Length + fndEndMatch.Index + 3);

                return new Tuple<string, int>(FuncCodeBlock, FuncCodeBlock.Length);
            }
            
            string currentCodeBlock = string.Empty;
            char[] trimLineChars = new char[] { ' ', '\t', '\r', '\n' };
            var remindingCodeBlock = codeBlock.Trim(trimLineChars);
            var udfList = new List<AUDF>();

            while (currentCodeBlock != null)
            {
                var currentFunc = GetFunctionBeginEnd(remindingCodeBlock);

                currentCodeBlock = currentFunc?.Item1.TrimEnd(trimLineChars);
                if (currentCodeBlock != null)
                {
                    udfList.Add(new AUDF(module, currentCodeBlock));

                    if (currentFunc.Item2 >= 0)
                    {
                        remindingCodeBlock = remindingCodeBlock.Substring(currentFunc.Item2).TrimStart(trimLineChars);
                    }
                    else
                    {
                        currentCodeBlock = null;
                    }
                }               
            }

            return udfList.ToArray();
        }
    }
}
