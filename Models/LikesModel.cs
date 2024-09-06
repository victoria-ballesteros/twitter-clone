using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace twitter_clone.Models
{
    [Table("LIKES_TABLE")]
    public class LikesModel : BaseModel
    {
        [PrimaryKey("like_id", false)]
        public Guid Like_id { get; set; }

        [Column("like_user_id")]
        public Guid Like_user_id { get; set; }

        [Column("like_tweet_id")]
        public Guid Like_tweet_id { get; set; }
    }
}