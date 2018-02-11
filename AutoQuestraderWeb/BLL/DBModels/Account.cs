using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BLL.DBModels
{
    public class Account
    {
        [Key]
        public int ID { get; set; }

        [StringLength(50)]
        public string Number { get; set; }
        [StringLength(50)]
        public string Type { get; set; }
        [StringLength(50)]
        public string Status { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsBilling { get; set; }
        [StringLength(50)]
        public string ClientAccountType { get; set; }

        public int UserID { get; set; }
        public User User { get; set; }

        public ICollection<AccountCategory> AccountCategories { get; set; }
    }
}
