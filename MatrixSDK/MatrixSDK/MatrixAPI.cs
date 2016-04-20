using System;
using System.Threading.Tasks;
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
using MatrixSDK.Backends;
namespace MatrixSDK
{
	public delegate void MatrixAPIRoomJoinedDelegate(string roomid, MatrixEventRoomJoined joined);
	public class MatrixAPI
	{
		public const string VERSION = "r0.0.1";
		public bool IsConnected { get; private set; }
		public bool RunningInitialSync { get; private set; }
		public int BadSyncTimeout = 25000;

		private string syncToken = "";
		MatrixLoginResponse current_login = null;
		Thread poll_thread;
		bool shouldRun = false;
		ConcurrentQueue<MatrixAPIPendingEvent> pendingMessages  = new ConcurrentQueue<MatrixAPIPendingEvent> ();
		Random rng;
		JSONSerializer matrixSerializer;
		IMatrixAPIBackend mbackend;



		public event MatrixAPIRoomJoinedDelegate SyncJoinEvent;

		/// <summary>
		/// Timeout in seconds between sync requests.
		/// </summary>
		public int SyncTimeout = 10000;

		public MatrixAPI (string URL,string token = "")
		{
			mbackend = new HttpBackend (URL);
			matrixSerializer = new JSONSerializer ();
			rng = new Random (DateTime.Now.Millisecond);
			syncToken = token;
			if (syncToken == "") {
				RunningInitialSync = true;
			}
		}

		private void pollThread_Run(){
			while (shouldRun) {
				try
				{
				ClientSync (true);
				}
				catch(Exception e){
					#if DEBUG
					Console.WriteLine ("[warn] A Matrix exception occured during sync!");
					Console.WriteLine (e);
					#endif
				}
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

		public void ClientTokenRefresh(string refreshToken){
			JObject request = new JObject ();
			request.Add ("refresh_token", refreshToken);
			JObject response;
			MatrixRequestError error = mbackend.Post ("/_matrix/r0/tokenrefresh", true, request,out response);
			if (!error.IsOk) {
				throw new MatrixServerError (error.MatrixErrorCode.ToString(), error.MatrixError);
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

		public void StopSyncThreads(){
			shouldRun = false;
			poll_thread.Join ();
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
			MatrixRequestError error = mbackend.Put (String.Format ("/_matrix/client/r0/rooms/{0}/send/{1}/{2}", System.Uri.EscapeDataString(msg.room_id), msg.type, msg.txnId), true, msgData,out result);

			#if DEBUG
			if(!error.IsOk){
				Console.WriteLine (error.GetErrorString());
			}
			#endif
			return error.IsOk;
		}

		[MatrixSpec("r0.0.1/client_server.html#post-matrix-client-r0-login")]
		public void ClientLogin(MatrixLogin login){
			JObject result;
			MatrixRequestError error = mbackend.Post ("/_matrix/client/r0/login",false,JObject.FromObject(login),out result);
			if (error.IsOk) {
				current_login = result.ToObject<MatrixLoginResponse> ();
				mbackend.SetAccessToken (current_login.access_token);
			} else {
				throw new MatrixException (error.ToString());//TODO: Need a better exception
			}
		}

		[MatrixSpec("r0.0.1/client_server.html#get-matrix-client-r0-profile-userid")]
		public MatrixProfile ClientProfile(string userid){
			JObject response;
			MatrixRequestError error = mbackend.Get ("/_matrix/client/r0/profile/" + userid,true, out response);
			if (error.IsOk) {
				return response.ToObject<MatrixProfile> ();
			} else {
				return null;
			}
		}

		[MatrixSpec("r0.0.1/client_server.html#get-matrix-client-r0-sync")]
		public void ClientSync(bool ConnectionFailureTimeout = false){
			JObject response;
			string url = "/_matrix/client/r0/sync?timeout="+SyncTimeout;
			if (!String.IsNullOrEmpty(syncToken)) {
				url += "&since=" + syncToken;
			}
			MatrixRequestError error = mbackend.Get (url,true, out response);
			if (error.IsOk) {
				try {
					MatrixSync sync = JsonConvert.DeserializeObject<MatrixSync> (response.ToString (), new JSONEventConverter ());
					processSync (sync);
					IsConnected = true;
				} catch (Exception e) {
					throw new MatrixException ("Could not decode sync", e);
				}
			} else if (ConnectionFailureTimeout) {
				IsConnected = false;
				Console.Error.WriteLine ("Couldn't reach the matrix home server during a sync.");
				Console.Error.WriteLine(error.ToString());
				Thread.Sleep (BadSyncTimeout);
			}
			if (RunningInitialSync)
				RunningInitialSync = false;
		}

		[MatrixSpec("r0.0.1/client_server.html#get-matrix-client-versions")]
		public string[] ClientVersions(){
			JObject result;
			MatrixRequestError error = mbackend.Get ("/_matrix/client/versions",false, out result);
			if (error.IsOk) {
				return result.GetValue ("versions").ToObject<string[]> ();
			} else {
				throw new MatrixException ("Non OK result returned from request");//TODO: Need a better exception
			}
		}

		[MatrixSpec("r0.0.1/client_server.html#post-matrix-client-r0-rooms-roomid-join")]
		public string ClientJoin(string roomid){
			JObject result;
			MatrixRequestError error = mbackend.Post(String.Format("/_matrix/client/r0/join/{0}",System.Uri.EscapeDataString(roomid)),true,null,out result);
			if (error.IsOk) {
				roomid = result ["room_id"].ToObject<string> ();
				return roomid;
			} else {
				return null;
			}
				
		}

		[MatrixSpec("r0.0.1/client_server.html#post-matrix-client-r0-rooms-roomid-leave")]
		public void RoomLeave(string roomid){
			JObject result;
			MatrixRequestError error = mbackend.Post(String.Format("/_matrix/client/r0/rooms/{0}/leave",System.Uri.EscapeDataString(roomid)),true,null,out result);
			if (!error.IsOk) {
				throw new Exception (error.ToString ());
			}
		}

		[MatrixSpec("r0.0.1/client_server.html#post-matrix-client-r0-createroom")]
		public string ClientCreateRoom(MatrixCreateRoom roomrequest = null){
			JObject result;
			JObject req = null;
			if (roomrequest != null) {
				req = ObjectToJson(roomrequest);
			}
			MatrixRequestError error = mbackend.Post ("/_matrix/client/r0/createRoom", true, req, out result);
			if (error.IsOk) {
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
			MatrixRequestError error = mbackend.Put (String.Format ("/_matrix/client/r0/rooms/{0}/state/{1}", System.Uri.EscapeDataString(roomid),type), true, msgData,out result);
			if (!error.IsOk) {
				throw new Exception (error.ToString());//TODO: Need a better exception
			}
		}

		[MatrixSpec("r0.0.1/client_server.html#post-matrix-client-r0-rooms-roomid-invite")]
		public void InviteToRoom(string roomid, string userid){
			JObject result;
			JObject msgData = JObject.FromObject(new {user_id=userid});
			MatrixRequestError error = mbackend.Post (String.Format ("/_matrix/client/r0/rooms/{0}/invite", System.Uri.EscapeDataString(roomid)), true, msgData,out result);
			if (!error.IsOk) {
				throw new Exception (error.ToString());//TODO: Need a better exception
			}
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

