using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MatrixSDK.Structures;
using Newtonsoft.Json;
namespace MatrixSDK.AppService
{
	public delegate void AliasRequestDelegate(string alias,HttpListenerContext context);
	public delegate void EventDelegate(MatrixEvent ev);
	public class MatrixAppservice
	{
		
		const int DEFAULT_MAXREQUESTS = 64;
		public readonly int Port;
		public readonly string Url;
		public readonly int MaximumRequests;
		public event AliasRequestDelegate OnAliasRequest;
		public event EventDelegate OnEvent;
		private Semaphore accept_semaphore;
		private HttpListener listener;
		private readonly Regex urlMatcher;
		public MatrixAppservice (string url = "http://localhost",int port = 9000,int maxrequests = DEFAULT_MAXREQUESTS)
		{
			Port = port;
			Url = url + ":"+port;
			MaximumRequests = maxrequests;
			urlMatcher = new Regex ("\\/(rooms|transactions|users)\\/(.+)", RegexOptions.Compiled | RegexOptions.ECMAScript);
		}

		public void Run(){
			listener = new HttpListener ();
			listener.Prefixes.Add (Url+"/rooms/");
			listener.Prefixes.Add (Url+"/transactions/");
			listener.Prefixes.Add (Url+"/users/");
			listener.Start ();
			accept_semaphore = new Semaphore (MaximumRequests, MaximumRequests);
			while(listener.IsListening){
				accept_semaphore.WaitOne ();
				listener.GetContextAsync ().ContinueWith (OnContext);
			}
		}
		
		private async void OnContext(Task<HttpListenerContext> task){
			await task;
			HttpListenerContext context = task.Result;
			Match match = urlMatcher.Match (context.Request.RawUrl);
			if (match.Groups.Count != 3) {
				context.Response.StatusCode = (int)HttpStatusCode.BadRequest; //Invalid response
			} else {
				context.Response.StatusCode = (int)HttpStatusCode.OK;
				if (OnAliasRequest != null && match.Groups[2].Value == "rooms") {
					if (context.Request.HttpMethod == "GET") {
						context.Response.StatusCode = (int)HttpStatusCode.NotFound;
						OnAliasRequest.Invoke (match.Groups [0].Value, context);
					} else {
						context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
					}
				}
				if (OnEvent != null && match.Groups[2].Value == "rooms") {
					if (context.Request.HttpMethod == "PUT") {
						byte[] data = new byte[context.Request.ContentLength64];
						context.Request.InputStream.Read (data, 0, data.Length);
						MatrixEvent[] events = JsonConvert.DeserializeObject<MatrixEvent[]> (System.Text.Encoding.UTF8.GetString(data), new JSONEventConverter ());
						foreach (MatrixEvent ev in events) {
							OnEvent.Invoke (ev);
						}
					} else {
						context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
					}
				}

			}
			context.Response.OutputStream.Write(new byte[2]{123,125},0,2);//{}
			context.Response.Close ();
			accept_semaphore.Release ();
		}
	}
}

