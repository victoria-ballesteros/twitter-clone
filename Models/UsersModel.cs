using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace twitter_clone.Models
{
    [Table("USERS_TABLE")]
    public class UsersModel : BaseModel
    {
        [PrimaryKey("user_id", false)]
        public Guid User_id { get; set; }

        [Column("user_first_name")]
        public string User_first_name { get; set;}

        [Column("user_last_name")]
        public string? User_last_name { get; set;}

        [Column("user_username")]
        public string User_username { get; set;}

        [Column("user_email")]
        public string User_email { get; set;}

        [Column("user_password_hash")]
        public string User_password_hash { get; set;}

        [Column("user_created_at")]
        public DateTimeOffset User_created_at { get; set;}

        [Column("user_profile_picture_path")]
        public string User_profile_picture_path { get; set; }

        [Column("user_followers")]
        public int User_followers { get; set; }

        [Column("user_followings")]
        public int User_followings { get; set; }

        [Column("user_description")]
        public string User_description { get; set; }

        [Column("user_banner_picture_path")]
        public string User_banner_picture_path { get; set; }
    }
}