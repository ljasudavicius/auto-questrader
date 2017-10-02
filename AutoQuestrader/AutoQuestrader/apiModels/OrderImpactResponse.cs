using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoQuestrader.apiModels
{
    public class OrderImpactResponse
    {
        public double estimatedCommissions { get; set; }
	    public double buyingPowerEffect { get; set; }
        public double buyingPowerResult { get; set; }
        public double maintExcessEffect { get; set; }
        public double maintExcessResult { get; set; }
        public string side { get; set; }
        public string tradeValueCalculation { get; set; }
        public double price { get; set; }
    }
}
