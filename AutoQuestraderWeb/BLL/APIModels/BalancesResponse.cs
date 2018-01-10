using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoQuestrader.APIModels
{
    public class BalancesResponse
    {
        public List<Balance> perCurrencyBalances { get; set; }
        public List<Balance> combinedBalances { get; set; }
        public List<Balance> sodPerCurrencyBalances { get; set; }
        public List<Balance> sodCombinedBalances { get; set; }
    }
}
