using System;
using System.Linq;
using Matrix.Backends;
using Matrix.Structures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Matrix
{
    public partial class MatrixAPI
    {		
        [MatrixSpec(EMatrixSpecApiVersion.R040, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-rooms-roomid-send")]
        private MatrixRequestError RoomSend (MatrixAPIPendingEvent msg)
        {
            JObject msgData = ObjectToJson (msg.content);
            MatrixRequestError error = mbackend.Put (String.Format ("/_matrix/client/r0/rooms/{0}/send/{1}/{2}", System.Uri.EscapeDataString (msg.room_id), msg.type, msg.txnId), true, msgData, out var result);

            #if DEBUG
            if(!error.IsOk){
                Console.WriteLine (error.GetErrorString());
            }
            #endif
            return error;
        }

        [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-rooms-roomid-leave")]
        public void RoomLeave(string roomid){
            MatrixRequestError error = mbackend.Post(String.Format("/_matrix/client/r0/rooms/{0}/leave",System.Uri.EscapeDataString(roomid)),true,null,out var result);
            if (!error.IsOk) {
                throw new MatrixException (error.ToString ());
            }
        }

		[MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "put-matrix-client-r0-rooms-roomid-state-eventtype")]
		public virtual void RoomStateSend(string roomid, string type, MatrixRoomStateEvent message,string key = ""){
			JObject msgData = ObjectToJson (message);
			MatrixRequestError error = mbackend.Put (String.Format ("/_matrix/client/r0/rooms/{0}/state/{1}/{2}", System.Uri.EscapeDataString(roomid),type,key), true, msgData, out var result);
			if (!error.IsOk) {
				throw new MatrixException (error.ToString());//TODO: Need a better exception
			}
		}

		[MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-rooms-roomid-invite")]
		public void InviteToRoom(string roomid, string userid){
			JObject msgData = JObject.FromObject(new {user_id=userid});
			MatrixRequestError error = mbackend.Post (String.Format ("/_matrix/client/r0/rooms/{0}/invite", System.Uri.EscapeDataString(roomid)), true, msgData, out var result);
			if (!error.IsOk) {
				throw new MatrixException (error.ToString());//TODO: Need a better exception
			}
		}

		[MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "put-matrix-client-r0-rooms-roomid-send-eventtype-txnid")]
		public void RoomMessageSend (string roomid, string type, MatrixMRoomMessage message)
		{
			bool collision = true;
			MatrixAPIPendingEvent evt = new MatrixAPIPendingEvent ();
			evt.room_id = roomid;
			evt.type = type;
			evt.content = message;
			if (((MatrixMRoomMessage)evt.content).body == null) {
				throw new Exception("Missing body in message");
			}
			while (collision) {
				evt.txnId = rng.Next (1, 64);
				collision = pendingMessages.FirstOrDefault (x => x.txnId == evt.txnId) != default(MatrixAPIPendingEvent);
			}
			pendingMessages.Enqueue (evt);
			if (IsAS) {
				FlushMessageQueue();
			}
		}

		[MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-rooms-roomid-receipt-receipttype-eventid")]
		public void RoomTypingSend (string roomid, bool typing, int timeout = 0)
		{
			JObject msgData;
			if (timeout == 0) {
				msgData = JObject.FromObject (new {typing = typing});
			} else {
				msgData = JObject.FromObject(new {typing=typing,timeout=timeout});
			}
			MatrixRequestError error = mbackend.Put (
				$"/_matrix/client/r0/rooms/{System.Uri.EscapeDataString(roomid)}/typing/{System.Uri.EscapeDataString(user_id)}", true, msgData,out _);
			if (!error.IsOk) {
				throw new MatrixException (error.ToString());
			}
		}

	    [MatrixSpec(EMatrixSpecApiVersion.R040, EMatrixSpecApi.ClientServer,
		    "get-matrix-client-r0-rooms-roomid-state")]
	    public MatrixEvent[] GetRoomState(string roomId)
	    {
		    MatrixRequestError error = mbackend.Get($"/_matrix/client/r0/rooms/{roomId}/state", true, out var result);
		    MatrixEvent[] events = JsonConvert.DeserializeObject<MatrixEvent[]> (result.ToString (), event_converter);
		    if (!error.IsOk) {
			    throw new MatrixException (error.ToString());
		    }
		    return events;
	    }

	    public ChunkedMessages GetRoomMessages(string roomId)
	    {
		    MatrixRequestError error = mbackend.Get($"/_matrix/client/r0/rooms/{roomId}/messages?limit=100&dir=b", true, out var result);
		    if (!error.IsOk) {
			    throw new MatrixException (error.ToString());
		    }
		    return JsonConvert.DeserializeObject<ChunkedMessages> (result.ToString (), event_converter);
	    }
	    
	    
	    [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-rooms-roomid-join")]
	    public string ClientJoin(string roomid){
		    MatrixRequestError error = mbackend.Post(String.Format("/_matrix/client/r0/join/{0}",System.Uri.EscapeDataString(roomid)),true,null,out var result);
		    if (error.IsOk) {
			    roomid = result ["room_id"].ToObject<string> ();
			    return roomid;
		    } else {
			    return null;
		    }

	    }
		
	    [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-createroom")]
	    public string ClientCreateRoom(MatrixCreateRoom roomrequest = null){
		    JObject req = null;
		    if (roomrequest != null) {
			    req = ObjectToJson(roomrequest);
		    }
		    MatrixRequestError error = mbackend.Post ("/_matrix/client/r0/createRoom", true, req, out var result);
		    if (error.IsOk) {
			    string roomid = result ["room_id"].ToObject<string> ();
			    return roomid;
		    } else {
			    return null;
		    }
	    }

	    [MatrixSpec(EMatrixSpecApiVersion.R040, EMatrixSpecApi.ClientServer,
		    "get-matrix-client-r0-user-userid-rooms-roomid-tags")]
	    public RoomTags RoomGetTags(string roomid)
	    {
		    MatrixRequestError error = mbackend.Get($"/_matrix/client/r0/user/{user_id}/rooms/{roomid}/tags", true, out var result);
		    if (!error.IsOk) {
			    throw new MatrixException (error.ToString());
		    }
		    return result.ToObject<RoomTags>();
	    }
		
	    [MatrixSpec(EMatrixSpecApiVersion.R040, EMatrixSpecApi.ClientServer,
		    "get-matrix-client-r0-user-userid-rooms-roomid-tags")]
	    public void RoomPutTag(string roomid, string tag, double order)
	    {
		    JObject req = new JObject();
		    req["order"] = order;
		    MatrixRequestError error = mbackend.Put($"/_matrix/client/r0/user/{user_id}/rooms/{roomid}/tags/{tag}", true, req, out var result);
		    if (!error.IsOk) {
			    throw new MatrixException (error.ToString());
		    }
	    }

    }
}