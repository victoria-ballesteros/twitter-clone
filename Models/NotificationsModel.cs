using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace twitter_clone.Models
{
    [Table("NOTIFICATIONS_TABLE")]
    public class NotificationsModel : BaseModel
    {
        [PrimaryKey("notification_id", false)]
        public Guid Notification_id { get; set; }

        [Column("notification_user_id")]
        public Guid Notification_user_id { get; set; }

        [Column("notification_content")]
        public string Notification_content { get; set; }

        [Column("notification_created_at")]
        public DateTimeOffset Notification_created_at { get; set; }
    }   
}