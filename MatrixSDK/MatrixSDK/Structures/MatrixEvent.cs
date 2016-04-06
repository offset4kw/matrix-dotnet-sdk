using System;
using System.Collections.Generic;
namespace MatrixSDK.Structures
{
	public class MatrixEvent
	{
		/// <summary>
		/// Following http://matrix.org/docs/spec/r0.0.1/client_server.html#get-matrix-client-r0-sync
		/// </summary>	
		public MatrixEventContent content;
		public Int64 origin_server_ts;
		public string sender;
		public string type;
		public string room_id;
		public MatrixEventUnsigned unsigned;
		public string state_key;

		public MatrixEvent ()
		{

		}
	}

	public class MatrixEventUnsigned{
		public MatrixEventUnsigned prev_content;
		public int age;
		public string transaction_id;
	}
	/// <summary>
	/// Do not use this class directly.
	/// </summary>
	public class MatrixEventContent{
		
	}

	public class MatrixTimeline{
		public bool limited;
		public string prev_batch;
		public MatrixEvent[] events;
	}
}

