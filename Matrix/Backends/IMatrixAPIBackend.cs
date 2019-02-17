using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Matrix.Backends
{
	public interface IMatrixAPIBackend
	{
		MatrixRequestError Get  (string apiPath, bool authenticate, out JToken result);
		Task<MatrixAPIResult> GetAsync  (string apiPath, bool authenticate);
		MatrixRequestError Delete (string apiPath, bool authenticate, out JToken result);
		MatrixRequestError Post (string apiPath, bool authenticate, JToken request, out JToken result);
		MatrixRequestError Put (string apiPath, bool authenticate, JToken request, out JToken result);
		Task<MatrixAPIResult> PutAsync (string apiPath, bool authenticate, JToken request);
		MatrixRequestError Post (string apiPath, bool authenticate, JToken request, Dictionary<string,string> headers, out JToken result);
		MatrixRequestError Post (string apiPath, bool authenticate, byte[] request , Dictionary<string,string> headers, out JToken result);

		void SetAccessToken(string access_token);
	}

	public struct MatrixAPIResult
	{
		public JToken result;
		public MatrixRequestError error;
	}
}

