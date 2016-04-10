using System;
using System.Collections.Generic;
using MatrixSDK.Structures;
namespace MatrixSDK
{
	public class MatrixRoom
	{
		const int MESSAGE_CAPACITY = 255;
		public readonly string ID;
		public string Name { get; private set; }
		public string Topic { get; private set; }
		public string Creator { get; private set; }
		public bool ShouldFederate { get; private set; }
		public string CanonicalAlias { get; private set; }
		public string[] Aliases { get; private set; }
		public EMatrixRoomJoinRules JoinRule { get; private set; }
		public MatrixMRoomPowerLevels PowerLevels { get; private set; } //TODO: Implement this inside this class

		private List<MatrixMRoomMessage> messages = new List<MatrixMRoomMessage>(MESSAGE_CAPACITY);
		public MatrixMRoomMessage[] Messages { get { return messages.ToArray (); } }

		private MatrixAPI api;


		//TODO: Implement push rules
		//MatixRoomMember

		public MatrixRoom (MatrixAPI API,string roomid)
		{
			ID = roomid;
			api = API;
		}

		public void FeedEvent(MatrixEvent evt){
			Type t = evt.content.GetType();
			if (t == typeof(MatrixMRoomCreate)) {
				Creator = ((MatrixMRoomCreate)evt.content).creator;
			} else if (t == typeof(MatrixMRoomName)) {
				Name = ((MatrixMRoomName)evt.content).name;
			} else if (t == typeof(MatrixMRoomTopic)) {
				Topic = ((MatrixMRoomTopic)evt.content).topic;
			} else if (t == typeof(MatrixMRoomAliases)) {
				Aliases = ((MatrixMRoomAliases)evt.content).aliases;
			} else if (t == typeof(MatrixMRoomCanonicalAlias)) {
				CanonicalAlias = ((MatrixMRoomCanonicalAlias)evt.content).alias;
			} else if (t == typeof(MatrixMRoomJoinRules)) {
				JoinRule = ((MatrixMRoomJoinRules)evt.content).join_rule;
			} else if (t == typeof(MatrixMRoomJoinRules)) {
				PowerLevels = ((MatrixMRoomPowerLevels)evt.content);
			} else if (t.IsSubclassOf(typeof(MatrixMRoomMessage))) {
				messages.Add ((MatrixMRoomMessage)evt.content);
			}
		}

		public void SetName(string newName){
			MatrixMRoomName nameEvent = new MatrixMRoomName ();
			nameEvent.name = newName;
			api.SendStateMessage (ID, "m.room.name", nameEvent); 
		}

		public void SetTopic(string newTopic){
			MatrixMRoomTopic topicEvent = new MatrixMRoomTopic ();
			topicEvent.topic = newTopic;
			api.SendStateMessage (ID, "m.room.topic", topicEvent);
		}

		public void SendMessage(MatrixMRoomMessage message){
			api.QueueRoomMessage (ID, "m.room.message", message);
		}

		public void SendMessage(string body){
			MMessageText message = new MMessageText ();
			message.body = body;
			SendMessage (message);
		}

		public void InviteToRoom(string userid){
			api.InviteToRoom (ID, userid);
		}

		public void InviteToRoom(MatrixUser user){
			InviteToRoom (user.UserID);
		}

	}
}

