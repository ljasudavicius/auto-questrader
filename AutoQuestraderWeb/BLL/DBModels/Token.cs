using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BLL.DBModels
{
    public class Token
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [StringLength(100)]
        public string LoginServer { get; set; }
        [StringLength(100)]
        public string AccessToken { get; set; }
        [StringLength(100)]
        public string RefreshToken { get; set; }
        [StringLength(100)]
        public string TokenType { get; set; }
        [StringLength(100)]
        public string ApiServer { get; set; }
        public int ExpiresIn { get; set; }
        public DateTimeOffset ExpiresDate { get; set; }

        public int UserID { get; set; }
        public User User { get; set; }
    }
}
