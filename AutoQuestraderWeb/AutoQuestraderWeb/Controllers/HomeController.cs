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
using BLL.APIModels;
using Newtonsoft.Json;

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

        public IActionResult Index(string code)
        {
            // var trader = new Trader(db);
            // trader.Main();

            ViewBag.QTAppKey = appSettings.QuestradeaAppKey;

            if (!string.IsNullOrEmpty(code)) {

                try
                {
                    var curToken = AuthHelper.GetRefreshToken(appSettings.QuestradeaAppKey, code, "https://automaticinvesting.ca", true);
                    RestClient client;
                    client = new RestClient(curToken.ApiServer);
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
