using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BLL.DBModels
{
    public class User
    {
        [Key]
        public int ID { get; set; }

        [StringLength(100)]
        public string Email { get; set; }
        [StringLength(100)]
        public string QuestradeID { get; set; }    

        public int TokenID { get; set; }
        public Token Token { get; set; }

        public ICollection<Account> Accounts { get; set; }
    }
}
