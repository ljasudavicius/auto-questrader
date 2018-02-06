using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AutoQuestraderWeb.WebSocketHelper
{
    public static class WebSocketMethods
    {
        public static string echo(string text, string text2)
        {
            return text+ " " + text2;
        }
    }
}
