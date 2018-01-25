using BLL.QTModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Models
{
    public class PendingOrder
    {
        public string AccountNumber { get; set; }
        public Symbol Symbol { get; set; }
        public Quote Quote { get; set; }
        public bool IsBuyOrder { get; set; }
        public double TargetValue { get; set; }
        public int Quantity { get; set; }
        public double TargetPercent { get; set; }
        public double OwnedPercent { get; set; }
    }
}
