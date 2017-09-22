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
        private static string liveServer = "https://login.questrade.com";
        private static string practiceServer = "https://practicelogin.questrade.com";

        public static Token RefreshToken(AutoQuestraderEntities db, bool live, bool forceRefresh = false)
        {
            var loginServer = live ? liveServer : practiceServer;

            var curToken = db.Tokens.FirstOrDefault(p => p.LoginServer == loginServer);

            if (curToken == null) {
                curToken = new Token();
                curToken.LoginServer = loginServer;
                curToken.ExpiresDate = DateTimeOffset.MinValue;
                db.Tokens.Add(curToken);
            }

            if (curToken.ExpiresDate <= DateTimeOffset.UtcNow || forceRefresh) 
            {
                return RefreshToken(db, curToken);
            }
            else {
                return curToken;
            }         
        }

        private static Token PromptForNewRefreshToken(Token curToken) {
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

        public static Token RefreshToken(AutoQuestraderEntities db, Token curToken) {
            try
            {
                var authClient = new RestClient(curToken.LoginServer);

                var request = new RestRequest("oauth2/token", Method.GET);
                request.AddParameter("grant_type", "refresh_token");
                request.AddParameter("refresh_token", curToken.RefreshToken);

                IRestResponse<AuthTokenResponse> responseToken = authClient.Execute<AuthTokenResponse>(request);

                curToken.ApiServer = responseToken.Data.api_server;
                curToken.AccessToken = responseToken.Data.access_token;
                curToken.ExpiresIn = responseToken.Data.expires_in;
                curToken.ExpiresDate = DateTimeOffset.UtcNow.AddSeconds(responseToken.Data.expires_in - 30); // create a 30 second buffer to account for network slowness
                curToken.RefreshToken = responseToken.Data.refresh_token;
                curToken.TokenType = responseToken.Data.token_type;

                db.SaveChanges();

                return curToken;
            }
            catch {
                return RefreshToken(db, PromptForNewRefreshToken(curToken));
            }
        }
    }
}
