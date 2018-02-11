using BLL;
using BLL.DBModels;
using BLL.Misc;
using BLL.Models;
using BLL.QTModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RestSharp;
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
            var response = new ApiResponse();

            var email = Context.Connection.GetHttpContext().Request.Query["email"].ToString().Trim().ToLower();
            var QTAppKeyOverride = Context.Connection.GetHttpContext().Request.Query["QTAppKeyOverride"];

            //TODO: Add proper email validation
            if (string.IsNullOrEmpty(email))
            {
                response.Success = false;
                response.Messages.Add("Email is invalid.");
                await Clients.Client(Context.ConnectionId).InvokeAsync("recievedLoginUrl", response);
            }

            try
            {
                var curUser = db.Users.FirstOrDefault(p => p.Email.ToLower() == email);
           
            if (curUser == null)
            {
                curUser = new BLL.DBModels.User();
                curUser.Email = email;

                db.Users.Add(curUser);
            }
            curUser.ConnectionId = Context.ConnectionId;
            db.SaveChanges();

            if (curUser.Token != null) {
                //TODO: attempt to login with existing token
            }
            }
            catch (Exception e)
            {
            }
            var redirectUrl = appSettings.BaseUrl + GlobalVars.LOGIN_REDIRECT_PATH + "?a=" + MiscHelpers.Base64Encode(email);

            var loginUrl = GlobalVars.QT_OAUTH_LOGIN_URL;
            loginUrl += "?client_id=" + (string.IsNullOrEmpty(QTAppKeyOverride)? appSettings.QuestradeaAppKey : QTAppKeyOverride.ToString());
            loginUrl += "&response_type=code";
            loginUrl += "&redirect_uri=" + redirectUrl;

            response.Payload = loginUrl;
          

            await Clients.Client(Context.ConnectionId).InvokeAsync("recievedLoginUrl", response);
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            var curUser = db.Users.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);

            if (curUser != null) {
                curUser.ConnectionId = null;
                db.SaveChanges();
            }
        }

        public async Task RequestAccounts()
        {
            var response = new ApiResponse();

            var curUser = db.Users.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (curUser == null || curUser.Token == null) {
                response.Success = false;
                response.Messages.Add("Invalid User Session");
                await Clients.Client(Context.ConnectionId).InvokeAsync("recievedAccounts", response);
            }

            RestClient client = new RestClient(curUser.Token.ApiServer);
            client.AddDefaultHeader("Authorization", curUser.Token.TokenType + " " + curUser.Token.AccessToken);

            var request = new RestRequest("/v1/accounts", Method.GET);
            var accounts = client.Execute<AccountsResponse>(request).Data;

            response.Payload = accounts;

            await Clients.All.InvokeAsync("recievedAccounts", response);
        }
       
    }
}
