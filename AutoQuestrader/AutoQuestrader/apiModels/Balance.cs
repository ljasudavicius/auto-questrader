using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoQuestrader.apiModels
{
    public class Balance
    {
        public string currency { get; set; }
        public double cash { get; set; }
        public double marketValue { get; set; }
        public double totalEquity { get; set; }
        public double buyingPower { get; set; }
        public double maintenanceExcess { get; set; }
        public bool isRealTime { get; set; }
    }
}
