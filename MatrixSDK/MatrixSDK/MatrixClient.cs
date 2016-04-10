using System;
using MatrixSDK.Exceptions;
using MatrixSDK.Structures;
using System.Linq;
namespace MatrixSDK
{
	public class MatrixClient : IDisposable
	{
		MatrixAPI api;

		/// <summary>
		/// Initializes a new instance of the <see cref="MatrixSDK.MatrixClient"/> class.
		/// The client will preform a connection and try to retrieve version information.
		/// If this fails, a MatrixUnsuccessfulConnection Exception will be thrown.
		/// </summary>
		/// <param name="URL">URL before /_matrix/</param>
		public MatrixClient (string URL)
		{
			api = new MatrixAPI (URL);
			try{
				string[] versions = api.GetVersions ();
				if(!MatrixAPI.IsVersionSupported(versions)){
					Console.WriteLine("Warning: Client version is not supported by the server");
					Console.WriteLine("Client supports up to "+MatrixAPI.VERSION);
				}
			}
			catch(MatrixException e){
				throw new MatrixException("An exception occured while trying to connect",e);
			}
		}

		public void LoginWithPassword(string username,string password){
			api.ClientLogin (new MatrixLoginPassword (username, password));
			api.ClientSync ();
			api.StartSyncThreads ();
		}

		public MatrixUser GetUser(string user){
			MatrixProfile profile = api.ClientProfile (user);
			if (profile != null) {
				return new MatrixUser (profile, user);
			}
			return null;
		}

		public MatrixRoom[] GetAllRooms(){
			return api.GetRooms ();
		}

		public MatrixRoom CreateRoom(MatrixCreateRoom roomdetails = null){
			return api.CreateRoom (roomdetails);
		}

		public MatrixRoom CreateRoom(string name, string alias = null,string topic = null){
			MatrixCreateRoom room = new MatrixCreateRoom ();
			room.name = name;
			room.room_alias_name = alias;
			room.topic = topic;
			return api.CreateRoom (room);
		}

		public MatrixRoom JoinRoom(string roomid){//TODO: Maybe add a try method.
			return api.JoinRoom (roomid);
		}
		//TODO: GetRoom could be rephrased
		public MatrixRoom GetRoom(string roomid){//TODO: Maybe add a try method.
			return api.GetRoom (roomid);
		}

		public MatrixRoom GetRoomByAlias(string alias){
			MatrixRoom room = api.GetRooms ().FirstOrDefault( x => {
				if(x.Aliases != null){
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

