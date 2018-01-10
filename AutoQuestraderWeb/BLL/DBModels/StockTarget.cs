using System;
using System.Collections.Generic;

namespace BLL.DBModels
{
    public partial class StockTarget
    {
        public string Symbol { get; set; }
        public double TargetPercent { get; set; }
        public string CategoryName { get; set; }
        public bool ShouldBuy { get; set; }
        public bool ShouldSell { get; set; }

        public Category CategoryNameNavigation { get; set; }
    }
}
