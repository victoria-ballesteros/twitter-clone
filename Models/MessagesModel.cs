using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace twitter_clone.Models
{
    [Table("MESSAGES_TABLE")]
    public class MessagesModel : BaseModel
    {
        [PrimaryKey("message_id", false)]
        public Guid Message_id { get; set; }

        [Column("message_content")]
        public string Message_content { get; set; }

        [Column("message_sender_id")]
        public Guid Message_sender_id { get; set; }

        [Column("message_receiver_id")]
        public Guid? Message_receiver_id { get; set; }

        [Column("message_created_at")]
        public DateTimeOffset Message_created_at { get; set;}

    }
}