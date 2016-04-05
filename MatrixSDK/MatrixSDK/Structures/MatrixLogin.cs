using System;

namespace MatrixSDK.Structures
{
	public abstract class MatrixLogin
	{
		
	}

	/// <summary>
	/// Following http://matrix.org/docs/spec/r0.0.1/client_server.html#id67
	/// </summary>
	public class MatrixLoginPassword : MatrixLogin{
		public MatrixLoginPassword(string user,string pass){
			this.user = user;
			password = pass;
		}
		public readonly string type = "m.login.password";
		public string user;
		public string password;
	}

	/// <summary>
	/// Following http://matrix.org/docs/spec/r0.0.1/client_server.html#id76
	/// </summary>
	public class MatrixLoginResponse{
		public string access_token;
		public string home_server;
		public string user_id;
		public string refresh_token;
	}
}

