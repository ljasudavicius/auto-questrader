using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BLL.DBModels
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [StringLength(100)]
        public string Email { get; set; }
        [StringLength(100)]
        public string QuestradeID { get; set; }
        [StringLength(100)]
        public string ConnectionId { get; set; }

        public Token Token { get; set; }

        public ICollection<Account> Accounts { get; set; }
        //public ICollection<Token> Tokens { get; set; }

    }
}
