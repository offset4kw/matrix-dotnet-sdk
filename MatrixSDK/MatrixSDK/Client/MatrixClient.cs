using System;
using System.Linq;
using System.Collections.Concurrent;
using MatrixSDK.Exceptions;
using MatrixSDK.Structures;
namespace MatrixSDK.Client
{
	public class MatrixClient : IDisposable
	{

		MatrixAPI api;
		public int SyncTimeout { get {return api.SyncTimeout;} set{ api.SyncTimeout = value; } }
		ConcurrentDictionary<string,MatrixRoom> rooms 			= new ConcurrentDictionary<string,MatrixRoom>();
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
				MatrixRoom room = new MatrixRoom (api, roomid);
				rooms.TryAdd (roomid, room);
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


		public MatrixRoom JoinRoom(string roomid){//TODO: Maybe add a try method.
			if (!rooms.ContainsKey (roomid)) {//TODO: Check the status of the room too.
				roomid = api.ClientJoin (roomid);
				MatrixRoom room = new MatrixRoom (api, roomid);
				rooms.TryAdd (room.ID, room);
			}
			return rooms [roomid];
		}

		//TODO: GetRoom could be rephrased
		public MatrixRoom GetRoom(string roomid){//TODO: Maybe add a try method.
			MatrixRoom room = null;
			rooms.TryGetValue(roomid,out room);
			return room;
		}

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

		public void Dispose(){
			api.StopSyncThreads ();
		}
	}
}

