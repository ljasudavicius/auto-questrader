using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoQuestrader.apiModels
{
    public class Position
    {
        public string symbol { get; set; }
        public string symbolId { get; set; }
        public string openQuantity { get; set; }
        public string currentMarketValue { get; set; }
        public string currentPrice { get; set; }
        public string averageEntryPrice { get; set; }
        public string closedPnl { get; set; }
        public string openPnl { get; set; }
        public string totalCost { get; set; }
        public string isRealTim { get; set; }
        public string isUnderReorg { get; set; }
    }
}
