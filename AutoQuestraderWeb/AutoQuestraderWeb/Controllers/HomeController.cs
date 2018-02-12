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

        public async Task<IActionResult> Login(string code, string a) {

            ViewBag.codeProvided = false;
            if (!string.IsNullOrEmpty(code))
            {
                ViewBag.codeProvided = true;
                try
                {
                    var response = new ApiResponse();

                    var email = MiscHelpers.Base64Decode(a);

                    var curUser = db.Users.FirstOrDefault(p => p.Email == email);

                    if (curUser == null)
                    {
                        response.Success = false;
                        response.Messages.Add("No user found with provided email.");
                        await traderHub.Clients.Client(curUser.ConnectionId).InvokeAsync("recievedAuthToken", response);
                        return View();
                    }

                    var redirectUrl = appSettings.BaseUrl + GlobalVars.LOGIN_REDIRECT_PATH;

                    var loginServer = "https://login.questrade.com";
                    var authClient = new RestClient(loginServer);

                    var request = new RestRequest("oauth2/token", Method.GET);
                    //request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };
                    request.AddParameter("client_id", appSettings.QuestradeaAppKey);
                    request.AddParameter("code", code);
                    request.AddParameter("grant_type", "authorization_code");
                    request.AddParameter("redirect_uri", redirectUrl);

                    var responseToken = authClient.Execute<AuthTokenResponse>(request);

                    await traderHub.Clients.Client(curUser.ConnectionId).InvokeAsync("recievedAuthToken", responseToken.Content);

                    var curToken = new Token();

                    curToken.ApiServer = responseToken.Data.api_server;
                    curToken.AccessToken = responseToken.Data.access_token;
                    curToken.ExpiresIn = responseToken.Data.expires_in;
                    curToken.ExpiresDate = DateTimeOffset.UtcNow.AddSeconds(responseToken.Data.expires_in - 30); // create a 30 second buffer to account for network slowness
                    curToken.RefreshToken = responseToken.Data.refresh_token;
                    curToken.TokenType = responseToken.Data.token_type;


                    //var curToken = AuthHelper.GetRefreshToken(appSettings.QuestradeaAppKey, code, redirectUrl, true);

                    if (curToken == null)
                    {
                        response.Messages.Add("2");
                        traderHub.Clients.Client(curUser.ConnectionId).InvokeAsync("recievedAuthToken", response);
                        return View();
                    }

                    if (curUser.Token == null)
                    {
                        curToken.UserID = curUser.ID;
                        db.Tokens.Add(curToken);
                    }
                    else
                    {
                        curToken.ID = curUser.Token.ID;
                        curUser.Token = curToken;
                    }

                    db.SaveChanges();

                    //TODO: I think this has potential to be a race condition
                    traderHub.Clients.Client(curUser.ConnectionId).InvokeAsync("recievedAuthToken", response);
                }
                catch (Exception e)
                {
                    ViewBag.e = JsonConvert.SerializeObject(e);
                }
            }

            return View();
        }

        public IActionResult Index(string code, string a)
        {
            // var trader = new Trader(db);
            // trader.Main();

            var email = MiscHelpers.Base64Decode(a);

            var redirectUrl = appSettings.BaseUrl + "?a=" + MiscHelpers.Base64Encode("luke");

            var loginUrl = "https://login.questrade.com/oauth2/authorize";
            loginUrl += "?client_id=" + appSettings.QuestradeaAppKey;
            loginUrl += "&response_type=code";
            loginUrl += "&redirect_uri=" + redirectUrl;

            ViewBag.testUrl = loginUrl;
            ViewBag.email = email;

            if (!string.IsNullOrEmpty(code))
            {
                try
                {
                    var curToken = AuthHelper.GetRefreshToken(appSettings.QuestradeaAppKey, code, appSettings.BaseUrl, true);
                    RestClient client = new RestClient(curToken.ApiServer);
                    client.AddDefaultHeader("Authorization", curToken.TokenType + " " + curToken.AccessToken);

                    var request = new RestRequest("/v1/accounts", Method.GET);
                    var accounts = client.Execute<AccountsResponse>(request).Data;

                    ViewBag.testResponse = JsonConvert.SerializeObject(accounts);
                }
                catch
                {
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
