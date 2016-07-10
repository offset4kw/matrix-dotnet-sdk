using System;
using System.Linq;
using System.Collections.Concurrent;
using MatrixSDK.Exceptions;
using MatrixSDK.Structures;
namespace MatrixSDK.Client
{
	/// <summary>
	/// The Matrix Client is a wrapper over the MatrixAPI object which provides a safe managed way
	/// to interact with a Matrix Home Server.
	/// </summary>
	public class MatrixClient : IDisposable
	{

		MatrixAPI api;
        public delegate void MatrixInviteDelegate(string roomid, MatrixEventRoomInvited joined);

		/// <summary>
		/// How long to poll for a Sync request before we retry.
		/// </summary>
		/// <value>The sync timeout in milliseconds.</value>
		public int SyncTimeout { get {return api.SyncTimeout;} set{ api.SyncTimeout = value; } }
		ConcurrentDictionary<string,MatrixRoom> rooms 			= new ConcurrentDictionary<string,MatrixRoom>();

        public event MatrixInviteDelegate OnInvite;



		/// <summary>
		/// Initializes a new instance of the <see cref="MatrixSDK.MatrixClient"/> class.
		/// The client will preform a connection and try to retrieve version information.
		/// If this fails, a MatrixUnsuccessfulConnection Exception will be thrown.
		/// </summary>
		/// <param name="URL">URL before /_matrix/</param>
		/// <param name="syncToken"> If you stored the sync token before, you can set it for the API here</param>
		public MatrixClient (string URL,string syncToken = "")
		{
			api = new MatrixAPI (URL,syncToken);
			try{
				string[] versions = api.ClientVersions ();
				if(!MatrixAPI.IsVersionSupported(versions)){
					Console.WriteLine("Warning: Client version is not supported by the server");
					Console.WriteLine("Client supports up to "+MatrixAPI.VERSION);
				}
				api.SyncJoinEvent += MatrixClient_OnEvent;
                api.SyncInviteEvent += MatrixClient_OnInvite;
			}
			catch(MatrixException e){
				throw new MatrixException("An exception occured while trying to connect",e);
			}
		}
		/// <summary>
		/// Initializes a new instance of the <see cref="MatrixSDK.Client.MatrixClient"/> class.
		/// This intended for Application Services only who want to preform actions as another user.
		/// Sync is not preformed.
		/// </summary>
		/// <param name="URL">URL before /_matrix/</param>
		/// <param name="application_token">Application token for the AS.</param>
		/// <param name="userid">Userid as the user you intend to go as.</param>
		public MatrixClient (string URL, string application_token,string userid)
		{
			api = new MatrixAPI (URL,application_token,userid);

			try{
				string[] versions = api.ClientVersions ();
				if(!MatrixAPI.IsVersionSupported(versions)){
					Console.WriteLine("Warning: Client version is not supported by the server");
					Console.WriteLine("Client supports up to "+MatrixAPI.VERSION);
				}
			}
			catch(MatrixException e){
				throw new MatrixException("An exception occured while trying to connect",e);
			}
		}
		/// <summary>
		/// Gets the sync token from the API. 
		/// </summary>
		/// <returns>The sync token.</returns>
		public string GetSyncToken(){
			return api.GetSyncToken ();
		}

        public void MatrixClient_OnInvite(string roomid, MatrixEventRoomInvited joined){
            if(OnInvite != null){
                OnInvite.Invoke(roomid,joined);
            }
        }

		private void MatrixClient_OnEvent (string roomid, MatrixEventRoomJoined joined)
		{
			MatrixRoom mroom;
			if (!rooms.ContainsKey (roomid)) {
				mroom = new MatrixRoom (api, roomid);
				rooms.TryAdd (roomid, mroom);
				//Update existing room
			} else {
				mroom = rooms [roomid];
			}
			joined.state.events.ToList ().ForEach (x => {mroom.FeedEvent (x);});
			joined.timeline.events.ToList ().ForEach (x => {mroom.FeedEvent (x);});
		}

		/// <summary>
		/// Login with a given username and password.
		/// This method will also start a sync with the server
		/// Currently, this is the only login method the SDK supports.
		/// </summary>
		/// <param name="username">Username</param>
		/// <param name="password">Password</param>
		public void LoginWithPassword(string username,string password){
			api.ClientLogin (new MatrixLoginPassword (username, password));
			api.ClientSync ();
			api.StartSyncThreads ();
		}

		/// <summary>
		/// Get information about a user from the server. 
		/// </summary>
		/// <returns>A MatrixUser object</returns>
		/// <param name="userid">User ID</param>
		public MatrixUser GetUser(string userid){
			MatrixProfile profile = api.ClientProfile (userid);
			if (profile != null) {
				return new MatrixUser (profile, userid);
			}
			return null;
		}

		/// <summary>
		/// Get all the Rooms that the user has joined.
		/// </summary>
		/// <returns>Array of MatrixRooms</returns>
		public MatrixRoom[] GetAllRooms(){
			return rooms.Values.ToArray ();
		}

		/// <summary>
		/// Creates a new room with the specified details, or a blank one otherwise.
		/// </summary>
		/// <returns>A MatrixRoom object</returns>
		/// <param name="roomdetails">Optional set of options to send to the server.</param>
		public MatrixRoom CreateRoom(MatrixCreateRoom roomdetails = null){
			string roomid = api.ClientCreateRoom (roomdetails);
			if (roomid != null) {
				MatrixRoom room = JoinRoom(roomid);
				return room;
			}
			return null;
		}

		/// <summary>
		/// Alias for <see cref="MatrixSDK.MatrixClient.CreateRoom"/> which lets you set common items before creation.
		/// </summary>
		/// <returns>A MatrixRoom object</returns>
		/// <param name="name">The room name.</param>
		/// <param name="alias">The primary alias</param>
		/// <param name="topic">The room topic</param>
		public MatrixRoom CreateRoom(string name, string alias = null,string topic = null){
			MatrixCreateRoom room = new MatrixCreateRoom ();
			room.name = name;
			room.room_alias_name = alias;
			room.topic = topic;
			return CreateRoom (room);
		}

		/// <summary>
		/// Join a matrix room.
		/// </summary>
		/// <returns>The room.</returns>
		/// <param name="roomid">roomid or alias</param>
		public MatrixRoom JoinRoom(string roomid){//TODO: Maybe add a try method.
			if (!rooms.ContainsKey (roomid)) {//TODO: Check the status of the room too.
				roomid = api.ClientJoin (roomid);
				if(roomid == null){
					return null;
				}
				MatrixRoom room = new MatrixRoom (api, roomid);
				rooms.TryAdd (room.ID, room);
			}
			return rooms [roomid];
		}

		public MatrixMediaFile UploadFile(string contentType,byte[] data){
			string url = api.MediaUpload(contentType,data);
			return new MatrixMediaFile(api,url,contentType);
		}
	
		/// <summary>
		/// Return a joined room object by it's roomid.
		/// </summary>
		/// <returns>The room.</returns>
		/// <param name="roomid">Roomid.</param>
		public MatrixRoom GetRoom(string roomid){//TODO: Maybe add a try method.
			MatrixRoom room = null;
			rooms.TryGetValue(roomid,out room);
			return room;
		}
		/// <summary>
		/// Get a room object by any of it's registered aliases.
		/// </summary>
		/// <returns>The room by alias.</returns>
		/// <param name="alias">CanonicalAlias or any Alias</param>
		public MatrixRoom GetRoomByAlias(string alias){
			MatrixRoom room = rooms.Values.FirstOrDefault( x => {
				if(x.CanonicalAlias == alias){
					return true;
				}
				else if(x.Aliases != null){
					return x.Aliases.Contains(alias);
				}
				return false;
			});
			if (room != default(MatrixRoom)) {
				return room;
			} else {
				return null;
			}
		}

		/// <summary>
		/// Releases all resource used by the <see cref="MatrixSDK.Client.MatrixClient"/> object.
		/// In addition, this will stop the sync thread.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="MatrixSDK.Client.MatrixClient"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="MatrixSDK.Client.MatrixClient"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="MatrixSDK.Client.MatrixClient"/>
		/// so the garbage collector can reclaim the memory that the <see cref="MatrixSDK.Client.MatrixClient"/> was occupying.</remarks>
		public void Dispose(){
			api.StopSyncThreads ();
		}
	}
}

