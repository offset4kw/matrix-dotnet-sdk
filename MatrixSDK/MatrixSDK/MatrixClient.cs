using System;
using MatrixSDK.Exceptions;
using MatrixSDK.Structures;
namespace MatrixSDK
{
	public class MatrixClient
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
			api.Login (new MatrixLoginPassword (username, password));
		}

		public MatrixUser GetUser(string user){
			return api.GetUser (user);
		}
	}
}

