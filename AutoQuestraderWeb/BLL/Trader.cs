using BLL.QTModels;
using BLL.Models;
using BLL.DBModels;
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

namespace BLL
{
    public class Trader
    {
        public readonly bool IS_LIVE = true;

        public AutoQuestraderContext db;
        public RestClient client;
        public User curUser;
        public static readonly string NG_SYMBOL_CAD = "DLR.TO";
        public static readonly string NG_SYMBOL_USD = "DLR.U.TO";
        public static readonly string CURRENCY_USD = "USD";
        public static readonly string CURRENCY_CAD = "CAD";
        public static readonly double ACCEPTABLE_COMMISSION_PERCENT_THRESHOLD = 0.01; // 1%
        public static readonly double ACCEPTABLE_NG_VALUE_THRESHOLD = 500;

        public Trader(AutoQuestraderContext db) {
            this.db = db;
        }

        public void Main()
        {
            Initialize();

            Console.WriteLine("\nCalculating target positions...");

            var pendingOrders = GetPendingOrdersForAllAccounts();
            Console.WriteLine("\n-- Pending Orders --");
            OutputPendingOrdersTable(pendingOrders);


            var ngRequirements = GetNorbertsGambitRequirements();
            HandlePurchasingNorbertsGambitRequirements(ngRequirements);


            HandlePurchasingOfPendingOrders(pendingOrders);


            Console.WriteLine("\n-- Purchasing Complete --");
            Console.WriteLine("\n-- Resulting Account Breakdown --");
            OutputFinalResults();


            Console.WriteLine("\n\nPress enter to close...");
            Console.ReadLine();
        }

        public void Initialize()
        {
            Console.WriteLine("Hi, welcome to AutoQuestrader.");
            Console.WriteLine("\nLogging in...");

            var token = AuthHelper.RefreshToken(db, IS_LIVE);

            client = new RestClient(token.ApiServer);
            client.AddDefaultHeader("Authorization", token.TokenType + " " + token.AccessToken);

            Console.WriteLine("\nGetting User Information...");

            var userResponse = client.Execute<User>(new RestRequest("v1/accounts", Method.GET));
            curUser = userResponse.Data;
        }

        public void OutputFinalResults()
        {
            foreach (var curAccount in curUser.accounts)
            {
                Console.WriteLine("\nAccount: " + curAccount.type);
                Console.WriteLine("Account number: " + curAccount.number);
                BalancesResponse balances = GetBalances(curAccount.number);
                Console.WriteLine("CAD Cash: " + balances.perCurrencyBalances.FirstOrDefault(p => p.currency == CURRENCY_CAD).cash);
                Console.WriteLine("USD Cash: " + balances.perCurrencyBalances.FirstOrDefault(p => p.currency == CURRENCY_USD).cash);
                var breakdownPendingOrders = GetAccountBreakdown(curAccount.number);
                Console.WriteLine("\n-- Account Breakdown --");
                OutputPendingOrdersTable(breakdownPendingOrders);
            }
        }

        public void HandlePurchasingNorbertsGambitRequirements(List<PendingOrder> ngRequirements)
        {
            foreach (var curNgRequirement in ngRequirements)
            {
                if (curNgRequirement.Quantity > 0)
                {
                    HandlePurchasingNorbertsGambitRequirement(curNgRequirement);
                }
            }
        }

        public void HandlePurchasingNorbertsGambitRequirement(PendingOrder curNgRequirement, bool firstTime = true)
        {
            if (firstTime)
            {
                Console.WriteLine("Currency conversion (Norberts Gambit) is required to purchase some securities in USD.");
            }

            if (curNgRequirement.Quantity <= 0)
            {
                Console.WriteLine("Order quantity reduced to zero.");
                Console.WriteLine("Skipping order.");
                return;
            }

            if (curNgRequirement.TargetValue < ACCEPTABLE_NG_VALUE_THRESHOLD)
            {
                Console.WriteLine("Current conversion amount is below acceptable threshold of: " + ACCEPTABLE_NG_VALUE_THRESHOLD);
                Console.WriteLine("Skipping conversion.");
                return;
            }

            var orderImpact = GetMarketOrderImpact(curNgRequirement);
            BalancesResponse balances = GetBalances(curNgRequirement.AccountNumber);

            var cashLevel = balances.perCurrencyBalances.FirstOrDefault(p => p.currency == curNgRequirement.Symbol.currency).cash;
            if (cashLevel < Math.Abs(orderImpact.buyingPowerEffect))
            {
                if (firstTime)
                {
                    Console.WriteLine("Order of " + Math.Abs(orderImpact.buyingPowerEffect) + " exceeds current cash level: " + cashLevel);
                    Console.WriteLine("Attempting to reduce quantity purchased...");

                    curNgRequirement.Quantity = (int)Math.Floor(cashLevel / orderImpact.price);
                }
                else
                {
                    curNgRequirement.Quantity -= 1;
                }

                HandlePurchasingNorbertsGambitRequirement(curNgRequirement, false);
                return;
            }

            Console.WriteLine("Proceeding to purchace $" + Math.Abs(orderImpact.buyingPowerEffect) + " in " + NG_SYMBOL_CAD);
            var success = CreateMarketOrder(curNgRequirement);
            if (success)
            {
                EmailHelper.SendNorbertsGambitEmail(curNgRequirement.AccountNumber, curNgRequirement.Quantity, db);
            }
        }

        public void HandlePurchasingOfPendingOrders(List<PendingOrder> pendingOrders)
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

        public bool HandlePurchaseOfPendingOrder(PendingOrder pendingOrder, bool firstTime = true)
        {
            if (firstTime)
            {
                Console.WriteLine("\nAttempting to purchase: " + pendingOrder.Symbol.symbol + " on account # " + pendingOrder.AccountNumber);
            }

            if (pendingOrder.Quantity <= 0)
            {
                Console.WriteLine("Order quantity reduced to zero.");
                Console.WriteLine("Skipping order.");
                return false;
            }

            var orderImpact = GetMarketOrderImpact(pendingOrder);

            if (orderImpact.estimatedCommissions / (orderImpact.price * pendingOrder.Quantity) > ACCEPTABLE_COMMISSION_PERCENT_THRESHOLD)
            {
                Console.WriteLine("Trade value calculation: " + orderImpact.tradeValueCalculation);
                Console.WriteLine("Commission: " + orderImpact.estimatedCommissions);

                Console.WriteLine("Commissions greater than " + ACCEPTABLE_COMMISSION_PERCENT_THRESHOLD * 100 + "% of value purchased.");
                Console.WriteLine("Skipping order.");
                return false;
            }

            BalancesResponse balances = GetBalances(pendingOrder.AccountNumber);
            var cashLevel = balances.perCurrencyBalances.FirstOrDefault(p => p.currency == pendingOrder.Symbol.currency).cash;
            if (cashLevel < Math.Abs(orderImpact.buyingPowerEffect))
            {
                if (firstTime)
                {
                    Console.WriteLine("Order of " + Math.Abs(orderImpact.buyingPowerEffect) + " exceeds current cash level: " + cashLevel);
                    Console.WriteLine("Attempting to reduce quantity purchased...");

                    pendingOrder.Quantity = (int)Math.Floor(cashLevel / orderImpact.price);
                }
                else
                {
                    pendingOrder.Quantity -= 1;
                }

                return HandlePurchaseOfPendingOrder(pendingOrder, false);
            }

            return CreateMarketOrder(pendingOrder);
        }

        public void OutputPendingOrdersTable(List<PendingOrder> pendingOrders)
        {

            Console.WriteLine("");
            var pendingOrderGroups = pendingOrders.GroupBy(p => p.AccountNumber);
            foreach (var curPendingOrderGroup in pendingOrderGroups)
            {
                var curAccountNumber = curPendingOrderGroup.Key;
                Console.WriteLine("Account Number: " + curAccountNumber);
                Console.WriteLine("");
                Console.WriteLine(String.Format("{0,11}{1,7}{2,13}{3,6}{4,10}{5,9}", "Symbol", "Qtty.", "Value", "Cur.", "Target %", "Owned %"));
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
                Console.WriteLine("\nTotal CAD Value: " + Math.Round(totalPendingCADValue, 2));

                var totalPendingUSDValue = curPendingOrderGroup.Where(p => p.Symbol.currency == CURRENCY_USD).Sum(p => p.TargetValue);
                Console.WriteLine("Total USD Value: " + Math.Round(totalPendingUSDValue, 2));
                Console.WriteLine("");
            }

            if (pendingOrders.Count() == 0)
            {
                Console.WriteLine("Empty");
                Console.WriteLine("");
            }
        }

        public void SellAllSecuritiesInAllAccounts()
        {

            foreach (var curAccount in curUser.accounts)
            {
                SellAllSecuritiesInAccount(curAccount.number);
            }
        }

        public void SellAllSecuritiesInAccount(string accountNumber)
        {
            if (IS_LIVE)
            {
                throw new Exception("Attempting to sell all securities on a LIVE account!");
            }
            else
            {

                PositionsResponse positions = GetPositions(accountNumber);
                foreach (var curPosition in positions.positions)
                {
                    if (curPosition.openQuantity > 0)
                    {
                        CreateMarketOrder(accountNumber, curPosition.symbol, curPosition.symbolId, false, (int)curPosition.openQuantity);
                    }
                }
            }
        }

        public void SellAllUSNGStocks(string curAccountNumber)
        {
            PositionsResponse positions = GetPositions(curAccountNumber);
            var ngPositionUSD = positions.positions.FirstOrDefault(p => p.symbol == NG_SYMBOL_USD);

            if (ngPositionUSD != null && ngPositionUSD.openQuantity > 0)
            {
                Console.WriteLine("\nSelling converted NG stocks...\n");
                CreateMarketOrder(curAccountNumber, ngPositionUSD.symbol, ngPositionUSD.symbolId, false, (int)ngPositionUSD.openQuantity);
            }
        }

        public List<PendingOrder> GetNorbertsGambitRequirements()
        {
            var NGSymbolUSD = GetSymbol(NG_SYMBOL_USD);
            var NGQuoteUSD = GetQuote(NGSymbolUSD.symbolId);
            var pendingNGOrders = new List<PendingOrder>();

            foreach (var curAccount in curUser.accounts)
            {
                // sell all NG stocks that are already converted to US
                SellAllUSNGStocks(curAccount.number);

                BalancesResponse balances = GetBalances(curAccount.number);

                // calculate total amount of USD that should be in the account
                var accountCategories = db.AccountCategory.Where(p => p.AccountNumber == curAccount.number);
                double requiredUSDValue = 0;
                foreach (var curAccountCategory in accountCategories)
                {
                    foreach (var curStockTarget in curAccountCategory.CategoryNameNavigation.StockTarget)
                    {
                        var symbol = GetSymbol(curStockTarget.Symbol);
                        if (symbol.currency == CURRENCY_USD)
                        {
                            double totalEquity = balances.combinedBalances.FirstOrDefault(p => p.currency == symbol.currency).totalEquity;
                            requiredUSDValue += totalEquity * ((curAccountCategory.Percent / 100) * (curStockTarget.TargetPercent / 100));
                        }
                    }
                }

                // reduce required amount by USD assets already owned
                PositionsResponse positions = GetPositions(curAccount.number);
                foreach (var curPosition in positions.positions)
                {
                    var symbol = GetSymbol(curPosition.symbol);
                    if (symbol.currency == CURRENCY_USD)
                    {
                        requiredUSDValue -= curPosition.currentMarketValue;
                    }
                }

                // reduce required amount by the value that is already being processed
                var ngPositionCAD = positions.positions.FirstOrDefault(p => p.symbol == NG_SYMBOL_CAD);
                if (ngPositionCAD != null)
                {
                    requiredUSDValue -= ngPositionCAD.openQuantity * NGQuoteUSD.bidPrice;
                }

                if (requiredUSDValue > 0)
                {
                    var numNGSharesNeeded = (int)Math.Ceiling(requiredUSDValue / NGQuoteUSD.bidPrice);

                    var NGSymbolCAD = GetSymbol(NG_SYMBOL_CAD);
                    var NGQuoteCAD = GetQuote(NGSymbolCAD.symbolId);

                    var valueToBuyCAD = numNGSharesNeeded * NGQuoteCAD.askPrice;

                    // if required to buy is greater than cash on hand, truncate to cash level.
                    var curCashCAD = balances.perCurrencyBalances.FirstOrDefault(p => p.currency == CURRENCY_CAD).cash;
                    if (valueToBuyCAD > curCashCAD)
                    {
                        numNGSharesNeeded = (int)Math.Floor(curCashCAD / NGQuoteCAD.askPrice);
                        valueToBuyCAD = numNGSharesNeeded * NGQuoteCAD.askPrice;
                    }

                    pendingNGOrders.Add(new PendingOrder()
                    {
                        AccountNumber = curAccount.number,
                        Symbol = NGSymbolCAD,
                        Quote = NGQuoteCAD,
                        Quantity = numNGSharesNeeded,
                        IsBuyOrder = true,
                        TargetValue = valueToBuyCAD
                    });
                }
            }

            return pendingNGOrders;
        }

        public List<PendingOrder> GetNorbertsGambitRequirements(List<PendingOrder> pendingOrders)
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

                // calculate value already being converted
                PositionsResponse positions = GetPositions(curAccountNumber);
                var ngPositionCAD = positions.positions.FirstOrDefault(p => p.symbol == NG_SYMBOL_CAD);
                var pendingNGValueUSD = 0.0;
                if (ngPositionCAD != null)
                {
                    pendingNGValueUSD = ngPositionCAD.openQuantity * NGQuoteUSD.bidPrice;
                }

                var remainingUSDRequired = requiredUSD - (curBalanceUSD.cash + pendingNGValueUSD);

                if (remainingUSDRequired > 0)
                {
                    var numNGSharesNeeded = (int)Math.Ceiling(remainingUSDRequired / NGQuoteUSD.bidPrice);

                    var NGSymbolCAD = GetSymbol(NG_SYMBOL_CAD);
                    var NGQuoteCAD = GetQuote(NGSymbolCAD.symbolId);

                    pendingNGOrders.Add(new PendingOrder()
                    {
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

        public List<PendingOrder> GetPendingOrdersForAllAccounts()
        {
            var pendingOrders = new List<PendingOrder>();
            foreach (var curAccount in curUser.accounts)
            {
                pendingOrders.AddRange(GetPendingOrdersForAccount(curAccount.number));
            }
            return pendingOrders;
        }

        public List<PendingOrder> GetPendingOrdersForAccount(string accountNumber)
        {
            var pendingOrders = new List<PendingOrder>();

            PositionsResponse positions = GetPositions(accountNumber);
            BalancesResponse balances = GetBalances(accountNumber);

            var accountCategories = db.AccountCategory.Where(p => p.AccountNumber == accountNumber);

            foreach (var curAccountCategory in accountCategories)
            {
                foreach (var curStockTarget in curAccountCategory.CategoryNameNavigation.StockTarget)
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

        public List<PendingOrder> GetAccountBreakdown(string accountNumber)
        {
            var pendingOrders = new List<PendingOrder>();

            PositionsResponse positions = GetPositions(accountNumber);
            BalancesResponse balances = GetBalances(accountNumber);

            var accountCategories = db.AccountCategory.Where(p => p.AccountNumber == accountNumber);

            foreach (var curAccountCategory in accountCategories)
            {
                foreach (var curStockTarget in curAccountCategory.CategoryNameNavigation.StockTarget)
                {
                    var symbol = GetSymbol(curStockTarget.Symbol);
                    var quote = GetQuote(symbol.symbolId);

                    var curPosition = positions.positions.FirstOrDefault(p => p.symbol == curStockTarget.Symbol);
                    double curPercentOwned = 0;
                    double curValue = 0;
                    int curQuantity = 0;
                    if (curPosition != null)
                    {
                        double totalEquity = balances.combinedBalances.FirstOrDefault(p => p.currency == symbol.currency).totalEquity;
                        curPercentOwned = curPosition != null ? (curPosition.currentMarketValue / totalEquity) * 100 : 0;
                        curValue = curPosition.currentMarketValue;
                        curQuantity = (int)curPosition.openQuantity;
                    }

                    double accountTargetPercent = ((curAccountCategory.Percent / 100) * (curStockTarget.TargetPercent / 100)) * 100;

                    pendingOrders.Add(new PendingOrder()
                    {
                        AccountNumber = accountNumber,
                        Symbol = symbol,
                        Quote = quote,
                        TargetValue = curValue,
                        Quantity = curQuantity,
                        TargetPercent = accountTargetPercent,
                        OwnedPercent = curPercentOwned,
                    });
                }
            }

            // add owned stocks that are not on the target list
            foreach (var curPosition in positions.positions)
            {
                if (pendingOrders.FirstOrDefault(p => p.Symbol.symbol == curPosition.symbol) == null)
                {
                    var symbol = GetSymbol(curPosition.symbol);
                    var quote = GetQuote(symbol.symbolId);

                    double totalEquity = balances.combinedBalances.FirstOrDefault(p => p.currency == symbol.currency).totalEquity;
                    double curPercentOwned = curPosition != null ? (curPosition.currentMarketValue / totalEquity) * 100 : 0;

                    pendingOrders.Add(new PendingOrder()
                    {
                        AccountNumber = accountNumber,
                        Symbol = symbol,
                        Quote = quote,
                        TargetValue = curPosition.currentMarketValue,
                        Quantity = (int)curPosition.openQuantity,
                        TargetPercent = 0,
                        OwnedPercent = curPercentOwned,
                    });
                }
            }

            return pendingOrders;
        }

        public PositionsResponse GetPositions(string accountNumber)
        {
            var request = new RestRequest("/v1/accounts/{accountNumber}/positions", Method.GET);
            request.AddUrlSegment("accountNumber", accountNumber);
            var response = client.Execute<PositionsResponse>(request).Data;
            return response;
        }

        public BalancesResponse GetBalances(string accountNumber)
        {
            var request = new RestRequest("/v1/accounts/{accountNumber}/balances", Method.GET);
            request.AddUrlSegment("accountNumber", accountNumber);
            var response = client.Execute<BalancesResponse>(request).Data;
            return response;
        }

        public Symbol GetSymbol(string symbolName)
        {
            var request = new RestRequest("/v1/symbols/", Method.GET);
            request.AddParameter("names", symbolName);
            var response = client.Execute<SymbolsResponse>(request).Data.symbols.FirstOrDefault();
            return response;
        }

        public Quote GetQuote(int symbolId)
        {
            var request = new RestRequest("/v1/markets/quotes/{symbolId}", Method.GET);
            request.AddUrlSegment("symbolId", symbolId.ToString());
            var response = client.Execute<QuotesResponse>(request).Data.quotes.FirstOrDefault();
            return response;
        }

        public OrderImpactResponse GetMarketOrderImpact(PendingOrder curPendingOrder)
        {
            return GetMarketOrderImpact(curPendingOrder.AccountNumber, curPendingOrder.Quote.symbolId, curPendingOrder.IsBuyOrder, curPendingOrder.Quantity);
        }

        public OrderImpactResponse GetMarketOrderImpact(string accountNumber, int symbolId, bool isBuyOrder, int quantity)
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

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(response.StatusDescription);
            }

            if (response.Data.buyingPowerEffect == 0)
            {
                throw new Exception("Error fetching order impact. Check response for more details.");
            }

            return response.Data;
        }

        public bool CreateMarketOrder(PendingOrder curPendingOrder)
        {
            return CreateMarketOrder(curPendingOrder.AccountNumber, curPendingOrder.Quote.symbol, curPendingOrder.Quote.symbolId, curPendingOrder.IsBuyOrder, curPendingOrder.Quantity);
        }

        public bool CreateMarketOrder(string accountNumber, string symbol, int symbolId, bool isBuyOrder, int quantity)
        {
            if (quantity <= 0)
            {
                throw new Exception("Attempting market order of 0 quantity.");
            }

            var action = isBuyOrder ? "Buy" : "Sell";
            var orderImpact = GetMarketOrderImpact(accountNumber, symbolId, isBuyOrder, quantity);

            if (IS_LIVE)
            {
                BalancesResponse balances = GetBalances(accountNumber);
                var cashLevelCAD = balances.perCurrencyBalances.FirstOrDefault(p => p.currency == CURRENCY_CAD).cash;
                var cashLevelUSD = balances.perCurrencyBalances.FirstOrDefault(p => p.currency == CURRENCY_USD).cash;

                Console.WriteLine("\n-- Attempthing market order --");
                Console.WriteLine("Account: " + accountNumber);
                Console.WriteLine("Symbol: " + symbol);
                Console.WriteLine("Action: " + action);
                Console.WriteLine("Value: " + orderImpact.tradeValueCalculation);
                Console.WriteLine("Estimated Commissions: " + orderImpact.estimatedCommissions);
                Console.WriteLine("Buying Power Effect: " + orderImpact.buyingPowerEffect);
                Console.WriteLine("Current Cash CAD: " + cashLevelCAD);
                Console.WriteLine("Current Cash USD: " + cashLevelUSD);

                Console.WriteLine("Allow? Y for yes");
                var input = Console.ReadLine().Trim();

                if (input != "y" && input != "Y")
                {
                    Console.WriteLine("Market order cancelled.");
                    return false;
                }
            }

            if (orderImpact.buyingPowerResult < 0)
            {
                throw new Exception("Order would cause negative buying power.");
            }

            if (orderImpact.estimatedCommissions / (orderImpact.price * quantity) > ACCEPTABLE_COMMISSION_PERCENT_THRESHOLD)
            {
                throw new Exception("Commissions greater than " + ACCEPTABLE_COMMISSION_PERCENT_THRESHOLD * 100 + "% of value purchased.");
            }

            var request = new RestRequest("/v1/accounts/{accountNumber}/orders", Method.POST);
            request.AddUrlSegment("accountNumber", accountNumber);

            var body = new
            {
                accountNumber = accountNumber,
                symbolId = symbolId,
                quantity = quantity,
                isAllOrNone = false,
                isAnonymous = false,
                orderType = "Market",
                timeInForce = "Day",
                action = action,
                primaryRoute = "AUTO",
                secondaryRoute = "AUTO",
            };

            request.RequestFormat = DataFormat.Json;
            request.AddBody(body);

            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine("-- Create Market Order Post --");
                Console.WriteLine("Action: " + action);
                Console.WriteLine("Symbol: " + symbol);
                Console.WriteLine("Quantity: " + quantity);
                Console.WriteLine("Response: " + response.Content);
                Console.WriteLine("");
                return true;
            }
            else
            {
                throw new Exception("Create market order request failed.");
            }
        }
    }
}
