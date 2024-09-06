namespace twitter_clone.Models
{
    public class UsersLoginModel
    {
        public required string User_username_or_email { get; set;}

        public required string User_password { get; set; }
    }
}