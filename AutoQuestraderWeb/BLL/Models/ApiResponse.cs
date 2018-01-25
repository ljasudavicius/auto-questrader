using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models
{
    public class ApiResponse
    {
        public bool success { get; set; }
        private List<string> _messages;
        public List<string> messages
        {
            get
            {
                if (_messages == null)
                {
                    _messages = new List<string>();
                }
                return _messages;
            }
            set
            {
                _messages = value;
            }
        }
        public object payload { get; set; }

        public ApiResponse()
        {
            success = true; //default success to true
        }

        public ApiResponse(object payload)
        {
            success = true; //default success to true
            this.payload = payload;
        }
    }
}
