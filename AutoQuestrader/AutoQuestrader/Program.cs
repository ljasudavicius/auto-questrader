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
        public static RestRequest symbolsRequest { get { return new RestRequest("/v1/symbols/", Method.GET); } }

        public static RestClient client;

        static void Main(string[] args)
        {
            var db = new AutoQuestraderEntities();

            var token = AuthHelper.RefreshToken(db, false);

            client = new RestClient(token.ApiServer);
            client.AddDefaultHeader("Authorization", token.TokenType + " " + token.AccessToken);
         
            User responseUser = client.Execute<User>(new RestRequest("v1/accounts", Method.GET)).Data;

            foreach (var curAccount in responseUser.accounts)
            {
                PositionsResponse positions = GetPositions(curAccount.number);

                BalancesResponse balances = GetBalances(curAccount.number);
                var totalEquityInCAD = balances.combinedBalances.FirstOrDefault(p => p.currency == "CAD").totalEquity;
                var totalEquityInUSD = balances.combinedBalances.FirstOrDefault(p => p.currency == "USD").totalEquity;

                var accountCategories = db.AccountCategories.Where(p => p.AccountNumber == curAccount.number);

                foreach (var curAccountCategory in accountCategories) {

                    foreach (var curStockTarget in curAccountCategory.Category.StockTargets)
                    {

                        var symbol = GetSymbol(curStockTarget.SymbolName);
                        var quote = GetQuote(symbol.symbolId);

                        var curPosition = positions.positions.FirstOrDefault(p => p.symbol == curStockTarget.SymbolName);

                        double currentPercent = 0;
                        double totalEquity = balances.combinedBalances.FirstOrDefault(p => p.currency == symbol.currency).totalEquity;
                        if (curPosition != null) {
                            currentPercent = (curPosition.currentMarketValue / totalEquity)*100;
                        }

                        double percentOfTarget = (currentPercent / curStockTarget.TargetPercent)*100;

                        if (percentOfTarget < 90) {

                            var valueToBuy = ((curStockTarget.TargetPercent - currentPercent)/100) * totalEquity;
                            int numSharesToBuy = (int)Math.Floor(valueToBuy / quote.askPrice);

                            CreateMarketBuyOrder(curAccount.number, symbol.symbolId, numSharesToBuy);
                        }
                    }
                }
            }
        }

        public static PositionsResponse GetPositions(string accountNumber) {
            var request = new RestRequest("/v1/accounts/{accountNumber}/positions", Method.GET);
            request.AddUrlSegment("accountNumber", accountNumber);
            return client.Execute<PositionsResponse>(request).Data;
        }

        public static BalancesResponse GetBalances(string accountNumber)
        {
            var request = new RestRequest("/v1/accounts/{accountNumber}/balances", Method.GET);
            request.AddUrlSegment("accountNumber", accountNumber);
            return client.Execute<BalancesResponse>(request).Data;
        }

        public static Symbol GetSymbol(string symbolName)
        {
            var request = new RestRequest("/v1/symbols/", Method.GET);
            request.AddParameter("names", symbolName);

            return client.Execute<SymbolsResponse>(request).Data.symbols.FirstOrDefault();
        }

        public static Quote GetQuote(int symbolId)
        {
            var request = new RestRequest("/v1/markets/quotes/{symbolId}", Method.GET);
            request.AddUrlSegment("symbolId", symbolId.ToString());

            return client.Execute<QuotesResponse>(request).Data.quotes.FirstOrDefault();
        }

        public static void CreateMarketBuyOrder(string accountNumber, int symbolId, int quantity)
        {
            var request = new RestRequest("/v1/accounts/{accountNumber}/orders", Method.POST);
            request.RequestFormat = DataFormat.Json;
            request.AddHeader("Content-Type", "application/json; charset=utf-8");
            request.AddHeader("Accept", "application/json");
            request.AddUrlSegment("accountNumber", accountNumber);


            //    var body = new {
            //        accountNumber = accountNumber,
            //        symbolId= symbolId,
            //        quantity= quantity,
            //        icebergQuantity= 1,
            //        limitPrice= 537,
            //        isAllOrNone= true,
            //        isAnonymous= false,
            //        orderType= "Limit",
            //        timeInForce= "GoodTillCanceled",
            //        action= "Buy",
            //        primaryRoute= "AUTO",
            //        secondaryRoute= "AUTO"
            //    };

            //request.AddParameter("text/json", body, ParameterType.RequestBody);

            request.AddParameter("symbolId", symbolId);
            request.AddParameter("quantity", quantity);
            request.AddParameter("orderType", "Market");
            request.AddParameter("action", "Buy");
            request.AddParameter("primaryRoute", "AUTO");
            request.AddParameter("secondaryRoute", "AUTO");
            request.AddParameter("timeInForce", "ImmediateOrCancel");

            request.AddParameter("icebergQuantity", 1);
            request.AddParameter("limitPrice", 0);
            request.AddParameter("stopPrice", 0);
            request.AddParameter("isAllOrNone", false);
            request.AddParameter("isAnonymous", false);

            var t = client.Execute(request);

        }

    }
}
