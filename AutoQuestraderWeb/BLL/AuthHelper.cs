using BLL.QTModels;
using BLL;
using BLL.DBModels;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL
{
    public static class AuthHelper
    {
        private static string liveServer = "https://login.questrade.com";
        private static string practiceServer = "https://practicelogin.questrade.com";

        public static Token RefreshToken(AutoQuestraderContext db, bool live, bool forceRefresh = false)
        {
            var loginServer = live ? liveServer : practiceServer;

            var curToken = db.Tokens.FirstOrDefault(p => p.LoginServer == loginServer);

            if (curToken == null)
            {
                curToken = new Token();
                curToken.LoginServer = loginServer;
                curToken.ExpiresDate = DateTimeOffset.MinValue;
                db.Tokens.Add(curToken);
            }

            if (curToken.ExpiresDate <= DateTimeOffset.UtcNow || forceRefresh)
            {
                return RefreshToken(db, curToken);
            }
            else
            {
                return curToken;
            }
        }

        private static Token PromptForNewRefreshToken(Token curToken)
        {
            Console.WriteLine("Please enter a valid token for: " + curToken.LoginServer);

            var input = Console.ReadLine();

            if (String.IsNullOrEmpty(input))
            {
                Console.WriteLine("Press enter to close...");
                Console.ReadLine();
                Environment.Exit(0);
            }
            else
            {
                curToken.RefreshToken = input;
            }

            return curToken;
        }

        public static Token GetRefreshToken(string client_id, string code, string redirectUri, bool live) {
            IRestResponse<AuthTokenResponse> responseToken = null;
            try
            {
                var loginServer = live ? liveServer : practiceServer;
                var authClient = new RestClient(loginServer);

                var request = new RestRequest("oauth2/token", Method.GET);
                request.AddParameter("client_id", client_id);
                request.AddParameter("code", code);
                request.AddParameter("grant_type", "authorization_code");
                request.AddParameter("redirect_uri", redirectUri);

                responseToken = authClient.Execute<AuthTokenResponse>(request);

                var curToken = new Token();
                
                curToken.ApiServer = responseToken.Data.api_server;
                curToken.AccessToken = responseToken.Data.access_token;
                curToken.ExpiresIn = responseToken.Data.expires_in;
                curToken.ExpiresDate = DateTimeOffset.UtcNow.AddSeconds(responseToken.Data.expires_in - 30); // create a 30 second buffer to account for network slowness
                curToken.RefreshToken = responseToken.Data.refresh_token;
                curToken.TokenType = responseToken.Data.token_type;

                return curToken;
            }
            catch
            {
                Console.WriteLine("Error logging in: " + responseToken.Content);
                return null;
            }
        }

        public static Token RefreshToken(AutoQuestraderContext db, Token curToken)
        {
            IRestResponse<AuthTokenResponse> responseToken = null;
            try
            {
                var authClient = new RestClient(curToken.LoginServer);

                var request = new RestRequest("oauth2/token", Method.GET);
                request.AddParameter("grant_type", "refresh_token");
                request.AddParameter("refresh_token", curToken.RefreshToken);

                responseToken = authClient.Execute<AuthTokenResponse>(request);

                curToken.ApiServer = responseToken.Data.api_server;
                curToken.AccessToken = responseToken.Data.access_token;
                curToken.ExpiresIn = responseToken.Data.expires_in;
                curToken.ExpiresDate = DateTimeOffset.UtcNow.AddSeconds(responseToken.Data.expires_in - 30); // create a 30 second buffer to account for network slowness
                curToken.RefreshToken = responseToken.Data.refresh_token;
                curToken.TokenType = responseToken.Data.token_type;

                db.SaveChanges();

                return curToken;
            }
            catch
            {
                Console.WriteLine("Error logging in: " + responseToken.Content);

                return RefreshToken(db, PromptForNewRefreshToken(curToken));
            }
        }
    }
}
