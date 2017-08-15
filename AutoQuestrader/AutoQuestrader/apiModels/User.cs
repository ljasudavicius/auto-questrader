﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoQuestrader.apiModels
{
    public class User
    {
        public string userId { get; set; }
        public List<Account> accounts { get; set; }
    }

    public class Account
    {
        public string type { get; set; }
        public string number { get; set; }
        public string status { get; set; }
        public bool isPrimary { get; set; }
        public bool isBilling { get; set; }
        public string clientAccountType { get; set; }
    }
}
