using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BLL.DBModels
{
    public class Token
    {
        [Key]
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

        public string UserID { get; set; }
        public User User { get; set; }
    }
}
