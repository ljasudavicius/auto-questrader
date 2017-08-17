using AutoQuestrader.apiModels;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoQuestrader
{
    public static class AuthHelper
    {
        public static Token RefreshToken(AutoQuestraderEntities db, bool live) {

            var loginServer = live ? "https://login.questrade.com" : "https://practicelogin.questrade.com";

            var curToken = db.Tokens.FirstOrDefault(p => p.LoginServer == loginServer);

            if (curToken != null)
            {
                var authClient = new RestClient(curToken.LoginServer);

                var request = new RestRequest("oauth2/token", Method.GET);
                request.AddParameter("grant_type", "refresh_token");
                request.AddParameter("refresh_token", curToken.RefreshToken);

                IRestResponse<AuthTokenResponse> responseToken = authClient.Execute<AuthTokenResponse>(request);

                curToken.ApiServer = responseToken.Data.api_server;
                curToken.AccessToken = responseToken.Data.access_token;
                curToken.ExpiresIn = responseToken.Data.expires_in;
                curToken.RefreshToken = responseToken.Data.refresh_token;
                curToken.TokenType = responseToken.Data.token_type;

                db.SaveChanges();

               return curToken;
            }
            else {
                throw new Exception("No token found in DB for that login server.");
            }
        }
    }
}
