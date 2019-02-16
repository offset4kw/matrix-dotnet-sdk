using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Matrix.Client;
using Matrix.Structures;
using Newtonsoft.Json;

namespace Matrix.AppService
{
	struct ASEventBatch{
		public MatrixEvent[] events;
	}

	public delegate void AliasRequestDelegate(string alias,out bool roomExists);
	public delegate void UserRequestDelegate(string userid,out bool userExists);
	public delegate void EventDelegate(MatrixEvent ev);
	public class MatrixAppservice
	{

		const int DEFAULT_MAXREQUESTS = 64;
		public readonly string AsUrl;
		public readonly string HsUrl;
		public readonly int MaximumRequests;
		public readonly string Domain;

		public event AliasRequestDelegate OnAliasRequest;
		public event EventDelegate OnEvent;
		public event UserRequestDelegate OnUserRequest;

		public readonly ServiceRegistration Registration;

		private Semaphore _acceptSemaphore;
		private HttpListener _listener;
		private readonly Regex _urlMatcher;
		private readonly MatrixAPI _api;
		private readonly string _botuserId;

		public MatrixAppservice (ServiceRegistration registration, string domain, string url = "http://localhost",int maxrequests = DEFAULT_MAXREQUESTS)
		{
			HsUrl = url;
			Domain = domain;
			MaximumRequests = maxrequests;
			Registration = registration;
			AsUrl = registration.URL;
			_botuserId = "@"+ registration.Localpart + ":"+Domain;
			_urlMatcher = new Regex ("\\/(rooms|transactions|users)\\/(.+)\\?access_token=(.+)", RegexOptions.Compiled | RegexOptions.ECMAScript);

			_api = new MatrixAPI (url,registration.AppServiceToken, "");

		}

		public void Run(){
			_listener = new HttpListener ();
			_listener.Prefixes.Add (AsUrl+"/rooms/");
			_listener.Prefixes.Add (AsUrl+"/transactions/");
			_listener.Prefixes.Add (AsUrl+"/users/");
			_listener.Start();
			_acceptSemaphore = new Semaphore (MaximumRequests, MaximumRequests);
			while(_listener.IsListening){
				_acceptSemaphore.WaitOne ();
				_listener.GetContextAsync ().ContinueWith (OnContext);
			}
		}

		public MatrixClient GetClientAsUser (string user = null)
		{
			if (user != null) {
				if (user.EndsWith (":" + Domain)) {
					user = user.Substring(0,user.LastIndexOf(':'));
				}
				if (user.StartsWith ("@")) {
					user = user.Substring(1);
				}
				CheckAndPerformRegistration (user);
				user = "@" + user;
				user = user + ":" + Domain;
			} else {
				user = _botuserId;
			}

			return new MatrixClient(HsUrl,Registration.AppServiceToken,user);
		}

		private void CheckAndPerformRegistration (string user)
		{
			MatrixProfile profile = _api.ClientProfile ("@"+user+":"+Domain);
			if (profile == null) {
				_api.RegisterUserAsAS(user);
			}
		}

		private async void OnContext (Task<HttpListenerContext> task)
		{
			await task;
			HttpListenerContext context = task.Result;
			Match match = _urlMatcher.Match (context.Request.RawUrl);
			if (match.Groups.Count != 4) {
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest; //Invalid response
				context.Response.Close ();
				_acceptSemaphore.Release ();
				return;
			}

			if (match.Groups [3].Value != Registration.HomeserverToken) {
				context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
				context.Response.Close ();
				_acceptSemaphore.Release ();
			}

			string type = match.Groups [1].Value;
			context.Response.StatusCode = (int)HttpStatusCode.OK;

			//Check methods
			switch (type) {
				case "users":
				case "rooms":
					if (context.Request.HttpMethod != "GET")
						context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
					break;
				case "transactions":
					if (context.Request.HttpMethod != "PUT")
						context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
					break;
			}

			bool exists = false;
			if (context.Response.StatusCode == (int)HttpStatusCode.OK) {
				if (type == "rooms") {
					context.Response.StatusCode = (int)HttpStatusCode.NotFound;
					string alias = Uri.UnescapeDataString(match.Groups [2].Value);
					OnAliasRequest?.Invoke (alias, out exists);
				} else if (type == "transactions") {
					byte[] data = new byte[context.Request.ContentLength64];
					context.Request.InputStream.Read (data, 0, data.Length);
					ASEventBatch batch = JsonConvert.DeserializeObject<ASEventBatch> (Encoding.UTF8.GetString (data), new JSONEventConverter ());
					foreach (MatrixEvent ev in batch.events) {
						OnEvent?.Invoke (ev);
					}
				} else if (type == "users") {
					string user = Uri.UnescapeDataString(match.Groups [2].Value);
					OnUserRequest?.Invoke (user, out exists);
					context.Response.StatusCode = (int)HttpStatusCode.NotFound;

				}
			}
			
			if (exists) {
				context.Response.StatusCode = 200;
			}
			
			context.Response.OutputStream.Write(new byte[2]{123,125},0,2);//{}
			context.Response.Close ();
			_acceptSemaphore.Release ();
		}
	}
}
