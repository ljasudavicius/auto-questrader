using System;
using System.Collections.Generic;

namespace BLL.DBModels
{
    public partial class AccountCategory
    {
        public string AccountNumber { get; set; }
        public string CategoryName { get; set; }
        public double Percent { get; set; }

        public Category CategoryNameNavigation { get; set; }
    }
}
