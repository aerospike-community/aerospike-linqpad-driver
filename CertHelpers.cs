using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;

namespace Aerospike.Database.LINQPadDriver
{
	internal static class CertHelpers
	{
		private static byte[] ConvertPEMtoDER(byte[] pemData)
		{
			// strips ---HEADERS--- then base64-decodes the PEM body to arrive at the
			// DER-encoded certificate data
			string b64String = Encoding.UTF8.GetString(pemData);
			b64String = Regex.Replace(b64String, "-+BEGIN CERTIFICATE-+", "");
			b64String = Regex.Replace(b64String, "-+END CERTIFICATE-+", "");
			return Convert.FromBase64String(b64String.Trim());
		}

		internal static X509Certificate2 LoadCertificateFromFile(string certificatePath)
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

		internal static bool Validate(string certificatePath, IEnumerable<string> caAndChainPaths = null)
		{

			using var certificateUnderValidation = LoadCertificateFromFile(certificatePath);
			return Validate(certificateUnderValidation, caAndChainPaths);
		}

		internal static bool Validate(X509Certificate2 certificateUnderValidation, IEnumerable<string> caAndChainPaths = null)
		{

			X509Certificate2Collection caAndChain = null;

			if(caAndChainPaths is not null && caAndChainPaths.Any())
			{
				caAndChain = new X509Certificate2Collection(caAndChainPaths.Select(file => LoadCertificateFromFile(file)).ToArray());
			}

			using var chain = new X509Chain();

			if(caAndChain is not null)
			{
				// .NET 5+ has a new 'CustomTrustStore' mode that permits ignoring the OS trust
				// and ExtraTrust stores, and explicitly verify against an expected root CA (and
				// its chain). This avoids the PartialChain issues in .NET Core 3 arising from
				// the use of AllowUnknownCertificateAuthority, and allows us to trust the
				// X509Chain.Build() verification result without extra steps.
				chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
				chain.ChainPolicy.CustomTrustStore.AddRange(caAndChain);
			}
			chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
			return chain.Build(certificateUnderValidation);
		}

		internal static bool Validate(X509CertificateCollection certificates, IEnumerable<string> caAndChainPaths = null)
		{
			foreach(X509Certificate2 cert in certificates)
			{
				if(!Validate(cert, caAndChainPaths))
				{
					return false;					
				}
			}

			return true;
		}
	}
}
