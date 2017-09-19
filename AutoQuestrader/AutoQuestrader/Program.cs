using AutoQuestrader.apiModels;
using AutoQuestrader.Models;
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
        public static AutoQuestraderEntities db;
        public static RestClient client;
        public static User curUser;

        static void Main(string[] args)
        {
            db = new AutoQuestraderEntities();

            var token = AuthHelper.RefreshToken(db, false);

            client = new RestClient(token.ApiServer);
            client.AddDefaultHeader("Authorization", token.TokenType + " " + token.AccessToken);
         
            curUser = client.Execute<User>(new RestRequest("v1/accounts", Method.GET)).Data;

            var pendingOrders = GetPendingOrders();

            var ngRequirment = GetNorbertsGambitRequirement(pendingOrders);
        }

        public static PendingOrder GetNorbertsGambitRequirement(List<PendingOrder> pendingOrders)
        {
            var accountGroups = pendingOrders.GroupBy(p => p.AccountNumber);



            return new PendingOrder();
        }

        public static List<PendingOrder> GetPendingOrders() {
            var pendingOrders = new List<PendingOrder>();

            foreach (var curAccount in curUser.accounts)
            {
                PositionsResponse positions = GetPositions(curAccount.number);
                BalancesResponse balances = GetBalances(curAccount.number);

                var totalEquityInCAD = balances.combinedBalances.FirstOrDefault(p => p.currency == "CAD").totalEquity;
                var totalEquityInUSD = balances.combinedBalances.FirstOrDefault(p => p.currency == "USD").totalEquity;

                var accountCategories = db.AccountCategories.Where(p => p.AccountNumber == curAccount.number);

                foreach (var curAccountCategory in accountCategories)
                {
                    foreach (var curStockTarget in curAccountCategory.Category.StockTargets)
                    {
                        var symbol = GetSymbol(curStockTarget.Symbol);
                        var quote = GetQuote(symbol.symbolId);

                        var curPosition = positions.positions.FirstOrDefault(p => p.symbol == curStockTarget.Symbol);

                        double totalEquity = balances.combinedBalances.FirstOrDefault(p => p.currency == symbol.currency).totalEquity;
                        double currentPercentOwned = curPosition != null ? (curPosition.currentMarketValue / totalEquity) * 100 : 0;
                      
                        double accountTargetPercent = ((curAccountCategory.Percent / 100) * (curStockTarget.TargetPercent / 100)) * 100;
                        double percentOfTarget = (currentPercentOwned / accountTargetPercent) * 100;

                        if (percentOfTarget < 95)
                        {
                            var valueToBuy = ((accountTargetPercent - currentPercentOwned) / 100) * totalEquity;
                            int numSharesToBuy = (int)Math.Floor(valueToBuy / quote.askPrice);

                            if (numSharesToBuy > 0)
                            {
                                pendingOrders.Add(new PendingOrder()
                                {
                                    AccountNumber = curAccount.number,
                                    Symbol = symbol,
                                    Quote = quote,
                                    IsBuyOrder = true,
                                    Value = valueToBuy,
                                    Quantity = numSharesToBuy
                                });
                            }
                        }
                    }
                }
            }

            return pendingOrders;
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

        public static void CreateMarketOrder(PendingOrder curPendingOrder)
        {
            CreateMarketOrder(curPendingOrder.AccountNumber, curPendingOrder.Quote.symbolId, curPendingOrder.IsBuyOrder, curPendingOrder.Quantity);
        }

        public static void CreateMarketOrder(string accountNumber, int symbolId, bool isBuyOrder, int quantity)
        {
            var request = new RestRequest("/v1/accounts/{accountNumber}/orders", Method.POST);
            request.AddUrlSegment("accountNumber", accountNumber);

            var body = new
            {
                accountNumber = accountNumber,
                symbolId = symbolId,
                quantity = quantity,
                icebergQuantity = 1,
                isAllOrNone = false,
                isAnonymous = false,
                orderType = "Market",
                timeInForce = "GoodTillCanceled",
                action = isBuyOrder ? "Buy" : "Sell",
                primaryRoute = "AUTO",
                secondaryRoute = "AUTO"
            };

            request.RequestFormat = DataFormat.Json;
            request.AddBody(body);

            client.Execute(request);

            Console.WriteLine("-- New Order Placed --");
            Console.WriteLine("Action: "+ body.action);
            Console.WriteLine("Symbol: "+ symbolId);
            Console.WriteLine("Quantity: " + quantity);
            Console.WriteLine("");
        }

    }
}
