using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using MatrixSDK.Structures;
namespace MatrixSDK
{
	public class JsonEventConverter : JsonConverter
	{

		Dictionary<string,Type> contentTypes = new Dictionary<string, Type>{
			{"m.presence",	typeof(MatrixMPresence)},
			{"m.notice",	typeof(MMessageNotice)},
			{"m.text",		typeof(MMessageText)},
			{"m.emote",		typeof(MMessageEmote)},
			{"m.image",		typeof(MMessageImage)},
			{"m.file",		typeof(MMessageFile)}
		};

		public JsonEventConverter(Dictionary<string,Type> customMsgTypes = null){
			if (customMsgTypes != null) {
				foreach (KeyValuePair<string,Type> item in customMsgTypes) {
					if (contentTypes.ContainsKey (item.Key)) {
						contentTypes [item.Key] = item.Value;
					} else {
						contentTypes.Add (item.Key, item.Value);
					}
				}
			}
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(MatrixEvent);
		}

		public MatrixEventContent GetContent(JsonReader reader,JsonSerializer serializer,string type){
			Type T;
			if (contentTypes.TryGetValue (type, out T)) {
				MatrixEventContent content = (MatrixEventContent)Activator.CreateInstance(T);
				serializer.Populate (reader, content);
				return content;
			} else {
				return new MatrixEventContent();
			}
		}

		public override object ReadJson(JsonReader reader, 
			Type objectType, 
			object existingValue, 
			JsonSerializer serializer)
		{
			// Load JObject from stream
			JObject jObject = JObject.Load(reader);
			// Populate the event itself
			MatrixEvent ev = new MatrixEvent();
			serializer.Populate (jObject.CreateReader (), ev);
			//Get the correct content type.
			ev.content = GetContent (jObject ["content"].CreateReader (), serializer, ev.type);
			return ev;
		}

		public override void WriteJson(JsonWriter writer, 
			object value,
			JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}

