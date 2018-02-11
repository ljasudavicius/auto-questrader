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

namespace AutoQuestraderWeb.Controllers
{
    public class HomeController : Controller
    {
        AutoQuestraderContext db;
        AppSettings appSettings;

        public HomeController(AutoQuestraderContext db, IOptions<AppSettings> appSettings)
        {
            this.db = db;
            this.appSettings = appSettings.Value;
        }

        public IActionResult GetLoginUrl(string email) {

            var response = new ApiResponse();

            var redirectUrl = appSettings.BaseUrl + GlobalVars.LOGIN_REDIRECT_PATH + "?a=" + MiscHelpers.Base64Encode(email);

            var loginUrl = "https://login.questrade.com/oauth2/authorize";
            loginUrl += "?client_id=" + appSettings.QuestradeaAppKey;
            loginUrl += "&response_type=code";
            loginUrl += "&redirect_uri=" + redirectUrl;

            response.Payload = loginUrl;

            return Json(response);
        }

        public IActionResult Login(string code, string a) {

            var response = new ApiResponse();

            var email = MiscHelpers.Base64Decode(a);

            var curToken = AuthHelper.GetRefreshToken(appSettings.QuestradeaAppKey, code, appSettings.BaseUrl, true);
            RestClient client = new RestClient(curToken.ApiServer);
            client.AddDefaultHeader("Authorization", curToken.TokenType + " " + curToken.AccessToken);

            var request = new RestRequest("/v1/accounts", Method.GET);
            var accounts = client.Execute<AccountsResponse>(request).Data;

            response.Payload = accounts;

            return Json(response);
        }

        public IActionResult Index(string code)
        {
            // var trader = new Trader(db);
            // trader.Main();

            ViewBag.QTAppKey = appSettings.QuestradeaAppKey;

            if (!string.IsNullOrEmpty(code)) {

                try
                {
                    var curToken = AuthHelper.GetRefreshToken(appSettings.QuestradeaAppKey, code, appSettings.BaseUrl, true);
                    RestClient client = new RestClient(curToken.ApiServer);
                    client.AddDefaultHeader("Authorization", curToken.TokenType + " " + curToken.AccessToken);

                    var request = new RestRequest("/v1/accounts", Method.GET);
                    var accounts = client.Execute<AccountsResponse>(request).Data;

                    ViewBag.testResponse = JsonConvert.SerializeObject(accounts);
                }
                catch {
                    ViewBag.message = "Error reading from QT, please try again.";
                }
            }

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
