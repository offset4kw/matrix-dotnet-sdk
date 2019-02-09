using System.Collections.Generic;

namespace Matrix.Structures
{
    public class PublicRooms
    {
        public List<PublicRoomsChunk> chunk;
        public string next_batch;
        public string prev_batch;
        public int total_room_count_estimate;
    }

    public class PublicRoomsChunk
    {
        public string[] aliases;
        public string canonical_alias;
        public string name;
        public int num_joined_members;
        public string room_id;
        public string topic;
        public bool world_readable;
        public bool guest_can_join;
        public string avatar_url;
    }
}