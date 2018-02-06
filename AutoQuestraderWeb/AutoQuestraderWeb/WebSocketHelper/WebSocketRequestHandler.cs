using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using AutoQuestraderWeb.WebSocketHelper;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using BLL.Helpers;

namespace WebSocketHelper
{
    public class WebSocketRequestHandler
    {
        public static Dictionary<string, MethodInfo> functions = new Dictionary<string, MethodInfo>() {
            { "echo",  typeof(WebSocketMethods).GetMethod("echo")}
        };


        public static async Task Handle(HttpContext httpContext, WebSocket webSocket)
        {
            /*We define a certain constant which will represent
            size of received data. It is established by us and 
            we can set any value. We know that in this case the size of the sent
            data is very small.
            */
            const int maxMessageSize = 1024;

            //Buffer for received bits.
            var receivedDataBuffer = new ArraySegment<Byte>(new Byte[maxMessageSize]);

            var cancellationToken = new CancellationToken();

            //Checks WebSocket state.
            while (webSocket.State == WebSocketState.Open)
            {
                //Reads data.
                WebSocketReceiveResult webSocketReceiveResult =
                    await webSocket.ReceiveAsync(receivedDataBuffer, cancellationToken);

                //If input frame is cancelation frame, send close command.
                if (webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                        string.Empty, cancellationToken);
                }
                else
                {
                    byte[] payloadData = receivedDataBuffer.Array.Where(b => b != 0).ToArray();

                    string receiveString = System.Text.Encoding.UTF8.GetString(payloadData, 0, payloadData.Length);

                    var wrapper = JsonConvert.DeserializeObject<WebSocketDataWrapper>(receiveString);

                    object methodResult = functions[wrapper.methodName].InvokeWithNamedParameters(null, wrapper.parameters);

                    var jsonString = JsonConvert.SerializeObject(methodResult);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(jsonString);

                    //Sends data back.
                    await webSocket.SendAsync(new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text, true, cancellationToken);
                }
            }
        }
    }
}
