using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace FirebaseAuth.Models
{
    public class LoginModel
    {
        [Required]
        public string UserIdentifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        [RegularExpression(@"^(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]).*$", ErrorMessage = "Password must contain at least one special character")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}