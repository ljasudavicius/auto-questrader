﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoQuestrader.APIModels
{
    public class User
    {
        public string userId { get; set; }
        public List<Account> accounts { get; set; }
    }

}
