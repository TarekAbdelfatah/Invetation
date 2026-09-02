using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Ibtikar.Validation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class NoHtmlAttribute : ValidationAttribute
    {
        private static readonly Regex DangerousPattern = new(
            @"[<>]|javascript\s*:|vbscript\s*:|<\?|[\x00-\x08\x0B\x0C\x0E-\x1F]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public NoHtmlAttribute()
            : base("يرجى إدخال نص عادي بدون وسوم HTML أو روابط برمزية (مثل <script> أو javascript:).")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is not string s || string.IsNullOrEmpty(s))
            {
                return true;
            }

            return !DangerousPattern.IsMatch(s);
        }
    }
}