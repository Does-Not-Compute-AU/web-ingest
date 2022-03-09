using System;
using System.ComponentModel.DataAnnotations;

namespace WebIngest.Common.Models.AuthModels
{
    public class LoginModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
    
    public class LoginResult
    {
        public bool Successful { get; set; }
        public string Error { get; set; }
        public string Token { get; set; }
        public DateTime Expiry { get; set; }
    }
}