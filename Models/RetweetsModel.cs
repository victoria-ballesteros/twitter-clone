using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace twitter_clone.Models
{
    [Table("RETWEETS_TABLE")]
    public class RetweetsModel : BaseModel
    {
        [PrimaryKey("retweet_id", false)]
        public Guid Retweet_id { get; set; }

        [Column("retweet_original_id")]
        public Guid Retweet_original_id { get; set; }

        [Column("retweet_user")]
        public Guid Retweet_user { get; set; }

        [Column("retweet_created_at")]
        public DateTimeOffset Retweet_created_at { get; set; }
    }
}