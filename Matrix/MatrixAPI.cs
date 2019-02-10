using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using Newtonsoft.Json.Linq;
using Matrix.Structures;
using Matrix.Backends;
using Microsoft.Extensions.Logging;

/**
 * This class contains all the methods needed to call the Matrix C2S API. The methods are split into files
 * inside ./Api.
 */

namespace Matrix
{
    public delegate void MatrixAPIRoomJoinedDelegate(string roomid, MatrixEventRoomJoined joined);
    public delegate void MatrixAPIRoomInviteDelegate(string roomid, MatrixEventRoomInvited invited);
	public partial class MatrixAPI
	{
		private ILogger log = Logger.Factory.CreateLogger<MatrixAPI>();
		public const string VERSION = "r0.0.1";
		public bool IsConnected { get; private set; }
		public virtual bool RunningInitialSync { get; private set; } = true;
		public virtual string BaseURL  { get; private set; }
		public int BadSyncTimeout { get; set; } = 25000;
		public int FailMessageAfter { get; set; } = 300;
		public virtual string user_id {get; set;} = null;

		string syncToken = "";
		bool IsAS;

		MatrixLoginResponse current_login = null;
		Thread poll_thread;
		bool shouldRun = false;
		ConcurrentQueue<MatrixAPIPendingEvent> pendingMessages  = new ConcurrentQueue<MatrixAPIPendingEvent> ();
		Random rng;

		static JSONSerializer matrixSerializer = new JSONSerializer ();

		JSONEventConverter event_converter;

		IMatrixAPIBackend mbackend;


        public event MatrixAPIRoomJoinedDelegate SyncJoinEvent;
        public event MatrixAPIRoomInviteDelegate SyncInviteEvent;

		/// <summary>
		/// Timeout in seconds between sync requests.
		/// </summary>
		public int SyncTimeout {get;set;} = 10000;

		public MatrixAPI (string URL)
		{
			if (!Uri.IsWellFormedUriString (URL, UriKind.Absolute)) {
				throw new MatrixException ("URL is not valid");
			}

			IsAS = false;
			mbackend = new HttpBackend (URL);
			BaseURL = URL;
			rng = new Random (DateTime.Now.Millisecond);
			event_converter = new JSONEventConverter ();
		}

		public MatrixAPI(string URL, string application_token, string user_id){
			if (!Uri.IsWellFormedUriString (URL, UriKind.Absolute)) {
				throw new MatrixException("URL is not valid");
			}

			IsAS = true;
			mbackend = new HttpBackend (URL,user_id);
			mbackend.SetAccessToken(application_token);
			this.user_id = user_id;
			BaseURL = URL;
			rng = new Random (DateTime.Now.Millisecond);
			event_converter = new JSONEventConverter ();
		}

		public MatrixAPI (string URL, IMatrixAPIBackend backend)
		{
			if (!Uri.IsWellFormedUriString (URL, UriKind.Absolute)) {
				throw new MatrixException("URL is not valid");
			}

			IsAS = true;
			mbackend = backend;
			BaseURL = URL;
			rng = new Random (DateTime.Now.Millisecond);
			event_converter = new JSONEventConverter ();
		}


		public void AddMessageType (string name, Type type)
		{
			event_converter.AddMessageType(name,type);
		}

		public void AddEventType (string msgtype, Type type)
		{
			event_converter.AddEventType(msgtype, type);
		}

        public void FlushMessageQueue ()
		{
			MatrixAPIPendingEvent evt;
			MatrixRequestError error;
			while (pendingMessages.TryDequeue (out evt)) {
				error = RoomSend(evt);
				if (!error.IsOk) {

					if (error.MatrixErrorCode != MatrixErrorCode.M_UNKNOWN) { //M_UNKNOWN unoffically means it failed to validate.
						Console.WriteLine("Trying to resend failed message of type " + evt.type);
						evt.backoff_duration += evt.backoff;
						evt.backoff = evt.backoff == 0 ? 2: (int)Math.Pow(evt.backoff,2);
						if (evt.backoff_duration > FailMessageAfter) {
							evt.backoff = 0;
							continue; //Give up trying to send
						}

						Console.WriteLine($"Waiting {evt.backoff} seconds before resending");

						Thread.Sleep(evt.backoff*1000);
						pendingMessages.Enqueue (evt);

					}
				}
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
                FlushMessageQueue();
				Thread.Sleep(250);
			}
		}

		public void SetSyncToken(string synctoken){
			syncToken = synctoken;
			RunningInitialSync = false;
		}

		public virtual string GetSyncToken(){
			return syncToken;
		}

		public virtual string GetAccessToken ()
		{
			if (current_login != null) {
				return current_login.access_token;
			}
			else
			{
				return null;
			}
		}

		public virtual MatrixLoginResponse GetCurrentLogin ()
		{
			return current_login;
		}

		public void SetLogin(MatrixLoginResponse response){
			current_login = response;
			user_id = response.user_id;
			mbackend.SetAccessToken(response.access_token);
		}

		public void ClientTokenRefresh(string refreshToken){
			JObject request = new JObject ();
			request.Add ("refresh_token", refreshToken);
			MatrixRequestError error = mbackend.Post ("/_matrix/r0/tokenrefresh", true, request,out var response);
			if (!error.IsOk) {
				throw new MatrixServerError (error.MatrixErrorCode.ToString(), error.MatrixError);
			}
		}

		public static JObject ObjectToJson (object data)
		{
			JObject container;
			using (JTokenWriter writer = new JTokenWriter ()) {
				try {
					matrixSerializer.Serialize (writer, data);
					container = (JObject)writer.Token;
				} catch (Exception e) {
					throw new Exception("Couldn't convert obj to JSON",e);
				}
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

		private void ProcessSync(MatrixSync syncData){
			syncToken = syncData.next_batch;
			//Grab data from rooms the user has joined.
			foreach (KeyValuePair<string,MatrixEventRoomJoined> room in syncData.rooms.join)
			{
				SyncJoinEvent?.Invoke (room.Key, room.Value);
			}
            foreach (KeyValuePair<string,MatrixEventRoomInvited> room in syncData.rooms.invite)
            {
	            SyncInviteEvent?.Invoke (room.Key, room.Value);
            }

		}

		[MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "get-matrix-client-versions")]
		public string[] ClientVersions(){
			MatrixRequestError error = mbackend.Get ("/_matrix/client/versions",false, out var result);
			if (error.IsOk) {
				return (result as JObject)?.GetValue ("versions").ToObject<string[]> ();
			} else {
				throw new MatrixException ("Non OK result returned from request");//TODO: Need a better exception
			}
		}

		[MatrixSpec(EMatrixSpecApiVersion.R040, EMatrixSpecApi.ClientServer,
			"get-matrix-client-r0-joined_rooms")]
		public List<string> GetJoinedRooms()
		{
			MatrixRequestError error = mbackend.Get("/_matrix/client/r0/joined_rooms", true, out var result);
			if (!error.IsOk) {
				throw new MatrixException (error.ToString());
			}
			return (result as JObject)?.GetValue("joined_rooms").ToObject<List<string>>();
		}

		public void RegisterUserAsAS (string user)
		{
			if(!IsAS){
				throw new MatrixException("This client is not registered as a application service client. You can't create new appservice users");
			}
			JObject request = JObject.FromObject( new {
				type = "m.login.application_service",
				user = user
			});

			MatrixRequestError error = mbackend.Post("/_matrix/client/r0/register",true,request,out var result);
			if (!error.IsOk) {
				throw new MatrixException (error.ToString());//TODO: Need a better exception
			}
		}
	}

	public class MatrixAPIPendingEvent : MatrixEvent{
		public int txnId;
		public int backoff = 0;
		public int backoff_duration = 0;
	}
}
