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
        public static RestRequest accountsRequest = new RestRequest("v1/accounts", Method.GET);
        public static RestRequest positionsRequest = new RestRequest("/v1/accounts/{accountNumber}/positions", Method.GET);
        public static RestRequest balancesRequest = new RestRequest("/v1/accounts/{accountNumber}/balances", Method.GET);


        static void Main(string[] args)
        {
            var db = new AutoQuestraderEntities();

            var token = AuthHelper.RefreshToken(db, false);

            var client = new RestClient(token.ApiServer);
            client.AddDefaultHeader("Authorization", token.TokenType + " " + token.AccessToken);

           
            User responseUser = client.Execute<User>(accountsRequest).Data;

            foreach (var curAccount in responseUser.accounts)
            {
                positionsRequest.AddUrlSegment("accountNumber", curAccount.number);
                var y = client.Execute<PositionsResponse>(positionsRequest);
                PositionsResponse positions = client.Execute<PositionsResponse>(positionsRequest).Data;

                balancesRequest.AddUrlSegment("accountNumber", curAccount.number);

                var t = client.Execute<BalancesResponse>(balancesRequest);
                List<BalancesResponse> balances = client.Execute<List<BalancesResponse>>(balancesRequest).Data;
            }

        }
    }
}
