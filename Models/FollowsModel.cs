using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace twitter_clone.Models
{   
    [Table("FOLLOWS_TABLE")]
    public class FollowsModel : BaseModel
    {
        [PrimaryKey("follow_id", false)]
        public Guid Follow_id { get; set; }

        [Column("follow_followed_id")]
        public Guid Follow_followed_id { get; set; }

        [Column("follow_follower_id")]
        public Guid Follow_follower_id { get; set; }

        [Column("follow_created_at")]
        public DateTimeOffset Follow_created_at { get; set;}
    }
}