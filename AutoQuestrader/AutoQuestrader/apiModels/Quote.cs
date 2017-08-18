using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoQuestrader.apiModels
{
    public class Quote
    {
        public string symbol { get; set; }
		public int symbolId { get; set; }
        public double bidPrice { get; set; }
        public double askPrice { get; set; }
        public bool delay { get; set; }
    }
}
