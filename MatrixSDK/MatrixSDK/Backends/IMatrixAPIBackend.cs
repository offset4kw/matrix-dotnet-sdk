using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace MatrixSDK.Backends
{
	public interface IMatrixAPIBackend
	{
		MatrixRequestError Get  (string apiPath, bool authenticate, out JObject result);
		MatrixRequestError Post (string apiPath, bool authenticate, JObject request, out JObject result);
		MatrixRequestError Post (string apiPath, bool authenticate, JObject request, Dictionary<string,string> headers, out JObject result);
		MatrixRequestError Post (string apiPath, bool authenticate, byte[] request , Dictionary<string,string> headers, out JObject result);
		MatrixRequestError Put  (string apiPath, bool authenticate, JObject request, out JObject result);

		void SetAccessToken(string access_token);
	}
}

