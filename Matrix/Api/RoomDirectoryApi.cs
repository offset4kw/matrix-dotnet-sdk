using System.Web;
using Matrix.Backends;
using Matrix.Structures;

namespace Matrix
{
    public partial class MatrixAPI
    {
        public PublicRooms PublicRooms(int limit, string since, string server)
        {
            ThrowIfNotSupported();
            var qs = HttpUtility.ParseQueryString(string.Empty);
            if (limit != 0)
                qs.Set("limit", limit.ToString());
            if (since != "")
                qs.Set("since", since);
            if (server != "")
                qs.Set("server", server);
            MatrixRequestError error = mbackend.Get($"/_matrix/client/r0/publicRooms?{qs}", true, out var result);
            if (!error.IsOk) {
                throw new MatrixException (error.ToString());
            }
            return result.ToObject<PublicRooms>();
        }
		
        public void DeleteFromRoomDirectory(string alias)
        {
            ThrowIfNotSupported();
            MatrixRequestError error = mbackend.Delete($"/_matrix/client/r0/directory/room/{alias}", true, out var _);
            if (!error.IsOk) {
                throw new MatrixException (error.ToString());
            }
        }

    }
}