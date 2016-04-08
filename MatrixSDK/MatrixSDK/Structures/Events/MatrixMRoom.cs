using System;

namespace MatrixSDK.Structures
{
	public enum EMatrixRoomJoinRules{
		Public,
		Knock,
		Invite,
		Private
	}

	public class MatrixMRoomAliases : MatrixEventContent
	{
		public string[] aliases;
	}

	public class MatrixMRoomCanonicalAlias : MatrixEventContent{
		public string alias;
	}

	public class MatrixMRoomCreate : MatrixEventContent{
		public bool mfederate = true;
		public string creator;	
	}

	public class MatrixMRoomJoinRules : MatrixEventContent{
		public EMatrixRoomJoinRules join_rule;
	}

	public class MatrixMRoomMember : MatrixEventContent{
			
	}
}

