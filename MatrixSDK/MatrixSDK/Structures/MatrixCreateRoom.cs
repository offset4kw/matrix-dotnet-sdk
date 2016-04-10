using System;
using MatrixSDK.Exceptions;
namespace MatrixSDK.Structures
{
	public class MatrixCreateRoom
	{
		/// <summary>
		/// A list of user IDs to invite to the room. This will tell the server to invite everyone in the list to the newly created room.
		/// </summary>
		public string[] invite;
		/// <summary>
		/// If this is included, an m.room.name event will be sent into the room to indicate the name of the room. See Room Events for more information on m.room.name
		/// </summary>
		public string name;
		/// <summary>
		/// A public visibility indicates that the room will be shown in the published room list. A private visibility will hide the room from the published room list. Rooms default to private visibility if this key is not included.
		/// </summary>
		public EMatrixCreateRoomVisibility visibility = EMatrixCreateRoomVisibility.Private;
		/// <summary>
		/// If this is included, an m.room.topic event will be sent into the room to indicate the topic for the room. See Room Events for more information on m.room.topic.
		/// </summary>
		public string topic;

		//TODO: Add invite_3pid
		//TODO: Add	preset
		//TODO: Add creation_content
		//TODO: Add initial_state
		private string _room_alias_name;
		public string room_alias_name {
			get { return _room_alias_name; }
			set
			{
				if (value.Contains ("#") || value.Contains(":")) {
					throw new MatrixBadFormatException (value, "local alias", "a local alias must not contain : or #");
				}
				_room_alias_name = value;
			}
		}
	}

	public enum EMatrixCreateRoomVisibility
	{
		Public,
		Private
	}
}

