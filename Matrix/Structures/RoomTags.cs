using System.Collections.Generic;

namespace Matrix.Structures
{
    /// <summary>
    /// Following https://matrix.org/docs/spec/client_server/r0.4.0.html#get-matrix-client-r0-user-userid-rooms-roomid-tags
    /// </summary>
    public class RoomTags
    {
        public Dictionary<string, RoomTag> tags;
    }

    public class RoomTag
    {
        /// <summary>
        /// A number in a range [0,1] describing a relative position of the room under the given tag.
        /// </summary>
        public double order;
    }
}