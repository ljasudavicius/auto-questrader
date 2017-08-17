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
        public string cash { get; set; }
        public string marketValue { get; set; }
        public string totalEquity { get; set; }
        public string buyingPower { get; set; }
        public string maintenanceExcess { get; set; }
        public string isRealTime { get; set; }
    }
}
