using System.ComponentModel.DataAnnotations;

namespace FairShare.Web.ViewModels
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Optional for display; if omitted we’ll use the email as the display name
        [Display(Name = "Name (optional)")]
        public string? Name { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
