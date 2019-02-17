using System;

namespace Matrix.Structures
{
	public abstract class MatrixLogin
	{

	}

	/// <summary>
	/// Following https://matrix.org/docs/spec/client_server/r0.4.0.html#id213
	/// </summary>
	public class MatrixLoginPassword : MatrixLogin{
		public MatrixLoginPassword(string user, string pass, string device_id = null, string device_display_name = null){
			this.user = user;
			password = pass;
			this.device_id = device_id;
			this.device_display_name = device_display_name;
		}
		public readonly string type = "m.login.password";
		public readonly string user;
		public readonly string password;
		public readonly string device_id;
		public readonly string device_display_name;
	}

	public class MatrixLoginToken : MatrixLogin {
		public MatrixLoginToken(string user,string token){
			this.user = user;
			this.token = token;
		}
		public readonly string user;
		public readonly string token;
		public readonly string txn_id = Guid.NewGuid().ToString();
		public readonly string type = "m.login.token";
	}

	/// <summary>
	/// Following http://matrix.org/docs/spec/r0.0.1/client_server.html#id76
	/// </summary>
	public class MatrixLoginResponse{
		public string access_token;
		public string home_server;
		public string user_id;
	}
}
