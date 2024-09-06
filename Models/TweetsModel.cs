using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace twitter_clone.Models
{
    [Table("TWEETS_TABLE")]
    public class TweetsModel : BaseModel
    {
        [PrimaryKey("tweet_id", false)]
        public Guid Tweet_id { get; set; }

        [Column("tweet_content")]
        public string Tweet_content { get; set; }

        [Column("tweet_user_id")]
        public Guid Tweet_user_id { get; set; }

        [Column("tweet_re_id")]
        public Guid? Tweet_re_id { get; set; }

        [Column("tweet_created_at")]
        public DateTimeOffset Tweet_created_at { get; set;}

        [Column("tweet_likes_quantity")]
        public int Tweet_likes_quantity { get; set; }

        [Column("tweet_picture_path")]
        public string Tweet_picture_path { get; set; }
    }
}