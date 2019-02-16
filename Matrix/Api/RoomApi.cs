using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Matrix.Backends;
using Matrix.Structures;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Matrix
{
    public partial class MatrixAPI
    {	
	    private Mutex eventSendMutex = new Mutex();

        [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-rooms-roomid-send")]
        private async Task<string> RoomSend (string roomId, string type, MatrixMRoomMessage msg, string txnId = "")
        {
	        ThrowIfNotSupported();
            JObject msgData = ObjectToJson (msg);
            var res = await mbackend.PutAsync(
	            $"/_matrix/client/r0/rooms/{roomId}/send/{type}/{txnId}", true, msgData
			);
	        if (!res.error.IsOk) {
		        throw new MatrixException (res.error.ToString());//TODO: Need a better exception
	        }
            return res.result["event_id"].ToObject<string>();
        }

        [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-rooms-roomid-leave")]
        public void RoomLeave(string roomid){
	        ThrowIfNotSupported();
            MatrixRequestError error = mbackend.Post(String.Format("/_matrix/client/r0/rooms/{0}/leave",Uri.EscapeDataString(roomid)),true,null,out var result);
            if (!error.IsOk) {
                throw new MatrixException (error.ToString ());
            }
        }

		[MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "put-matrix-client-r0-rooms-roomid-state-eventtype")]
		public virtual string RoomStateSend(string roomid, string type, MatrixRoomStateEvent message,string key = ""){
			ThrowIfNotSupported();
			JObject msgData = ObjectToJson (message);
			MatrixRequestError error = mbackend.Put (String.Format ("/_matrix/client/r0/rooms/{0}/state/{1}/{2}", Uri.EscapeDataString(roomid),type,key), true, msgData, out var result);
			if (!error.IsOk) {
				throw new MatrixException (error.ToString());//TODO: Need a better exception
			}
			return result["event_id"].ToObject<string>();
		}

		[MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-rooms-roomid-invite")]
		public void InviteToRoom(string roomid, string userid){
			ThrowIfNotSupported();
			JObject msgData = JObject.FromObject(new {user_id=userid});
			MatrixRequestError error = mbackend.Post (String.Format ("/_matrix/client/r0/rooms/{0}/invite", Uri.EscapeDataString(roomid)), true, msgData, out var result);
			if (!error.IsOk) {
				throw new MatrixException (error.ToString());//TODO: Need a better exception
			}
		}

		[MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "put-matrix-client-r0-rooms-roomid-send-eventtype-txnid")]
		public async Task<string> RoomMessageSend (string roomid, string type, MatrixMRoomMessage message)
		{
			ThrowIfNotSupported();
			if (message.body == null) {
				throw new Exception("Missing body in message");
			}
			if (message.msgtype == null) {
				throw new Exception("Missing msgtype in message");
			}

			int txnId = rng.Next (int.MinValue, int.MaxValue);
			JObject msgData = ObjectToJson (message);
			// Send messages in order.
			eventSendMutex.WaitOne();
			while (true)
			{
				try
				{
					var res = await mbackend.PutAsync(
						$"/_matrix/client/r0/rooms/{roomid}/send/{type}/{txnId}", true, msgData
					);
					if (res.error.IsOk)
					{
						eventSendMutex.ReleaseMutex();
						return res.result["event_id"].ToObject<string>();
					}

					if (res.error.MatrixErrorCode == MatrixErrorCode.M_LIMIT_EXCEEDED)
					{
						int backoff = res.error.RetryAfter != -1 ? res.error.RetryAfter : 1000;
						log.LogWarning($"Sending m{txnId} failed. Will retry in {backoff}ms");
						await Task.Delay(backoff);
					}
					else
					{
						throw new MatrixException (res.error.ToString());//TODO: Need a better exception
					}
				}
				catch (Exception)
				{
					eventSendMutex.ReleaseMutex();
					throw;
				}
			}
		}

		[MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-rooms-roomid-receipt-receipttype-eventid")]
		public void RoomTypingSend (string roomid, bool typing, int timeout = 0)
		{
			ThrowIfNotSupported();
			JObject msgData;
			if (timeout == 0) {
				msgData = JObject.FromObject (new {typing});
			} else {
				msgData = JObject.FromObject(new {typing,timeout});
			}
			MatrixRequestError error = mbackend.Put (
				$"/_matrix/client/r0/rooms/{Uri.EscapeDataString(roomid)}/typing/{Uri.EscapeDataString(UserId)}", true, msgData,out _);
			if (!error.IsOk) {
				throw new MatrixException (error.ToString());
			}
		}

	    [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer,
		    "get-matrix-client-r0-rooms-roomid-state")]
	    public MatrixEvent[] GetRoomState(string roomId)
	    {
		    ThrowIfNotSupported();
		    MatrixRequestError error = mbackend.Get($"/_matrix/client/r0/rooms/{roomId}/state", true, out var result);
		    MatrixEvent[] events = JsonConvert.DeserializeObject<MatrixEvent[]> (result.ToString (), event_converter);
		    if (!error.IsOk) {
			    throw new MatrixException (error.ToString());
		    }
		    return events;
	    }

	    [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer,
		    "get-matrix-client-r0-rooms-roomid-messages")]
	    public ChunkedMessages GetRoomMessages(string roomId)
	    {
		    ThrowIfNotSupported();
		    MatrixRequestError error = mbackend.Get($"/_matrix/client/r0/rooms/{roomId}/messages?limit=100&dir=b", true, out var result);
		    if (!error.IsOk) {
			    throw new MatrixException (error.ToString());
		    }
		    return JsonConvert.DeserializeObject<ChunkedMessages> (result.ToString (), event_converter);
	    }
	    
	    
	    [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-rooms-roomid-join")]
	    public string ClientJoin(string roomid) {
		    ThrowIfNotSupported();
		    MatrixRequestError error = mbackend.Post(String.Format("/_matrix/client/r0/join/{0}",Uri.EscapeDataString(roomid)),true,null,out var result);
		    if (error.IsOk) {
			    roomid = result ["room_id"].ToObject<string> ();
			    return roomid;
		    }
		    return null;

	    }
		
	    [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-createroom")]
	    public string ClientCreateRoom(MatrixCreateRoom roomrequest = null) {
		    ThrowIfNotSupported();
		    JObject req = null;
		    if (roomrequest != null) {
			    req = ObjectToJson(roomrequest);
		    }
		    MatrixRequestError error = mbackend.Post ("/_matrix/client/r0/createRoom", true, req, out var result);
		    if (error.IsOk) {
			    string roomid = result ["room_id"].ToObject<string> ();
			    return roomid;
		    }

		    return null;
	    }

	    [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer,
		    "get-matrix-client-r0-user-userid-rooms-roomid-tags")]
	    public RoomTags RoomGetTags(string roomid)
	    {
		    ThrowIfNotSupported();
		    MatrixRequestError error = mbackend.Get($"/_matrix/client/r0/user/{UserId}/rooms/{roomid}/tags", true, out var result);
		    if (!error.IsOk) {
			    throw new MatrixException (error.ToString());
		    }
		    return result.ToObject<RoomTags>();
	    }
		
	    [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer,
		    "get-matrix-client-r0-user-userid-rooms-roomid-tags")]
	    public void RoomPutTag(string roomid, string tag, double order)
	    {
		    ThrowIfNotSupported();
		    JObject req = new JObject();
		    req["order"] = order;
		    MatrixRequestError error = mbackend.Put($"/_matrix/client/r0/user/{UserId}/rooms/{roomid}/tags/{tag}", true, req, out var result);
		    if (!error.IsOk) {
			    throw new MatrixException (error.ToString());
		    }
	    }

    }
}