using System;
using Matrix.Backends;
using Matrix.Structures;
using Newtonsoft.Json.Linq;

namespace Matrix
{
    public partial class MatrixAPI
    {
        [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "get-matrix-client-r0-profile-userid")]
        public virtual MatrixProfile ClientProfile(string userid){
            ThrowIfNotSupported();
            MatrixRequestError error = mbackend.Get ("/_matrix/client/r0/profile/" + userid,true, out var response);
            if (error.IsOk) {
                return response.ToObject<MatrixProfile> ();
            }

            return null;
        }

        [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "get-matrix-client-r0-profile-displayname")]
        public void ClientSetDisplayName(string userid,string displayname){
            ThrowIfNotSupported();
            JObject request = new JObject();
            request.Add("displayname",JToken.FromObject(displayname));
            MatrixRequestError error = mbackend.Put (string.Format("/_matrix/client/r0/profile/{0}/displayname",Uri.EscapeUriString(userid)),true,request, out var response);
            if (!error.IsOk) {
                throw new MatrixException (error.ToString());//TODO: Need a better exception
            }
        }

        [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "get-matrix-client-r0-profile-userid-displayname")]
        public void ClientSetAvatar(string userid,string avatar_url){
            ThrowIfNotSupported();
            JObject request = new JObject();
            request.Add("avatar_url",JToken.FromObject(avatar_url));
            MatrixRequestError error = mbackend.Put (string.Format("/_matrix/client/r0/profile/{0}/avatar_url",Uri.EscapeUriString(userid)),true,request, out var response);
            if (!error.IsOk) {
                throw new MatrixException (error.ToString());//TODO: Need a better exception
            }
        }
    }
}