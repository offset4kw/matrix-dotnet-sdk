using System.Threading.Tasks;
using Matrix.Backends;
using Matrix.Structures;
using Newtonsoft.Json.Linq;

namespace Matrix
{
    public partial class MatrixAPI
    {
        [MatrixSpec(EMatrixSpecApiVersion.R030, EMatrixSpecApi.ClientServer, "get-matrix-client-r0-devices")]
        public async Task<Device[]> GetDevices() {
            ThrowIfNotSupported();
            var res = await mbackend.GetAsync("/_matrix/client/r0/devices", true);
            if (res.error.IsOk) {
                return res.result.ToObject<Device[]> ();
            }
            throw new MatrixException (res.error.ToString());
        }
    }
}