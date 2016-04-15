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
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using MatrixSDK.Exceptions;
using MatrixSDK.Structures;
namespace MatrixSDK
{
	public delegate void MatrixAPIRoomJoinedDelegate(string roomid, MatrixEventRoomJoined joined);
	public class MatrixAPI
	{
		public const string VERSION = "r0.0.1";
		string baseurl;
		private string syncToken = "";
		HttpClient client;
		MatrixLoginResponse current_login = null;
		Thread poll_thread;
		bool shouldRun = false;
		ConcurrentQueue<MatrixAPIPendingEvent> pendingMessages  = new ConcurrentQueue<MatrixAPIPendingEvent> ();
		Random rng;
		public bool RunningInitialSync { get; private set; }
		JSONSerializer matrixSerializer;

		public event MatrixAPIRoomJoinedDelegate SyncJoinEvent;

		/// <summary>
		/// Timeout in seconds between sync requests.
		/// </summary>
		public int SyncTimeout = 10000;

		public MatrixAPI (string URL,string token = "")
		{
			ServicePointManager.ServerCertificateValidationCallback += acceptCertificate;
			matrixSerializer = new JSONSerializer ();
			baseurl = URL;
			if (baseurl.EndsWith ("/")) {
				baseurl = baseurl.Substring (0, baseurl.Length - 1);
			}
			client = new HttpClient ();
			rng = new Random (DateTime.Now.Millisecond);
			syncToken = token;
			if (syncToken == "") {
				RunningInitialSync = true;
			}
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

		public string GetSyncToken(){
			return syncToken;
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

		public void StopSyncThreads(){
			shouldRun = false;
			poll_thread.Join ();
		}

		private bool acceptCertificate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors){
			return true;//Find a better way to handle mono certs.
		}

		private HttpStatusCode GetRequest(string apiPath, bool authenticate, out JObject result){
			apiPath = baseurl + apiPath;
			#if DEBUG
			Console.WriteLine(apiPath);
			#endif
			if(authenticate){
				apiPath	+= (apiPath.Contains ("?") ? "&" : "?") + "access_token=" + current_login.access_token;
			}
			Task<HttpResponseMessage> task = client.GetAsync (apiPath);
			return GenericRequest (task, out result);
		}

		private HttpStatusCode PutRequest(string apiPath, bool authenticate, JObject data, out JObject result){
			apiPath = baseurl + apiPath;
			StringContent content = new StringContent (data.ToString (), Encoding.UTF8, "application/json");
			if(authenticate){
				apiPath	+= (apiPath.Contains ("?") ? "&" : "?") + "access_token=" + current_login.access_token;
			}
			Task<HttpResponseMessage> task = client.PutAsync(apiPath,content);
			return GenericRequest (task, out result);
		}

		private HttpStatusCode PostRequest(string apiPath, bool authenticate, JObject data, out JObject result){
			apiPath = baseurl + apiPath;
			StringContent content;
			if (data != null) {
				content = new StringContent (data.ToString (), Encoding.UTF8, "application/json");
			} else {
				content = new StringContent ("{}");
			}
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
				result = JObject.Parse (stask.Result);
				if (result ["errcode"] != null) {
					throw new MatrixServerError (result ["errcode"].ToObject<string> (), result ["error"].ToObject<string> ());
				}
			}
			return task.Result.StatusCode;
			
		}

		public JObject ObjectToJson(object data){
			JObject container;
			using(JTokenWriter writer = new JTokenWriter()){
				matrixSerializer.Serialize(writer,data);
				container = (JObject)writer.Token;
			}
			return container;
		}
			
		public static bool IsVersionSupported(string[] version){
			return (new List<string> (version).Contains (VERSION));//TODO: Support version checking properly.
		}

		public bool IsLoggedIn(){
			//TODO: Check token is still valid
			return current_login != null;
		}

		private void processSync(MatrixSync syncData){
			syncToken = syncData.next_batch;
			//Grab data from rooms the user has joined.
			foreach (KeyValuePair<string,MatrixEventRoomJoined> room in syncData.rooms.join) {
				if (SyncJoinEvent != null) {
					SyncJoinEvent.Invoke (room.Key, room.Value);
				}
			}
		}

		private bool sendRoomMessage(MatrixAPIPendingEvent msg){
			JObject msgData = ObjectToJson (msg.content);
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
		}

		[MatrixSpec("r0.0.1/client_server.html#post-matrix-client-r0-login")]
		public void ClientLogin(MatrixLogin login){
			JObject result;
			HttpStatusCode code = PostRequest ("/_matrix/client/r0/login",false,JObject.FromObject(login),out result);
			if (code == HttpStatusCode.OK) {
				current_login = result.ToObject<MatrixLoginResponse> ();
			} else {
				throw new MatrixException ("Non OK result returned from request");//TODO: Need a better exception
			}
		}

		[MatrixSpec("r0.0.1/client_server.html#get-matrix-client-r0-profile-userid")]
		public MatrixProfile ClientProfile(string userid){
			JObject response;
			try
			{
				HttpStatusCode code = GetRequest ("/_matrix/client/r0/profile/" + userid,true, out response);
				if (code == HttpStatusCode.OK) {
					return response.ToObject<MatrixProfile> ();
				}
			}
			catch(MatrixServerError){
				return null;
			}
			return null;
		}

		[MatrixSpec("r0.0.1/client_server.html#get-matrix-client-r0-sync")]
		public void ClientSync(){
			JObject response;
			string url = "/_matrix/client/r0/sync?timeout="+SyncTimeout;
			if (!String.IsNullOrEmpty(syncToken)) {
				url += "&since=" + syncToken;
			}
			HttpStatusCode code = GetRequest (url,true, out response);
			if (code == HttpStatusCode.OK) {
				try
				{
					MatrixSync sync = JsonConvert.DeserializeObject<MatrixSync> (response.ToString (),new JSONEventConverter()	);
					processSync(sync);
				}
				catch(Exception e){
					throw new MatrixException ("Could not decode sync", e);
				}
			}
			if (RunningInitialSync)
				RunningInitialSync = false;
		}

		[MatrixSpec("r0.0.1/client_server.html#get-matrix-client-versions")]
		public string[] ClientVersions(){
			JObject result;
			HttpStatusCode code = GetRequest ("/_matrix/client/versions",false, out result);
			if (code == HttpStatusCode.OK) {
				return result.GetValue ("versions").ToObject<string[]> ();
			} else {
				throw new MatrixException ("Non OK result returned from request");//TODO: Need a better exception
			}
		}

		[MatrixSpec("r0.0.1/client_server.html#post-matrix-client-r0-rooms-roomid-join")]
		public string ClientJoin(string roomid){
			JObject result;
			HttpStatusCode code = PostRequest(String.Format("/_matrix/client/r0/join/{0}",System.Uri.EscapeDataString(roomid)),true,null,out result);
			if (code == HttpStatusCode.OK) {
				roomid = result ["room_id"].ToObject<string> ();
				return roomid;
			} else {
				return null;
			}
				
		}

		[MatrixSpec("r0.0.1/client_server.html#post-matrix-client-r0-rooms-roomid-leave")]
		public void RoomLeave(string roomid){
			JObject result;
			PostRequest(String.Format("/_matrix/client/r0/rooms/{0}/leave",System.Uri.EscapeDataString(roomid)),true,null,out result);
		}

		[MatrixSpec("r0.0.1/client_server.html#post-matrix-client-r0-createroom")]
		public string ClientCreateRoom(MatrixCreateRoom roomrequest = null){
			JObject result;
			JObject req = null;
			if (roomrequest != null) {
				req = ObjectToJson(roomrequest);
			}
			HttpStatusCode code = PostRequest ("/_matrix/client/r0/createRoom", true, req, out result);
			if (code == HttpStatusCode.OK) {
				string roomid = result ["room_id"].ToObject<string> ();
				return roomid;
			} else {
				return null;
			}
		}

		[MatrixSpec("r0.0.1/client_server.html#put-matrix-client-r0-rooms-roomid-state-eventtype")]
		public void RoomStateSend(string roomid,string type,MatrixRoomStateEvent message){
			JObject msgData = JObject.FromObject (message);
			JObject result;
			PutRequest (String.Format ("/_matrix/client/r0/rooms/{0}/state/{1}", System.Uri.EscapeDataString(roomid),type), true, msgData,out result);
		}

		[MatrixSpec("r0.0.1/client_server.html#post-matrix-client-r0-rooms-roomid-invite")]
		public void InviteToRoom(string roomid, string userid){
			JObject result;
			JObject msgData = JObject.FromObject(new {user_id=userid});
			PostRequest (String.Format ("/_matrix/client/r0/rooms/{0}/invite", System.Uri.EscapeDataString(roomid)), true, msgData,out result);

		}


		[MatrixSpec("r0.0.1/client_server.html#put-matrix-client-r0-rooms-roomid-send-eventtype-txnid")]
		public void RoomMessageSend(string roomid,string type,MatrixMRoomMessage message){
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
	}

	public class MatrixAPIPendingEvent{
		public string type;
		public string room_id;
		public int txnId;
		public MatrixEventContent content;
	}
}

