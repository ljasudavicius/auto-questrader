using AutoQuestrader.apiModels;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AutoQuestrader
{
    class Program
    {
        static void Main(string[] args)
        {
            string qtToken = "FYU1kZ68CoYXCfIBdfmDdCT6p14A8sTo0";

            var authClient = new RestClient("https://login.questrade.com");

            var request = new RestRequest("oauth2/token", Method.GET);
            request.AddParameter("grant_type", "refresh_token");
            request.AddParameter("refresh_token", qtToken);

            IRestResponse<Token> responseToken = authClient.Execute<Token>(request);

   
            var client = new RestClient(responseToken.Data.api_server);
            client.AddDefaultHeader("Authorization", responseToken.Data.token_type + " " + responseToken.Data.access_token);

            var accountsRequest = new RestRequest("v1/accounts", Method.GET);

            IRestResponse<User> responseUser = client.Execute<User>(accountsRequest);
        }
    }
}
