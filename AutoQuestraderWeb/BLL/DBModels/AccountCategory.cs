using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BLL.DBModels
{
    public class AccountCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public double Percent { get; set; }

        public int AccountID { get; set; }
        public Account Account { get; set; }

        public int CategoryID { get; set; }
        public Category Category { get; set; }


    }
}
