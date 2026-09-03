using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Services.Helpers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class IbtikarAuthorizeAttribute : TypeFilterAttribute
    {
        public IbtikarAuthorizeAttribute(params string[] roles) 
            : base(typeof(IbtikarAuthorizationFilter))
        {
            Arguments = new object[] { roles };
        }
    }
}
