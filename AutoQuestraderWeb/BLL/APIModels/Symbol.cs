using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoQuestrader.APIModels
{
    public class Symbol
    {
        public string symbol { get; set; }
		public int symbolId { get; set; }
        public double prevDayClosePrice { get; set; }
        public double highPrice52 { get; set; }
        public double lowPrice52 { get; set; }
        public int averageVol3Months { get; set; }
        public int averageVol20Days { get; set; }
        public int outstandingShares { get; set; }
        public double eps { get; set; }
        public double pe { get; set; }
        public double dividend { get; set; }
        public double yield { get; set; }
        public DateTime exDate { get; set; }
        public double marketCap { get; set; }
        public string currency { get; set; }
        public string listingExchange { get; set; }
        public string description { get; set; }
        public string industrySector { get; set; }
        public string industryGroup { get; set; }
        public string industrySubGroup { get; set; }
    }
}
