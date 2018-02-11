using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BLL.DBModels
{
    public class StockTarget
    {
        [Key]
        public int ID { get; set; }

        [StringLength(50)]
        public string Symbol { get; set; }
        public double TargetPercent { get; set; }    
        public bool ShouldBuy { get; set; }
        public bool ShouldSell { get; set; }

        public int CategoryID { get; set; }
        public Category Category { get; set; }
    }
}
