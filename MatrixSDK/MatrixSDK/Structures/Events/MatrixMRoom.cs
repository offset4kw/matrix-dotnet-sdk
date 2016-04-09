using System;

namespace MatrixSDK.Structures
{
	public enum EMatrixRoomJoinRules{
		Public,
		Knock,
		Invite,
		Private
	}

	public enum EMatrixRoomHistoryVisibility{
		Invited,
		Joined,
		Shared,
		World_Readable
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

	public class MatrixMRoomName : MatrixEventContent{
		public string name;
	}

	public class MatrixMRoomTopic : MatrixEventContent{
		public string topic;
	}

	public class MatrixMRoomHistoryVisibility : MatrixEventContent{
		public EMatrixRoomHistoryVisibility history_visibility;
	}
}

