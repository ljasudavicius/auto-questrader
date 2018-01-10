using System;
using System.Collections.Generic;

namespace BLL.DBModels
{
    public partial class Token
    {
        public string LoginServer { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
        public string ApiServer { get; set; }
        public int? ExpiresIn { get; set; }
        public DateTimeOffset? ExpiresDate { get; set; }
    }
}
