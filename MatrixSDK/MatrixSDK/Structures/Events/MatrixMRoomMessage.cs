using System;

namespace MatrixSDK.Structures
{
	public class MRoomMessage : MatrixEventContent
	{
		public virtual string msgtype { get { return null; }}
		public string body;
	}
		
	public class MMessageNotice : MRoomMessage
	{
		public override string msgtype { get { return "m.notice"; }}
	}

	public class MMessageText : MRoomMessage
	{
		public override string msgtype { get { return "m.text"; }}
	}

	public class MMessageEmote : MRoomMessage
	{
		public override string msgtype { get { return "m.emote"; }}
	}

	public class MMessageImage : MRoomMessage
	{
		public override string msgtype { get { return "m.image"; }}
		public MatrixImageInfo info;
		public MatrixImageInfo thumbnail_info;
		public string url;
		public string thumbnail_url;
	}

	public class MMessageFile : MRoomMessage
	{
		public override string msgtype { get { return "m.file"; }}
		public MatrixFileInfo info;
		public MatrixImageInfo thumbnail_info;
		public string url;
		public string thumbnail_url;
		public string filename;
	}

	public class MMessageLocation : MRoomMessage{
		public override string msgtype { get { return "m.location"; }}
		public string geo_url;
		public string thumbnail_url;
		public MatrixImageInfo thumbnail_info;
	}


}

