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
            Success = true; //default success to true
        }

        public ApiResponse(object payload)
        {
            Success = true; //default success to true
            this.Payload = payload;
        }
    }
}
