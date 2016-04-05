using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
namespace MatrixSDK {
	/// <summary>
	/// This class will accept any HTTPS certificate given to it. Mono has no proper way (as of writing) to determine trust reliably so we leave it to the user to be aware of this and act accordingly.
	/// </summary>
	public class CertificatePolicy : ICertificatePolicy {
		public bool CheckValidationResult (ServicePoint sp,
			X509Certificate certificate, WebRequest request, int error)
		{
			return true;
		}
	}
}