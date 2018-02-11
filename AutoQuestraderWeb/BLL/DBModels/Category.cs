using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BLL.DBModels
{
    public class Category
    {
        [Key]
        public int ID { get; set; }

        [StringLength(50)]
        public string Name { get; set; }

        public ICollection<AccountCategory> AccountCategories { get; set; }
        public ICollection<StockTarget> StockTargets { get; set; }
    }
}
