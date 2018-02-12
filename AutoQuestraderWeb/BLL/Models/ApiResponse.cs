using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BLL.Models
{
    public class ApiResponse
    {
        [JsonProperty(PropertyName = "success")]
        public bool Success { get; set; }

        [JsonProperty(PropertyName = "payload")]
        public object Payload { get; set; }

        private List<string> _messages;
        [JsonProperty(PropertyName = "messages")]
        public List<string> Messages
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

        public ApiResponse()
        {
            Success = true;
        }
    
        public ApiResponse(object payload)
        {
            Success = true;
            Payload = payload;
        }

        public ApiResponse(object payload = null, bool success = true, string message = null)
            :this(payload, success, new List<string>() { message })
        {
        }

        public ApiResponse(object payload = null, bool success = true, List<string> messages = null)
        {
            Success = success; 
            this.Payload = payload;
            this.Messages = messages;
        }

    }
}
