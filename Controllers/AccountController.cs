using System.Security.Claims;
using Ibtikar.DTOs.Account;
using Ibtikar.Models;
using Ibtikar.Services.Helpers;
using Ibtikar.Services.Implementations;
using Ibtikar.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ibtikar.Controllers
{
    public class AccountController : Controller
    {
        public readonly record struct DemoUser(string Username, string FullName, string RoleName);

        public AccountController()
        {
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}