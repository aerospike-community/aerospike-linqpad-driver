using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;

namespace Aerospike.Database.LINQPadDriver
{
	public static partial class CertHelpers
	{
		public enum ResultCodes
		{
			Unknown = 0,
			Success,
			NotFound,
			Expired,
			Premature,
			NoTLSCommonName,
			WrongTLSCommonName,
			InvalidChain
		}

		public static X509Certificate2 LoadCertificateFromFile(string certificatePath)
		{
			byte[] derData;

			//if(certificatePath.EndsWith(".pem"))
			//{
			//	var pemData = File.ReadAllBytes(certificatePath);
			//	derData = ConvertPEMtoDER(pemData);
			//}
			//else
			//{
			// Already using a DER-formatted certificate
			derData = File.ReadAllBytes(certificatePath);
			//}
			return new X509Certificate2(derData);
		}

		public static (ResultCodes, string) Validate(string certificatePath, IEnumerable<string> caAndChainPaths = null)
		{
			if(string.IsNullOrEmpty(certificatePath)
				|| !File.Exists(certificatePath))
			{
				return (ResultCodes.NotFound, null);
			}

			using var certificateUnderValidation = LoadCertificateFromFile(certificatePath);
			return Validate(certificateUnderValidation, caAndChainPaths);
		}

		public static (ResultCodes, string) Validate(X509Certificate2 certificateUnderValidation, IEnumerable<string> caAndChainPaths = null)
		{
			if(certificateUnderValidation is null)
			{
				return (ResultCodes.NotFound, null);
			}

			var subject = certificateUnderValidation.Subject;

			if(string.IsNullOrEmpty(subject))
			{
				return (ResultCodes.NoTLSCommonName, null);
			}
			
			using var chain = new X509Chain();

			if(caAndChainPaths is not null && caAndChainPaths.Any())
			{
				// .NET 5+ has a new 'CustomTrustStore' mode that permits ignoring the OS trust
				// and ExtraTrust stores, and explicitly verify against an expected root CA (and
				// its chain). This avoids the PartialChain issues in .NET Core 3 arising from
				// the use of AllowUnknownCertificateAuthority, and allows us to trust the
				// X509Chain.Build() verification result without extra steps.
				chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
				chain.ChainPolicy
						.CustomTrustStore
						.AddRange(new X509Certificate2Collection(caAndChainPaths
																	.Select(file =>
																			LoadCertificateFromFile(file))
																				.ToArray()));
			}
			chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

			if(DateTime.Now <= certificateUnderValidation.NotBefore)
			{
				return (ResultCodes.Premature, subject);
			}
			if(certificateUnderValidation.NotAfter < DateTime.Now)
			{
				return (ResultCodes.Expired, subject);
			}
			
			return chain.Build(certificateUnderValidation)
					? (ResultCodes.Success, subject)
					: (ResultCodes.InvalidChain, subject);
		}

		public static (ResultCodes, string) Validate(X509CertificateCollection certificates, IEnumerable<string> caAndChainPaths = null)
		{
			if(certificates is null)
			{
				return (ResultCodes.NotFound, null);
			}

			string lastSubject = null;

			foreach(var cert in certificates.Cast<X509Certificate2>())
			{
				var result = Validate(cert, caAndChainPaths);
				if(result.Item1 != ResultCodes.Success)
				{
					return result;
				}
				lastSubject = result.Item2;
			}

			return (ResultCodes.Success, lastSubject);
		}

		const string CertSubjectStr = @"CN=(?<issueto>[^, ]+),?\s*";

#if NET7_0_OR_GREATER
		//CN=tls1, O=Aerolab, S=CA, C=US
		[GeneratedRegex(CertSubjectStr,
							RegexOptions.IgnoreCase | RegexOptions.Compiled)]
		private static partial Regex CertSubjectRegEx();
#else
        readonly static Regex CertSubjectRegExVar = new Regex(CertSubjectStr,
																RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex CertSubjectRegEx() => CertSubjectRegExVar;
#endif

		public static string ToIssuer(string subject)
		{
			if(string.IsNullOrEmpty(subject))
				return null;

			var match = CertSubjectRegEx().Match(subject);

			if(match.Success)
				return match.Groups["issueto"].Value;

			return null;
		}

	}
}
