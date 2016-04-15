using System;
using MatrixSDK.Structures;
namespace MatrixSDK.Client
{
	public class MatrixUser
	{
		public MatrixUser(MatrixProfile Profile,string user){
			profile = Profile;
			userid = user;
		}

		MatrixProfile profile;
		string userid;

		public string AvatarURL { get { return profile.avatar_url; } }
		public string DisplayName { get { return profile.displayname; } }
		public string UserID{ get { return userid; } }
	}
}

