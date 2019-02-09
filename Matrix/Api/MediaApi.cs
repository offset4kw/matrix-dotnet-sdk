using System.Collections.Generic;
using Matrix.Backends;
using Newtonsoft.Json.Linq;

namespace Matrix
{
    public partial class MatrixAPI
    {
        public string MediaUpload(string contentType,byte[] data)
        {
            MatrixRequestError error = mbackend.Post("/_matrix/media/r0/upload",true,data,new Dictionary<string,string>(){{"Content-Type",contentType}},out var result);
            if (!error.IsOk) {
                throw new MatrixException (error.ToString());
            }
            return (result as JObject)?.GetValue("content_uri").ToObject<string>();
        }
    }
}