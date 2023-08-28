using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FilterTracker.Models
{
    public class LoginViewModel : ModelBase
    {
        [Required()]
        [MaxLength(200)]
        [EmailAddress]
        public string Username { get; set; }

        [Required()]
        [MaxLength(20)]
        [MinLength(6)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }

        public string RedirectTo { get; set; }
    }

    public class ChangePasswordViewModel : ModelBase
    {
        [Required()]
        [MaxLength(20, ErrorMessage = "20 characters max")]
        [MinLength(6, ErrorMessage = "6 characters min")]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required()]
        [MaxLength(20, ErrorMessage = "20 characters max")]
        [MinLength(6, ErrorMessage = "6 characters min")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required()]
        [MaxLength(20, ErrorMessage = "20 characters max")]
        [MinLength(6, ErrorMessage = "6 characters min")]
        [Display(Name = "Repeat New Password")]
        public string RepeatedNewPassword { get; set; }


    }
}