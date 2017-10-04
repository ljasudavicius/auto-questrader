using AutoQuestrader.apiModels;
using AutoQuestrader.Models;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace AutoQuestrader
{
    class Program
    {
        public static readonly bool IS_LIVE = true;

        public static AutoQuestraderEntities db;
        public static RestClient client;
        public static User curUser;
        public static readonly string NG_SYMBOL_CAD = "DLR.TO";
        public static readonly string NG_SYMBOL_USD = "DLR.U.TO";
        public static readonly string CURRENCY_USD = "USD";
        public static readonly string CURRENCY_CAD = "CAD";
        public static readonly double UNACCEPTABLE_COMMISSION_PECENT_THRESHOLD = 0.01; // 1%
        public static readonly double UNACCEPTABLE_NG_THRESHOLD = 500; 

        static void Main(string[] args)
        {
            Console.WriteLine("Hi, welcome to AutoQuestrader.");
            Console.WriteLine("\nCalculating...");

            db = new AutoQuestraderEntities();

            var token = AuthHelper.RefreshToken(db, IS_LIVE);

            client = new RestClient(token.ApiServer);
            client.AddDefaultHeader("Authorization", token.TokenType + " " + token.AccessToken);

            var userResponse = client.Execute<User>(new RestRequest("v1/accounts", Method.GET));
            curUser = userResponse.Data;


            var pendingOrders = GetPendingOrdersForAllAccounts();
            Console.WriteLine("\n-- Pending Orders --");
            OutputPendingOrdersTable(pendingOrders);

        
            var ngRequirements = GetNorbertsGambitRequirements(pendingOrders);
            HandleNorbertsGambitRequriements(ngRequirements);
           
            HandlePurchasingOfPendingOrders(pendingOrders);

            Console.WriteLine("\nPurchasing Complete.");
            foreach (var curAccount in curUser.accounts) {
                Console.WriteLine("\nAccount number: "+ curAccount.number);
                BalancesResponse balances = GetBalances(curAccount.number);
                Console.WriteLine("CAD Cash: " + balances.perCurrencyBalances.FirstOrDefault(p => p.currency == CURRENCY_CAD).cash);
                Console.WriteLine("USD Cash: " + balances.perCurrencyBalances.FirstOrDefault(p => p.currency == CURRENCY_USD).cash);
            }

            Console.WriteLine("\n\nPress enter to close...");
            Console.ReadLine();
        }

        public static void HandleNorbertsGambitRequriements(List<PendingOrder> ngRequirements)
        {
            foreach (var curNgRequirement in ngRequirements)
            {
                Console.WriteLine("Currency conversion (Norberts Gambit) is required to purchase some securities in USD.");

                if (curNgRequirement.TargetValue < UNACCEPTABLE_NG_THRESHOLD) {
                    Console.WriteLine("Current conversion amount is below acceptable threshold of: "+ UNACCEPTABLE_NG_THRESHOLD);
                    Console.WriteLine("Skipping conversion.");
                    continue;
                }

                var orderImpact = GetMarketOrderImpact(curNgRequirement);
                BalancesResponse balances = GetBalances(curNgRequirement.AccountNumber);

                if (balances.perCurrencyBalances.FirstOrDefault(p => p.currency == curNgRequirement.Symbol.currency).cash < Math.Abs(orderImpact.buyingPowerEffect)) {
                    Console.WriteLine("Order exceeds current cash level.");
                    Console.WriteLine("Skipping conversion.");
                    continue;
                }

                Console.WriteLine("Proceeding to purchace $" + Math.Round(curNgRequirement.TargetValue, 2) + " in " + NG_SYMBOL_CAD);
                CreateMarketOrder(curNgRequirement);
                EmailHelper.SendNorbertsGambitEmail(curNgRequirement.AccountNumber, curNgRequirement.Quantity);
            }
        }

        public static void HandlePurchasingOfPendingOrders(List<PendingOrder> pendingOrders)
        {
            // attempt to the buy stocks in random order
            var random = new Random();
            do
            {
                int i = random.Next(pendingOrders.Count);
                var curPendingOrder = pendingOrders[i];

                HandlePurchaseOfPendingOrder(curPendingOrder);

                pendingOrders.Remove(curPendingOrder);
            } while (pendingOrders.Count() > 0);
        }

        public static void HandlePurchaseOfPendingOrder(PendingOrder pendingOrder) {
            Console.WriteLine("\nAttempting to purchase: " + pendingOrder.Symbol.symbol);

            var orderImpact = GetMarketOrderImpact(pendingOrder);
            Console.WriteLine("Trade value calculation: " + orderImpact.tradeValueCalculation);
            Console.WriteLine("Commission: " + orderImpact.estimatedCommissions);

            if (orderImpact.estimatedCommissions / (orderImpact.price * pendingOrder.Quantity) > UNACCEPTABLE_COMMISSION_PECENT_THRESHOLD)
            {
                Console.WriteLine("Commissions greater than " + UNACCEPTABLE_COMMISSION_PECENT_THRESHOLD * 100 + "% of value purchased.");
                Console.WriteLine("Skipping order.");
                return;
            }

            BalancesResponse balances = GetBalances(pendingOrder.AccountNumber);
            var cashLevel = balances.perCurrencyBalances.FirstOrDefault(p => p.currency == pendingOrder.Symbol.currency).cash;
            if (cashLevel < Math.Abs(orderImpact.buyingPowerEffect))
            {
                Console.WriteLine("Order of "+ Math.Abs(orderImpact.buyingPowerEffect) + " exceeds current cash level: "+ cashLevel);
                Console.WriteLine("Attempt to reduce quantity purchased.");

                pendingOrder.Quantity -= 1;

                if (pendingOrder.Quantity <= 0) {
                    Console.WriteLine("Order quantity reduced to zero.");
                    Console.WriteLine("Skipping order.");
                    return;
                }

                HandlePurchaseOfPendingOrder(pendingOrder);
            }

            Console.WriteLine("|| CREATEMAKERTORDER||");
            //CreateMarketOrder(pendingOrder);

        }

        public static void OutputPendingOrdersTable(List<PendingOrder> pendingOrders) {

            Console.WriteLine("");
            var pendingOrderGroups = pendingOrders.GroupBy(p => p.AccountNumber);
            foreach (var curPendingOrderGroup in pendingOrderGroups)
            {
                var curAccountNumber = curPendingOrderGroup.Key;
                Console.WriteLine("Account Number: "+ curAccountNumber);
                Console.WriteLine("");
                Console.WriteLine(String.Format("{0,11}{1,7}{2,13}{3,6}{4,10}{5,9}", "Symbol", "Qtty.", "Tgt.Val.", "Cur.", "Target %", "Owned %"));
                Console.WriteLine("--------------------------------------------------------");

                foreach (var curPendingOrder in curPendingOrderGroup)
                {
                    Console.WriteLine(String.Format("{0,11}{1,7}{2,13}{3,6}{4,10}{5,9}", 
                        curPendingOrder.Symbol.symbol, 
                        curPendingOrder.Quantity, 
                        Math.Round(curPendingOrder.TargetValue, 2), 
                        curPendingOrder.Symbol.currency, 
                        Math.Round(curPendingOrder.TargetPercent, 2),
                        Math.Round(curPendingOrder.OwnedPercent, 2)
                    ));
                }

                var totalPendingCADValue = curPendingOrderGroup.Where(p => p.Symbol.currency == CURRENCY_CAD).Sum(p => p.TargetValue);
                Console.WriteLine("\nTotal Pending CAD Value: " + Math.Round(totalPendingCADValue, 2));

                var totalPendingUSDValue = curPendingOrderGroup.Where(p => p.Symbol.currency == CURRENCY_USD).Sum(p => p.TargetValue);
                Console.WriteLine("Total Pending USD Value: " + Math.Round(totalPendingUSDValue, 2));
                Console.WriteLine("");
            }

            if (pendingOrders.Count() == 0) {
                Console.WriteLine("No pending orders.");
                Console.WriteLine("");
            }
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
                throw new Exception("Attempting to sell all securities on a LIVE account!");
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

        public static void SellAllUSNGStocks(string curAccountNumber) {
            PositionsResponse positions = GetPositions(curAccountNumber);
            var ngPositionUSD = positions.positions.FirstOrDefault(p => p.symbol == NG_SYMBOL_USD);

            if (ngPositionUSD != null && ngPositionUSD.openQuantity > 0)
            { 
                CreateMarketOrder(curAccountNumber, ngPositionUSD.symbolId, false, (int)ngPositionUSD.openQuantity);
            }
        }

        public static List<PendingOrder> GetNorbertsGambitRequirements(List<PendingOrder> pendingOrders)
        {
            var NGSymbolUSD = GetSymbol(NG_SYMBOL_USD);
            var NGQuoteUSD = GetQuote(NGSymbolUSD.symbolId);

            var pendingNGOrders = new List<PendingOrder>();

            var pendingOrderGroups = pendingOrders.GroupBy(p => p.AccountNumber);
            foreach (var curPendingOrderGroup in pendingOrderGroups)
            {
                var curAccountNumber = curPendingOrderGroup.Key;

                // sell all NG stocks that are already converted to US
                SellAllUSNGStocks(curAccountNumber);

                // get total USD required
                double requiredUSD = 0;
                foreach (var curPendingOrder in curPendingOrderGroup)
                {
                    if (curPendingOrder.Symbol.currency == CURRENCY_USD)
                    {
                        requiredUSD += curPendingOrder.TargetValue;
                    }
                }

                //get total USD that is waiting to be converted
                BalancesResponse balances = GetBalances(curAccountNumber);
                var curBalanceUSD = balances.perCurrencyBalances.FirstOrDefault(p => p.currency == CURRENCY_USD);

                PositionsResponse positions = GetPositions(curAccountNumber);
                var ngPositionCAD = positions.positions.FirstOrDefault(p => p.symbol == NG_SYMBOL_CAD);
                var pendingNGValueUSD = 0.0;
                if (ngPositionCAD != null)
                {
                    pendingNGValueUSD = ngPositionCAD.openQuantity * NGQuoteUSD.bidPrice;
                }

                var remainingUSDRequired = requiredUSD - (curBalanceUSD.cash + pendingNGValueUSD);

                if (remainingUSDRequired > 0) {

                    var numNGSharesNeeded = (int)Math.Ceiling(remainingUSDRequired / NGQuoteUSD.bidPrice);

                    var NGSymbolCAD = GetSymbol(NG_SYMBOL_CAD);
                    var NGQuoteCAD = GetQuote(NGSymbolCAD.symbolId);

                    pendingNGOrders.Add(new PendingOrder() {
                        AccountNumber = curAccountNumber,
                        Symbol = NGSymbolCAD,
                        Quote = NGQuoteCAD,
                        Quantity = numNGSharesNeeded,
                        IsBuyOrder = true,
                        TargetValue = numNGSharesNeeded * NGQuoteCAD.askPrice
                    });
                }
            }

            return pendingNGOrders;
        }

        public static List<PendingOrder> GetPendingOrdersForAllAccounts()
        {
            var pendingOrders = new List<PendingOrder>();
            foreach (var curAccount in curUser.accounts) {
                pendingOrders.AddRange(GetPendingOrdersForAccount(curAccount.number));
            }
            return pendingOrders;
        }

        public static List<PendingOrder> GetPendingOrdersForAccount(string accountNumber) {
            var pendingOrders = new List<PendingOrder>();

            PositionsResponse positions = GetPositions(accountNumber);
            BalancesResponse balances = GetBalances(accountNumber);

            var accountCategories = db.AccountCategories.Where(p => p.AccountNumber == accountNumber);

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

                    if (percentOfTarget < 100)
                    {
                        var valueToBuy = ((accountTargetPercent - curPercentOwned) / 100) * totalEquity;
                        int numSharesToBuy = (int)Math.Floor(valueToBuy / quote.askPrice);

                        if (numSharesToBuy > 0)
                        {
                            pendingOrders.Add(new PendingOrder()
                            {
                                AccountNumber = accountNumber,
                                Symbol = symbol,
                                Quote = quote,
                                IsBuyOrder = true,
                                TargetValue = valueToBuy,
                                Quantity = numSharesToBuy,
                                TargetPercent = accountTargetPercent,
                                OwnedPercent = curPercentOwned,
                            });
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

        public static OrderImpactResponse GetMarketOrderImpact(PendingOrder curPendingOrder)
        {
            return GetMarketOrderImpact(curPendingOrder.AccountNumber, curPendingOrder.Quote.symbolId, curPendingOrder.IsBuyOrder, curPendingOrder.Quantity);
        }

        public static OrderImpactResponse GetMarketOrderImpact(string accountNumber, int symbolId, bool isBuyOrder, int quantity)
        {
            var request = new RestRequest("/v1/accounts/{accountNumber}/orders/impact", Method.POST);
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

            var response = client.Execute<OrderImpactResponse>(request);

            return response.Data;
        }

        public static void CreateMarketOrder(PendingOrder curPendingOrder)
        {
            CreateMarketOrder(curPendingOrder.AccountNumber, curPendingOrder.Quote.symbolId, curPendingOrder.IsBuyOrder, curPendingOrder.Quantity);
        }

        public static void CreateMarketOrder(string accountNumber, int symbolId, bool isBuyOrder, int quantity)
        {
            var action = isBuyOrder ? "Buy" : "Sell";
            var orderImpact = GetMarketOrderImpact(accountNumber, symbolId, isBuyOrder, quantity);

            if (IS_LIVE)
            {
                Console.WriteLine("\n-- Attempthing market order --");
                Console.WriteLine("Action: " + action);
                Console.WriteLine("Symbol: " + symbolId);
                Console.WriteLine("Value: " + orderImpact.tradeValueCalculation);
                Console.WriteLine("Buying Power Result: " + orderImpact.buyingPowerResult);
                Console.WriteLine("Estimated Commissions: " + orderImpact.estimatedCommissions);
                Console.WriteLine("Allow? Y for yes, otherwise, program will close.");
                var input = Console.ReadLine().Trim();

                if (input != "y" || input != "Y")
                {
                    Console.WriteLine("Market order cancelled.");
                    return;
                }
            }

            if (orderImpact.buyingPowerResult < 0) {
                throw new Exception("Order would cause negative buying power.");
            }

            if (orderImpact.estimatedCommissions / (orderImpact.price * quantity) > UNACCEPTABLE_COMMISSION_PECENT_THRESHOLD) {
                throw new Exception("Commissions greater than "+ UNACCEPTABLE_COMMISSION_PECENT_THRESHOLD * 100+ "% of value purchased.");
            }

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
                action = action,
                primaryRoute = "AUTO",
                secondaryRoute = "AUTO"
            };

            request.RequestFormat = DataFormat.Json;
            request.AddBody(body);

            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("-- Create Market Order Post --");
                Console.WriteLine("Action: " + action);
                Console.WriteLine("Symbol: " + symbolId);
                Console.WriteLine("Quantity: " + quantity);
                Console.WriteLine("Response: " + response.Content);
                Console.WriteLine("");
            }
            else {
                throw new Exception("Create market order request failed.");
            }
        }

    }
}
