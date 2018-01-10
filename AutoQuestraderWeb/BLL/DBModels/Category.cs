using System;
using System.Collections.Generic;

namespace BLL.DBModels
{
    public partial class Category
    {
        public Category()
        {
            AccountCategory = new HashSet<AccountCategory>();
            StockTarget = new HashSet<StockTarget>();
        }

        public string Name { get; set; }

        public ICollection<AccountCategory> AccountCategory { get; set; }
        public ICollection<StockTarget> StockTarget { get; set; }
    }
}
