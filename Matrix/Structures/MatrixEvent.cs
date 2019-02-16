using System;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Matrix.Structures
{
	public class MatrixEvent
	{
		/// <summary>
		/// Following http://matrix.org/docs/spec/r0.0.1/client_server.html#get-matrix-client-r0-sync
		/// </summary>
		public MatrixEventContent content;
		public Int64 origin_server_ts;
		public Int64 age;
		public string sender;
		public string type;
		public string event_id;
		public string room_id;
		public MatrixEventUnsigned unsigned;
		public string state_key;

		// Special case for https://matrix.org/docs/spec/r0.0.1/client_server.html#m-room-member
		public MatrixStrippedState[] invite_room_state;

		public override string ToString ()
		{
			string str = "Event {";
			foreach (PropertyInfo prop in typeof(MatrixEvent).GetProperties()) {
				str += "   " + (prop.Name + ": " + prop.GetValue (this));
			}
			str += "}";
			return str;
		}
	}

	public class MatrixEventUnsigned {
		public MatrixEventUnsigned prev_content;
		public Int64 age;
		public string transaction_id;
	}

	/// <summary>
	/// Base content class.
	/// </summary>
	public class MatrixEventContent
	{
		public JObject mxContent = null;
	}

	public class MatrixTimeline{
		public bool limited;
		public string prev_batch;
		public MatrixEvent[] events;
	}

	public static class MatrixEventType
	{
		public const string RoomMember = "m.room.member";
	}
}
