using BLL;
using BLL.DBModels;
using BLL.Misc;
using BLL.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoQuestraderWeb.Hubs
{
    public class TraderHub : Hub
    {
        AutoQuestraderContext db;
        AppSettings appSettings;

        public TraderHub(AutoQuestraderContext db, IOptions<AppSettings> appSettings)
        {
            this.db = db;
            this.appSettings = appSettings.Value;
        }

        public override async Task OnConnectedAsync()
        {
            var email = Context.Connection.GetHttpContext().Request.Query["email"];

            var response = new ApiResponse();

            var redirectUrl = appSettings.BaseUrl + GlobalVars.LOGIN_REDIRECT_PATH + "?a=" + MiscHelpers.Base64Encode(email);

            var loginUrl = GlobalVars.QT_OAUTH_LOGIN_URL;
            loginUrl += "?client_id=" + appSettings.QuestradeaAppKey;
            loginUrl += "&response_type=code";
            loginUrl += "&redirect_uri=" + redirectUrl;

            response.Payload = loginUrl;

            await Clients.Client(Context.ConnectionId).InvokeAsync("recievedLoginUrl", response);
        }

        public void Send(string name, string message)
        {
            // Call the broadcastMessage method to update clients.
            Clients.All.InvokeAsync("broadcastMessage", name, message);
        }
    }
}
