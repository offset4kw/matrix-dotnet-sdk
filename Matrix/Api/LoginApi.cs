using Matrix.Backends;
using Matrix.Structures;
using Newtonsoft.Json.Linq;

namespace Matrix
{
    public partial class MatrixAPI
    {
        [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "post-matrix-client-r0-login")]
        public MatrixLoginResponse ClientLogin(MatrixLogin login) {
            ThrowIfNotSupported();
            MatrixRequestError error = mbackend.Post ("/_matrix/client/r0/login",false,JObject.FromObject(login),out var result);
            if (error.IsOk) {
                return result.ToObject<MatrixLoginResponse> ();
            }
            throw new MatrixException (error.ToString());//TODO: Need a better exception
        }
    }
}