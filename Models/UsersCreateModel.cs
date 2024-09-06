using System.ComponentModel.DataAnnotations;

namespace twitter_clone.Models
{
    public class UsersCreateModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string User_first_name { get; set; }

        [Display(Name = "Last Name")]
        public string? User_last_name { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string User_username { get; set; }

        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string User_email { get; set; }

        [Required]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string User_password_hash { get; set; }
    }
}
