namespace twitter_clone.Models
{
    public class TweetingModel
    {
        public string Tweet_content { get; set; }

        public Guid Tweet_user_id { get; set; }

        public Guid? Tweet_re_id { get; set; }
        
        public IFormFile? Tweet_image { get; set; }

    }
}