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
        public int symbolId { get; set; }
        public double openQuantity { get; set; }
        public double currentMarketValue { get; set; }
        public double currentPrice { get; set; }
        public double averageEntryPrice { get; set; }
        public double closedPnl { get; set; }
        public double openPnl { get; set; }
        public double totalCost { get; set; }
        public bool isRealTime { get; set; }
        public bool isUnderReorg { get; set; }
    }
}
