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
        public static readonly bool IS_LIVE = false;

        public static AutoQuestraderEntities db;
        public static RestClient client;
        public static User curUser;
        public static readonly string NG_SYMBOL_CAD = "DLR.TO";
        public static readonly string NG_SYMBOL_USD = "DLR.U.TO";
        public static readonly string CURRENCY_USD = "USD";
        public static readonly string CURRENCY_CAD = "CAD";

        static void Main(string[] args)
        {
            db = new AutoQuestraderEntities();

            var token = AuthHelper.RefreshToken(db, IS_LIVE);

            client = new RestClient(token.ApiServer);
            client.AddDefaultHeader("Authorization", token.TokenType + " " + token.AccessToken);
         
            curUser = client.Execute<User>(new RestRequest("v1/accounts", Method.GET)).Data;

            //SellAllSecuritiesInAllAccounts(); 

            var pendingOrders = GetPendingOrders();

            var ngRequirments = GetNorbertsGambitRequirements(pendingOrders);
        }

        public static void SellAllSecuritiesInAllAccounts() {

            foreach (var curAccount in curUser.accounts)
            {
                SellAllSecuritiesInAccount(curAccount.number);
            }
        }

        public static void SellAllSecuritiesInAccount(string accountNumber)
        {
            if (IS_LIVE) {
                throw new Exception("Attempting to sell all on LIVE account!");
            }
            else {

                PositionsResponse positions = GetPositions(accountNumber);
                foreach (var curPosition in positions.positions)
                {
                    if (curPosition.openQuantity > 0)
                    {
                        CreateMarketOrder(accountNumber, curPosition.symbolId, false, (int)curPosition.openQuantity);
                    }
                }
            }
        }

        public static List<PendingOrder> GetNorbertsGambitRequirements(List<PendingOrder> pendingOrders)
        {
            var pendingNGOrders = new List<PendingOrder>();

            var accountGroups = pendingOrders.GroupBy(p => p.AccountNumber);
           
            foreach (var curAccountGroup in accountGroups) {
                var accountNumber = curAccountGroup.Key;
                double requiredUSD = 0;

                foreach (var curPendingOrder in curAccountGroup)
                {
                    if (curPendingOrder.Symbol.currency == CURRENCY_USD)
                    {
                        requiredUSD += curPendingOrder.Value;
                    }
                }

                var NGSymbolUSD = GetSymbol(NG_SYMBOL_USD);
                var NGQuoteUSD = GetQuote(NGSymbolUSD.symbolId);

                PositionsResponse positions = GetPositions(accountNumber);
                var ngPositionUSD = positions.positions.FirstOrDefault(p => p.symbol == NG_SYMBOL_USD);

                if (ngPositionUSD != null) { // sell all NG stocks
                    CreateMarketOrder(accountNumber, ngPositionUSD.symbolId, false, (int)ngPositionUSD.openQuantity);
                }

                BalancesResponse balances = GetBalances(accountNumber);
                var curBalanceUSD = balances.perCurrencyBalances.FirstOrDefault(p => p.currency == CURRENCY_USD);

                var ngPositionCAD = positions.positions.FirstOrDefault(p => p.symbol == NG_SYMBOL_CAD);
                var pendingNGValueUSD = ngPositionCAD.openQuantity * NGQuoteUSD.bidPrice;

                var balanceUSDRequired = requiredUSD - (curBalanceUSD.cash + pendingNGValueUSD);

                if (balanceUSDRequired > 0) {

                    var numNGSharesNeeded = (int)Math.Ceiling(balanceUSDRequired / NGQuoteUSD.bidPrice);

                    var NGSymbolCAD = GetSymbol(NG_SYMBOL_CAD);
                    var NGQuoteCAD = GetQuote(NGSymbolCAD.symbolId);

                    pendingNGOrders.Add(new PendingOrder() {
                        Symbol = NGSymbolCAD,
                        Quote = NGQuoteCAD,
                        Quantity = numNGSharesNeeded,
                        IsBuyOrder = true,
                        Value = numNGSharesNeeded * NGQuoteCAD.askPrice
                    });
                }
            }

            return pendingNGOrders;
        }

        public static List<PendingOrder> GetPendingOrders() {
            var pendingOrders = new List<PendingOrder>();

            foreach (var curAccount in curUser.accounts)
            {
                PositionsResponse positions = GetPositions(curAccount.number);
                BalancesResponse balances = GetBalances(curAccount.number);

                var totalEquityInCAD = balances.combinedBalances.FirstOrDefault(p => p.currency == CURRENCY_CAD).totalEquity;
                var totalEquityInUSD = balances.combinedBalances.FirstOrDefault(p => p.currency == CURRENCY_USD).totalEquity;

                var accountCategories = db.AccountCategories.Where(p => p.AccountNumber == curAccount.number);

                foreach (var curAccountCategory in accountCategories)
                {
                    foreach (var curStockTarget in curAccountCategory.Category.StockTargets)
                    {
                        var symbol = GetSymbol(curStockTarget.Symbol);
                        var quote = GetQuote(symbol.symbolId);

                        var curPosition = positions.positions.FirstOrDefault(p => p.symbol == curStockTarget.Symbol);

                        double totalEquity = balances.combinedBalances.FirstOrDefault(p => p.currency == symbol.currency).totalEquity;
                        double curPercentOwned = curPosition != null ? (curPosition.currentMarketValue / totalEquity) * 100 : 0;
                      
                        double accountTargetPercent = ((curAccountCategory.Percent / 100) * (curStockTarget.TargetPercent / 100)) * 100;
                        double percentOfTarget = (curPercentOwned / accountTargetPercent) * 100;

                        //Console.WriteLine("-- Stock Target --");
                        //Console.WriteLine("Symbol: "+ curStockTarget.Symbol);
                        //Console.WriteLine("Account Target Percent: " + accountTargetPercent);
                        //Console.WriteLine("Current Percent Owned: " + curPercentOwned);
                        //Console.WriteLine("");

                        if (percentOfTarget < 95)
                        {
                            var valueToBuy = ((accountTargetPercent - curPercentOwned) / 100) * totalEquity;
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
                                    Quantity = numSharesToBuy,
                                    TargetPercent = accountTargetPercent,
                                    OwnedPercent = percentOfTarget,
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

            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("-- New Order Placed --");
                Console.WriteLine("Action: " + body.action);
                Console.WriteLine("Symbol: " + symbolId);
                Console.WriteLine("Quantity: " + quantity);
                Console.WriteLine("Message: " + response.Content);
                Console.WriteLine("");
            }
            else {
                throw new Exception("Create market order request failed.");
            }
        }

    }
}
