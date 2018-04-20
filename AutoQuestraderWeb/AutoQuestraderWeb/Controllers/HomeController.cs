using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoQuestraderWeb.Models;
using BLL.DBModels;
using BLL;
using BLL.Models;
using Microsoft.Extensions.Options;
using RestSharp;
using BLL.QTModels;
using Newtonsoft.Json;
using BLL.Misc;
using AutoQuestraderWeb.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace AutoQuestraderWeb.Controllers
{
    public class HomeController : Controller
    {
        AutoQuestraderContext db;
        AppSettings appSettings;
        IHubContext<TraderHub> traderHub;

        public HomeController(AutoQuestraderContext db, IOptions<AppSettings> appSettings, IHubContext<TraderHub> traderHub)
        {
            this.db = db;
            this.appSettings = appSettings.Value;
            this.traderHub = traderHub;
        }

        public IActionResult AuthTest()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email)) {
                return Json(new ApiResponse(success: false, message: "Error: No email provided"));
            }

            var identity = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Email, email),
            }, CookieAuthenticationDefaults.AuthenticationScheme);

            var principle = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principle);

            return Json(new ApiResponse(message: "Logged in."));
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Json(new ApiResponse(message: "Logged out."));
        }
        public async Task<IActionResult> QTLogin(string code, string a)
        {
            ViewBag.codeProvided = false;
            if (!string.IsNullOrEmpty(code))
            {
                ViewBag.codeProvided = true;

                var response = new ApiResponse();

                var email = MiscHelpers.Base64Decode(a);

                var curUser = db.Users.Include(p => p.Token).FirstOrDefault(p => p.Email == email);

                if (curUser == null)
                {
                    response.Success = false;
                    response.Messages.Add("No user found with provided email.");
                    await traderHub.Clients.Client(curUser.ConnectionId).InvokeAsync("recievedAuthToken", response);
                    return View();
                }

                var redirectUrl = appSettings.BaseUrl + GlobalVars.LOGIN_REDIRECT_PATH;
                try
                {
                    var curToken = AuthHelper.GetRefreshToken(appSettings.QuestradeaAppKey, code, redirectUrl, true);

                    if (curToken == null)
                    {
                        response.Success = false;
                        response.Messages.Add("Error fetching auth token. Please contact administrator.");
                        await traderHub.Clients.Client(curUser.ConnectionId).InvokeAsync("recievedAuthToken", response);
                        return View();
                    }

                    if (curUser.Token != null)
                    {
                        db.Tokens.Remove(curUser.Token);
                        db.SaveChanges();
                    }

                    curToken.UserID = curUser.ID;

                    db.Tokens.Add(curToken);
                    db.SaveChanges();

                    //TODO: I think this has potential to be a race condition, make sure the email provided matches the current connection
                    await traderHub.Clients.Client(curUser.ConnectionId).InvokeAsync("recievedAuthToken", response);
                }
                catch (Exception e)
                {
                    await traderHub.Clients.Client(curUser.ConnectionId).InvokeAsync("recievedAuthToken", e);
                }
            }

            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
