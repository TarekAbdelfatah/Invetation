using System.ComponentModel.DataAnnotations;
using Ibtikar.Validation;

namespace Ibtikar.ViewModels
{
    public sealed class LoginVm
    {
        [Required(ErrorMessage = "اسم المستخدم مطلوب.")]
        [NoHtml]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}