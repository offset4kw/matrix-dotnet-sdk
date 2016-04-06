using System;

namespace MatrixSDK
{
	public enum EMatrixRoomJoinRules{
		Public,
		Knock,
		Invite,
		Private
	}

	public class MatrixMRoomAliases
	{
		public string[] aliases;
	}

	public class MatrixMRoomCanonicalAlias{
		public string alias;
	}

	public class MatrixMRoomCreate{
		public bool mfederate = true;
		public string creator;
	}

	public class MatrixMRoomJoinRules{
		public EMatrixRoomJoinRules join_rule;
	}

	public class MatrixMRoomMember{
			
	}
}

