using System;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using MatrixSDK.Exceptions;
using MatrixSDK.Structures;
namespace MatrixSDK
{
	public class MatrixAPI
	{
		public const string VERSION = "r0.0.1";
		string baseurl;
		string syncToken = "";
		HttpClient client;
		MatrixLoginResponse current_login = null;
		Thread poll_thread;
		bool shouldRun = false;
		Dictionary<string,MatrixRoom> rooms = new Dictionary<string,MatrixRoom>();
		ConcurrentQueue<MatrixAPIPendingEvent> pendingMessages = new ConcurrentQueue<MatrixAPIPendingEvent> ();
		Random rng;

		/// <summary>
		/// Timeout in seconds between sync requests.
		/// </summary>
		public int SyncTimeout = 10000;

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
			rng = new Random (DateTime.Now.Millisecond);
		}

		private void pollThread_Run(){
			while (shouldRun) {
				ClientSync ();
				MatrixAPIPendingEvent evt;
				while (pendingMessages.TryDequeue(out evt)) {
					if (!sendRoomMessage (evt)) {
						pendingMessages.Enqueue(evt);
					}
				}
				Thread.Sleep(250);
			}
		}

		public void StartSyncThreads(){
			if (poll_thread == null) {
				poll_thread = new Thread (pollThread_Run);
				poll_thread.Start ();
				shouldRun = true;
			} else {
				if (poll_thread.IsAlive) {
					throw new Exception ("Can't start thread, already running");
				} else {
					poll_thread.Start ();
				}
			}

		}

		private bool acceptCertificate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors){
			return true;//Find a better way to handle mono certs.
		}

		private HttpStatusCode GetRequest(string apiPath, bool authenticate, out JObject result){
			if(authenticate){
				apiPath	+= (apiPath.Contains ("?") ? "&" : "?") + "access_token=" + current_login.access_token;
			}
			Task<HttpResponseMessage> task = client.GetAsync (apiPath);
			return GenericRequest (task, out result);
		}

		private HttpStatusCode PutRequest(string apiPath, bool authenticate, JObject data, out JObject result){
			StringContent content = new StringContent (data.ToString (), Encoding.UTF8, "application/json");
			if(authenticate){
				apiPath	+= (apiPath.Contains ("?") ? "&" : "?") + "access_token=" + current_login.access_token;
			}
			Task<HttpResponseMessage> task = client.PutAsync(apiPath,content);
			return GenericRequest (task, out result);
		}

		private HttpStatusCode PostRequest(string apiPath, bool authenticate, JObject data, out JObject result){
			StringContent content = new StringContent (data.ToString (), Encoding.UTF8, "application/json");
			if(authenticate){
				apiPath	+= (apiPath.Contains ("?") ? "&" : "?") + "access_token=" + current_login.access_token;
			}
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
				System.IO.File.WriteAllText ("/tmp/sync.json", stask.Result);
				result = JObject.Parse (stask.Result);
				if (result ["errcode"] != null) {
					throw new MatrixServerError (result ["errcode"].ToObject<string> (), result ["error"].ToObject<string> ());
				}
			}
			return task.Result.StatusCode;
			
		}
			
		public static bool IsVersionSupported(string[] version){
			return (new List<string> (version).Contains (VERSION));//TODO: Support version checking properly.
		}

		public void ClientLogin(MatrixLogin login){
			JObject result;
			HttpStatusCode code = PostRequest ("/_matrix/client/r0/login",false,JObject.FromObject(login),out result);
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

		public MatrixProfile ClientProfile(string userid){
			JObject response;
			HttpStatusCode code = GetRequest ("client/r0/profile/" + userid,true, out response);
			if (code == HttpStatusCode.OK) {
				return response.ToObject<MatrixProfile> ();
			}
			return null;
		}

		public void ClientSync(){
			JObject response;
			string url = "client/r0/sync?timeout"+SyncTimeout;
			if (!String.IsNullOrEmpty(syncToken)) {
				url += "&since=" + syncToken;
			}
			HttpStatusCode code = GetRequest (url,true, out response);
			if (code == HttpStatusCode.OK) {
				try
				{
					MatrixSync sync = JsonConvert.DeserializeObject<MatrixSync> (response.ToString (),new JsonEventConverter()	);
					processSync(sync);
				}
				catch(Exception e){
					throw new MatrixException ("Could not decode sync", e);
				}
			}
		}

		private void processSync(MatrixSync syncData){
			syncToken = syncData.next_batch;
			//Grab data from rooms the user has joined.
			foreach (KeyValuePair<string,MatrixEventRoomJoined> room in syncData.rooms.join) {
				if (rooms.ContainsKey (room.Key)) {
					//Update existing room
				} else {
					MatrixRoom mroom = new MatrixRoom (this,room.Key);
					room.Value.state.events.ToList ().ForEach (x => {mroom.FeedEvent (x);});
					room.Value.timeline.events.ToList ().ForEach (x => {mroom.FeedEvent (x);});
					rooms.Add (room.Key, mroom);
				}
			}
		}

		public string[] GetVersions(){
			JObject result;
			HttpStatusCode code = GetRequest ("client/versions",false, out result);
			if (code == HttpStatusCode.OK) {
				return result.GetValue ("versions").ToObject<string[]> ();
			} else {
				throw new MatrixException ("Non OK result returned from request");//TODO: Need a better exception
			}
		}


		public MatrixRoom GetRoom(string roomid){
			if (rooms.ContainsKey (roomid)) { //TODO: Find room by alias or name
				return rooms [roomid];
			} else {
				return null;
				//TODO: Attempt to find the room if not in the timeline.
			}
		}

		public MatrixRoom[] GetRooms(){
			return rooms.Values.ToArray ();
		}

		public void QueueRoomMessage(string roomid,string type,MatrixMRoomMessage message){
			bool collision = true;
			MatrixAPIPendingEvent evt = new MatrixAPIPendingEvent ();
			evt.room_id = roomid;
			evt.type = type;
			evt.content = message;
			while (collision) {
				evt.txnId = rng.Next (1,64);
				collision = pendingMessages.FirstOrDefault (x => x.txnId == evt.txnId) != default(MatrixAPIPendingEvent);
			}
			pendingMessages.Enqueue(evt);
		}

		private bool sendRoomMessage(MatrixAPIPendingEvent msg){
			JObject msgData = JObject.FromObject (msg.content);
			JObject result;
			try
			{
				HttpStatusCode code = PutRequest (String.Format ("/_matrix/client/r0/rooms/{0}/send/{1}/{2}", System.Uri.EscapeDataString(msg.room_id), msg.type, msg.txnId), true, msgData,out result);
				return code == HttpStatusCode.OK;
			}
			catch(MatrixException e){
				Console.WriteLine ("Exception occured sending message: " + e.Message);
				return false;
			}

			return true;
		}

	}

	public class MatrixAPIPendingEvent{
		public string type;
		public string room_id;
		public int txnId;
		public MatrixEventContent content;
	}
}

