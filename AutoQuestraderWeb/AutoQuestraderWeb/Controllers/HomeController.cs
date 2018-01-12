using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoQuestraderWeb.Models;
using BLL.DBModels;
using BLL;

namespace AutoQuestraderWeb.Controllers
{
    public class HomeController : Controller
    {
        //AutoQuestraderContext db;

        //public HomeController(AutoQuestraderContext db) {
        //    this.db = db;
        //}

        public IActionResult Index()
        {
           // var trader = new Trader(db);
           // trader.Main();

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
