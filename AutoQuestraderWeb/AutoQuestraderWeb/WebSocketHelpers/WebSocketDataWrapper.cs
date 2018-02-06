using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoQuestraderWeb.WebSocketHelpers
{
    public class WebSocketDataWrapper
    {
        public string methodName {get; set;}
        public Dictionary<string, object> parameters { get; set; }
    }
}
