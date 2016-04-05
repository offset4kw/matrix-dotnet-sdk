using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json.Linq;
using MatrixSDK.Exceptions;
using MatrixSDK.Structures;
namespace MatrixSDK
{
	public class MatrixAPI
	{
		public const string VERSION = "r0.0.1";
		string baseurl;
		HttpClient client;

		MatrixLoginResponse current_login = null;

		public MatrixAPI (string URL)
		{
			ServicePointManager.MaxServicePoints = 10;
			ServicePointManager.ServerCertificateValidationCallback += acceptCertificate;
			baseurl = URL;
			if (!URL.Contains ("/_matrix/")) {
				baseurl += "/_matrix/";
			}
			client = new HttpClient ();
			client.BaseAddress = new Uri (baseurl);
		}

		private bool acceptCertificate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors){
			return true;//Find a better way to handle mono certs.
		}

		private HttpStatusCode GetRequest(string apiPath, out JObject result){
			Task<HttpResponseMessage> task = client.GetAsync (apiPath);
			return GenericRequest (task, out result);
		}

		private HttpStatusCode PostRequest(string apiPath,JObject data, out JObject result){
			StringContent content = new StringContent (data.ToString (), Encoding.UTF8, "application/json");
			Task<HttpResponseMessage> task = client.PostAsync(apiPath,content);
			return GenericRequest (task, out result);
		}

		private HttpStatusCode GenericRequest(Task<HttpResponseMessage> task, out JObject result){//Cleanup
			Task<string> stask = null;
			result = null;
			try
			{
				task.Wait();
				if (task.Status == TaskStatus.RanToCompletion) {
						stask = task.Result.Content.ReadAsStringAsync();
						stask.Wait();
				}
				else
				{
					return task.Result.StatusCode;
				}
			}
			catch(WebException e){
				throw e;
			}
			catch(AggregateException e){
				throw new MatrixException (e.InnerException.Message,e.InnerException);
			}
			if (stask.Status == TaskStatus.RanToCompletion) {
				result = JObject.Parse (stask.Result);
				if (result ["errcode"] != null) {
					throw new MatrixServerError (result ["errcode"].ToObject<string> (), result ["error"].ToObject<string> ());
				}
			}
			return task.Result.StatusCode;
			
		}

		public string[] GetVersions(){
			JObject result;
			HttpStatusCode code = GetRequest ("client/versions", out result);
			if (code == HttpStatusCode.OK) {
				return result.GetValue ("versions").ToObject<string[]> ();
			} else {
				throw new MatrixException ("Non OK result returned from request");//TODO: Need a better exception
			}
		}

		public static bool IsVersionSupported(string[] version){
			return (new List<string> (version).Contains (VERSION));//TODO: Support version checking properly.
		}

		public void Login(MatrixLogin login){
			JObject result;
			HttpStatusCode code = PostRequest ("/_matrix/client/r0/login",JObject.FromObject(login),out result);
			if (code == HttpStatusCode.OK) {
				current_login = result.ToObject<MatrixLoginResponse> ();
			} else {
				throw new MatrixException ("Non OK result returned from request");//TODO: Need a better exception
			}
		}

		public bool IsLoggedIn(){
			//TODO: Check token is still valid
			return current_login != null;
		}

		public MatrixUser GetUser(string userid){
			JObject response;
			HttpStatusCode code = GetRequest ("client/r0/profile/" + userid,out response);
			if (code == HttpStatusCode.OK) {
				MatrixUser user = response.ToObject<MatrixUser> ();
				user.userid = userid;
				return user;
			}
			return null;
		}
	}
}

